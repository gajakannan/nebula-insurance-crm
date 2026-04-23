using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nebula.Infrastructure.Persistence;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260422021000_F0018_PolicyLifecycleAggregate")]
public partial class F0018_PolicyLifecycleAggregate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CarrierRefs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                NaicCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_CarrierRefs", x => x.Id));

        migrationBuilder.Sql(
            """
            INSERT INTO "CarrierRefs" ("Id", "Name", "NaicCode", "IsActive", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
            VALUES
                ('17000000-0000-0000-0000-000000000001', 'Archway Specialty', '10001', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
                ('17000000-0000-0000-0000-000000000002', 'Blue Atlas Insurance', '10002', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
                ('17000000-0000-0000-0000-000000000003', 'Summit National', '10003', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
                ('17000000-0000-0000-0000-000000000004', 'Frontier Casualty', '10004', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
                ('17000000-0000-0000-0000-000000000005', 'Compass Mutual', '10005', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
                ('17000000-0000-0000-0000-000000000006', 'Harbor Re', '10006', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
                ('17000000-0000-0000-0000-000000000007', 'Northstar Indemnity', '10007', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
                ('17000000-0000-0000-0000-000000000008', 'Sterling Insurance Co.', '10008', TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
                ('17000000-0000-0000-0000-000000000999', 'Legacy Carrier', NULL, TRUE, TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-04-22T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE);
            """);

        migrationBuilder.AddColumn<Guid>(
            name: "CarrierId",
            table: "Policies",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("17000000-0000-0000-0000-000000000999"));

        migrationBuilder.AddColumn<string>(
            name: "PremiumCurrency",
            table: "Policies",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            defaultValue: "USD");

        migrationBuilder.AddColumn<Guid>(
            name: "CurrentVersionId",
            table: "Policies",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "BoundAt",
            table: "Policies",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "IssuedAt",
            table: "Policies",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CancelledAt",
            table: "Policies",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CancellationEffectiveDate",
            table: "Policies",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CancellationReasonCode",
            table: "Policies",
            type: "character varying(60)",
            maxLength: 60,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CancellationReasonDetail",
            table: "Policies",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ReinstatementDeadline",
            table: "Policies",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ExpiredAt",
            table: "Policies",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "PredecessorPolicyId",
            table: "Policies",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ProducerUserId",
            table: "Policies",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ImportSource",
            table: "Policies",
            type: "character varying(40)",
            maxLength: 40,
            nullable: false,
            defaultValue: "manual");

        migrationBuilder.AddColumn<string>(
            name: "ExternalPolicyReference",
            table: "Policies",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "Policies"
            SET
                "LineOfBusiness" = COALESCE("LineOfBusiness", 'GeneralLiability'),
                "Premium" = COALESCE("Premium", 0),
                "PremiumCurrency" = 'USD',
                "CurrentStatus" = CASE
                    WHEN "CurrentStatus" IN ('Active', 'Bound', 'Expiring') THEN 'Issued'
                    WHEN "CurrentStatus" IN ('Pending', 'Issued', 'Cancelled', 'Expired') THEN "CurrentStatus"
                    ELSE 'Issued'
                END,
                "IssuedAt" = CASE WHEN "CurrentStatus" IN ('Active', 'Bound', 'Expiring', 'Issued', 'Expired') THEN "CreatedAt" ELSE NULL END,
                "BoundAt" = CASE WHEN "CurrentStatus" IN ('Active', 'Bound', 'Expiring', 'Issued', 'Expired') THEN "CreatedAt" ELSE NULL END,
                "ExpiredAt" = CASE WHEN "ExpirationDate" < CURRENT_DATE THEN "ExpirationDate"::timestamp AT TIME ZONE 'UTC' ELSE NULL END;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "LineOfBusiness",
            table: "Policies",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(50)",
            oldMaxLength: 50,
            oldNullable: true);

        migrationBuilder.AlterColumn<decimal>(
            name: "Premium",
            table: "Policies",
            type: "decimal(18,2)",
            nullable: false,
            defaultValue: 0m,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "CurrentStatus",
            table: "Policies",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "Pending",
            oldClrType: typeof(string),
            oldType: "character varying(30)",
            oldMaxLength: 30,
            oldDefaultValue: "Active");

        migrationBuilder.CreateTable(
            name: "PolicyVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                VersionNumber = table.Column<int>(type: "integer", nullable: false),
                VersionReason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                EndorsementId = table.Column<Guid>(type: "uuid", nullable: true),
                EffectiveDate = table.Column<DateTime>(type: "date", nullable: false),
                ExpirationDate = table.Column<DateTime>(type: "date", nullable: false),
                TotalPremium = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                PremiumCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                ProfileSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                CoverageSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                PremiumSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
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
                table.PrimaryKey("PK_PolicyVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_PolicyVersions_Policies_PolicyId",
                    column: x => x.PolicyId,
                    principalTable: "Policies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PolicyEndorsements",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                EndorsementNumber = table.Column<int>(type: "integer", nullable: false),
                PolicyVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                EndorsementReasonCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                EndorsementReasonDetail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                EffectiveDate = table.Column<DateTime>(type: "date", nullable: false),
                PremiumDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                PremiumCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
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
                table.PrimaryKey("PK_PolicyEndorsements", x => x.Id);
                table.ForeignKey(
                    name: "FK_PolicyEndorsements_Policies_PolicyId",
                    column: x => x.PolicyId,
                    principalTable: "Policies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PolicyEndorsements_PolicyVersions_PolicyVersionId",
                    column: x => x.PolicyVersionId,
                    principalTable: "PolicyVersions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PolicyCoverageLines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                PolicyVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                VersionNumber = table.Column<int>(type: "integer", nullable: false),
                CoverageCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                CoverageName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Limit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Deductible = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                Premium = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                PremiumCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                ExposureBasis = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                ExposureQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                IsCurrent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                table.PrimaryKey("PK_PolicyCoverageLines", x => x.Id);
                table.ForeignKey(
                    name: "FK_PolicyCoverageLines_Policies_PolicyId",
                    column: x => x.PolicyId,
                    principalTable: "Policies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PolicyCoverageLines_PolicyVersions_PolicyVersionId",
                    column: x => x.PolicyVersionId,
                    principalTable: "PolicyVersions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.Sql(
            """
            CREATE TEMP TABLE f0018_policy_version_ids ON COMMIT DROP AS
            SELECT
                "Id" AS policy_id,
                (substr(md5("Id"::text || ':version:1'), 1, 8) || '-' ||
                 substr(md5("Id"::text || ':version:1'), 9, 4) || '-' ||
                 substr(md5("Id"::text || ':version:1'), 13, 4) || '-' ||
                 substr(md5("Id"::text || ':version:1'), 17, 4) || '-' ||
                 substr(md5("Id"::text || ':version:1'), 21, 12))::uuid AS version_id
            FROM "Policies";

            INSERT INTO "PolicyVersions"
                ("Id", "PolicyId", "VersionNumber", "VersionReason", "EffectiveDate", "ExpirationDate", "TotalPremium",
                 "PremiumCurrency", "ProfileSnapshotJson", "CoverageSnapshotJson", "PremiumSnapshotJson",
                 "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
            SELECT
                v.version_id,
                p."Id",
                1,
                'IssuedInitial',
                p."EffectiveDate",
                p."ExpirationDate",
                p."Premium",
                p."PremiumCurrency",
                jsonb_build_object('accountId', p."AccountId", 'brokerOfRecordId', p."BrokerId", 'carrierId', p."CarrierId", 'producerUserId', p."ProducerUserId"),
                '[]'::jsonb,
                jsonb_build_object('totalPremium', p."Premium", 'premiumCurrency', p."PremiumCurrency"),
                p."CreatedAt",
                p."CreatedByUserId",
                p."UpdatedAt",
                p."UpdatedByUserId",
                FALSE
            FROM "Policies" p
            JOIN f0018_policy_version_ids v ON v.policy_id = p."Id";

            UPDATE "Policies" p
            SET "CurrentVersionId" = v.version_id
            FROM f0018_policy_version_ids v
            WHERE v.policy_id = p."Id";

            INSERT INTO "PolicyCoverageLines"
                ("Id", "PolicyId", "PolicyVersionId", "VersionNumber", "CoverageCode", "CoverageName", "Limit", "Premium",
                 "PremiumCurrency", "IsCurrent", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
            SELECT
                (substr(md5(p."Id"::text || ':coverage:1'), 1, 8) || '-' ||
                 substr(md5(p."Id"::text || ':coverage:1'), 9, 4) || '-' ||
                 substr(md5(p."Id"::text || ':coverage:1'), 13, 4) || '-' ||
                 substr(md5(p."Id"::text || ':coverage:1'), 17, 4) || '-' ||
                 substr(md5(p."Id"::text || ':coverage:1'), 21, 12))::uuid,
                p."Id",
                p."CurrentVersionId",
                1,
                p."LineOfBusiness",
                p."LineOfBusiness",
                p."Premium" * 10,
                p."Premium",
                p."PremiumCurrency",
                TRUE,
                p."CreatedAt",
                p."CreatedByUserId",
                p."UpdatedAt",
                p."UpdatedByUserId",
                FALSE
            FROM "Policies" p
            WHERE p."CurrentVersionId" IS NOT NULL;
            """);

        migrationBuilder.Sql(
            """
            INSERT INTO "WorkflowSlaThresholds" ("Id", "EntityType", "Status", "LineOfBusiness", "WarningDays", "TargetDays", "CreatedAt", "UpdatedAt")
            SELECT new_id, 'policy', 'ReinstatementWindow', lob, 7, target_days, TIMESTAMPTZ '2026-04-22T00:00:00Z', TIMESTAMPTZ '2026-04-22T00:00:00Z'
            FROM (VALUES
                ('18000000-0000-0000-0000-000000000000'::uuid, NULL::text, 30),
                ('18000000-0000-0000-0000-000000000001'::uuid, 'Property', 30),
                ('18000000-0000-0000-0000-000000000002'::uuid, 'GeneralLiability', 30),
                ('18000000-0000-0000-0000-000000000003'::uuid, 'CommercialAuto', 30),
                ('18000000-0000-0000-0000-000000000004'::uuid, 'WorkersCompensation', 45),
                ('18000000-0000-0000-0000-000000000005'::uuid, 'ProfessionalLiability', 30),
                ('18000000-0000-0000-0000-000000000006'::uuid, 'Marine', 30),
                ('18000000-0000-0000-0000-000000000007'::uuid, 'Umbrella', 30),
                ('18000000-0000-0000-0000-000000000008'::uuid, 'Surety', 30),
                ('18000000-0000-0000-0000-000000000009'::uuid, 'Cyber', 15),
                ('18000000-0000-0000-0000-000000000010'::uuid, 'DirectorsOfficers', 30)
            ) AS rows(new_id, lob, target_days)
            WHERE NOT EXISTS (
                SELECT 1 FROM "WorkflowSlaThresholds" existing
                WHERE existing."EntityType" = 'policy'
                  AND existing."Status" = 'ReinstatementWindow'
                  AND COALESCE(existing."LineOfBusiness", '__default__') = COALESCE(rows.lob, '__default__')
            );
            """);

        migrationBuilder.CreateIndex("UX_CarrierRefs_Name", "CarrierRefs", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_Policies_CarrierId", "Policies", "CarrierId");
        migrationBuilder.CreateIndex("IX_Policies_CurrentStatus", "Policies", "CurrentStatus");
        migrationBuilder.CreateIndex("IX_Policies_CurrentVersionId", "Policies", "CurrentVersionId");
        migrationBuilder.CreateIndex("IX_Policies_PredecessorPolicyId", "Policies", "PredecessorPolicyId");
        migrationBuilder.CreateIndex("IX_Policies_ProducerUserId", "Policies", "ProducerUserId");
        migrationBuilder.CreateIndex("UX_PolicyVersions_PolicyId_VersionNumber", "PolicyVersions", new[] { "PolicyId", "VersionNumber" }, unique: true);
        migrationBuilder.CreateIndex("IX_PolicyVersions_EndorsementId", "PolicyVersions", "EndorsementId");
        migrationBuilder.CreateIndex("UX_PolicyEndorsements_PolicyId_EndorsementNumber", "PolicyEndorsements", new[] { "PolicyId", "EndorsementNumber" }, unique: true);
        migrationBuilder.CreateIndex("IX_PolicyEndorsements_PolicyVersionId", "PolicyEndorsements", "PolicyVersionId");
        migrationBuilder.CreateIndex("IX_PolicyCoverageLines_PolicyId_IsCurrent", "PolicyCoverageLines", new[] { "PolicyId", "IsCurrent" });
        migrationBuilder.CreateIndex("IX_PolicyCoverageLines_PolicyVersionId", "PolicyCoverageLines", "PolicyVersionId");

        migrationBuilder.AddForeignKey(
            name: "FK_Policies_CarrierRefs_CarrierId",
            table: "Policies",
            column: "CarrierId",
            principalTable: "CarrierRefs",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Policies_Policies_PredecessorPolicyId",
            table: "Policies",
            column: "PredecessorPolicyId",
            principalTable: "Policies",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Policies_UserProfiles_ProducerUserId",
            table: "Policies",
            column: "ProducerUserId",
            principalTable: "UserProfiles",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_Policies_UserProfiles_ProducerUserId", "Policies");
        migrationBuilder.DropForeignKey("FK_Policies_Policies_PredecessorPolicyId", "Policies");
        migrationBuilder.DropForeignKey("FK_Policies_CarrierRefs_CarrierId", "Policies");

        migrationBuilder.DropTable("PolicyCoverageLines");
        migrationBuilder.DropTable("PolicyEndorsements");
        migrationBuilder.DropTable("PolicyVersions");
        migrationBuilder.DropTable("CarrierRefs");

        migrationBuilder.DropIndex("IX_Policies_CarrierId", "Policies");
        migrationBuilder.DropIndex("IX_Policies_CurrentStatus", "Policies");
        migrationBuilder.DropIndex("IX_Policies_CurrentVersionId", "Policies");
        migrationBuilder.DropIndex("IX_Policies_PredecessorPolicyId", "Policies");
        migrationBuilder.DropIndex("IX_Policies_ProducerUserId", "Policies");

        migrationBuilder.DropColumn("CarrierId", "Policies");
        migrationBuilder.DropColumn("PremiumCurrency", "Policies");
        migrationBuilder.DropColumn("CurrentVersionId", "Policies");
        migrationBuilder.DropColumn("BoundAt", "Policies");
        migrationBuilder.DropColumn("IssuedAt", "Policies");
        migrationBuilder.DropColumn("CancelledAt", "Policies");
        migrationBuilder.DropColumn("CancellationEffectiveDate", "Policies");
        migrationBuilder.DropColumn("CancellationReasonCode", "Policies");
        migrationBuilder.DropColumn("CancellationReasonDetail", "Policies");
        migrationBuilder.DropColumn("ReinstatementDeadline", "Policies");
        migrationBuilder.DropColumn("ExpiredAt", "Policies");
        migrationBuilder.DropColumn("PredecessorPolicyId", "Policies");
        migrationBuilder.DropColumn("ProducerUserId", "Policies");
        migrationBuilder.DropColumn("ImportSource", "Policies");
        migrationBuilder.DropColumn("ExternalPolicyReference", "Policies");

        migrationBuilder.AlterColumn<string>(
            name: "LineOfBusiness",
            table: "Policies",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(50)",
            oldMaxLength: 50);

        migrationBuilder.AlterColumn<decimal>(
            name: "Premium",
            table: "Policies",
            type: "decimal(18,2)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)");

        migrationBuilder.AlterColumn<string>(
            name: "CurrentStatus",
            table: "Policies",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "Active",
            oldClrType: typeof(string),
            oldType: "character varying(30)",
            oldMaxLength: 30,
            oldDefaultValue: "Pending");
    }
}
