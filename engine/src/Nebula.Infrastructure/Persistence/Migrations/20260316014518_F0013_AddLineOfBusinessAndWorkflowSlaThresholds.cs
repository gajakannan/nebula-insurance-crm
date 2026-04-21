using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F0013_AddLineOfBusinessAndWorkflowSlaThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LineOfBusiness",
                table: "Submissions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LineOfBusiness",
                table: "Renewals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkflowSlaThresholds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WarningDays = table.Column<int>(type: "integer", nullable: false),
                    TargetDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSlaThresholds", x => x.Id);
                    table.CheckConstraint("CK_WorkflowSlaThresholds_WarningLessThanTarget", "\"WarningDays\" < \"TargetDays\"");
                });

            migrationBuilder.InsertData(
                table: "WorkflowSlaThresholds",
                columns: new[] { "Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays" },
                values: new object[,]
                {
                    { new Guid("0e5f31e6-af58-4e30-8ea0-f2d6f862994e"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "Early", 30, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 7 },
                    { new Guid("0ef57fce-6bd8-42e7-b1ef-767e44a02817"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "BindRequested", 5, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 2 },
                    { new Guid("1fef8cb4-2f9b-41e8-9329-3c1a5d22790a"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "Received", 2, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { new Guid("2a620479-fc25-4a25-b0c5-1dce00a3693a"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "WaitingOnBroker", 10, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 5 },
                    { new Guid("3047cb13-59f8-4d87-a79d-e80e9dcf28ea"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "Quoted", 21, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 7 },
                    { new Guid("30efe68f-9e5c-4e7f-9191-e68ee0f8eb26"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "Created", 3, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { new Guid("33cc5f8d-33ea-4f8a-a737-2f64946f044f"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "WaitingOnDocuments", 10, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 5 },
                    { new Guid("379f3ad6-68f0-4d2f-b52f-5ab9bb40f157"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "WaitingOnBroker", 10, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 5 },
                    { new Guid("77ca3fa9-fddd-47ec-b4d2-84bcbf001687"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "InReview", 14, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 5 },
                    { new Guid("8b43ed42-17f2-426a-a14a-442f6a7d43d4"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "InReview", 14, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 5 },
                    { new Guid("95db58fe-ef54-4c7b-b707-0cdf6458cd5b"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "RequoteRequested", 21, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 7 },
                    { new Guid("bb695667-05cf-43dd-a89c-c05e4747967c"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "OutreachStarted", 7, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 3 },
                    { new Guid("d7fe40cd-c9a5-4fd5-b09c-47f10ff0f20f"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "Negotiation", 21, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 7 },
                    { new Guid("ec690f3d-84c8-4709-8b32-ff1efde52e52"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "ReadyForUWReview", 7, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 3 },
                    { new Guid("ecf8f24e-8ead-4a44-b123-b85b6527db31"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "QuotePreparation", 7, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 3 },
                    { new Guid("f0f6f093-7e6e-45f5-ac84-76510ddfe371"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "DataReview", 5, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 2 },
                    { new Guid("f419f936-3f6f-4135-9d9b-7744bb5e43b8"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "Binding", 5, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 2 },
                    { new Guid("f501f5dd-23d4-4250-9eab-65a70d0c08f5"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "Quoted", 21, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 7 },
                    { new Guid("f6969f8b-7ab9-4ab0-a6e9-26f95f57b21c"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "Triaging", 5, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 2 },
                    { new Guid("fdf17afe-4182-46e4-bf8b-3079e74b3579"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "BindRequested", 5, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 2 }
                });

            migrationBuilder.CreateIndex(
                name: "UX_WorkflowSlaThresholds_EntityType_Status",
                table: "WorkflowSlaThresholds",
                columns: new[] { "EntityType", "Status" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowSlaThresholds");

            migrationBuilder.DropColumn(
                name: "LineOfBusiness",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "LineOfBusiness",
                table: "Renewals");
        }
    }
}
