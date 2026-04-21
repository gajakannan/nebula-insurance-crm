using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nebula.Infrastructure.Persistence;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260415120000_F0016_MergeHardening")]
public partial class F0016_MergeHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Recreate AccountRelationshipHistory(AccountId, EffectiveAt DESC) so
        //    "latest first" reads use the index direction directly.
        migrationBuilder.DropIndex(
            name: "IX_AccountRelationshipHistory_AccountId_EffectiveAt",
            table: "AccountRelationshipHistory");

        migrationBuilder.Sql(
            """
            CREATE INDEX "IX_AccountRelationshipHistory_AccountId_EffectiveAt"
            ON "AccountRelationshipHistory" ("AccountId", "EffectiveAt" DESC);
            """);

        // 2. Backfill any rows left null by races since the F0016 backfill, then enforce NOT NULL
        //    on the dependent fallback contract columns (ADR-017: link-time guarantee).
        migrationBuilder.Sql(
            """
            UPDATE "Submissions" s
            SET
                "AccountDisplayNameAtLink" = COALESCE(s."AccountDisplayNameAtLink", a."StableDisplayName", a."DisplayName"),
                "AccountStatusAtRead" = COALESCE(s."AccountStatusAtRead", a."Status")
            FROM "Accounts" a
            WHERE a."Id" = s."AccountId"
              AND (s."AccountDisplayNameAtLink" IS NULL OR s."AccountStatusAtRead" IS NULL);

            UPDATE "Renewals" r
            SET
                "AccountDisplayNameAtLink" = COALESCE(r."AccountDisplayNameAtLink", a."StableDisplayName", a."DisplayName"),
                "AccountStatusAtRead" = COALESCE(r."AccountStatusAtRead", a."Status")
            FROM "Accounts" a
            WHERE a."Id" = r."AccountId"
              AND (r."AccountDisplayNameAtLink" IS NULL OR r."AccountStatusAtRead" IS NULL);

            UPDATE "Policies" p
            SET
                "AccountDisplayNameAtLink" = COALESCE(p."AccountDisplayNameAtLink", a."StableDisplayName", a."DisplayName"),
                "AccountStatusAtRead" = COALESCE(p."AccountStatusAtRead", a."Status")
            FROM "Accounts" a
            WHERE a."Id" = p."AccountId"
              AND (p."AccountDisplayNameAtLink" IS NULL OR p."AccountStatusAtRead" IS NULL);
            """);

        migrationBuilder.AlterColumn<string>(
            name: "AccountDisplayNameAtLink",
            table: "Submissions",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: string.Empty,
            oldClrType: typeof(string),
            oldType: "character varying(200)",
            oldMaxLength: 200,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "AccountStatusAtRead",
            table: "Submissions",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: string.Empty,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "AccountDisplayNameAtLink",
            table: "Renewals",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: string.Empty,
            oldClrType: typeof(string),
            oldType: "character varying(200)",
            oldMaxLength: 200,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "AccountStatusAtRead",
            table: "Renewals",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: string.Empty,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "AccountDisplayNameAtLink",
            table: "Policies",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: string.Empty,
            oldClrType: typeof(string),
            oldType: "character varying(200)",
            oldMaxLength: 200,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "AccountStatusAtRead",
            table: "Policies",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: string.Empty,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20,
            oldNullable: true);

        // 3. Normalize stored TaxId values to UPPER(TRIM(...)) and replace the functional
        //    LOWER(TRIM(...)) unique index with a plain expression index so the EF model
        //    matches the database (validation finding #6).
        migrationBuilder.Sql(
            """
            UPDATE "Accounts"
            SET "TaxId" = UPPER(TRIM("TaxId"))
            WHERE "TaxId" IS NOT NULL AND "TaxId" <> UPPER(TRIM("TaxId"));
            """);

        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Accounts_TaxId_Active";""");

        migrationBuilder.CreateIndex(
            name: "IX_Accounts_TaxId_Active",
            table: "Accounts",
            column: "TaxId",
            unique: true,
            filter: "\"Status\" = 'Active' AND \"TaxId\" IS NOT NULL AND \"IsDeleted\" = false");

        // 4. Idempotency-Key store backing the merge endpoint replay semantics
        //    (PRD reliability NFR — no duplicate timeline events on retry).
        migrationBuilder.CreateTable(
            name: "IdempotencyRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Operation = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                ResponseStatusCode = table.Column<int>(type: "integer", nullable: false),
                ResponsePayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IdempotencyRecords_Key_Operation",
            table: "IdempotencyRecords",
            columns: new[] { "IdempotencyKey", "Operation" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "IdempotencyRecords");

        migrationBuilder.DropIndex(
            name: "IX_Accounts_TaxId_Active",
            table: "Accounts");

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX "IX_Accounts_TaxId_Active"
            ON "Accounts" (LOWER(TRIM("TaxId")))
            WHERE "Status" = 'Active' AND "TaxId" IS NOT NULL AND "IsDeleted" = false;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "AccountDisplayNameAtLink",
            table: "Submissions",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(200)",
            oldMaxLength: 200);

        migrationBuilder.AlterColumn<string>(
            name: "AccountStatusAtRead",
            table: "Submissions",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20);

        migrationBuilder.AlterColumn<string>(
            name: "AccountDisplayNameAtLink",
            table: "Renewals",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(200)",
            oldMaxLength: 200);

        migrationBuilder.AlterColumn<string>(
            name: "AccountStatusAtRead",
            table: "Renewals",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20);

        migrationBuilder.AlterColumn<string>(
            name: "AccountDisplayNameAtLink",
            table: "Policies",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(200)",
            oldMaxLength: 200);

        migrationBuilder.AlterColumn<string>(
            name: "AccountStatusAtRead",
            table: "Policies",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20);

        migrationBuilder.DropIndex(
            name: "IX_AccountRelationshipHistory_AccountId_EffectiveAt",
            table: "AccountRelationshipHistory");

        migrationBuilder.CreateIndex(
            name: "IX_AccountRelationshipHistory_AccountId_EffectiveAt",
            table: "AccountRelationshipHistory",
            columns: new[] { "AccountId", "EffectiveAt" });
    }
}
