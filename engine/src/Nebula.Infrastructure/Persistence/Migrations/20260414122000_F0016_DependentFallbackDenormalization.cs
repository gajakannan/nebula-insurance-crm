using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nebula.Infrastructure.Persistence;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260414122000_F0016_DependentFallbackDenormalization")]
public partial class F0016_DependentFallbackDenormalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AccountDisplayNameAtLink",
            table: "Submissions",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AccountStatusAtRead",
            table: "Submissions",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "AccountSurvivorId",
            table: "Submissions",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AccountDisplayNameAtLink",
            table: "Renewals",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AccountStatusAtRead",
            table: "Renewals",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "AccountSurvivorId",
            table: "Renewals",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AccountDisplayNameAtLink",
            table: "Policies",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AccountStatusAtRead",
            table: "Policies",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "AccountSurvivorId",
            table: "Policies",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "Submissions" s
            SET
                "AccountDisplayNameAtLink" = COALESCE(a."StableDisplayName", a."DisplayName"),
                "AccountStatusAtRead" = a."Status",
                "AccountSurvivorId" = a."MergedIntoAccountId"
            FROM "Accounts" a
            WHERE a."Id" = s."AccountId";
            """);

        migrationBuilder.Sql(
            """
            UPDATE "Renewals" r
            SET
                "AccountDisplayNameAtLink" = COALESCE(a."StableDisplayName", a."DisplayName"),
                "AccountStatusAtRead" = a."Status",
                "AccountSurvivorId" = a."MergedIntoAccountId"
            FROM "Accounts" a
            WHERE a."Id" = r."AccountId";
            """);

        migrationBuilder.Sql(
            """
            UPDATE "Policies" p
            SET
                "AccountDisplayNameAtLink" = COALESCE(a."StableDisplayName", a."DisplayName"),
                "AccountStatusAtRead" = a."Status",
                "AccountSurvivorId" = a."MergedIntoAccountId"
            FROM "Accounts" a
            WHERE a."Id" = p."AccountId";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AccountDisplayNameAtLink",
            table: "Submissions");

        migrationBuilder.DropColumn(
            name: "AccountStatusAtRead",
            table: "Submissions");

        migrationBuilder.DropColumn(
            name: "AccountSurvivorId",
            table: "Submissions");

        migrationBuilder.DropColumn(
            name: "AccountDisplayNameAtLink",
            table: "Renewals");

        migrationBuilder.DropColumn(
            name: "AccountStatusAtRead",
            table: "Renewals");

        migrationBuilder.DropColumn(
            name: "AccountSurvivorId",
            table: "Renewals");

        migrationBuilder.DropColumn(
            name: "AccountDisplayNameAtLink",
            table: "Policies");

        migrationBuilder.DropColumn(
            name: "AccountStatusAtRead",
            table: "Policies");

        migrationBuilder.DropColumn(
            name: "AccountSurvivorId",
            table: "Policies");
    }
}
