using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703185200_F0008BrokerInsights")]
    public partial class F0008BrokerInsights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrokerInsightProjections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MetricKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MetricLabel = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    MetricFamily = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    Bucket = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Value = table.Column<decimal>(type: "numeric", nullable: true),
                    Denominator = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ComparisonValue = table.Column<decimal>(type: "numeric", nullable: true),
                    ComparisonPeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    ComparisonPeriodEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    SourceObjectTypesJson = table.Column<string>(type: "jsonb", nullable: false),
                    SourceRecordCount = table.Column<int>(type: "integer", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProducerId = table.Column<Guid>(type: "uuid", nullable: true),
                    TerritoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    LineOfBusiness = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Region = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    LastSourceUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProjectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProjectionStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
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
                    table.PrimaryKey("PK_BrokerInsightProjections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrokerInsight_Broker_Period",
                table: "BrokerInsightProjections",
                columns: new[] { "BrokerId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_BrokerInsight_Dimensions",
                table: "BrokerInsightProjections",
                columns: new[] { "ProgramId", "ProducerId", "TerritoryId", "Region", "LineOfBusiness" });

            migrationBuilder.CreateIndex(
                name: "IX_BrokerInsight_Metric_Period",
                table: "BrokerInsightProjections",
                columns: new[] { "MetricKey", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_BrokerInsight_ProjectedAt",
                table: "BrokerInsightProjections",
                column: "ProjectedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BrokerInsightProjections");
        }
    }
}
