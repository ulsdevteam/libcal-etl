# LibCal ETL

This console application pulls data from the LibCal API and uploads it to an oracle database.

The console app supports three commands: `update`, `batch`, and `print-schema`. 

## Commands
### `update`

Pulls data from the LibCal API and updates the database with it. Which data sources are updated, and what date range data will be pulled in, are configurable with command line arguments. Run `libcal-etl update --help` to see argument syntax. Requires `LIBCAL_CLIENT_ID`, `LIBCAL_CLIENT_SECRET`, and `CONNECTION_STRING` environment variables to be set.

### `batch`

Loads Spaces/Room Booking data from .csv files exported from the LibCal UI and inserts it into the database.
Batches can be exported from LibCal by going to `Spaces > Booking Explorer`. Requires the `CONNECTION_STRING` environment variable.

### `print-schema`

Outputs the `CREATE` statements used to make the tables the program interacts with. This requires the `CONNECTION_STRING` environment variable in order to determine the SQL dialect.

## Configuration

Will check for a `.env` file to read environment variables from.

The SQL dialect is determined by the format of the connection string. If it starts with `Filename=`, it will assume it is a Sqlite database file. Otherwise, it will assume it is a connection string for an Oracle database.

## Building

Building a Linux executable on Windows:
`dotnet publish -c Release --os linux -p:PublishSingleFile=true --sc`