using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nebula.Infrastructure.Persistence;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260703133000_F0022_WorkQueuesRouting")]
public partial class F0022_WorkQueuesRouting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WorkQueues",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                WorkType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                IsFallback = table.Column<bool>(type: "boolean", nullable: false),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_WorkQueues", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AssignmentRules",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkQueueId = table.Column<Guid>(type: "uuid", nullable: false),
                RuleType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Precedence = table.Column<int>(type: "integer", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ConditionsJson = table.Column<string>(type: "jsonb", nullable: false),
                OutcomeJson = table.Column<string>(type: "jsonb", nullable: false),
                ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ActivatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
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
                table.PrimaryKey("PK_AssignmentRules", x => x.Id);
                table.ForeignKey(
                    name: "FK_AssignmentRules_WorkQueues_WorkQueueId",
                    column: x => x.WorkQueueId,
                    principalTable: "WorkQueues",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CoverageWindows",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CoveredUserId = table.Column<Guid>(type: "uuid", nullable: false),
                BackupUserId = table.Column<Guid>(type: "uuid", nullable: false),
                WorkQueueId = table.Column<Guid>(type: "uuid", nullable: true),
                StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                table.PrimaryKey("PK_CoverageWindows", x => x.Id);
                table.ForeignKey(
                    name: "FK_CoverageWindows_UserProfiles_BackupUserId",
                    column: x => x.BackupUserId,
                    principalTable: "UserProfiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CoverageWindows_UserProfiles_CoveredUserId",
                    column: x => x.CoveredUserId,
                    principalTable: "UserProfiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CoverageWindows_WorkQueues_WorkQueueId",
                    column: x => x.WorkQueueId,
                    principalTable: "WorkQueues",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "QueueWorkItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkQueueId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                QueueStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                RoutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RuleVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                MatchReason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                IdempotencyKey = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
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
                table.PrimaryKey("PK_QueueWorkItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_QueueWorkItems_UserProfiles_AssignedToUserId",
                    column: x => x.AssignedToUserId,
                    principalTable: "UserProfiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_QueueWorkItems_WorkQueues_WorkQueueId",
                    column: x => x.WorkQueueId,
                    principalTable: "WorkQueues",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "WorkQueueMembers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                WorkQueueId = table.Column<Guid>(type: "uuid", nullable: false),
                UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                table.PrimaryKey("PK_WorkQueueMembers", x => x.Id);
                table.ForeignKey(
                    name: "FK_WorkQueueMembers_UserProfiles_UserProfileId",
                    column: x => x.UserProfileId,
                    principalTable: "UserProfiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_WorkQueueMembers_WorkQueues_WorkQueueId",
                    column: x => x.WorkQueueId,
                    principalTable: "WorkQueues",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RoutingDecisionLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                QueueWorkItemId = table.Column<Guid>(type: "uuid", nullable: true),
                SourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                Outcome = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ReasonCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DecisionPayloadJson = table.Column<string>(type: "jsonb", nullable: false),
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
                table.PrimaryKey("PK_RoutingDecisionLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_RoutingDecisionLogs_QueueWorkItems_QueueWorkItemId",
                    column: x => x.QueueWorkItemId,
                    principalTable: "QueueWorkItems",
                    principalColumn: "Id");
            });

        migrationBuilder.Sql("""
            INSERT INTO "WorkQueues" ("Id", "Name", "WorkType", "Status", "IsFallback", "Description", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
            VALUES
              ('22000000-0000-0000-0000-000000000001', 'Task fallback queue', 'Task', 'Active', TRUE, 'Manager-visible fallback queue for unrouted tasks.', TIMESTAMPTZ '2026-07-03T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-07-03T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('22000000-0000-0000-0000-000000000002', 'Submission fallback queue', 'Submission', 'Active', TRUE, 'Manager-visible fallback queue for unrouted submissions.', TIMESTAMPTZ '2026-07-03T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-07-03T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('22000000-0000-0000-0000-000000000003', 'Renewal fallback queue', 'Renewal', 'Active', TRUE, 'Manager-visible fallback queue for unrouted renewals.', TIMESTAMPTZ '2026-07-03T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-07-03T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE);
            """);

        migrationBuilder.CreateIndex("IX_AssignmentRules_RuleType_Status_Precedence", "AssignmentRules", new[] { "RuleType", "Status", "Precedence" });
        migrationBuilder.CreateIndex("IX_AssignmentRules_WorkQueueId_Status_Precedence", "AssignmentRules", new[] { "WorkQueueId", "Status", "Precedence" });
        migrationBuilder.CreateIndex("IX_CoverageWindows_BackupUserId", "CoverageWindows", "BackupUserId");
        migrationBuilder.CreateIndex("IX_CoverageWindows_CoveredUserId_StartsAt_EndsAt_Status", "CoverageWindows", new[] { "CoveredUserId", "StartsAt", "EndsAt", "Status" });
        migrationBuilder.CreateIndex("IX_CoverageWindows_WorkQueueId_StartsAt_EndsAt", "CoverageWindows", new[] { "WorkQueueId", "StartsAt", "EndsAt" });
        migrationBuilder.CreateIndex("IX_QueueWorkItems_AssignedToUserId", "QueueWorkItems", "AssignedToUserId");
        migrationBuilder.CreateIndex("IX_QueueWorkItems_SourceType_SourceId_IdempotencyKey", "QueueWorkItems", new[] { "SourceType", "SourceId", "IdempotencyKey" }, unique: true);
        migrationBuilder.CreateIndex("IX_QueueWorkItems_WorkQueueId_QueueStatus_RoutedAt", "QueueWorkItems", new[] { "WorkQueueId", "QueueStatus", "RoutedAt" });
        migrationBuilder.CreateIndex("IX_RoutingDecisionLogs_QueueWorkItemId_OccurredAt", "RoutingDecisionLogs", new[] { "QueueWorkItemId", "OccurredAt" });
        migrationBuilder.CreateIndex("IX_RoutingDecisionLogs_SourceType_SourceId_OccurredAt", "RoutingDecisionLogs", new[] { "SourceType", "SourceId", "OccurredAt" });
        migrationBuilder.CreateIndex("IX_WorkQueueMembers_UserProfileId_EffectiveTo", "WorkQueueMembers", new[] { "UserProfileId", "EffectiveTo" });
        migrationBuilder.CreateIndex("IX_WorkQueueMembers_WorkQueueId_UserProfileId_EffectiveFrom", "WorkQueueMembers", new[] { "WorkQueueId", "UserProfileId", "EffectiveFrom" });
        migrationBuilder.CreateIndex("IX_WorkQueues_Name_WorkType", "WorkQueues", new[] { "Name", "WorkType" }, unique: true, filter: "\"IsDeleted\" = false");
        migrationBuilder.CreateIndex("IX_WorkQueues_WorkType_IsFallback", "WorkQueues", new[] { "WorkType", "IsFallback" }, filter: "\"IsFallback\" = true AND \"Status\" = 'Active' AND \"IsDeleted\" = false");
        migrationBuilder.CreateIndex("IX_WorkQueues_WorkType_Status", "WorkQueues", new[] { "WorkType", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AssignmentRules");
        migrationBuilder.DropTable(name: "CoverageWindows");
        migrationBuilder.DropTable(name: "RoutingDecisionLogs");
        migrationBuilder.DropTable(name: "WorkQueueMembers");
        migrationBuilder.DropTable(name: "QueueWorkItems");
        migrationBuilder.DropTable(name: "WorkQueues");
    }
}
