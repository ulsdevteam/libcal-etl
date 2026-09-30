using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

/// <summary>
/// Loads batches of rows into Snowflake via stage -> PUT -> COPY INTO -> MERGE, instead of the
/// one-SELECT-plus-one-INSERT/UPDATE-per-row pattern that EF Core's SaveChanges uses. Each Snowflake
/// round trip costs a few hundred ms to over a second regardless of how small the statement is, so
/// for a batch of N rows this trades ~2N round trips for roughly 3 fixed-cost round trips total.
///
/// This only works against Snowflake (stages, PUT, and MERGE aren't available in Sqlite).
/// </summary>
static class SnowflakeBulkLoader
{
    /// <summary>
    /// Builds (column name, value accessor) pairs for an entity type directly from Database.cs's EF model,
    /// instead of hand-writing them - so a column rename/add/remove in OnModelCreating is picked up here
    /// automatically instead of silently drifting out of sync with a parallel hand-written list.
    ///
    /// This walks scalar properties, plus any OwnsOne navigations recursively (so Event's Location/Campus/
    /// Owner/Calendar/Url all flatten into LIBCAL_EVENTS' own columns, same as EF's own table-splitting
    /// would do). OwnsMany navigations (Category, Registrants, FutureDates, Answers, Options) are NOT
    /// walked - those are separate child tables, loaded via their own BulkReplaceChildrenAsync call.
    ///
    /// Two things this can't get from the model, because there's no CLR member behind them at all:
    /// - Shadow foreign key properties (Registrant's EVENT_ID, FutureDate's ORIGINAL_EVENT_ID) - see
    ///   <see cref="OwnedCollectionForeignKeyColumn"/> for those.
    /// - Value converters (e.g. Uri -> string) aren't applied here, since this reads the raw CLR value via
    ///   reflection rather than going through EF's materialization pipeline. WriteCsvField below handles
    ///   the small set of CLR types this project actually needs (bool/long/string/DateTime(Offset)/Uri)
    ///   directly instead.
    /// Auto-incrementing identity columns (ArchivedSpaceBooking.Id) ARE handled here: any property with
    /// ValueGenerated.OnAdd is skipped, since Snowflake assigns those on insert.
    /// </summary>
    public static (string Column, Func<T, object?> Value)[] MapColumns<T>(IModel model)
    {
        var entityType = model.FindEntityType(typeof(T))
            ?? throw new InvalidOperationException($"{typeof(T)} is not part of the EF model");
        return Walk(entityType, entity => entity).ToArray();

        static IEnumerable<(string Column, Func<T, object?> Value)> Walk(IEntityType entityType, Func<T, object?> ownerAccessor)
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.PropertyInfo is null) { continue; } // shadow property - see doc comment above
                if (property.ValueGenerated == ValueGenerated.OnAdd) { continue; } // e.g. an identity column
                var propertyInfo = property.PropertyInfo;
                var columnName = property.GetColumnName();
                yield return (columnName, entity =>
                {
                    var owner = ownerAccessor(entity);
                    return owner is null ? null : propertyInfo.GetValue(owner);
                });
            }

            foreach (var navigation in entityType.GetNavigations())
            {
                if (!navigation.ForeignKey.IsOwnership || navigation.IsCollection) { continue; } // only flatten OwnsOne
                var navigationProperty = navigation.PropertyInfo;
                if (navigationProperty is null) { continue; }

                Func<T, object?> nestedOwnerAccessor = entity =>
                {
                    var owner = ownerAccessor(entity);
                    return owner is null ? null : navigationProperty.GetValue(owner);
                };

                foreach (var nested in Walk(navigation.TargetEntityType, nestedOwnerAccessor))
                {
                    yield return nested;
                }
            }
        }
    }

    /// <summary>
    /// The column names making up an entity type's primary key, read from the model instead of assuming "ID".
    /// </summary>
    public static string[] PrimaryKeyColumns(IModel model, Type entityClrType)
    {
        var entityType = model.FindEntityType(entityClrType)
            ?? throw new InvalidOperationException($"{entityClrType} is not part of the EF model");
        var key = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException($"{entityClrType} has no primary key");
        return key.Properties.Select(p => p.GetColumnName()).ToArray();
    }

    /// <summary>
    /// The column name EF assigned to the foreign key of an owned-collection navigation (e.g.
    /// Event.Registrants' EVENT_ID, Event.FutureDates' ORIGINAL_EVENT_ID), read from the model rather
    /// than needing to know or guess the shadow property's CLR-side name.
    /// </summary>
    public static string OwnedCollectionForeignKeyColumn(IModel model, Type ownerClrType, string navigationName)
    {
        var ownerType = model.FindEntityType(ownerClrType)
            ?? throw new InvalidOperationException($"{ownerClrType} is not part of the EF model");
        var navigation = ownerType.FindNavigation(navigationName)
            ?? throw new InvalidOperationException($"{ownerClrType} has no navigation named '{navigationName}'");
        return navigation.ForeignKey.Properties.Single().GetColumnName();
    }

    /// <summary>
    /// Adapts columns that read from <typeparamref name="TInner"/> so they instead read from a wrapper type
    /// that also carries the parent key alongside it - needed for Registrant/FutureDate in Program.cs, since
    /// their parent Event's id has to travel with them despite not being a real property on either type.
    /// </summary>
    public static (string Column, Func<TOuter, object?> Value)[] Reparent<TInner, TOuter>(
        this (string Column, Func<TInner, object?> Value)[] columns, Func<TOuter, TInner?> select) where TInner : class
    {
        return columns.Select(c => (c.Column, (Func<TOuter, object?>)(outer =>
        {
            var inner = select(outer);
            return inner is null ? null : c.Value(inner);
        }))).ToArray();
    }

    /// <summary>
    /// Upsert a batch of rows into <paramref name="table"/> keyed by <paramref name="keyColumns"/>,
    /// using one MERGE statement instead of one SELECT + one INSERT/UPDATE per row.
    /// </summary>
    public static async Task BulkUpsertAsync<T>(DatabaseFacade database, string table, string[] keyColumns,
        (string Column, Func<T, object?> Value)[] columns, IReadOnlyCollection<T> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) { return; }

        var conn = await EnsureOpenAsync(database, cancellationToken);
        var stagingTable = $"{table}_STAGE_{Guid.NewGuid():N}";

        try
        {
            await ExecuteAsync(conn, $"CREATE TEMPORARY TABLE \"{stagingTable}\" LIKE \"{table}\"", cancellationToken);
            await LoadIntoTableAsync(conn, stagingTable, columns.Select(c => c.Column).ToArray(),
                Project(rows, columns), cancellationToken);

            var updateColumns = columns.Select(c => c.Column)
                .Except(keyColumns, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var onClause = string.Join(" AND ", keyColumns.Select(k => $"tgt.\"{k}\" = src.\"{k}\""));
            var updateClause = string.Join(", ", updateColumns.Select(c => $"\"{c}\" = src.\"{c}\""));
            var allColumns = string.Join(", ", columns.Select(c => $"\"{c.Column}\""));
            var sourceColumns = string.Join(", ", columns.Select(c => $"src.\"{c.Column}\""));

            // WHEN MATCHED THEN UPDATE with no columns left (an all-key table) is invalid SQL, so skip the clause
            var updateWhenMatched = updateColumns.Length > 0
                ? $"WHEN MATCHED THEN UPDATE SET {updateClause}\n"
                : "";

            await ExecuteAsync(conn, $"""
                MERGE INTO "{table}" AS tgt
                USING "{stagingTable}" AS src
                ON {onClause}
                {updateWhenMatched}WHEN NOT MATCHED THEN INSERT ({allColumns}) VALUES ({sourceColumns})
                """, cancellationToken);
        }
        finally
        {
            await ExecuteAsync(conn, $"DROP TABLE IF EXISTS \"{stagingTable}\"", cancellationToken);
        }
    }

    /// <summary>
    /// Replace every existing child row for each parent touched by this batch (e.g. all Categories for a
    /// batch of Events) with the rows in <paramref name="rows"/>, using one DELETE + one bulk INSERT.
    /// This matches the "owned collection" semantics the EF model used - the incoming list is the full,
    /// authoritative set of children for whichever parents appear in it - and additionally cleans up
    /// children that were removed upstream, which the previous per-row Upsert never did.
    /// </summary>
    public static async Task BulkReplaceChildrenAsync<T>(DatabaseFacade database, string table, string parentKeyColumn,
        (string Column, Func<T, object?> Value)[] columns, IReadOnlyCollection<T> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) { return; }

        var conn = await EnsureOpenAsync(database, cancellationToken);
        var stagingTable = $"{table}_STAGE_{Guid.NewGuid():N}";

        try
        {
            await ExecuteAsync(conn, $"CREATE TEMPORARY TABLE \"{stagingTable}\" LIKE \"{table}\"", cancellationToken);
            await LoadIntoTableAsync(conn, stagingTable, columns.Select(c => c.Column).ToArray(),
                Project(rows, columns), cancellationToken);

            await ExecuteAsync(conn,
                $"""
                DELETE FROM "{table}"
                WHERE "{parentKeyColumn}" IN (SELECT DISTINCT "{parentKeyColumn}" FROM "{stagingTable}")
                """, cancellationToken);

            var allColumns = string.Join(", ", columns.Select(c => $"\"{c.Column}\""));
            await ExecuteAsync(conn,
                $"INSERT INTO \"{table}\" ({allColumns}) SELECT {allColumns} FROM \"{stagingTable}\"",
                cancellationToken);
        }
        finally
        {
            await ExecuteAsync(conn, $"DROP TABLE IF EXISTS \"{stagingTable}\"", cancellationToken);
        }
    }

    /// <summary>
    /// Plain bulk insert with no upsert/replace semantics, for append-only loads (e.g. the CSV batch import,
    /// where rows are never revisited and the target has an auto-incrementing key that shouldn't be staged).
    /// </summary>
    public static async Task BulkInsertAsync<T>(DatabaseFacade database, string table,
        (string Column, Func<T, object?> Value)[] columns, IReadOnlyCollection<T> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) { return; }

        var conn = await EnsureOpenAsync(database, cancellationToken);
        await LoadIntoTableAsync(conn, table, columns.Select(c => c.Column).ToArray(), Project(rows, columns),
            cancellationToken);
    }

    static IEnumerable<object?[]> Project<T>(IReadOnlyCollection<T> rows, (string Column, Func<T, object?> Value)[] columns) =>
        rows.Select(row => columns.Select(c => c.Value(row)).ToArray());

    static async Task<DbConnection> EnsureOpenAsync(DatabaseFacade database, CancellationToken cancellationToken)
    {
        var conn = database.GetDbConnection();
        if (conn.State != ConnectionState.Open) { await database.OpenConnectionAsync(cancellationToken); }
        return conn;
    }

    /// <summary>
    /// Writes rows to a local CSV file, PUTs it to <paramref name="table"/>'s implicit table stage, then
    /// COPY INTOs it into that same table, and finally removes the local file.
    /// </summary>
    static async Task LoadIntoTableAsync(DbConnection conn, string table, string[] columns,
        IEnumerable<object?[]> rows, CancellationToken cancellationToken)
    {
        var localPath = Path.Combine(Path.GetTempPath(), $"{table}_{Guid.NewGuid():N}.csv");
        try
        {
            await using (var writer = new StreamWriter(localPath, false, new UTF8Encoding(false)))
            {
                foreach (var row in rows) { WriteCsvRow(writer, row); }
            }

            // file:// with forward slashes is accepted by the Snowflake .NET driver's PUT parser on both
            // Windows and Linux; verify this against the actual driver version in use.
            var uploadUri = "file://" + localPath.Replace('\\', '/');
            // PUT must run synchronously - see ExecuteSync's comment for why
            ExecuteSync(conn, $"PUT '{uploadUri}' @%\"{table}\" OVERWRITE = TRUE AUTO_COMPRESS = TRUE");

            var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
            await ExecuteAsync(conn, $"""
                COPY INTO "{table}" ({columnList})
                FROM @%"{table}"
                FILE_FORMAT = (TYPE = CSV FIELD_OPTIONALLY_ENCLOSED_BY = '"' NULL_IF = ('\\N') EMPTY_FIELD_AS_NULL = FALSE)
                ON_ERROR = ABORT_STATEMENT
                PURGE = TRUE
                """, cancellationToken);
        }
        finally
        {
            if (File.Exists(localPath)) { File.Delete(localPath); }
        }
    }

    static void WriteCsvRow(TextWriter writer, object?[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0) { writer.Write(','); }
            WriteCsvField(writer, values[i]);
        }

        writer.Write('\r');
        writer.Write('\n');
    }

    // \N is used as the null token (rather than an empty field) so a genuine empty string can still be
    // written as a quoted "" without being mistaken for null.
    static void WriteCsvField(TextWriter writer, object? value)
    {
        if (value is null) { writer.Write(@"\N"); return; }

        var text = value switch
        {
            bool b => b ? "TRUE" : "FALSE",
            DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
            Uri uri => uri.ToString(),
            byte or sbyte or short or ushort or int or uint or long or ulong =>
                Convert.ToString(value, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };

        if (text is null) { writer.Write(@"\N"); return; }

        if (text.Length == 0) { writer.Write("\"\""); return; }

        if (text.IndexOfAny(['"', ',', '\r', '\n']) >= 0)
        {
            writer.Write('"');
            writer.Write(text.Replace("\"", "\"\""));
            writer.Write('"');
        }
        else { writer.Write(text); }
    }

    static async Task ExecuteAsync(DbConnection conn, string sql, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    // PUT (and GET) are handled client-side by the Snowflake .NET driver and are only supported through
    // the synchronous ADO.NET API - ExecuteNonQueryAsync throws "Get and Put are not supported in async
    // calls" for these specifically. Everything else (CREATE/MERGE/COPY INTO/DELETE/INSERT/DROP) goes
    // through ExecuteAsync above as normal.
    static void ExecuteSync(DbConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

}