using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nebula.Infrastructure.Persistence;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260707150000_F0025_CommissionRevenue")]
    public partial class F0025_CommissionRevenue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommissionSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierMarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineOfBusiness = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    State = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Basis = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RatePercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    FlatAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    SourceNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_CommissionSchedules", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ExpectedCommissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CarrierMarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommissionScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProducerSplitAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PremiumBasisAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ExpectedGrossCommission = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ApprovedAdjustmentTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AdjustedExpectedCommission = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExceptionState = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SourceSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ExpectedCommissions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "ProducerSplitAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ProducerSplitAssignments", x => x.Id));

            migrationBuilder.CreateTable(
                name: "RevenueAttributionProjections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedCommissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProducerId = table.Column<Guid>(type: "uuid", nullable: true),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: true),
                    TerritoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CarrierMarketId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyPeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PolicyPeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    LineOfBusiness = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ExpectedGrossCommission = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedAdjustmentTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AdjustedExpectedCommission = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProducerAllocationAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnresolvedExceptionCount = table.Column<int>(type: "integer", nullable: false),
                    SourceRefreshedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_RevenueAttributionProjections", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CommissionAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedCommissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_CommissionAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionAdjustments_ExpectedCommissions_ExpectedCommissionId",
                        column: x => x.ExpectedCommissionId,
                        principalTable: "ExpectedCommissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProducerSplitParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProducerSplitAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProducerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SplitPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    SourceOwnershipSnapshotJson = table.Column<string>(type: "jsonb", nullable: true),
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
                    table.PrimaryKey("PK_ProducerSplitParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProducerSplitParticipants_ProducerSplitAssignments_ProducerSplitAssignmentId",
                        column: x => x.ProducerSplitAssignmentId,
                        principalTable: "ProducerSplitAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_CommissionAdjustments_Commission_Status", table: "CommissionAdjustments", columns: new[] { "ExpectedCommissionId", "Status" });
            migrationBuilder.CreateIndex(name: "IX_CommissionSchedules_Scope_Effective", table: "CommissionSchedules", columns: new[] { "CarrierMarketId", "LineOfBusiness", "State", "ProductCode", "EffectiveFrom", "EffectiveTo" });
            migrationBuilder.CreateIndex(name: "IX_ExpectedCommissions_Carrier_Status", table: "ExpectedCommissions", columns: new[] { "CarrierMarketId", "Status" });
            migrationBuilder.CreateIndex(name: "IX_ExpectedCommissions_Policy_Status_Exception", table: "ExpectedCommissions", columns: new[] { "PolicyId", "Status", "ExceptionState" });
            migrationBuilder.CreateIndex(name: "IX_ProducerSplitAssignments_Policy_Effective", table: "ProducerSplitAssignments", columns: new[] { "PolicyId", "EffectiveFrom", "EffectiveTo" });
            migrationBuilder.CreateIndex(name: "IX_ProducerSplitParticipants_Assignment_Producer", table: "ProducerSplitParticipants", columns: new[] { "ProducerSplitAssignmentId", "ProducerId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_RevenueAttributionProjections_Commission", table: "RevenueAttributionProjections", column: "ExpectedCommissionId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_RevenueAttributionProjections_Dimensions", table: "RevenueAttributionProjections", columns: new[] { "ProducerId", "BrokerId", "TerritoryId", "CarrierMarketId" });
            migrationBuilder.CreateIndex(name: "IX_RevenueAttributionProjections_Period", table: "RevenueAttributionProjections", columns: new[] { "PolicyPeriodStart", "PolicyPeriodEnd" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CommissionAdjustments");
            migrationBuilder.DropTable(name: "CommissionSchedules");
            migrationBuilder.DropTable(name: "ProducerSplitParticipants");
            migrationBuilder.DropTable(name: "RevenueAttributionProjections");
            migrationBuilder.DropTable(name: "ExpectedCommissions");
            migrationBuilder.DropTable(name: "ProducerSplitAssignments");
        }
    }
}
