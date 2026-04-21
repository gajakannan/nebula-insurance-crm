using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nebula.Infrastructure.Persistence;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260414121000_F0016_AccountContactsAndRelationshipHistory")]
public partial class F0016_AccountContactsAndRelationshipHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AccountContacts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                IsPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                table.PrimaryKey("PK_AccountContacts", x => x.Id);
                table.ForeignKey(
                    name: "FK_AccountContacts_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AccountRelationshipHistory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                RelationshipType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                PreviousValue = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                NewValue = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                EffectiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccountRelationshipHistory", x => x.Id);
                table.ForeignKey(
                    name: "FK_AccountRelationshipHistory_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AccountContacts_AccountId",
            table: "AccountContacts",
            column: "AccountId");

        migrationBuilder.CreateIndex(
            name: "IX_AccountContacts_AccountId_Primary",
            table: "AccountContacts",
            column: "AccountId",
            unique: true,
            filter: "\"IsPrimary\" = true AND \"IsDeleted\" = false");

        migrationBuilder.CreateIndex(
            name: "IX_AccountRelationshipHistory_AccountId_EffectiveAt",
            table: "AccountRelationshipHistory",
            columns: new[] { "AccountId", "EffectiveAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AccountContacts");

        migrationBuilder.DropTable(
            name: "AccountRelationshipHistory");
    }
}
