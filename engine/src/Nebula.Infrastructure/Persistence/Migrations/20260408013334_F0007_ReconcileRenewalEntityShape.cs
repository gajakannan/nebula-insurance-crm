using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F0007_ReconcileRenewalEntityShape : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Renewals_Submissions_SubmissionId",
                table: "Renewals");

            migrationBuilder.DropIndex(
                name: "IX_Renewals_RenewalDate_Status",
                table: "Renewals");

            migrationBuilder.AddColumn<Guid>(
                name: "BoundPolicyId",
                table: "Renewals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LostReasonCode",
                table: "Renewals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LostReasonDetail",
                table: "Renewals",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PolicyExpirationDate",
                table: "Renewals",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PolicyId",
                table: "Renewals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TargetOutreachDate",
                table: "Renewals",
                type: "date",
                nullable: true);

            migrationBuilder.RenameColumn(
                name: "SubmissionId",
                table: "Renewals",
                newName: "RenewalSubmissionId");

            migrationBuilder.RenameIndex(
                name: "IX_Renewals_SubmissionId",
                table: "Renewals",
                newName: "IX_Renewals_RenewalSubmissionId");

            migrationBuilder.Sql("""
                UPDATE "Renewals"
                SET
                    "PolicyId" = COALESCE("PolicyId", "Id"),
                    "PolicyExpirationDate" = COALESCE("PolicyExpirationDate", "RenewalDate"::date),
                    "TargetOutreachDate" = COALESCE(
                        "TargetOutreachDate",
                        ("RenewalDate"::date - INTERVAL '90 days')::date)
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PolicyExpirationDate",
                table: "Renewals",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PolicyId",
                table: "Renewals",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "TargetOutreachDate",
                table: "Renewals",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "RenewalDate",
                table: "Renewals");

            migrationBuilder.CreateIndex(
                name: "IX_Renewals_PolicyExpirationDate_CurrentStatus",
                table: "Renewals",
                columns: new[] { "PolicyExpirationDate", "CurrentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Renewals_PolicyId_Active",
                table: "Renewals",
                column: "PolicyId",
                unique: true,
                filter: "\"IsDeleted\" = false AND \"CurrentStatus\" NOT IN ('Completed', 'Lost')");

            migrationBuilder.CreateIndex(
                name: "IX_Renewals_TargetOutreachDate",
                table: "Renewals",
                column: "TargetOutreachDate",
                filter: "\"IsDeleted\" = false AND \"CurrentStatus\" = 'Identified'");

            migrationBuilder.AddForeignKey(
                name: "FK_Renewals_Submissions_RenewalSubmissionId",
                table: "Renewals",
                column: "RenewalSubmissionId",
                principalTable: "Submissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Renewals_UserProfiles_AssignedToUserId",
                table: "Renewals",
                column: "AssignedToUserId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Renewals_Submissions_RenewalSubmissionId",
                table: "Renewals");

            migrationBuilder.DropForeignKey(
                name: "FK_Renewals_UserProfiles_AssignedToUserId",
                table: "Renewals");

            migrationBuilder.DropIndex(
                name: "IX_Renewals_PolicyExpirationDate_CurrentStatus",
                table: "Renewals");

            migrationBuilder.DropIndex(
                name: "IX_Renewals_PolicyId_Active",
                table: "Renewals");

            migrationBuilder.DropIndex(
                name: "IX_Renewals_TargetOutreachDate",
                table: "Renewals");

            migrationBuilder.DropColumn(
                name: "BoundPolicyId",
                table: "Renewals");

            migrationBuilder.DropColumn(
                name: "LostReasonCode",
                table: "Renewals");

            migrationBuilder.DropColumn(
                name: "LostReasonDetail",
                table: "Renewals");

            migrationBuilder.DropColumn(
                name: "PolicyExpirationDate",
                table: "Renewals");

            migrationBuilder.DropColumn(
                name: "PolicyId",
                table: "Renewals");

            migrationBuilder.DropColumn(
                name: "TargetOutreachDate",
                table: "Renewals");

            migrationBuilder.AddColumn<DateTime>(
                name: "RenewalDate",
                table: "Renewals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.RenameColumn(
                name: "RenewalSubmissionId",
                table: "Renewals",
                newName: "SubmissionId");

            migrationBuilder.RenameIndex(
                name: "IX_Renewals_RenewalSubmissionId",
                table: "Renewals",
                newName: "IX_Renewals_SubmissionId");

            migrationBuilder.Sql("""
                UPDATE "Renewals"
                SET "RenewalDate" = ("PolicyExpirationDate"::timestamp AT TIME ZONE 'UTC')
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RenewalDate",
                table: "Renewals",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Renewals_RenewalDate_Status",
                table: "Renewals",
                columns: new[] { "RenewalDate", "CurrentStatus" });

            migrationBuilder.AddForeignKey(
                name: "FK_Renewals_Submissions_SubmissionId",
                table: "Renewals",
                column: "SubmissionId",
                principalTable: "Submissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
