using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Loads batches of rows into Snowflake via stage -> PUT -> COPY INTO -> MERGE, instead of the
/// one-SELECT-plus-one-INSERT/UPDATE-per-row pattern that EF Core's SaveChanges uses. Each Snowflake
/// round trip costs a few hundred ms to over a second regardless of how small the statement is, so
/// for a batch of N rows this trades ~2N round trips for roughly 3 fixed-cost round trips total.
///
/// This only works against Snowflake (stages, PUT, and MERGE aren't available in Sqlite), so callers
/// should check <c>Database.IsSnowflake</c> and fall back to the row-by-row EF path otherwise.
///
/// NOTE: this has not been run against a live Snowflake warehouse. The SQL text (particularly the PUT
/// file:// URI form and the FILE_FORMAT options) should be verified against a real account before
/// relying on it for production loads - please treat this as a draft to validate, not tested code.
/// </summary>
static class SnowflakeBulkLoader
{
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
            await ExecuteAsync(conn, $"PUT '{uploadUri}' @%\"{table}\" OVERWRITE = TRUE AUTO_COMPRESS = TRUE",
                cancellationToken);

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
}