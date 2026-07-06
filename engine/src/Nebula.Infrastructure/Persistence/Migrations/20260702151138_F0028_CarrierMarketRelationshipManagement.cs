using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F0028_CarrierMarketRelationshipManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarrierMarkets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NaicCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    AmBestRating = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MarketType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RelationshipOwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GeneralEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    MainPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierMarkets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarrierAppetiteNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierMarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineOfBusiness = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Region = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AppetiteLevel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Summary = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Detail = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierAppetiteNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarrierAppetiteNotes_CarrierMarkets_CarrierMarketId",
                        column: x => x.CarrierMarketId,
                        principalTable: "CarrierMarkets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarrierAppointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierMarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    States = table.Column<string[]>(type: "text[]", nullable: false),
                    LineOfBusiness = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AppointmentNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarrierAppointments_CarrierMarkets_CarrierMarketId",
                        column: x => x.CarrierMarketId,
                        principalTable: "CarrierMarkets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarrierMarketActivityLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierMarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationshipKind = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierMarketActivityLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarrierMarketActivityLinks_CarrierMarkets_CarrierMarketId",
                        column: x => x.CarrierMarketId,
                        principalTable: "CarrierMarkets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarrierMarketContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierMarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Roles = table.Column<string[]>(type: "text[]", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarrierMarketContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarrierMarketContacts_CarrierMarkets_CarrierMarketId",
                        column: x => x.CarrierMarketId,
                        principalTable: "CarrierMarkets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarrierAppetiteNotes_Market_Lob_Region",
                table: "CarrierAppetiteNotes",
                columns: new[] { "CarrierMarketId", "LineOfBusiness", "Region" });

            migrationBuilder.CreateIndex(
                name: "IX_CarrierAppointments_Market_Status",
                table: "CarrierAppointments",
                columns: new[] { "CarrierMarketId", "AppointmentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_CarrierMarketActivityLinks_Market_Related",
                table: "CarrierMarketActivityLinks",
                columns: new[] { "CarrierMarketId", "RelatedEntityType", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_CarrierMarketContacts_Market_Primary",
                table: "CarrierMarketContacts",
                columns: new[] { "CarrierMarketId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_CarrierMarkets_Code",
                table: "CarrierMarkets",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarrierMarkets_Status_MarketType",
                table: "CarrierMarkets",
                columns: new[] { "Status", "MarketType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarrierAppetiteNotes");

            migrationBuilder.DropTable(
                name: "CarrierAppointments");

            migrationBuilder.DropTable(
                name: "CarrierMarketActivityLinks");

            migrationBuilder.DropTable(
                name: "CarrierMarketContacts");

            migrationBuilder.DropTable(
                name: "CarrierMarkets");
        }
    }
}
