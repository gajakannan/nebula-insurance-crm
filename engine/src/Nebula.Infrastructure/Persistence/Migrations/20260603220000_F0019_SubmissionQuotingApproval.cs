using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nebula.Infrastructure.Persistence;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260603220000_F0019_SubmissionQuotingApproval")]
public partial class F0019_SubmissionQuotingApproval : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "Submissions"
                ADD COLUMN IF NOT EXISTS "ArchivedAt" timestamp with time zone;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "Submissions"
                ADD COLUMN IF NOT EXISTS "ArchivedByUserId" uuid;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE "Submissions"
                ADD COLUMN IF NOT EXISTS "IsArchived" boolean NOT NULL DEFAULT false;
            """);

        migrationBuilder.CreateTable(
            name: "SubmissionQuotePackets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Draft"),
                LinkedDocumentRefsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                RecordedPremiumAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                RecordedLimits = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                RecordedDeductibles = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                EffectiveDate = table.Column<DateTime>(type: "date", nullable: true),
                CarrierMarket = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                ReadinessState = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Draft"),
                ReadyAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ReadyByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SubmissionQuotePackets", x => x.Id);
                table.ForeignKey(
                    name: "FK_SubmissionQuotePackets_Submissions_SubmissionId",
                    column: x => x.SubmissionId,
                    principalTable: "Submissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SubmissionApprovalDecisions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                Decision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                ApproverUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                AuthorityContextJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                BlockingConditionsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SubmissionApprovalDecisions", x => x.Id);
                table.ForeignKey(
                    name: "FK_SubmissionApprovalDecisions_Submissions_SubmissionId",
                    column: x => x.SubmissionId,
                    principalTable: "Submissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SubmissionBindHandoffs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                IdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                PayloadSnapshotJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SubmissionBindHandoffs", x => x.Id);
                table.ForeignKey(
                    name: "FK_SubmissionBindHandoffs_Submissions_SubmissionId",
                    column: x => x.SubmissionId,
                    principalTable: "Submissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS "IX_Submissions_IsArchived"
                ON "Submissions" ("IsArchived");
            """);

        migrationBuilder.CreateIndex(
            name: "IX_SubmissionQuotePackets_SubmissionId",
            table: "SubmissionQuotePackets",
            column: "SubmissionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SubmissionApprovalDecisions_SubmissionId_DecidedAt",
            table: "SubmissionApprovalDecisions",
            columns: new[] { "SubmissionId", "DecidedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_SubmissionBindHandoffs_SubmissionId_IdempotencyKey",
            table: "SubmissionBindHandoffs",
            columns: new[] { "SubmissionId", "IdempotencyKey" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SubmissionApprovalDecisions");
        migrationBuilder.DropTable(name: "SubmissionBindHandoffs");
        migrationBuilder.DropTable(name: "SubmissionQuotePackets");

        migrationBuilder.DropIndex(
            name: "IX_Submissions_IsArchived",
            table: "Submissions");

        migrationBuilder.DropColumn(name: "ArchivedAt", table: "Submissions");
        migrationBuilder.DropColumn(name: "ArchivedByUserId", table: "Submissions");
        migrationBuilder.DropColumn(name: "IsArchived", table: "Submissions");
    }
}
