using System;
using EFCore.Snowflake.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace libcal_etl.Migrations
{
    /// <inheritdoc />
    public partial class SnowflakeInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LIBCAL_APPOINTMENT_BOOKINGS",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    USER_HASH = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    FROM_DATE = table.Column<DateTimeOffset>(type: "TIMESTAMP_TZ(9)", nullable: false),
                    TO_DATE = table.Column<DateTimeOffset>(type: "TIMESTAMP_TZ(9)", nullable: false),
                    USER_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    LOCATION = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    LOCATION_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    GROUP = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    GROUP_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    CATEGORY_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    DIRECTIONS = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    CANCELLED = table.Column<bool>(type: "BOOLEAN", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LIBCAL_APPOINTMENT_BOOKINGS", x => x.ID);
                })
                .Annotation("Snowflake:TableType", SnowflakeTableType.Permanent);

            migrationBuilder.CreateTable(
                name: "LIBCAL_APPOINTMENT_QUESTIONS",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    LABEL = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    TYPE = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    REQUIRED = table.Column<bool>(type: "BOOLEAN", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LIBCAL_APPOINTMENT_QUESTIONS", x => x.ID);
                })
                .Annotation("Snowflake:TableType", SnowflakeTableType.Permanent);

            migrationBuilder.CreateTable(
                name: "LIBCAL_APPOINTMENT_USERS",
                columns: table => new
                {
                    USER_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    FIRST_NAME = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    LAST_NAME = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    NICKNAME = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    EMAIL = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    URL = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    DESCRIPTION = table.Column<string>(type: "VARCHAR(16777216)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LIBCAL_APPOINTMENT_USERS", x => x.USER_ID);
                })
                .Annotation("Snowflake:TableType", SnowflakeTableType.Permanent);

            migrationBuilder.CreateTable(
                name: "LIBCAL_ARCHIVED_SPACE_BOOKINGS",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false)
                        .Annotation("Snowflake:Identity", "START 1 INCREMENT 1 ORDER")
                        .Annotation("Snowflake:ValueGenerationStrategy", SnowflakeValueGenerationStrategy.AutoIncrement),
                    BOOKING_ID = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    SPACE_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: true),
                    SPACE_NAME = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    LOCATION = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    ZONE = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    CATEGORY = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    USER_HASH = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    FROM_DATE = table.Column<DateTime>(type: "TIMESTAMP_NTZ(9)", nullable: true),
                    TO_DATE = table.Column<DateTime>(type: "TIMESTAMP_NTZ(9)", nullable: true),
                    CREATED_DATE = table.Column<DateTime>(type: "TIMESTAMP_NTZ(9)", nullable: true),
                    EVENT_ID = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    EVENT_TITLE = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    EVENT_START = table.Column<DateTime>(type: "TIMESTAMP_NTZ(9)", nullable: true),
                    EVENT_END = table.Column<DateTime>(type: "TIMESTAMP_NTZ(9)", nullable: true),
                    STATUS = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    CANCELLED_BY_USER = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    CANCELLED_AT = table.Column<DateTime>(type: "TIMESTAMP_NTZ(9)", nullable: true),
                    SHOWED_UP = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    CHECKED_IN_DATE = table.Column<DateTime>(type: "TIMESTAMP_NTZ(9)", nullable: true),
                    CHECKED_OUT_DATE = table.Column<DateTime>(type: "TIMESTAMP_NTZ(9)", nullable: true),
                    COST = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    BOOKING_FORM_ANSWERS = table.Column<string>(type: "VARCHAR(16777216)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LIBCAL_ARCHIVED_SPACE_BOOKINGS", x => x.ID);
                })
                .Annotation("Snowflake:TableType", SnowflakeTableType.Permanent);

            migrationBuilder.CreateTable(
                name: "LIBCAL_EVENTS",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    TITLE = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    ALL_DAY = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    START = table.Column<DateTimeOffset>(type: "TIMESTAMP_TZ(9)", nullable: false),
                    END = table.Column<DateTimeOffset>(type: "TIMESTAMP_TZ(9)", nullable: false),
                    DESCRIPTION = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    URL_PUBLIC = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    URL_ADMIN = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    LOCATION_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: true),
                    LOCATION_TYPE = table.Column<long>(type: "NUMBER(19,0)", nullable: true),
                    LOCATION_NAME = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    CAMPUS_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: true),
                    CAMPUS_NAME = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    OWNER_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: true),
                    OWNER_NAME = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    PRESENTER = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    CALENDAR_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: true),
                    CALENDAR_NAME = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    CALENDAR_PUBLIC = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    CALENDAR_ADMIN = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    SEATS = table.Column<long>(type: "NUMBER(19,0)", nullable: true),
                    REGISTRATION = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    HAS_REGISTRATION_OPENED = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    HAS_REGISTRATION_CLOSED = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    PHYSICAL_SEATS = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    PHYSICAL_SEATS_TAKEN = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    ONLINE_SEATS = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    ONLINE_SEATS_TAKEN = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    SEATS_TAKEN = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    WAIT_LIST = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    COLOR = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    FEATURED_IMAGE = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    MORE_INFO = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    SETUP_TIME = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    TEARDOWN_TIME = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    ONLINE_USER_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    ZOOM_EMAIL = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    ONLINE_MEETING_ID = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    ONLINE_HOST_URL = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    ONLINE_JOIN_URL = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    ONLINE_JOIN_PASSWORD = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    ONLINE_PROVIDER = table.Column<string>(type: "VARCHAR(16777216)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LIBCAL_EVENTS", x => x.ID);
                })
                .Annotation("Snowflake:TableType", SnowflakeTableType.Permanent);

            migrationBuilder.CreateTable(
                name: "LIBCAL_SPACE_BOOKINGS",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    USER_HASH = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    BOOK_ID = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    ITEM_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    CATEGORY_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    LOCATION_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    FROM_DATE = table.Column<DateTimeOffset>(type: "TIMESTAMP_TZ(9)", nullable: false),
                    TO_DATE = table.Column<DateTimeOffset>(type: "TIMESTAMP_TZ(9)", nullable: false),
                    CREATED = table.Column<DateTimeOffset>(type: "TIMESTAMP_TZ(9)", nullable: false),
                    STATUS = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    LOCATION_NAME = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    CATEGORY_NAME = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    ITEM_NAME = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    CANCELLED = table.Column<DateTimeOffset>(type: "TIMESTAMP_TZ(9)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LIBCAL_SPACE_BOOKINGS", x => x.ID);
                })
                .Annotation("Snowflake:TableType", SnowflakeTableType.Permanent);

            migrationBuilder.CreateTable(
                name: "LIBCAL_QUESTION_ANSWERS",
                columns: table => new
                {
                    BOOKING_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    QUESTION_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    ANSWER = table.Column<string>(type: "VARCHAR(16777216)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LIBCAL_QUESTION_ANSWERS", x => new { x.BOOKING_ID, x.QUESTION_ID });
                    table.ForeignKey(
                        name: "FK_LIBCAL_QUESTION_ANSWERS_APPOINTMENT_QUESTION_QUESTION_ID",
                        column: x => x.QUESTION_ID,
                        principalTable: "LIBCAL_APPOINTMENT_QUESTIONS",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_LIBCAL_QUESTION_ANSWERS_LIBCAL_APPOINTMENT_BOOKINGS_BOOKING_ID",
                        column: x => x.BOOKING_ID,
                        principalTable: "LIBCAL_APPOINTMENT_BOOKINGS",
                        principalColumn: "ID");
                })
                .Annotation("Snowflake:TableType", SnowflakeTableType.Permanent);

            migrationBuilder.CreateTable(
                name: "LIBCAL_QUESTION_OPTIONS",
                columns: table => new
                {
                    QUESTION_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    OPTION = table.Column<string>(type: "VARCHAR(16777216)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LIBCAL_QUESTION_OPTIONS", x => new { x.QUESTION_ID, x.OPTION });
                    table.ForeignKey(
                        name: "FK_LIBCAL_QUESTION_OPTIONS_LIBCAL_APPOINTMENT_QUESTIONS_QUESTION_ID",
                        column: x => x.QUESTION_ID,
                        principalTable: "LIBCAL_APPOINTMENT_QUESTIONS",
                        principalColumn: "ID");
                })
                .Annotation("Snowflake:TableType", SnowflakeTableType.Permanent);

            migrationBuilder.CreateTable(
                name: "LIBCAL_CATEGORIES",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    EVENT_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    NAME = table.Column<string>(type: "VARCHAR(16777216)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LIBCAL_CATEGORIES", x => new { x.EVENT_ID, x.ID });
                    table.ForeignKey(
                        name: "FK_LIBCAL_CATEGORIES_LIBCAL_EVENTS_EVENT_ID",
                        column: x => x.EVENT_ID,
                        principalTable: "LIBCAL_EVENTS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Snowflake:TableType", SnowflakeTableType.Permanent);

            migrationBuilder.CreateTable(
                name: "LIBCAL_EVENT_REGISTRANTS",
                columns: table => new
                {
                    BOOKING_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    USER_HASH = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    REGISTRATION_TYPE = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    BARCODE = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    REGISTERED_DATE = table.Column<DateTimeOffset>(type: "TIMESTAMP_TZ(9)", nullable: false),
                    ATTENDANCE = table.Column<string>(type: "VARCHAR(16777216)", nullable: true),
                    EVENT_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LIBCAL_EVENT_REGISTRANTS", x => x.BOOKING_ID);
                    table.ForeignKey(
                        name: "FK_LIBCAL_EVENT_REGISTRANTS_LIBCAL_EVENTS_EVENT_ID",
                        column: x => x.EVENT_ID,
                        principalTable: "LIBCAL_EVENTS",
                        principalColumn: "ID");
                })
                .Annotation("Snowflake:TableType", SnowflakeTableType.Permanent);

            migrationBuilder.CreateTable(
                name: "LIBCAL_FUTURE_DATES",
                columns: table => new
                {
                    FUTURE_EVENT_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    ORIGINAL_EVENT_ID = table.Column<long>(type: "NUMBER(19,0)", nullable: false),
                    START = table.Column<DateTimeOffset>(type: "TIMESTAMP_TZ(9)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LIBCAL_FUTURE_DATES", x => new { x.ORIGINAL_EVENT_ID, x.FUTURE_EVENT_ID });
                    table.ForeignKey(
                        name: "FK_LIBCAL_FUTURE_DATES_LIBCAL_EVENTS_ORIGINAL_EVENT_ID",
                        column: x => x.ORIGINAL_EVENT_ID,
                        principalTable: "LIBCAL_EVENTS",
                        principalColumn: "ID");
                })
                .Annotation("Snowflake:TableType", SnowflakeTableType.Permanent);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LIBCAL_APPOINTMENT_USERS");

            migrationBuilder.DropTable(
                name: "LIBCAL_ARCHIVED_SPACE_BOOKINGS");

            migrationBuilder.DropTable(
                name: "LIBCAL_CATEGORIES");

            migrationBuilder.DropTable(
                name: "LIBCAL_EVENT_REGISTRANTS");

            migrationBuilder.DropTable(
                name: "LIBCAL_FUTURE_DATES");

            migrationBuilder.DropTable(
                name: "LIBCAL_QUESTION_ANSWERS");

            migrationBuilder.DropTable(
                name: "LIBCAL_QUESTION_OPTIONS");

            migrationBuilder.DropTable(
                name: "LIBCAL_SPACE_BOOKINGS");

            migrationBuilder.DropTable(
                name: "LIBCAL_EVENTS");

            migrationBuilder.DropTable(
                name: "LIBCAL_APPOINTMENT_BOOKINGS");

            migrationBuilder.DropTable(
                name: "LIBCAL_APPOINTMENT_QUESTIONS");
        }
    }
}
