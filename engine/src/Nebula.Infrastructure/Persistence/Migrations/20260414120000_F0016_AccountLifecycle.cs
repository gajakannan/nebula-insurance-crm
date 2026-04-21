using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nebula.Infrastructure.Persistence;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260414120000_F0016_AccountLifecycle")]
public partial class F0016_AccountLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS pg_trgm;""");

        migrationBuilder.DropIndex(
            name: "IX_Accounts_Region",
            table: "Accounts");

        migrationBuilder.RenameColumn(
            name: "Name",
            table: "Accounts",
            newName: "DisplayName");

        migrationBuilder.RenameColumn(
            name: "PrimaryState",
            table: "Accounts",
            newName: "State");

        migrationBuilder.AlterColumn<string>(
            name: "Industry",
            table: "Accounts",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100);

        migrationBuilder.AlterColumn<string>(
            name: "State",
            table: "Accounts",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(2)",
            oldMaxLength: 2);

        migrationBuilder.AlterColumn<string>(
            name: "Region",
            table: "Accounts",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(50)",
            oldMaxLength: 50);

        migrationBuilder.AddColumn<string>(
            name: "LegalName",
            table: "Accounts",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TaxId",
            table: "Accounts",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PrimaryLineOfBusiness",
            table: "Accounts",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "BrokerOfRecordId",
            table: "Accounts",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "PrimaryProducerUserId",
            table: "Accounts",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TerritoryCode",
            table: "Accounts",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Address1",
            table: "Accounts",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Address2",
            table: "Accounts",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "City",
            table: "Accounts",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PostalCode",
            table: "Accounts",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Country",
            table: "Accounts",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StableDisplayName",
            table: "Accounts",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<Guid>(
            name: "MergedIntoAccountId",
            table: "Accounts",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeleteReasonCode",
            table: "Accounts",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DeleteReasonDetail",
            table: "Accounts",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "RemovedAt",
            table: "Accounts",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "Accounts"
            SET "StableDisplayName" = COALESCE(NULLIF(TRIM("DisplayName"), ''), 'Account-' || SUBSTRING(REPLACE("Id"::text, '-', '') FROM 1 FOR 8))
            WHERE "StableDisplayName" = '';
            """);

        migrationBuilder.CreateIndex(
            name: "IX_Accounts_BrokerOfRecordId",
            table: "Accounts",
            column: "BrokerOfRecordId");

        migrationBuilder.CreateIndex(
            name: "IX_Accounts_MergedIntoAccountId",
            table: "Accounts",
            column: "MergedIntoAccountId");

        migrationBuilder.CreateIndex(
            name: "IX_Accounts_Status_Region",
            table: "Accounts",
            columns: new[] { "Status", "Region" });

        migrationBuilder.CreateIndex(
            name: "IX_Accounts_TerritoryCode",
            table: "Accounts",
            column: "TerritoryCode");

        migrationBuilder.AddForeignKey(
            name: "FK_Accounts_Accounts_MergedIntoAccountId",
            table: "Accounts",
            column: "MergedIntoAccountId",
            principalTable: "Accounts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Accounts_Brokers_BrokerOfRecordId",
            table: "Accounts",
            column: "BrokerOfRecordId",
            principalTable: "Brokers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Accounts_UserProfiles_PrimaryProducerUserId",
            table: "Accounts",
            column: "PrimaryProducerUserId",
            principalTable: "UserProfiles",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX "IX_Accounts_TaxId_Active"
            ON "Accounts" (LOWER(TRIM("TaxId")))
            WHERE "Status" = 'Active' AND "TaxId" IS NOT NULL AND "IsDeleted" = false;
            """);

        migrationBuilder.Sql(
            """
            CREATE INDEX "IX_Accounts_DisplayName_Trgm"
            ON "Accounts" USING gin ("DisplayName" gin_trgm_ops);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Accounts_DisplayName_Trgm";""");
        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Accounts_TaxId_Active";""");

        migrationBuilder.DropForeignKey(
            name: "FK_Accounts_Accounts_MergedIntoAccountId",
            table: "Accounts");

        migrationBuilder.DropForeignKey(
            name: "FK_Accounts_Brokers_BrokerOfRecordId",
            table: "Accounts");

        migrationBuilder.DropForeignKey(
            name: "FK_Accounts_UserProfiles_PrimaryProducerUserId",
            table: "Accounts");

        migrationBuilder.DropIndex(
            name: "IX_Accounts_BrokerOfRecordId",
            table: "Accounts");

        migrationBuilder.DropIndex(
            name: "IX_Accounts_MergedIntoAccountId",
            table: "Accounts");

        migrationBuilder.DropIndex(
            name: "IX_Accounts_Status_Region",
            table: "Accounts");

        migrationBuilder.DropIndex(
            name: "IX_Accounts_TerritoryCode",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "Address1",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "Address2",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "BrokerOfRecordId",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "City",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "Country",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "DeleteReasonCode",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "DeleteReasonDetail",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "LegalName",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "MergedIntoAccountId",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "PostalCode",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "PrimaryLineOfBusiness",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "PrimaryProducerUserId",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "RemovedAt",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "StableDisplayName",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "TaxId",
            table: "Accounts");

        migrationBuilder.DropColumn(
            name: "TerritoryCode",
            table: "Accounts");

        migrationBuilder.Sql(
            """
            UPDATE "Accounts"
            SET
                "Industry" = COALESCE("Industry", 'Unknown'),
                "Region" = COALESCE("Region", 'Unknown'),
                "State" = LEFT(COALESCE(NULLIF("State", ''), 'NA'), 2);
            """);

        migrationBuilder.AlterColumn<string>(
            name: "Industry",
            table: "Accounts",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "Unknown",
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "State",
            table: "Accounts",
            type: "character varying(2)",
            maxLength: 2,
            nullable: false,
            defaultValue: "NA",
            oldClrType: typeof(string),
            oldType: "character varying(50)",
            oldMaxLength: 50,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "Region",
            table: "Accounts",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "Unknown",
            oldClrType: typeof(string),
            oldType: "character varying(50)",
            oldMaxLength: 50,
            oldNullable: true);

        migrationBuilder.RenameColumn(
            name: "DisplayName",
            table: "Accounts",
            newName: "Name");

        migrationBuilder.RenameColumn(
            name: "State",
            table: "Accounts",
            newName: "PrimaryState");

        migrationBuilder.CreateIndex(
            name: "IX_Accounts_Region",
            table: "Accounts",
            column: "Region");
    }
}
