using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F0006_SubmissionIntakeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReferenceSubmissionStatuses_DisplayOrder",
                table: "ReferenceSubmissionStatuses");

            migrationBuilder.Sql("""
                UPDATE "Submissions"
                SET "CurrentStatus" = CASE "CurrentStatus"
                    WHEN 'WaitingOnDocuments' THEN 'WaitingOnBroker'
                    WHEN 'QuotePreparation' THEN 'InReview'
                    WHEN 'RequoteRequested' THEN 'Quoted'
                    WHEN 'Binding' THEN 'BindRequested'
                    WHEN 'NotQuoted' THEN 'Declined'
                    WHEN 'Lost' THEN 'Withdrawn'
                    WHEN 'Expired' THEN 'Withdrawn'
                    ELSE "CurrentStatus"
                END;
                """);

            migrationBuilder.Sql("""
                UPDATE "WorkflowTransitions"
                SET "FromState" = CASE "FromState"
                    WHEN 'WaitingOnDocuments' THEN 'WaitingOnBroker'
                    WHEN 'QuotePreparation' THEN 'InReview'
                    WHEN 'RequoteRequested' THEN 'Quoted'
                    WHEN 'Binding' THEN 'BindRequested'
                    WHEN 'NotQuoted' THEN 'Declined'
                    WHEN 'Lost' THEN 'Withdrawn'
                    WHEN 'Expired' THEN 'Withdrawn'
                    ELSE "FromState"
                END,
                "ToState" = CASE "ToState"
                    WHEN 'WaitingOnDocuments' THEN 'WaitingOnBroker'
                    WHEN 'QuotePreparation' THEN 'InReview'
                    WHEN 'RequoteRequested' THEN 'Quoted'
                    WHEN 'Binding' THEN 'BindRequested'
                    WHEN 'NotQuoted' THEN 'Declined'
                    WHEN 'Lost' THEN 'Withdrawn'
                    WHEN 'Expired' THEN 'Withdrawn'
                    ELSE "ToState"
                END
                WHERE "WorkflowType" = 'Submission';
                """);

            migrationBuilder.DeleteData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "Binding");

            migrationBuilder.DeleteData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "Expired");

            migrationBuilder.DeleteData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "Lost");

            migrationBuilder.DeleteData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "NotQuoted");

            migrationBuilder.DeleteData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "QuotePreparation");

            migrationBuilder.DeleteData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "RequoteRequested");

            migrationBuilder.DeleteData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "WaitingOnDocuments");

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("0ef57fce-6bd8-42e7-b1ef-767e44a02817"));

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("3047cb13-59f8-4d87-a79d-e80e9dcf28ea"));

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("33cc5f8d-33ea-4f8a-a737-2f64946f044f"));

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("8b43ed42-17f2-426a-a14a-442f6a7d43d4"));

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("95db58fe-ef54-4c7b-b707-0cdf6458cd5b"));

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("ec690f3d-84c8-4709-8b32-ff1efde52e52"));

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("ecf8f24e-8ead-4a44-b123-b85b6527db31"));

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("f419f936-3f6f-4135-9d9b-7744bb5e43b8"));

            migrationBuilder.AlterColumn<string>(
                name: "FromState",
                table: "WorkflowTransitions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<decimal>(
                name: "PremiumEstimate",
                table: "Submissions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Submissions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpirationDate",
                table: "Submissions",
                type: "date",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "BindRequested",
                column: "DisplayOrder",
                value: (short)7);

            migrationBuilder.UpdateData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "Bound",
                column: "DisplayOrder",
                value: (short)8);

            migrationBuilder.UpdateData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "Declined",
                column: "DisplayOrder",
                value: (short)9);

            migrationBuilder.UpdateData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "InReview",
                column: "DisplayOrder",
                value: (short)5);

            migrationBuilder.UpdateData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "Quoted",
                column: "DisplayOrder",
                value: (short)6);

            migrationBuilder.UpdateData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "ReadyForUWReview",
                column: "DisplayOrder",
                value: (short)4);

            migrationBuilder.UpdateData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "Withdrawn",
                column: "DisplayOrder",
                value: (short)10);

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("379f3ad6-68f0-4d2f-b52f-5ab9bb40f157"),
                columns: new[] { "TargetDays", "WarningDays" },
                values: new object[] { 3, 2 });

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("f6969f8b-7ab9-4ab0-a6e9-26f95f57b21c"),
                columns: new[] { "TargetDays", "WarningDays" },
                values: new object[] { 2, 1 });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceSubmissionStatuses_DisplayOrder",
                table: "ReferenceSubmissionStatuses",
                column: "DisplayOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AssignedToUserId",
                table: "Submissions",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_EffectiveDate",
                table: "Submissions",
                column: "EffectiveDate");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_UserProfiles_AssignedToUserId",
                table: "Submissions",
                column: "AssignedToUserId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_UserProfiles_AssignedToUserId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_ReferenceSubmissionStatuses_DisplayOrder",
                table: "ReferenceSubmissionStatuses");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_AssignedToUserId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_EffectiveDate",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "ExpirationDate",
                table: "Submissions");

            migrationBuilder.AlterColumn<string>(
                name: "FromState",
                table: "WorkflowTransitions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PremiumEstimate",
                table: "Submissions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "BindRequested",
                column: "DisplayOrder",
                value: (short)10);

            migrationBuilder.UpdateData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "Bound",
                column: "DisplayOrder",
                value: (short)12);

            migrationBuilder.UpdateData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "Declined",
                column: "DisplayOrder",
                value: (short)13);

            migrationBuilder.UpdateData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "InReview",
                column: "DisplayOrder",
                value: (short)6);

            migrationBuilder.UpdateData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "Quoted",
                column: "DisplayOrder",
                value: (short)8);

            migrationBuilder.UpdateData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "ReadyForUWReview",
                column: "DisplayOrder",
                value: (short)5);

            migrationBuilder.UpdateData(
                table: "ReferenceSubmissionStatuses",
                keyColumn: "Code",
                keyValue: "Withdrawn",
                column: "DisplayOrder",
                value: (short)14);

            migrationBuilder.InsertData(
                table: "ReferenceSubmissionStatuses",
                columns: new[] { "Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal" },
                values: new object[,]
                {
                    { "Binding", "decision", "Binding and issuance processing in progress", "Binding", (short)11, false },
                    { "Expired", "lost", "Submission expired before disposition completed", "Expired", (short)17, true },
                    { "Lost", "lost", "Opportunity lost to another market or strategy change", "Lost", (short)16, true },
                    { "NotQuoted", "lost", "Submission closed without quote issued", "Not Quoted", (short)15, true },
                    { "QuotePreparation", "decision", "Preparing quote terms for broker", "Quote Preparation", (short)7, false },
                    { "RequoteRequested", "decision", "Broker requested revised quote terms", "Requote Requested", (short)9, false },
                    { "WaitingOnDocuments", "waiting", "Awaiting required underwriting documents", "Waiting on Documents", (short)4, false }
                });

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("379f3ad6-68f0-4d2f-b52f-5ab9bb40f157"),
                columns: new[] { "TargetDays", "WarningDays" },
                values: new object[] { 10, 5 });

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("f6969f8b-7ab9-4ab0-a6e9-26f95f57b21c"),
                columns: new[] { "TargetDays", "WarningDays" },
                values: new object[] { 5, 2 });

            migrationBuilder.InsertData(
                table: "WorkflowSlaThresholds",
                columns: new[] { "Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays" },
                values: new object[,]
                {
                    { new Guid("0ef57fce-6bd8-42e7-b1ef-767e44a02817"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "BindRequested", 5, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 2 },
                    { new Guid("3047cb13-59f8-4d87-a79d-e80e9dcf28ea"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "Quoted", 21, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 7 },
                    { new Guid("33cc5f8d-33ea-4f8a-a737-2f64946f044f"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "WaitingOnDocuments", 10, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 5 },
                    { new Guid("8b43ed42-17f2-426a-a14a-442f6a7d43d4"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "InReview", 14, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 5 },
                    { new Guid("95db58fe-ef54-4c7b-b707-0cdf6458cd5b"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "RequoteRequested", 21, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 7 },
                    { new Guid("ec690f3d-84c8-4709-8b32-ff1efde52e52"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "ReadyForUWReview", 7, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 3 },
                    { new Guid("ecf8f24e-8ead-4a44-b123-b85b6527db31"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "QuotePreparation", 7, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 3 },
                    { new Guid("f419f936-3f6f-4135-9d9b-7744bb5e43b8"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "submission", "Binding", 5, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceSubmissionStatuses_DisplayOrder",
                table: "ReferenceSubmissionStatuses",
                column: "DisplayOrder",
                unique: true);
        }
    }
}
