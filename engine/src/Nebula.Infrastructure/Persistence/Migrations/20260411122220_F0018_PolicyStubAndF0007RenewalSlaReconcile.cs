using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F0018_PolicyStubAndF0007RenewalSlaReconcile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_WorkflowSlaThresholds_EntityType_Status",
                table: "WorkflowSlaThresholds");

            migrationBuilder.AddColumn<string>(
                name: "LineOfBusiness",
                table: "WorkflowSlaThresholds",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Carrier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LineOfBusiness = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "date", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "date", nullable: false),
                    Premium = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CurrentStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Active"),
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
                    table.PrimaryKey("PK_Policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Policies_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Policies_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("1fef8cb4-2f9b-41e8-9329-3c1a5d22790a"),
                column: "LineOfBusiness",
                value: null);

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("30efe68f-9e5c-4e7f-9191-e68ee0f8eb26"),
                columns: new[] { "LineOfBusiness", "TargetDays", "WarningDays" },
                values: new object[] { null, 90, 60 });

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("379f3ad6-68f0-4d2f-b52f-5ab9bb40f157"),
                column: "LineOfBusiness",
                value: null);

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("77ca3fa9-fddd-47ec-b4d2-84bcbf001687"),
                column: "LineOfBusiness",
                value: null);

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("bb695667-05cf-43dd-a89c-c05e4747967c"),
                column: "LineOfBusiness",
                value: null);

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("f501f5dd-23d4-4250-9eab-65a70d0c08f5"),
                column: "LineOfBusiness",
                value: null);

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("f6969f8b-7ab9-4ab0-a6e9-26f95f57b21c"),
                column: "LineOfBusiness",
                value: null);

            migrationBuilder.InsertData(
                table: "WorkflowSlaThresholds",
                columns: new[] { "Id", "CreatedAt", "EntityType", "LineOfBusiness", "Status", "TargetDays", "UpdatedAt", "WarningDays" },
                values: new object[,]
                {
                    { new Guid("0ebb7f8c-9709-4b54-a6a4-dcff0b2d3de5"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "ProfessionalLiability", "Identified", 90, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 60 },
                    { new Guid("1e92d4d0-b89a-4b5e-9e01-7d4cf14ed564"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "Property", "Identified", 90, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 60 },
                    { new Guid("c47d2142-e4b2-4dc3-90c8-3f0da6a07f8b"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "GeneralLiability", "Identified", 90, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 60 },
                    { new Guid("d5bc3dd5-17ec-4f56-a8c6-f5b503f17f0d"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "Cyber", "Identified", 60, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 45 },
                    { new Guid("d7286c4c-38d5-4e57-9837-2b44cf2a86cf"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "WorkersCompensation", "Identified", 120, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 90 }
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "Policies"
                    ("Id", "PolicyNumber", "AccountId", "BrokerId", "Carrier", "LineOfBusiness", "EffectiveDate", "ExpirationDate", "Premium", "CurrentStatus", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
                SELECT DISTINCT ON (r."PolicyId")
                    r."PolicyId",
                    'LEGACY-' || SUBSTRING(REPLACE(r."PolicyId"::text, '-', '') FROM 1 FOR 12),
                    r."AccountId",
                    r."BrokerId",
                    NULL,
                    r."LineOfBusiness",
                    (r."PolicyExpirationDate" - INTERVAL '1 year')::date,
                    r."PolicyExpirationDate",
                    NULL,
                    'Active',
                    r."CreatedAt",
                    r."CreatedByUserId",
                    r."UpdatedAt",
                    r."UpdatedByUserId",
                    FALSE
                FROM "Renewals" r
                LEFT JOIN "Policies" p ON p."Id" = r."PolicyId"
                WHERE p."Id" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "Policies"
                    ("Id", "PolicyNumber", "AccountId", "BrokerId", "Carrier", "LineOfBusiness", "EffectiveDate", "ExpirationDate", "Premium", "CurrentStatus", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
                SELECT DISTINCT ON (r."BoundPolicyId")
                    r."BoundPolicyId",
                    'BOUND-' || SUBSTRING(REPLACE(r."BoundPolicyId"::text, '-', '') FROM 1 FOR 12),
                    r."AccountId",
                    r."BrokerId",
                    NULL,
                    r."LineOfBusiness",
                    r."PolicyExpirationDate",
                    (r."PolicyExpirationDate" + INTERVAL '1 year')::date,
                    NULL,
                    'Bound',
                    r."CreatedAt",
                    r."CreatedByUserId",
                    r."UpdatedAt",
                    r."UpdatedByUserId",
                    FALSE
                FROM "Renewals" r
                LEFT JOIN "Policies" p ON p."Id" = r."BoundPolicyId"
                WHERE r."BoundPolicyId" IS NOT NULL
                  AND p."Id" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "UX_WorkflowSlaThresholds_EntityType_Status_LineOfBusiness"
                ON "WorkflowSlaThresholds" ("EntityType", "Status", COALESCE("LineOfBusiness", '__default__'));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Renewals_BoundPolicyId",
                table: "Renewals",
                column: "BoundPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_AccountId",
                table: "Policies",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_BrokerId",
                table: "Policies",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_ExpirationDate",
                table: "Policies",
                column: "ExpirationDate");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_PolicyNumber",
                table: "Policies",
                column: "PolicyNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Renewals_Policies_BoundPolicyId",
                table: "Renewals",
                column: "BoundPolicyId",
                principalTable: "Policies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Renewals_Policies_PolicyId",
                table: "Renewals",
                column: "PolicyId",
                principalTable: "Policies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Renewals_Policies_BoundPolicyId",
                table: "Renewals");

            migrationBuilder.DropForeignKey(
                name: "FK_Renewals_Policies_PolicyId",
                table: "Renewals");

            migrationBuilder.DropTable(
                name: "Policies");

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "UX_WorkflowSlaThresholds_EntityType_Status_LineOfBusiness";
                """);

            migrationBuilder.DropIndex(
                name: "IX_Renewals_BoundPolicyId",
                table: "Renewals");

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("0ebb7f8c-9709-4b54-a6a4-dcff0b2d3de5"));

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("1e92d4d0-b89a-4b5e-9e01-7d4cf14ed564"));

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("c47d2142-e4b2-4dc3-90c8-3f0da6a07f8b"));

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("d5bc3dd5-17ec-4f56-a8c6-f5b503f17f0d"));

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("d7286c4c-38d5-4e57-9837-2b44cf2a86cf"));

            migrationBuilder.DropColumn(
                name: "LineOfBusiness",
                table: "WorkflowSlaThresholds");

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("30efe68f-9e5c-4e7f-9191-e68ee0f8eb26"),
                columns: new[] { "TargetDays", "WarningDays" },
                values: new object[] { 30, 7 });

            migrationBuilder.CreateIndex(
                name: "UX_WorkflowSlaThresholds_EntityType_Status",
                table: "WorkflowSlaThresholds",
                columns: new[] { "EntityType", "Status" },
                unique: true);
        }
    }
}
