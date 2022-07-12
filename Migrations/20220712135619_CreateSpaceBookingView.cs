using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace libcal_etl.Migrations
{
    public partial class CreateSpaceBookingView : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                create view LIBCAL_SPACE_BOOKINGS_V as
                select
                BOOK_ID as BOOKING_ID,
                LOCATION_NAME as LOCATION,
                CATEGORY_NAME as CATEGORY,
                ITEM_NAME as SPACE_NAME,
                FROM_DATE,
                TO_DATE,
                CREATED as CREATED_DATE,
                FIRST_NAME,
                LAST_NAME,
                EMAIL,
                ACCOUNT,
                STATUS,
                NICKNAME,
                CANCELLED
                from LIBCAL_SPACE_BOOKINGS
                UNION ALL
                select
                BOOKING_ID,
                LOCATION,
                CATEGORY,
                SPACE_NAME,
                FROM_DATE,
                TO_DATE,
                CREATED_DATE,
                FIRST_NAME,
                LAST_NAME,
                EMAIL,
                ACCOUNT,
                STATUS,
                PUBLIC_NICKNAME as NICKNAME,
                CANCELLED_BY_USER as CANCELLED
                from LIBCAL_ARCHIVED_SPACE_BOOKINGS;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"drop view LIBCAL_SPACE_BOOKINGS_V;");
        }
    }
}
