using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nebula.Infrastructure.Persistence;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260706140000_F0032_AdminConfiguration")]
    public partial class F0032_AdminConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfigurationDomains",
                columns: table => new
                {
                    DomainKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    OwningModule = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EditableSchemaRef = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    SupportsRollback = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ConfigurationDomains", x => x.DomainKey));

            migrationBuilder.CreateTable(
                name: "ConfigurationDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DomainKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    BasePublishedVersion = table.Column<int>(type: "integer", nullable: false),
                    DraftVersion = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
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
                    table.PrimaryKey("PK_ConfigurationDrafts", x => x.Id);
                    table.ForeignKey("FK_ConfigurationDrafts_ConfigurationDomains_DomainKey", x => x.DomainKey, "ConfigurationDomains", "DomainKey", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PublishedOperationalConfigurationSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DomainKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PublishedVersion = table.Column<int>(type: "integer", nullable: false),
                    PayloadSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PublishedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublishReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_PublishedOperationalConfigurationSets", x => x.Id);
                    table.ForeignKey("FK_PublishedOperationalConfigurationSets_ConfigurationDomains_DomainKey", x => x.DomainKey, "ConfigurationDomains", "DomainKey", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DomainKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedSetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SummaryJson = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_ConfigurationAuditEvents", x => x.Id);
                    table.ForeignKey("FK_ConfigurationAuditEvents_ConfigurationDomains_DomainKey", x => x.DomainKey, "ConfigurationDomains", "DomainKey", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationValidationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DraftPayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BlockingErrorsJson = table.Column<string>(type: "jsonb", nullable: false),
                    WarningsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CompareSummaryJson = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_ConfigurationValidationResults", x => x.Id);
                    table.ForeignKey("FK_ConfigurationValidationResults_ConfigurationDrafts_DraftId", x => x.DraftId, "ConfigurationDrafts", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationRefreshStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublishedSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RefreshedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_ConfigurationRefreshStatuses", x => x.Id);
                    table.ForeignKey("FK_ConfigurationRefreshStatuses_PublishedOperationalConfigurationSets_PublishedSetId", x => x.PublishedSetId, "PublishedOperationalConfigurationSets", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "ConfigurationDomains"
                    ("DomainKey", "Id", "DisplayName", "OwningModule", "Status", "EditableSchemaRef", "SupportsRollback", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
                VALUES
                    ('queue-routing', '7c4b1546-f20f-4645-91c1-a2c9d85e4c99', 'Queue and Routing', 'F0022', 'Supported', 'planning-mds/schemas/admin-configuration-domain.schema.json', TRUE, TIMESTAMPTZ '2026-07-06T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-07-06T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
                    ('workflow-sla-thresholds', 'dfd4904c-cfae-440f-9af2-8329888ca987', 'Workflow SLA Thresholds', 'F0032', 'Supported', 'planning-mds/schemas/admin-configuration-draft.schema.json', TRUE, TIMESTAMPTZ '2026-07-06T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-07-06T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
                    ('search-report-defaults', '5fd8c5b6-e56d-402c-95a2-a101712c1638', 'Search and Report Defaults', 'F0023', 'Supported', 'planning-mds/schemas/admin-configuration-draft.schema.json', TRUE, TIMESTAMPTZ '2026-07-06T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-07-06T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
                    ('template-metadata', '7db99f11-312b-491e-b9ad-8c0e31a9c737', 'Template Metadata', 'F0027', 'Supported', 'planning-mds/schemas/admin-configuration-draft.schema.json', TRUE, TIMESTAMPTZ '2026-07-06T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-07-06T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE)
                ON CONFLICT ("DomainKey") DO NOTHING;
                """);

            migrationBuilder.CreateIndex("IX_ConfigurationDomains_OwningModule", "ConfigurationDomains", "OwningModule");
            migrationBuilder.CreateIndex("IX_ConfigurationDrafts_Domain_Status", "ConfigurationDrafts", new[] { "DomainKey", "Status" });
            migrationBuilder.CreateIndex("IX_ConfigurationValidationResults_Draft_CreatedAt", "ConfigurationValidationResults", new[] { "DraftId", "CreatedAt" });
            migrationBuilder.CreateIndex("UX_PublishedOperationalConfigurationSets_Domain_Version", "PublishedOperationalConfigurationSets", new[] { "DomainKey", "PublishedVersion" }, unique: true);
            migrationBuilder.CreateIndex("IX_ConfigurationRefreshStatuses_PublishedSet_Consumer", "ConfigurationRefreshStatuses", new[] { "PublishedSetId", "ConsumerKey" });
            migrationBuilder.CreateIndex("IX_ConfigurationAuditEvents_Domain_CreatedAt", "ConfigurationAuditEvents", new[] { "DomainKey", "CreatedAt" });
            migrationBuilder.CreateIndex("IX_ConfigurationAuditEvents_Action_Outcome", "ConfigurationAuditEvents", new[] { "Action", "Outcome" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("ConfigurationRefreshStatuses");
            migrationBuilder.DropTable("ConfigurationValidationResults");
            migrationBuilder.DropTable("ConfigurationAuditEvents");
            migrationBuilder.DropTable("PublishedOperationalConfigurationSets");
            migrationBuilder.DropTable("ConfigurationDrafts");
            migrationBuilder.DropTable("ConfigurationDomains");
        }
    }
}
