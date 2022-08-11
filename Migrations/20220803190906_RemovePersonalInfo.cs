using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace libcal_etl.Migrations
{
    public partial class RemovePersonalInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ACCOUNT",
                table: "LIBCAL_SPACE_BOOKINGS");

            migrationBuilder.DropColumn(
                name: "EMAIL",
                table: "LIBCAL_SPACE_BOOKINGS");

            migrationBuilder.DropColumn(
                name: "FIRST_NAME",
                table: "LIBCAL_SPACE_BOOKINGS");

            migrationBuilder.DropColumn(
                name: "LAST_NAME",
                table: "LIBCAL_SPACE_BOOKINGS");

            migrationBuilder.DropColumn(
                name: "EMAIL",
                table: "LIBCAL_EVENT_REGISTRANTS");

            migrationBuilder.DropColumn(
                name: "FIRST_NAME",
                table: "LIBCAL_EVENT_REGISTRANTS");

            migrationBuilder.DropColumn(
                name: "LAST_NAME",
                table: "LIBCAL_EVENT_REGISTRANTS");

            migrationBuilder.DropColumn(
                name: "ACCOUNT",
                table: "LIBCAL_ARCHIVED_SPACE_BOOKINGS");

            migrationBuilder.DropColumn(
                name: "EMAIL",
                table: "LIBCAL_ARCHIVED_SPACE_BOOKINGS");

            migrationBuilder.DropColumn(
                name: "FIRST_NAME",
                table: "LIBCAL_ARCHIVED_SPACE_BOOKINGS");

            migrationBuilder.DropColumn(
                name: "LAST_NAME",
                table: "LIBCAL_ARCHIVED_SPACE_BOOKINGS");

            migrationBuilder.DropColumn(
                name: "EMAIL",
                table: "LIBCAL_APPOINTMENT_BOOKINGS");

            migrationBuilder.DropColumn(
                name: "FIRST_NAME",
                table: "LIBCAL_APPOINTMENT_BOOKINGS");

            migrationBuilder.DropColumn(
                name: "NICKNAME",
                table: "LIBCAL_SPACE_BOOKINGS");

            migrationBuilder.DropColumn(
                name: "PHONE",
                table: "LIBCAL_EVENT_REGISTRANTS");

            migrationBuilder.DropColumn(
                name: "PUBLIC_NICKNAME",
                table: "LIBCAL_ARCHIVED_SPACE_BOOKINGS");

            migrationBuilder.DropColumn(
                name: "LAST_NAME",
                table: "LIBCAL_APPOINTMENT_BOOKINGS");
            
            migrationBuilder.AddColumn<string>(
                name: "USER_HASH",
                table: "LIBCAL_SPACE_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);
            
            migrationBuilder.AddColumn<string>(
                name: "USER_HASH",
                table: "LIBCAL_EVENT_REGISTRANTS",
                type: "NVARCHAR2(2000)",
                nullable: true);
            
            migrationBuilder.AddColumn<string>(
                name: "USER_HASH",
                table: "LIBCAL_ARCHIVED_SPACE_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);
            
            migrationBuilder.AddColumn<string>(
                name: "USER_HASH",
                table: "LIBCAL_APPOINTMENT_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "USER_HASH",
                table: "LIBCAL_SPACE_BOOKINGS");

            migrationBuilder.DropColumn(
                name: "USER_HASH",
                table: "LIBCAL_EVENT_REGISTRANTS");

            migrationBuilder.DropColumn(
                name: "USER_HASH",
                table: "LIBCAL_ARCHIVED_SPACE_BOOKINGS");

            migrationBuilder.DropColumn(
                name: "USER_HASH",
                table: "LIBCAL_APPOINTMENT_BOOKINGS");
            
            migrationBuilder.AddColumn<string>(
                name: "NICKNAME",
                table: "LIBCAL_SPACE_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);
            
            migrationBuilder.AddColumn<string>(
                name: "PHONE",
                table: "LIBCAL_EVENT_REGISTRANTS",
                type: "NVARCHAR2(2000)",
                nullable: true);
            
            migrationBuilder.AddColumn<string>(
                name: "PUBLIC_NICKNAME",
                table: "LIBCAL_ARCHIVED_SPACE_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);
            
            migrationBuilder.AddColumn<string>(
                name: "LAST_NAME",
                table: "LIBCAL_APPOINTMENT_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ACCOUNT",
                table: "LIBCAL_SPACE_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EMAIL",
                table: "LIBCAL_SPACE_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FIRST_NAME",
                table: "LIBCAL_SPACE_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LAST_NAME",
                table: "LIBCAL_SPACE_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EMAIL",
                table: "LIBCAL_EVENT_REGISTRANTS",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FIRST_NAME",
                table: "LIBCAL_EVENT_REGISTRANTS",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LAST_NAME",
                table: "LIBCAL_EVENT_REGISTRANTS",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ACCOUNT",
                table: "LIBCAL_ARCHIVED_SPACE_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EMAIL",
                table: "LIBCAL_ARCHIVED_SPACE_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FIRST_NAME",
                table: "LIBCAL_ARCHIVED_SPACE_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LAST_NAME",
                table: "LIBCAL_ARCHIVED_SPACE_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EMAIL",
                table: "LIBCAL_APPOINTMENT_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FIRST_NAME",
                table: "LIBCAL_APPOINTMENT_BOOKINGS",
                type: "NVARCHAR2(2000)",
                nullable: true);
        }
    }
}
