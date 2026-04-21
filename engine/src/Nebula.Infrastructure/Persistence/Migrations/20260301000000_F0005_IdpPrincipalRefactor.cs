using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// F0005 — IdP Migration: Keycloak → authentik.
    /// Renames all string subject/assigned-to/actor fields to uuid counterparts
    /// and restructures UserProfile to use (IdpIssuer, IdpSubject) instead of Subject.
    ///
    /// NOTE: This migration drops string columns and adds uuid columns.
    /// Existing dev data will have Guid.Empty as placeholder values.
    /// Run `docker-compose down -v &amp;&amp; docker-compose up` to reseed from scratch.
    ///
    /// Designer.cs (model snapshot) must be regenerated via:
    ///   dotnet ef migrations add --project Nebula.Infrastructure --startup-project Nebula.Api
    /// Replace that migration with the content here, then delete the generated files.
    /// </summary>
    public partial class F0005_IdpPrincipalRefactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Drop indexes that reference columns being renamed ─────────────────

            migrationBuilder.DropIndex(name: "IX_UserProfiles_Subject", table: "UserProfiles");
            migrationBuilder.DropIndex(name: "IX_Submissions_AssignedTo_CurrentStatus", table: "Submissions");
            migrationBuilder.DropIndex(name: "IX_Renewals_AssignedTo_CurrentStatus", table: "Renewals");
            migrationBuilder.DropIndex(name: "IX_Tasks_AssignedTo_Status_DueDate", table: "Tasks");
            migrationBuilder.DropIndex(name: "IX_Brokers_ManagedBySubject", table: "Brokers");
            migrationBuilder.DropIndex(name: "IX_Programs_ManagedBySubject", table: "Programs");

            // ── UserProfiles: Subject → (IdpIssuer, IdpSubject) ──────────────────

            migrationBuilder.AddColumn<string>(
                name: "IdpIssuer",
                table: "UserProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdpSubject",
                table: "UserProfiles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropColumn(name: "Subject", table: "UserProfiles");

            // ── ActivityTimelineEvents: ActorSubject → ActorUserId ────────────────

            migrationBuilder.AddColumn<Guid>(
                name: "ActorUserId",
                table: "ActivityTimelineEvents",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.DropColumn(name: "ActorSubject", table: "ActivityTimelineEvents");

            // ── WorkflowTransitions: ActorSubject → ActorUserId ──────────────────

            migrationBuilder.AddColumn<Guid>(
                name: "ActorUserId",
                table: "WorkflowTransitions",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.DropColumn(name: "ActorSubject", table: "WorkflowTransitions");

            // ── Submissions ───────────────────────────────────────────────────────

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "Submissions",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Submissions",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Submissions",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Submissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.DropColumn(name: "AssignedTo", table: "Submissions");
            migrationBuilder.DropColumn(name: "CreatedBy", table: "Submissions");
            migrationBuilder.DropColumn(name: "UpdatedBy", table: "Submissions");
            migrationBuilder.DropColumn(name: "DeletedBy", table: "Submissions");

            // ── Renewals ──────────────────────────────────────────────────────────

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "Renewals",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Renewals",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Renewals",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Renewals",
                type: "uuid",
                nullable: true);

            migrationBuilder.DropColumn(name: "AssignedTo", table: "Renewals");
            migrationBuilder.DropColumn(name: "CreatedBy", table: "Renewals");
            migrationBuilder.DropColumn(name: "UpdatedBy", table: "Renewals");
            migrationBuilder.DropColumn(name: "DeletedBy", table: "Renewals");

            // ── Tasks ─────────────────────────────────────────────────────────────

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "Tasks",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Tasks",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Tasks",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.DropColumn(name: "AssignedTo", table: "Tasks");
            migrationBuilder.DropColumn(name: "CreatedBy", table: "Tasks");
            migrationBuilder.DropColumn(name: "UpdatedBy", table: "Tasks");
            migrationBuilder.DropColumn(name: "DeletedBy", table: "Tasks");

            // ── Brokers ───────────────────────────────────────────────────────────

            migrationBuilder.AddColumn<Guid>(
                name: "ManagedByUserId",
                table: "Brokers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Brokers",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Brokers",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Brokers",
                type: "uuid",
                nullable: true);

            migrationBuilder.DropColumn(name: "ManagedBySubject", table: "Brokers");
            migrationBuilder.DropColumn(name: "CreatedBy", table: "Brokers");
            migrationBuilder.DropColumn(name: "UpdatedBy", table: "Brokers");
            migrationBuilder.DropColumn(name: "DeletedBy", table: "Brokers");

            // ── Programs ──────────────────────────────────────────────────────────

            migrationBuilder.AddColumn<Guid>(
                name: "ManagedByUserId",
                table: "Programs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Programs",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Programs",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Programs",
                type: "uuid",
                nullable: true);

            migrationBuilder.DropColumn(name: "ManagedBySubject", table: "Programs");
            migrationBuilder.DropColumn(name: "CreatedBy", table: "Programs");
            migrationBuilder.DropColumn(name: "UpdatedBy", table: "Programs");
            migrationBuilder.DropColumn(name: "DeletedBy", table: "Programs");

            // ── Accounts ──────────────────────────────────────────────────────────

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Accounts",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Accounts",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.DropColumn(name: "CreatedBy", table: "Accounts");
            migrationBuilder.DropColumn(name: "UpdatedBy", table: "Accounts");
            migrationBuilder.DropColumn(name: "DeletedBy", table: "Accounts");

            // ── MGAs ──────────────────────────────────────────────────────────────

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "MGAs",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "MGAs",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "MGAs",
                type: "uuid",
                nullable: true);

            migrationBuilder.DropColumn(name: "CreatedBy", table: "MGAs");
            migrationBuilder.DropColumn(name: "UpdatedBy", table: "MGAs");
            migrationBuilder.DropColumn(name: "DeletedBy", table: "MGAs");

            // ── Contacts ──────────────────────────────────────────────────────────

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Contacts",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Contacts",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Contacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.DropColumn(name: "CreatedBy", table: "Contacts");
            migrationBuilder.DropColumn(name: "UpdatedBy", table: "Contacts");
            migrationBuilder.DropColumn(name: "DeletedBy", table: "Contacts");

            // ── Recreate indexes on new uuid columns ──────────────────────────────

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_IdpIssuer_IdpSubject",
                table: "UserProfiles",
                columns: new[] { "IdpIssuer", "IdpSubject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AssignedToUserId_CurrentStatus",
                table: "Submissions",
                columns: new[] { "AssignedToUserId", "CurrentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Renewals_AssignedToUserId_CurrentStatus",
                table: "Renewals",
                columns: new[] { "AssignedToUserId", "CurrentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AssignedToUserId_Status_DueDate",
                table: "Tasks",
                columns: new[] { "AssignedToUserId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Brokers_ManagedByUserId",
                table: "Brokers",
                column: "ManagedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Programs_ManagedByUserId",
                table: "Programs",
                column: "ManagedByUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop new indexes
            migrationBuilder.DropIndex(name: "IX_UserProfiles_IdpIssuer_IdpSubject", table: "UserProfiles");
            migrationBuilder.DropIndex(name: "IX_Submissions_AssignedToUserId_CurrentStatus", table: "Submissions");
            migrationBuilder.DropIndex(name: "IX_Renewals_AssignedToUserId_CurrentStatus", table: "Renewals");
            migrationBuilder.DropIndex(name: "IX_Tasks_AssignedToUserId_Status_DueDate", table: "Tasks");
            migrationBuilder.DropIndex(name: "IX_Brokers_ManagedByUserId", table: "Brokers");
            migrationBuilder.DropIndex(name: "IX_Programs_ManagedByUserId", table: "Programs");

            // UserProfiles
            migrationBuilder.AddColumn<string>(name: "Subject", table: "UserProfiles", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.DropColumn(name: "IdpIssuer", table: "UserProfiles");
            migrationBuilder.DropColumn(name: "IdpSubject", table: "UserProfiles");

            // ActivityTimelineEvents
            migrationBuilder.AddColumn<string>(name: "ActorSubject", table: "ActivityTimelineEvents", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.DropColumn(name: "ActorUserId", table: "ActivityTimelineEvents");

            // WorkflowTransitions
            migrationBuilder.AddColumn<string>(name: "ActorSubject", table: "WorkflowTransitions", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.DropColumn(name: "ActorUserId", table: "WorkflowTransitions");

            // Submissions
            migrationBuilder.AddColumn<string>(name: "AssignedTo", table: "Submissions", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "CreatedBy", table: "Submissions", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "UpdatedBy", table: "Submissions", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "DeletedBy", table: "Submissions", type: "character varying(255)", maxLength: 255, nullable: true);
            migrationBuilder.DropColumn(name: "AssignedToUserId", table: "Submissions");
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "Submissions");
            migrationBuilder.DropColumn(name: "UpdatedByUserId", table: "Submissions");
            migrationBuilder.DropColumn(name: "DeletedByUserId", table: "Submissions");

            // Renewals
            migrationBuilder.AddColumn<string>(name: "AssignedTo", table: "Renewals", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "CreatedBy", table: "Renewals", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "UpdatedBy", table: "Renewals", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "DeletedBy", table: "Renewals", type: "character varying(255)", maxLength: 255, nullable: true);
            migrationBuilder.DropColumn(name: "AssignedToUserId", table: "Renewals");
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "Renewals");
            migrationBuilder.DropColumn(name: "UpdatedByUserId", table: "Renewals");
            migrationBuilder.DropColumn(name: "DeletedByUserId", table: "Renewals");

            // Tasks
            migrationBuilder.AddColumn<string>(name: "AssignedTo", table: "Tasks", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "CreatedBy", table: "Tasks", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "UpdatedBy", table: "Tasks", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "DeletedBy", table: "Tasks", type: "character varying(255)", maxLength: 255, nullable: true);
            migrationBuilder.DropColumn(name: "AssignedToUserId", table: "Tasks");
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "Tasks");
            migrationBuilder.DropColumn(name: "UpdatedByUserId", table: "Tasks");
            migrationBuilder.DropColumn(name: "DeletedByUserId", table: "Tasks");

            // Brokers
            migrationBuilder.AddColumn<string>(name: "ManagedBySubject", table: "Brokers", type: "character varying(255)", maxLength: 255, nullable: true);
            migrationBuilder.AddColumn<string>(name: "CreatedBy", table: "Brokers", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "UpdatedBy", table: "Brokers", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "DeletedBy", table: "Brokers", type: "character varying(255)", maxLength: 255, nullable: true);
            migrationBuilder.DropColumn(name: "ManagedByUserId", table: "Brokers");
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "Brokers");
            migrationBuilder.DropColumn(name: "UpdatedByUserId", table: "Brokers");
            migrationBuilder.DropColumn(name: "DeletedByUserId", table: "Brokers");

            // Programs
            migrationBuilder.AddColumn<string>(name: "ManagedBySubject", table: "Programs", type: "character varying(255)", maxLength: 255, nullable: true);
            migrationBuilder.AddColumn<string>(name: "CreatedBy", table: "Programs", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "UpdatedBy", table: "Programs", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "DeletedBy", table: "Programs", type: "character varying(255)", maxLength: 255, nullable: true);
            migrationBuilder.DropColumn(name: "ManagedByUserId", table: "Programs");
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "Programs");
            migrationBuilder.DropColumn(name: "UpdatedByUserId", table: "Programs");
            migrationBuilder.DropColumn(name: "DeletedByUserId", table: "Programs");

            // Accounts
            migrationBuilder.AddColumn<string>(name: "CreatedBy", table: "Accounts", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "UpdatedBy", table: "Accounts", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "DeletedBy", table: "Accounts", type: "character varying(255)", maxLength: 255, nullable: true);
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "Accounts");
            migrationBuilder.DropColumn(name: "UpdatedByUserId", table: "Accounts");
            migrationBuilder.DropColumn(name: "DeletedByUserId", table: "Accounts");

            // MGAs
            migrationBuilder.AddColumn<string>(name: "CreatedBy", table: "MGAs", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "UpdatedBy", table: "MGAs", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "DeletedBy", table: "MGAs", type: "character varying(255)", maxLength: 255, nullable: true);
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "MGAs");
            migrationBuilder.DropColumn(name: "UpdatedByUserId", table: "MGAs");
            migrationBuilder.DropColumn(name: "DeletedByUserId", table: "MGAs");

            // Contacts
            migrationBuilder.AddColumn<string>(name: "CreatedBy", table: "Contacts", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "UpdatedBy", table: "Contacts", type: "character varying(255)", maxLength: 255, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "DeletedBy", table: "Contacts", type: "character varying(255)", maxLength: 255, nullable: true);
            migrationBuilder.DropColumn(name: "CreatedByUserId", table: "Contacts");
            migrationBuilder.DropColumn(name: "UpdatedByUserId", table: "Contacts");
            migrationBuilder.DropColumn(name: "DeletedByUserId", table: "Contacts");

            // Recreate old indexes
            migrationBuilder.CreateIndex(name: "IX_UserProfiles_Subject", table: "UserProfiles", column: "Subject", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Submissions_AssignedTo_CurrentStatus", table: "Submissions", columns: new[] { "AssignedTo", "CurrentStatus" });
            migrationBuilder.CreateIndex(name: "IX_Renewals_AssignedTo_CurrentStatus", table: "Renewals", columns: new[] { "AssignedTo", "CurrentStatus" });
            migrationBuilder.CreateIndex(name: "IX_Tasks_AssignedTo_Status_DueDate", table: "Tasks", columns: new[] { "AssignedTo", "Status", "DueDate" });
            migrationBuilder.CreateIndex(name: "IX_Brokers_ManagedBySubject", table: "Brokers", column: "ManagedBySubject");
            migrationBuilder.CreateIndex(name: "IX_Programs_ManagedBySubject", table: "Programs", column: "ManagedBySubject");
        }
    }
}
