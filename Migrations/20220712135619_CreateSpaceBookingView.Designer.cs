﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oracle.EntityFrameworkCore.Metadata;

#nullable disable

namespace libcal_etl.Migrations
{
    [DbContext(typeof(Database))]
    [Migration("20220712135619_CreateSpaceBookingView")]
    partial class CreateSpaceBookingView
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            OracleModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("LibCalTypes.AppointmentBooking", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("ID");

                    b.Property<bool>("Cancelled")
                        .HasColumnType("NUMBER(1)")
                        .HasColumnName("CANCELLED");

                    b.Property<long>("CategoryId")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("CATEGORY_ID");

                    b.Property<string>("Directions")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("DIRECTIONS");

                    b.Property<string>("Email")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("EMAIL");

                    b.Property<string>("FirstName")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("FIRST_NAME");

                    b.Property<DateTimeOffset>("FromDate")
                        .HasColumnType("TIMESTAMP(7) WITH TIME ZONE")
                        .HasColumnName("FROM_DATE");

                    b.Property<string>("Group")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("GROUP");

                    b.Property<long>("GroupId")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("GROUP_ID");

                    b.Property<string>("LastName")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("LAST_NAME");

                    b.Property<string>("Location")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("LOCATION");

                    b.Property<long>("LocationId")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("LOCATION_ID");

                    b.Property<DateTimeOffset>("ToDate")
                        .HasColumnType("TIMESTAMP(7) WITH TIME ZONE")
                        .HasColumnName("TO_DATE");

                    b.Property<long>("UserId")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("USER_ID");

                    b.HasKey("Id")
                        .HasName("PK_LIBCAL_APPOINTMENT_BOOKINGS");

                    b.ToTable("LIBCAL_APPOINTMENT_BOOKINGS", (string)null);
                });

            modelBuilder.Entity("LibCalTypes.AppointmentQuestion", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("ID");

                    b.Property<string>("Label")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("LABEL");

                    b.Property<bool>("Required")
                        .HasColumnType("NUMBER(1)")
                        .HasColumnName("REQUIRED");

                    b.Property<string>("Type")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("TYPE");

                    b.HasKey("Id")
                        .HasName("PK_LIBCAL_APPOINTMENT_QUESTIONS");

                    b.ToTable("LIBCAL_APPOINTMENT_QUESTIONS", (string)null);
                });

            modelBuilder.Entity("LibCalTypes.AppointmentUser", b =>
                {
                    b.Property<long>("UserId")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("USER_ID");

                    b.Property<string>("Description")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("DESCRIPTION");

                    b.Property<string>("Email")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("EMAIL");

                    b.Property<string>("FirstName")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("FIRST_NAME");

                    b.Property<string>("LastName")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("LAST_NAME");

                    b.Property<string>("Nickname")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("NICKNAME");

                    b.Property<string>("Url")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("URL");

                    b.HasKey("UserId")
                        .HasName("PK_LIBCAL_APPOINTMENT_USERS");

                    b.ToTable("LIBCAL_APPOINTMENT_USERS", (string)null);
                });

            modelBuilder.Entity("LibCalTypes.ArchivedSpaceBooking", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("ID");

                    OraclePropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<string>("Account")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("ACCOUNT");

                    b.Property<string>("BookingFormAnswers")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("BOOKING_FORM_ANSWERS");

                    b.Property<string>("BookingId")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("BOOKING_ID");

                    b.Property<DateTime?>("CancelledAt")
                        .HasColumnType("TIMESTAMP(7)")
                        .HasColumnName("CANCELLED_AT");

                    b.Property<string>("CancelledByUser")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("CANCELLED_BY_USER");

                    b.Property<string>("Category")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("CATEGORY");

                    b.Property<DateTime?>("CheckedInDate")
                        .HasColumnType("TIMESTAMP(7)")
                        .HasColumnName("CHECKED_IN_DATE");

                    b.Property<DateTime?>("CheckedOutDate")
                        .HasColumnType("TIMESTAMP(7)")
                        .HasColumnName("CHECKED_OUT_DATE");

                    b.Property<string>("Cost")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("COST");

                    b.Property<DateTime?>("CreatedDate")
                        .HasColumnType("TIMESTAMP(7)")
                        .HasColumnName("CREATED_DATE");

                    b.Property<string>("Email")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("EMAIL");

                    b.Property<DateTime?>("EventEnd")
                        .HasColumnType("TIMESTAMP(7)")
                        .HasColumnName("EVENT_END");

                    b.Property<string>("EventId")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("EVENT_ID");

                    b.Property<DateTime?>("EventStart")
                        .HasColumnType("TIMESTAMP(7)")
                        .HasColumnName("EVENT_START");

                    b.Property<string>("EventTitle")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("EVENT_TITLE");

                    b.Property<string>("FirstName")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("FIRST_NAME");

                    b.Property<DateTime?>("FromDate")
                        .HasColumnType("TIMESTAMP(7)")
                        .HasColumnName("FROM_DATE");

                    b.Property<string>("LastName")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("LAST_NAME");

                    b.Property<string>("Location")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("LOCATION");

                    b.Property<string>("PublicNickname")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("PUBLIC_NICKNAME");

                    b.Property<string>("ShowedUp")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("SHOWED_UP");

                    b.Property<string>("SpaceId")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("SPACE_ID");

                    b.Property<string>("SpaceName")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("SPACE_NAME");

                    b.Property<string>("Status")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("STATUS");

                    b.Property<DateTime?>("ToDate")
                        .HasColumnType("TIMESTAMP(7)")
                        .HasColumnName("TO_DATE");

                    b.Property<string>("Zone")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("ZONE");

                    b.HasKey("Id")
                        .HasName("PK_LIBCAL_ARCHIVED_SPACE_BOOKINGS");

                    b.ToTable("LIBCAL_ARCHIVED_SPACE_BOOKINGS", (string)null);
                });

            modelBuilder.Entity("LibCalTypes.Event", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("ID");

                    b.Property<bool>("AllDay")
                        .HasColumnType("NUMBER(1)")
                        .HasColumnName("ALL_DAY");

                    b.Property<string>("Color")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("COLOR");

                    b.Property<string>("Description")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("DESCRIPTION");

                    b.Property<DateTimeOffset>("End")
                        .HasColumnType("TIMESTAMP(7) WITH TIME ZONE")
                        .HasColumnName("END");

                    b.Property<string>("FeaturedImage")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("FEATURED_IMAGE");

                    b.Property<bool>("HasRegistrationClosed")
                        .HasColumnType("NUMBER(1)")
                        .HasColumnName("HAS_REGISTRATION_CLOSED");

                    b.Property<bool>("HasRegistrationOpened")
                        .HasColumnType("NUMBER(1)")
                        .HasColumnName("HAS_REGISTRATION_OPENED");

                    b.Property<string>("MoreInfo")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("MORE_INFO");

                    b.Property<string>("OnlineHostUrl")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("ONLINE_HOST_URL");

                    b.Property<string>("OnlineJoinPassword")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("ONLINE_JOIN_PASSWORD");

                    b.Property<string>("OnlineJoinUrl")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("ONLINE_JOIN_URL");

                    b.Property<string>("OnlineMeetingId")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("ONLINE_MEETING_ID");

                    b.Property<string>("OnlineProvider")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("ONLINE_PROVIDER");

                    b.Property<long>("OnlineSeats")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("ONLINE_SEATS");

                    b.Property<long>("OnlineSeatsTaken")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("ONLINE_SEATS_TAKEN");

                    b.Property<long>("OnlineUserId")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("ONLINE_USER_ID");

                    b.Property<long>("PhysicalSeats")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("PHYSICAL_SEATS");

                    b.Property<long>("PhysicalSeatsTaken")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("PHYSICAL_SEATS_TAKEN");

                    b.Property<string>("Presenter")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("PRESENTER");

                    b.Property<bool>("Registration")
                        .HasColumnType("NUMBER(1)")
                        .HasColumnName("REGISTRATION");

                    b.Property<long?>("Seats")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("SEATS");

                    b.Property<long>("SeatsTaken")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("SEATS_TAKEN");

                    b.Property<long>("SetupTime")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("SETUP_TIME");

                    b.Property<DateTimeOffset>("Start")
                        .HasColumnType("TIMESTAMP(7) WITH TIME ZONE")
                        .HasColumnName("START");

                    b.Property<long>("TeardownTime")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("TEARDOWN_TIME");

                    b.Property<string>("Title")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("TITLE");

                    b.Property<bool>("WaitList")
                        .HasColumnType("NUMBER(1)")
                        .HasColumnName("WAIT_LIST");

                    b.Property<string>("ZoomEmail")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("ZOOM_EMAIL");

                    b.HasKey("Id")
                        .HasName("PK_LIBCAL_EVENTS");

                    b.ToTable("LIBCAL_EVENTS", (string)null);
                });

            modelBuilder.Entity("LibCalTypes.SpaceBooking", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("ID");

                    b.Property<string>("Account")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("ACCOUNT");

                    b.Property<string>("BookId")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("BOOK_ID");

                    b.Property<DateTimeOffset?>("Cancelled")
                        .HasColumnType("TIMESTAMP(7) WITH TIME ZONE")
                        .HasColumnName("CANCELLED");

                    b.Property<long>("CategoryId")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("CATEGORY_ID");

                    b.Property<string>("CategoryName")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("CATEGORY_NAME");

                    b.Property<DateTimeOffset>("Created")
                        .HasColumnType("TIMESTAMP(7) WITH TIME ZONE")
                        .HasColumnName("CREATED");

                    b.Property<string>("Email")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("EMAIL");

                    b.Property<string>("FirstName")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("FIRST_NAME");

                    b.Property<DateTimeOffset>("FromDate")
                        .HasColumnType("TIMESTAMP(7) WITH TIME ZONE")
                        .HasColumnName("FROM_DATE");

                    b.Property<long>("ItemId")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("ITEM_ID");

                    b.Property<string>("ItemName")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("ITEM_NAME");

                    b.Property<string>("LastName")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("LAST_NAME");

                    b.Property<long>("LocationId")
                        .HasColumnType("NUMBER(19)")
                        .HasColumnName("LOCATION_ID");

                    b.Property<string>("LocationName")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("LOCATION_NAME");

                    b.Property<string>("Nickname")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("NICKNAME");

                    b.Property<string>("Status")
                        .HasColumnType("NVARCHAR2(2000)")
                        .HasColumnName("STATUS");

                    b.Property<DateTimeOffset>("ToDate")
                        .HasColumnType("TIMESTAMP(7) WITH TIME ZONE")
                        .HasColumnName("TO_DATE");

                    b.HasKey("Id")
                        .HasName("PK_LIBCAL_SPACE_BOOKINGS");

                    b.ToTable("LIBCAL_SPACE_BOOKINGS", (string)null);
                });

            modelBuilder.Entity("LibCalTypes.AppointmentBooking", b =>
                {
                    b.OwnsMany("LibCalTypes.QuestionAnswer", "Answers", b1 =>
                        {
                            b1.Property<long>("BookingId")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("BOOKING_ID");

                            b1.Property<long>("QuestionId")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("QUESTION_ID");

                            b1.Property<string>("Answer")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("ANSWER");

                            b1.HasKey("BookingId", "QuestionId")
                                .HasName("PK_LIBCAL_QUESTION_ANSWERS");

                            b1.HasIndex("QuestionId")
                                .HasDatabaseName("IX_LIBCAL_QUESTION_ANSWERS_QUESTION_ID");

                            b1.ToTable("LIBCAL_QUESTION_ANSWERS", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("BookingId")
                                .HasConstraintName("FK_LIBCAL_QUESTION_ANSWERS_LIBCAL_APPOINTMENT_BOOKINGS_APPOINTMENT_BOOKING_ID");

                            b1.HasOne("LibCalTypes.AppointmentQuestion", null)
                                .WithMany()
                                .HasForeignKey("QuestionId")
                                .OnDelete(DeleteBehavior.Cascade)
                                .IsRequired()
                                .HasConstraintName("FK_LIBCAL_QUESTION_ANSWERS_APPOINTMENT_QUESTION_APPOINTMENT_QUESTION_ID");
                        });

                    b.Navigation("Answers");
                });

            modelBuilder.Entity("LibCalTypes.AppointmentQuestion", b =>
                {
                    b.OwnsMany("LibCalTypes.QuestionOption", "Options", b1 =>
                        {
                            b1.Property<long>("QuestionId")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("QUESTION_ID");

                            b1.Property<string>("Option")
                                .HasColumnType("NVARCHAR2(450)")
                                .HasColumnName("OPTION");

                            b1.HasKey("QuestionId", "Option")
                                .HasName("PK_LIBCAL_QUESTION_OPTIONS");

                            b1.ToTable("LIBCAL_QUESTION_OPTIONS", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("QuestionId")
                                .HasConstraintName("FK_LIBCAL_QUESTION_OPTIONS_LIBCAL_APPOINTMENT_QUESTIONS_APPOINTMENT_QUESTION_ID");
                        });

                    b.Navigation("Options");
                });

            modelBuilder.Entity("LibCalTypes.Event", b =>
                {
                    b.OwnsOne("LibCalTypes.Calendar", "Calendar", b1 =>
                        {
                            b1.Property<long>("EventId")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("ID");

                            b1.Property<string>("Admin")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("CALENDAR_ADMIN");

                            b1.Property<long>("Id")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("CALENDAR_ID");

                            b1.Property<string>("Name")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("CALENDAR_NAME");

                            b1.Property<string>("Public")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("CALENDAR_PUBLIC");

                            b1.HasKey("EventId");

                            b1.ToTable("LIBCAL_EVENTS");

                            b1.WithOwner()
                                .HasForeignKey("EventId")
                                .HasConstraintName("FK_LIBCAL_EVENTS_LIBCAL_EVENTS_ID");
                        });

                    b.OwnsOne("LibCalTypes.Campus", "Campus", b1 =>
                        {
                            b1.Property<long>("EventId")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("ID");

                            b1.Property<long>("Id")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("CAMPUS_ID");

                            b1.Property<string>("Name")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("CAMPUS_NAME");

                            b1.HasKey("EventId");

                            b1.ToTable("LIBCAL_EVENTS");

                            b1.WithOwner()
                                .HasForeignKey("EventId")
                                .HasConstraintName("FK_LIBCAL_EVENTS_LIBCAL_EVENTS_ID");
                        });

                    b.OwnsMany("LibCalTypes.Category", "Category", b1 =>
                        {
                            b1.Property<long>("EventId")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("EVENT_ID");

                            b1.Property<long>("Id")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("ID");

                            b1.Property<string>("Name")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("NAME");

                            b1.HasKey("EventId", "Id")
                                .HasName("PK_LIBCAL_CATEGORIES");

                            b1.ToTable("LIBCAL_CATEGORIES", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("EventId")
                                .HasConstraintName("FK_LIBCAL_CATEGORIES_LIBCAL_EVENTS_EVENT_ID");
                        });

                    b.OwnsOne("LibCalTypes.Location", "Location", b1 =>
                        {
                            b1.Property<long>("EventId")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("ID");

                            b1.Property<long>("Id")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("LOCATION_ID");

                            b1.Property<string>("Name")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("LOCATION_NAME");

                            b1.Property<long>("Type")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("LOCATION_TYPE");

                            b1.HasKey("EventId");

                            b1.ToTable("LIBCAL_EVENTS");

                            b1.WithOwner()
                                .HasForeignKey("EventId")
                                .HasConstraintName("FK_LIBCAL_EVENTS_LIBCAL_EVENTS_ID");
                        });

                    b.OwnsOne("LibCalTypes.Owner", "Owner", b1 =>
                        {
                            b1.Property<long>("EventId")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("ID");

                            b1.Property<long>("Id")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("OWNER_ID");

                            b1.Property<string>("Name")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("OWNER_NAME");

                            b1.HasKey("EventId");

                            b1.ToTable("LIBCAL_EVENTS");

                            b1.WithOwner()
                                .HasForeignKey("EventId")
                                .HasConstraintName("FK_LIBCAL_EVENTS_LIBCAL_EVENTS_ID");
                        });

                    b.OwnsMany("LibCalTypes.Registrant", "Registrants", b1 =>
                        {
                            b1.Property<long>("BookingId")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("BOOKING_ID");

                            b1.Property<string>("Attendance")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("ATTENDANCE");

                            b1.Property<string>("Barcode")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("BARCODE");

                            b1.Property<string>("Email")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("EMAIL");

                            b1.Property<long>("EventId")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("EVENT_ID");

                            b1.Property<string>("FirstName")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("FIRST_NAME");

                            b1.Property<string>("LastName")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("LAST_NAME");

                            b1.Property<string>("Phone")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("PHONE");

                            b1.Property<DateTimeOffset>("RegisteredDate")
                                .HasColumnType("TIMESTAMP(7) WITH TIME ZONE")
                                .HasColumnName("REGISTERED_DATE");

                            b1.Property<string>("RegistrationType")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("REGISTRATION_TYPE");

                            b1.HasKey("BookingId")
                                .HasName("PK_LIBCAL_EVENT_REGISTRANTS");

                            b1.HasIndex("EventId")
                                .HasDatabaseName("IX_LIBCAL_EVENT_REGISTRANTS_EVENT_ID");

                            b1.ToTable("LIBCAL_EVENT_REGISTRANTS", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("EventId")
                                .HasConstraintName("FK_LIBCAL_EVENT_REGISTRANTS_LIBCAL_EVENTS_EVENT_ID");
                        });

                    b.OwnsOne("LibCalTypes.Url", "Url", b1 =>
                        {
                            b1.Property<long>("EventId")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("ID");

                            b1.Property<string>("Admin")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("URL_ADMIN");

                            b1.Property<string>("Public")
                                .HasColumnType("NVARCHAR2(2000)")
                                .HasColumnName("URL_PUBLIC");

                            b1.HasKey("EventId");

                            b1.ToTable("LIBCAL_EVENTS");

                            b1.WithOwner()
                                .HasForeignKey("EventId")
                                .HasConstraintName("FK_LIBCAL_EVENTS_LIBCAL_EVENTS_ID");
                        });

                    b.OwnsMany("LibCalTypes.FutureDate", "FutureDates", b1 =>
                        {
                            b1.Property<long>("OriginalEventId")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("ORIGINAL_EVENT_ID");

                            b1.Property<long>("FutureEventId")
                                .HasColumnType("NUMBER(19)")
                                .HasColumnName("FUTURE_EVENT_ID");

                            b1.Property<DateTimeOffset>("Start")
                                .HasColumnType("TIMESTAMP(7) WITH TIME ZONE")
                                .HasColumnName("START");

                            b1.HasKey("OriginalEventId", "FutureEventId")
                                .HasName("PK_LIBCAL_FUTURE_DATES");

                            b1.ToTable("LIBCAL_FUTURE_DATES", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("OriginalEventId")
                                .HasConstraintName("FK_LIBCAL_FUTURE_DATES_LIBCAL_EVENTS_EVENT_ID");
                        });

                    b.Navigation("Calendar");

                    b.Navigation("Campus");

                    b.Navigation("Category");

                    b.Navigation("FutureDates");

                    b.Navigation("Location");

                    b.Navigation("Owner");

                    b.Navigation("Registrants");

                    b.Navigation("Url");
                });
#pragma warning restore 612, 618
        }
    }
}
