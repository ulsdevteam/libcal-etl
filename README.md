# LibCal ETL

This console application pulls data from the LibCal API and uploads it to a Snowflake database.

The console app supports two commands: `update`, and `print-schema`.

## Commands

### `update`

Pulls data from the LibCal API and updates the database with it. Which data sources are updated, and what date range
data will be pulled in, are configurable with command line arguments. Run `libcal-etl update --help` to see argument
syntax. Requires `LIBCAL_CLIENT_ID`, `LIBCAL_CLIENT_SECRET`, and `CONNECTION_STRING` environment variables to be set.

### `print-schema`

Outputs the `CREATE` statements used to make the tables the program interacts with. This requires
the `CONNECTION_STRING` environment variable in order to determine the SQL dialect.

## Configuration

Will check for a `.env` file to read environment variables from.

## Building

Building a Linux executable on Windows:
`dotnet publish -c Release --os linux -p:PublishSingleFile=true --sc`

## Migrations

Using the EF cli to generate an idempotent update script:
`dotnet ef migrations script -i`