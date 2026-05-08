using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nebula.Infrastructure.Persistence;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260507030000_F0034_ProductSchemaRegistryAndLobAttributes")]
public partial class F0034_ProductSchemaRegistryAndLobAttributes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LobProducts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProductKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                LineOfBusiness = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Active"),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_LobProducts", x => x.Id));

        migrationBuilder.CreateTable(
            name: "LobProductVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                LobProductId = table.Column<Guid>(type: "uuid", nullable: false),
                Version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Active"),
                EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false),
                DeprecatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                table.PrimaryKey("PK_LobProductVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_LobProductVersions_LobProducts_LobProductId",
                    column: x => x.LobProductId,
                    principalTable: "LobProducts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "LobSchemaBundles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                LobProductVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                SchemaVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Draft"),
                DataSchemaJson = table.Column<string>(type: "jsonb", nullable: false),
                UiSchemaJson = table.Column<string>(type: "jsonb", nullable: false),
                RulesJson = table.Column<string>(type: "jsonb", nullable: false),
                ProjectionMapJson = table.Column<string>(type: "jsonb", nullable: false),
                ContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ActivatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                RetiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                RetiredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
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
                table.PrimaryKey("PK_LobSchemaBundles", x => x.Id);
                table.ForeignKey(
                    name: "FK_LobSchemaBundles_LobProductVersions_LobProductVersionId",
                    column: x => x.LobProductVersionId,
                    principalTable: "LobProductVersions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "LobBundleActivationEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                LobSchemaBundleId = table.Column<Guid>(type: "uuid", nullable: false),
                FromStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                ToStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                ChangeNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                table.PrimaryKey("PK_LobBundleActivationEvents", x => x.Id);
                table.ForeignKey(
                    name: "FK_LobBundleActivationEvents_LobSchemaBundles_LobSchemaBundleId",
                    column: x => x.LobSchemaBundleId,
                    principalTable: "LobSchemaBundles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddColumn<Guid>(name: "LobProductVersionId", table: "Submissions", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<string>(name: "LobAttributesJson", table: "Submissions", type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb");
        migrationBuilder.AddColumn<Guid>(name: "LobProductVersionId", table: "Renewals", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<string>(name: "LobAttributesJson", table: "Renewals", type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb");
        migrationBuilder.AddColumn<string>(name: "LineOfBusiness", table: "PolicyVersions", type: "character varying(50)", maxLength: 50, nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "LobProductVersionId", table: "PolicyVersions", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<string>(name: "LobAttributesJson", table: "PolicyVersions", type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb");
        migrationBuilder.AddColumn<string>(name: "LineOfBusiness", table: "PolicyEndorsements", type: "character varying(50)", maxLength: 50, nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "LobProductVersionId", table: "PolicyEndorsements", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<string>(name: "LobAttributesJson", table: "PolicyEndorsements", type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb");

        migrationBuilder.Sql(
            """
            INSERT INTO "LobProducts" ("Id", "ProductKey", "LineOfBusiness", "Name", "Status", "Description", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
            VALUES
              ('7b8f0034-0000-5000-9000-000000000000', '_unspecified', NULL, 'Unspecified product attributes', 'Internal', 'F0034 sentinel for null LOB carriers with empty attributes.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0000-5000-9000-000000000001', '_legacy_property', 'Property', 'Legacy Property attributes', 'Internal', 'F0034 pass-through sentinel for legacy Property carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0000-5000-9000-000000000002', '_legacy_general_liability', 'GeneralLiability', 'Legacy General Liability attributes', 'Internal', 'F0034 pass-through sentinel for legacy General Liability carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0000-5000-9000-000000000003', '_legacy_commercial_auto', 'CommercialAuto', 'Legacy Commercial Auto attributes', 'Internal', 'F0034 pass-through sentinel for legacy Commercial Auto carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0000-5000-9000-000000000004', '_legacy_workers_compensation', 'WorkersCompensation', 'Legacy Workers Compensation attributes', 'Internal', 'F0034 pass-through sentinel for legacy Workers Compensation carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0000-5000-9000-000000000005', '_legacy_professional_liability', 'ProfessionalLiability', 'Legacy Professional Liability attributes', 'Internal', 'F0034 pass-through sentinel for legacy Professional Liability carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0000-5000-9000-000000000006', '_legacy_marine', 'Marine', 'Legacy Marine attributes', 'Internal', 'F0034 pass-through sentinel for legacy Marine carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0000-5000-9000-000000000007', '_legacy_umbrella', 'Umbrella', 'Legacy Umbrella attributes', 'Internal', 'F0034 pass-through sentinel for legacy Umbrella carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0000-5000-9000-000000000008', '_legacy_surety', 'Surety', 'Legacy Surety attributes', 'Internal', 'F0034 pass-through sentinel for legacy Surety carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0000-5000-9000-000000000009', '_legacy_cyber', 'Cyber', 'Legacy Cyber attributes', 'Internal', 'F0034 pass-through sentinel for legacy Cyber carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0000-5000-9000-000000000010', '_legacy_directors_officers', 'DirectorsOfficers', 'Legacy Directors Officers attributes', 'Internal', 'F0034 pass-through sentinel for legacy Directors Officers carriers.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('34000000-0000-0000-0000-000000000001', 'cyber', 'Cyber', 'Cyber Liability', 'Active', 'Cyber product schema registry entry for F0034.', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE);

            INSERT INTO "LobProductVersions" ("Id", "LobProductId", "Version", "Status", "EffectiveFrom", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
            VALUES
              ('aa901058-2402-5370-9978-66eb184066be', '7b8f0034-0000-5000-9000-000000000000', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0001-5000-9000-000000000001', '7b8f0034-0000-5000-9000-000000000001', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0001-5000-9000-000000000002', '7b8f0034-0000-5000-9000-000000000002', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0001-5000-9000-000000000003', '7b8f0034-0000-5000-9000-000000000003', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0001-5000-9000-000000000004', '7b8f0034-0000-5000-9000-000000000004', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0001-5000-9000-000000000005', '7b8f0034-0000-5000-9000-000000000005', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0001-5000-9000-000000000006', '7b8f0034-0000-5000-9000-000000000006', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0001-5000-9000-000000000007', '7b8f0034-0000-5000-9000-000000000007', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0001-5000-9000-000000000008', '7b8f0034-0000-5000-9000-000000000008', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('4ffc79e6-4e32-5d39-a82c-891b6034ab9e', '7b8f0034-0000-5000-9000-000000000009', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('7b8f0034-0001-5000-9000-000000000010', '7b8f0034-0000-5000-9000-000000000010', '0.0.0', 'Internal', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE),
              ('48f5f86a-7396-50bf-92dd-a3a36fe63c20', '34000000-0000-0000-0000-000000000001', '1.0.0', 'Active', DATE '2026-05-07', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE);
            """);

        migrationBuilder.Sql(
            """
            INSERT INTO "LobSchemaBundles" ("Id", "LobProductVersionId", "SchemaVersion", "Status", "DataSchemaJson", "UiSchemaJson", "RulesJson", "ProjectionMapJson", "ContentHash", "ActivatedAt", "ActivatedByUserId", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
            SELECT
              ('7b8f0034-0002-5000-9000-' || lpad(row_number() OVER ()::text, 12, '0'))::uuid,
              v."Id",
              '0.0.0',
              'Internal',
              '{"type":"object","additionalProperties":true}'::jsonb,
              '{"sections":[]}'::jsonb,
              '{"rules":[]}'::jsonb,
              '{}'::jsonb,
              'sha256:f0034-sentinel-' || v."Id"::text,
              TIMESTAMPTZ '2026-05-07T00:00:00Z',
              '00000000-0000-0000-0000-000000000000',
              TIMESTAMPTZ '2026-05-07T00:00:00Z',
              '00000000-0000-0000-0000-000000000000',
              TIMESTAMPTZ '2026-05-07T00:00:00Z',
              '00000000-0000-0000-0000-000000000000',
              FALSE
            FROM "LobProductVersions" v
            WHERE v."Version" = '0.0.0';

            INSERT INTO "LobSchemaBundles" ("Id", "LobProductVersionId", "SchemaVersion", "Status", "DataSchemaJson", "UiSchemaJson", "RulesJson", "ProjectionMapJson", "ContentHash", "ActivatedAt", "ActivatedByUserId", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
            VALUES (
                '34000000-0000-0000-0000-000000000201',
                '48f5f86a-7396-50bf-92dd-a3a36fe63c20',
                '1.0.0',
                'Active',
                $json${
                  "type": "object",
                  "required": ["revenueBand", "recordsHeld", "controls", "requestedLimit", "requestedRetention"],
                  "properties": {
                    "revenueBand": { "type": "string", "enum": ["0-10M", "10-50M", "50-250M", "250M+"] },
                    "recordsHeld": { "type": "integer", "minimum": 0 },
                    "controls": {
                      "type": "object",
                      "required": ["mfaEnabled", "edrEnabled", "backupEnabled", "trainingFrequency"],
                      "properties": {
                        "mfaEnabled": { "type": "boolean" },
                        "mfaMaturity": { "type": ["string", "null"], "enum": ["Implemented", "Partial", "Planned", null] },
                        "edrEnabled": { "type": "boolean" },
                        "backupEnabled": { "type": "boolean" },
                        "trainingFrequency": { "type": "string", "enum": ["Annual", "SemiAnnual", "Quarterly"] }
                      },
                      "additionalProperties": false
                    },
                    "requestedLimit": {
                      "type": "object",
                      "required": ["amountMinor", "currency"],
                      "properties": {
                        "amountMinor": { "type": "integer", "minimum": 1 },
                        "currency": { "type": "string", "enum": ["USD"] }
                      },
                      "additionalProperties": false
                    },
                    "requestedRetention": {
                      "type": "object",
                      "required": ["amountMinor", "currency"],
                      "properties": {
                        "amountMinor": { "type": "integer", "minimum": 0 },
                        "currency": { "type": "string", "enum": ["USD"] }
                      },
                      "additionalProperties": false
                    }
                  },
                  "additionalProperties": true
                }$json$::jsonb,
                $json${
                  "sections": [
                    { "id": "exposure", "title": "Exposure", "fields": ["revenueBand", "recordsHeld"] },
                    { "id": "controls", "title": "Controls", "fields": ["controls.mfaEnabled", "controls.mfaMaturity", "controls.edrEnabled", "controls.backupEnabled", "controls.trainingFrequency"] },
                    { "id": "terms", "title": "Requested Terms", "fields": ["requestedLimit", "requestedRetention"] }
                  ]
                }$json$::jsonb,
                $json${
                  "rules": [
                    { "id": "mfa_required_for_high_record_count", "when": "recordsHeld >= 1000000", "path": "controls.mfaEnabled" },
                    { "id": "minimum_retention_not_met", "when": "requestedRetention.amountMinor < requestedLimit.amountMinor / 100" }
                  ]
                }$json$::jsonb,
                $json${
                  "submission": { "attributes": "LobAttributesJson", "productVersionId": "LobProductVersionId" },
                  "policyVersion": { "attributes": "LobAttributesJson", "productVersionId": "LobProductVersionId" },
                  "renewal": { "attributes": "LobAttributesJson", "productVersionId": "LobProductVersionId" }
                }$json$::jsonb,
                'sha256:f0034-cyber-1-0-0-seed',
                TIMESTAMPTZ '2026-05-07T00:00:00Z',
                '00000000-0000-0000-0000-000000000000',
                TIMESTAMPTZ '2026-05-07T00:00:00Z',
                '00000000-0000-0000-0000-000000000000',
                TIMESTAMPTZ '2026-05-07T00:00:00Z',
                '00000000-0000-0000-0000-000000000000',
                FALSE);

            INSERT INTO "LobBundleActivationEvents" ("Id", "LobSchemaBundleId", "FromStatus", "ToStatus", "ChangeNote", "ActorUserId", "OccurredAt", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId", "IsDeleted")
            VALUES ('34000000-0000-0000-0000-000000000301', '34000000-0000-0000-0000-000000000201', 'Draft', 'Active', 'Seed Cyber 1.0.0 bundle for F0034.', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', TIMESTAMPTZ '2026-05-07T00:00:00Z', '00000000-0000-0000-0000-000000000000', FALSE);
            """);

        migrationBuilder.Sql(
            """
            UPDATE "PolicyVersions" pv
            SET "LineOfBusiness" = COALESCE(p."LineOfBusiness", 'GeneralLiability')
            FROM "Policies" p
            WHERE pv."PolicyId" = p."Id";

            UPDATE "PolicyEndorsements" pe
            SET "LineOfBusiness" = COALESCE(p."LineOfBusiness", 'GeneralLiability')
            FROM "Policies" p
            WHERE pe."PolicyId" = p."Id";

            UPDATE "Submissions"
            SET "LobProductVersionId" = CASE
                WHEN "LineOfBusiness" IS NULL THEN 'aa901058-2402-5370-9978-66eb184066be'::uuid
                WHEN "LineOfBusiness" = 'Property' THEN '7b8f0034-0001-5000-9000-000000000001'::uuid
                WHEN "LineOfBusiness" = 'GeneralLiability' THEN '7b8f0034-0001-5000-9000-000000000002'::uuid
                WHEN "LineOfBusiness" = 'CommercialAuto' THEN '7b8f0034-0001-5000-9000-000000000003'::uuid
                WHEN "LineOfBusiness" = 'WorkersCompensation' THEN '7b8f0034-0001-5000-9000-000000000004'::uuid
                WHEN "LineOfBusiness" = 'ProfessionalLiability' THEN '7b8f0034-0001-5000-9000-000000000005'::uuid
                WHEN "LineOfBusiness" = 'Marine' THEN '7b8f0034-0001-5000-9000-000000000006'::uuid
                WHEN "LineOfBusiness" = 'Umbrella' THEN '7b8f0034-0001-5000-9000-000000000007'::uuid
                WHEN "LineOfBusiness" = 'Surety' THEN '7b8f0034-0001-5000-9000-000000000008'::uuid
                WHEN "LineOfBusiness" = 'Cyber' THEN '4ffc79e6-4e32-5d39-a82c-891b6034ab9e'::uuid
                WHEN "LineOfBusiness" = 'DirectorsOfficers' THEN '7b8f0034-0001-5000-9000-000000000010'::uuid
                ELSE 'aa901058-2402-5370-9978-66eb184066be'::uuid
            END;

            UPDATE "Renewals"
            SET "LobProductVersionId" = CASE
                WHEN "LineOfBusiness" IS NULL THEN 'aa901058-2402-5370-9978-66eb184066be'::uuid
                WHEN "LineOfBusiness" = 'Property' THEN '7b8f0034-0001-5000-9000-000000000001'::uuid
                WHEN "LineOfBusiness" = 'GeneralLiability' THEN '7b8f0034-0001-5000-9000-000000000002'::uuid
                WHEN "LineOfBusiness" = 'CommercialAuto' THEN '7b8f0034-0001-5000-9000-000000000003'::uuid
                WHEN "LineOfBusiness" = 'WorkersCompensation' THEN '7b8f0034-0001-5000-9000-000000000004'::uuid
                WHEN "LineOfBusiness" = 'ProfessionalLiability' THEN '7b8f0034-0001-5000-9000-000000000005'::uuid
                WHEN "LineOfBusiness" = 'Marine' THEN '7b8f0034-0001-5000-9000-000000000006'::uuid
                WHEN "LineOfBusiness" = 'Umbrella' THEN '7b8f0034-0001-5000-9000-000000000007'::uuid
                WHEN "LineOfBusiness" = 'Surety' THEN '7b8f0034-0001-5000-9000-000000000008'::uuid
                WHEN "LineOfBusiness" = 'Cyber' THEN '4ffc79e6-4e32-5d39-a82c-891b6034ab9e'::uuid
                WHEN "LineOfBusiness" = 'DirectorsOfficers' THEN '7b8f0034-0001-5000-9000-000000000010'::uuid
                ELSE 'aa901058-2402-5370-9978-66eb184066be'::uuid
            END;

            UPDATE "PolicyVersions"
            SET "LobProductVersionId" = CASE
                WHEN "LineOfBusiness" = 'Property' THEN '7b8f0034-0001-5000-9000-000000000001'::uuid
                WHEN "LineOfBusiness" = 'GeneralLiability' THEN '7b8f0034-0001-5000-9000-000000000002'::uuid
                WHEN "LineOfBusiness" = 'CommercialAuto' THEN '7b8f0034-0001-5000-9000-000000000003'::uuid
                WHEN "LineOfBusiness" = 'WorkersCompensation' THEN '7b8f0034-0001-5000-9000-000000000004'::uuid
                WHEN "LineOfBusiness" = 'ProfessionalLiability' THEN '7b8f0034-0001-5000-9000-000000000005'::uuid
                WHEN "LineOfBusiness" = 'Marine' THEN '7b8f0034-0001-5000-9000-000000000006'::uuid
                WHEN "LineOfBusiness" = 'Umbrella' THEN '7b8f0034-0001-5000-9000-000000000007'::uuid
                WHEN "LineOfBusiness" = 'Surety' THEN '7b8f0034-0001-5000-9000-000000000008'::uuid
                WHEN "LineOfBusiness" = 'Cyber' THEN '4ffc79e6-4e32-5d39-a82c-891b6034ab9e'::uuid
                WHEN "LineOfBusiness" = 'DirectorsOfficers' THEN '7b8f0034-0001-5000-9000-000000000010'::uuid
                ELSE '7b8f0034-0001-5000-9000-000000000002'::uuid
            END;

            UPDATE "PolicyEndorsements"
            SET "LobProductVersionId" = CASE
                WHEN "LineOfBusiness" = 'Property' THEN '7b8f0034-0001-5000-9000-000000000001'::uuid
                WHEN "LineOfBusiness" = 'GeneralLiability' THEN '7b8f0034-0001-5000-9000-000000000002'::uuid
                WHEN "LineOfBusiness" = 'CommercialAuto' THEN '7b8f0034-0001-5000-9000-000000000003'::uuid
                WHEN "LineOfBusiness" = 'WorkersCompensation' THEN '7b8f0034-0001-5000-9000-000000000004'::uuid
                WHEN "LineOfBusiness" = 'ProfessionalLiability' THEN '7b8f0034-0001-5000-9000-000000000005'::uuid
                WHEN "LineOfBusiness" = 'Marine' THEN '7b8f0034-0001-5000-9000-000000000006'::uuid
                WHEN "LineOfBusiness" = 'Umbrella' THEN '7b8f0034-0001-5000-9000-000000000007'::uuid
                WHEN "LineOfBusiness" = 'Surety' THEN '7b8f0034-0001-5000-9000-000000000008'::uuid
                WHEN "LineOfBusiness" = 'Cyber' THEN '4ffc79e6-4e32-5d39-a82c-891b6034ab9e'::uuid
                WHEN "LineOfBusiness" = 'DirectorsOfficers' THEN '7b8f0034-0001-5000-9000-000000000010'::uuid
                ELSE '7b8f0034-0001-5000-9000-000000000002'::uuid
            END;
            """);

        migrationBuilder.AlterColumn<Guid>(name: "LobProductVersionId", table: "Submissions", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        migrationBuilder.AlterColumn<Guid>(name: "LobProductVersionId", table: "Renewals", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "LineOfBusiness", table: "PolicyVersions", type: "character varying(50)", maxLength: 50, nullable: false, oldClrType: typeof(string), oldType: "character varying(50)", oldMaxLength: 50, oldNullable: true);
        migrationBuilder.AlterColumn<Guid>(name: "LobProductVersionId", table: "PolicyVersions", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "LineOfBusiness", table: "PolicyEndorsements", type: "character varying(50)", maxLength: 50, nullable: false, oldClrType: typeof(string), oldType: "character varying(50)", oldMaxLength: 50, oldNullable: true);
        migrationBuilder.AlterColumn<Guid>(name: "LobProductVersionId", table: "PolicyEndorsements", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

        migrationBuilder.CreateIndex(name: "UX_LobProducts_ProductKey", table: "LobProducts", column: "ProductKey", unique: true);
        migrationBuilder.CreateIndex(name: "IX_LobProducts_LineOfBusiness_Status", table: "LobProducts", columns: ["LineOfBusiness", "Status"]);
        migrationBuilder.CreateIndex(name: "UX_LobProductVersions_Product_Version", table: "LobProductVersions", columns: ["LobProductId", "Version"], unique: true);
        migrationBuilder.CreateIndex(name: "UX_LobSchemaBundles_ProductVersion_SchemaVersion", table: "LobSchemaBundles", columns: ["LobProductVersionId", "SchemaVersion"], unique: true);
        migrationBuilder.CreateIndex(name: "IX_LobSchemaBundles_ProductVersion_Status", table: "LobSchemaBundles", columns: ["LobProductVersionId", "Status"]);
        migrationBuilder.CreateIndex(name: "IX_LobBundleActivationEvents_Bundle_OccurredAt", table: "LobBundleActivationEvents", columns: ["LobSchemaBundleId", "OccurredAt"]);
        migrationBuilder.CreateIndex(name: "IX_Submissions_LobProductVersionId", table: "Submissions", column: "LobProductVersionId");
        migrationBuilder.CreateIndex(name: "IX_Renewals_LobProductVersionId", table: "Renewals", column: "LobProductVersionId");
        migrationBuilder.CreateIndex(name: "IX_PolicyVersions_LobProductVersionId", table: "PolicyVersions", column: "LobProductVersionId");
        migrationBuilder.CreateIndex(name: "IX_PolicyEndorsements_LobProductVersionId", table: "PolicyEndorsements", column: "LobProductVersionId");

        migrationBuilder.AddForeignKey(name: "FK_Submissions_LobProductVersions_LobProductVersionId", table: "Submissions", column: "LobProductVersionId", principalTable: "LobProductVersions", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_Renewals_LobProductVersions_LobProductVersionId", table: "Renewals", column: "LobProductVersionId", principalTable: "LobProductVersions", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_PolicyVersions_LobProductVersions_LobProductVersionId", table: "PolicyVersions", column: "LobProductVersionId", principalTable: "LobProductVersions", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_PolicyEndorsements_LobProductVersions_LobProductVersionId", table: "PolicyEndorsements", column: "LobProductVersionId", principalTable: "LobProductVersions", principalColumn: "Id", onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION nebula_lob_carrier_consistency()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            DECLARE
                product_key text;
                product_lob text;
                old_product_key text;
            BEGIN
                SELECT p."ProductKey", p."LineOfBusiness"
                INTO product_key, product_lob
                FROM "LobProductVersions" v
                JOIN "LobProducts" p ON p."Id" = v."LobProductId"
                WHERE v."Id" = NEW."LobProductVersionId";

                IF product_key IS NULL THEN
                    RAISE EXCEPTION 'LOB_SCHEMA_NOT_FOUND' USING ERRCODE = 'P0001';
                END IF;

                IF TG_OP = 'UPDATE'
                   AND OLD."LobProductVersionId" IS DISTINCT FROM NEW."LobProductVersionId"
                   AND COALESCE(current_setting('app.lob_migration_in_progress', true), 'false') <> 'true' THEN
                    SELECT p."ProductKey"
                    INTO old_product_key
                    FROM "LobProductVersions" v
                    JOIN "LobProducts" p ON p."Id" = v."LobProductId"
                    WHERE v."Id" = OLD."LobProductVersionId";

                    IF NOT (
                        (
                            old_product_key = '_unspecified'
                            AND OLD."LineOfBusiness" IS NULL
                            AND NEW."LineOfBusiness" IS NOT NULL
                            AND NEW."LineOfBusiness" IS NOT DISTINCT FROM product_lob
                            AND NEW."LobAttributesJson" <> '{}'::jsonb
                        )
                        OR (
                            old_product_key LIKE '\_legacy\_%' ESCAPE '\'
                            AND OLD."LineOfBusiness" IS NOT NULL
                            AND OLD."LineOfBusiness" IS NOT DISTINCT FROM NEW."LineOfBusiness"
                            AND NEW."LineOfBusiness" IS NOT DISTINCT FROM product_lob
                            AND OLD."LobAttributesJson" = '{}'::jsonb
                            AND NEW."LobAttributesJson" <> '{}'::jsonb
                        )
                        OR (
                            TG_TABLE_NAME IN ('Submissions', 'Renewals')
                            AND (old_product_key = '_unspecified' OR old_product_key LIKE '\_legacy\_%' ESCAPE '\')
                            AND OLD."LobAttributesJson" = '{}'::jsonb
                            AND NEW."LobAttributesJson" = '{}'::jsonb
                            AND (
                                (product_key = '_unspecified' AND NEW."LineOfBusiness" IS NULL)
                                OR product_key LIKE '\_legacy\_%' ESCAPE '\'
                            )
                        )
                    ) THEN
                        RAISE EXCEPTION 'LOB_PRODUCT_VERSION_IMMUTABLE' USING ERRCODE = 'P0001';
                    END IF;
                END IF;

                IF product_key = '_unspecified' THEN
                    IF TG_TABLE_NAME IN ('Submissions', 'Renewals')
                       AND NEW."LineOfBusiness" IS NULL
                       AND NEW."LobAttributesJson" = '{}'::jsonb THEN
                        RETURN NEW;
                    END IF;
                    RAISE EXCEPTION 'LOB_PRODUCT_MISMATCH' USING ERRCODE = 'P0001';
                END IF;

                IF product_key LIKE '\_legacy\_%' ESCAPE '\' THEN
                    IF product_lob IS NOT DISTINCT FROM NEW."LineOfBusiness"
                       AND NEW."LobAttributesJson" = '{}'::jsonb THEN
                        RETURN NEW;
                    END IF;
                    RAISE EXCEPTION 'LOB_PRODUCT_MISMATCH' USING ERRCODE = 'P0001';
                END IF;

                IF product_lob IS DISTINCT FROM NEW."LineOfBusiness" THEN
                    RAISE EXCEPTION 'LOB_PRODUCT_MISMATCH' USING ERRCODE = 'P0001';
                END IF;

                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER trg_lob_carrier_consistency
            BEFORE INSERT OR UPDATE OF "LineOfBusiness", "LobProductVersionId", "LobAttributesJson" ON "Submissions"
            FOR EACH ROW EXECUTE FUNCTION nebula_lob_carrier_consistency();

            CREATE TRIGGER trg_lob_carrier_consistency
            BEFORE INSERT OR UPDATE OF "LineOfBusiness", "LobProductVersionId", "LobAttributesJson" ON "Renewals"
            FOR EACH ROW EXECUTE FUNCTION nebula_lob_carrier_consistency();

            CREATE TRIGGER trg_lob_carrier_consistency
            BEFORE INSERT OR UPDATE OF "LineOfBusiness", "LobProductVersionId", "LobAttributesJson" ON "PolicyVersions"
            FOR EACH ROW EXECUTE FUNCTION nebula_lob_carrier_consistency();

            CREATE TRIGGER trg_lob_carrier_consistency
            BEFORE INSERT OR UPDATE OF "LineOfBusiness", "LobProductVersionId", "LobAttributesJson" ON "PolicyEndorsements"
            FOR EACH ROW EXECUTE FUNCTION nebula_lob_carrier_consistency();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER IF EXISTS trg_lob_carrier_consistency ON "PolicyEndorsements";
            DROP TRIGGER IF EXISTS trg_lob_carrier_consistency ON "PolicyVersions";
            DROP TRIGGER IF EXISTS trg_lob_carrier_consistency ON "Renewals";
            DROP TRIGGER IF EXISTS trg_lob_carrier_consistency ON "Submissions";
            DROP FUNCTION IF EXISTS nebula_lob_carrier_consistency();
            """);

        migrationBuilder.DropForeignKey(name: "FK_Submissions_LobProductVersions_LobProductVersionId", table: "Submissions");
        migrationBuilder.DropForeignKey(name: "FK_Renewals_LobProductVersions_LobProductVersionId", table: "Renewals");
        migrationBuilder.DropForeignKey(name: "FK_PolicyVersions_LobProductVersions_LobProductVersionId", table: "PolicyVersions");
        migrationBuilder.DropForeignKey(name: "FK_PolicyEndorsements_LobProductVersions_LobProductVersionId", table: "PolicyEndorsements");

        migrationBuilder.DropIndex(name: "IX_Submissions_LobProductVersionId", table: "Submissions");
        migrationBuilder.DropIndex(name: "IX_Renewals_LobProductVersionId", table: "Renewals");
        migrationBuilder.DropIndex(name: "IX_PolicyVersions_LobProductVersionId", table: "PolicyVersions");
        migrationBuilder.DropIndex(name: "IX_PolicyEndorsements_LobProductVersionId", table: "PolicyEndorsements");

        migrationBuilder.DropColumn(name: "LobProductVersionId", table: "Submissions");
        migrationBuilder.DropColumn(name: "LobAttributesJson", table: "Submissions");
        migrationBuilder.DropColumn(name: "LobProductVersionId", table: "Renewals");
        migrationBuilder.DropColumn(name: "LobAttributesJson", table: "Renewals");
        migrationBuilder.DropColumn(name: "LineOfBusiness", table: "PolicyVersions");
        migrationBuilder.DropColumn(name: "LobProductVersionId", table: "PolicyVersions");
        migrationBuilder.DropColumn(name: "LobAttributesJson", table: "PolicyVersions");
        migrationBuilder.DropColumn(name: "LineOfBusiness", table: "PolicyEndorsements");
        migrationBuilder.DropColumn(name: "LobProductVersionId", table: "PolicyEndorsements");
        migrationBuilder.DropColumn(name: "LobAttributesJson", table: "PolicyEndorsements");

        migrationBuilder.DropTable(name: "LobBundleActivationEvents");
        migrationBuilder.DropTable(name: "LobSchemaBundles");
        migrationBuilder.DropTable(name: "LobProductVersions");
        migrationBuilder.DropTable(name: "LobProducts");
    }
}
