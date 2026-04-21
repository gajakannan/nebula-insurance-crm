using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F0007_ReconcileRenewalWorkflowStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Renewals"
                SET "CurrentStatus" = CASE "CurrentStatus"
                    WHEN 'Created' THEN 'Identified'
                    WHEN 'Early' THEN 'Identified'
                    WHEN 'OutreachStarted' THEN 'Outreach'
                    WHEN 'DataReview' THEN 'InReview'
                    WHEN 'WaitingOnBroker' THEN 'InReview'
                    WHEN 'Negotiation' THEN 'Quoted'
                    WHEN 'BindRequested' THEN 'Quoted'
                    WHEN 'Bound' THEN 'Completed'
                    WHEN 'NotRenewed' THEN 'Lost'
                    WHEN 'Lapsed' THEN 'Lost'
                    WHEN 'Withdrawn' THEN 'Lost'
                    WHEN 'Expired' THEN 'Lost'
                    ELSE "CurrentStatus"
                END;
                """);

            migrationBuilder.Sql("""
                UPDATE "WorkflowTransitions"
                SET "FromState" = CASE "FromState"
                    WHEN 'Created' THEN 'Identified'
                    WHEN 'Early' THEN 'Identified'
                    WHEN 'OutreachStarted' THEN 'Outreach'
                    WHEN 'DataReview' THEN 'InReview'
                    WHEN 'WaitingOnBroker' THEN 'InReview'
                    WHEN 'Negotiation' THEN 'Quoted'
                    WHEN 'BindRequested' THEN 'Quoted'
                    WHEN 'Bound' THEN 'Completed'
                    WHEN 'NotRenewed' THEN 'Lost'
                    WHEN 'Lapsed' THEN 'Lost'
                    WHEN 'Withdrawn' THEN 'Lost'
                    WHEN 'Expired' THEN 'Lost'
                    ELSE "FromState"
                END,
                "ToState" = CASE "ToState"
                    WHEN 'Created' THEN 'Identified'
                    WHEN 'Early' THEN 'Identified'
                    WHEN 'OutreachStarted' THEN 'Outreach'
                    WHEN 'DataReview' THEN 'InReview'
                    WHEN 'WaitingOnBroker' THEN 'InReview'
                    WHEN 'Negotiation' THEN 'Quoted'
                    WHEN 'BindRequested' THEN 'Quoted'
                    WHEN 'Bound' THEN 'Completed'
                    WHEN 'NotRenewed' THEN 'Lost'
                    WHEN 'Lapsed' THEN 'Lost'
                    WHEN 'Withdrawn' THEN 'Lost'
                    WHEN 'Expired' THEN 'Lost'
                    ELSE "ToState"
                END
                WHERE "WorkflowType" = 'Renewal';
                """);

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "BindRequested");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Bound");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Created");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "DataReview");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Early");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Expired");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Lapsed");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Negotiation");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "NotRenewed");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "OutreachStarted");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "WaitingOnBroker");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Withdrawn");

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("0e5f31e6-af58-4e30-8ea0-f2d6f862994e"));

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("2a620479-fc25-4a25-b0c5-1dce00a3693a"));

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("d7fe40cd-c9a5-4fd5-b09c-47f10ff0f20f"));

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("f0f6f093-7e6e-45f5-ac84-76510ddfe371"));

            migrationBuilder.DeleteData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("fdf17afe-4182-46e4-bf8b-3079e74b3579"));

            migrationBuilder.AlterColumn<string>(
                name: "CurrentStatus",
                table: "Renewals",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Identified",
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldDefaultValue: "Created");

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "InReview",
                columns: new[] { "Description", "DisplayOrder" },
                values: new object[] { "Underwriting is reviewing the renewal", (short)3 });

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Lost",
                columns: new[] { "Description", "DisplayOrder" },
                values: new object[] { "Renewal not retained", (short)6 });

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Quoted",
                columns: new[] { "Description", "DisplayOrder" },
                values: new object[] { "Quote has been prepared and shared", (short)4 });

            migrationBuilder.InsertData(
                table: "ReferenceRenewalStatuses",
                columns: new[] { "Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal" },
                values: new object[,]
                {
                    { "Completed", "won", "Renewal successfully bound; linked to a policy or submission", "Completed", (short)5, true },
                    { "Identified", "intake", "Renewal created from expiring policy; not yet worked", "Identified", (short)1, false },
                    { "Outreach", "waiting", "Distribution has initiated broker/account contact", "Outreach", (short)2, false }
                });

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("30efe68f-9e5c-4e7f-9191-e68ee0f8eb26"),
                columns: new[] { "Status", "TargetDays", "WarningDays" },
                values: new object[] { "Identified", 30, 7 });

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("bb695667-05cf-43dd-a89c-c05e4747967c"),
                column: "Status",
                value: "Outreach");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Renewals"
                SET "CurrentStatus" = CASE "CurrentStatus"
                    WHEN 'Identified' THEN 'Created'
                    WHEN 'Outreach' THEN 'OutreachStarted'
                    WHEN 'Completed' THEN 'Bound'
                    ELSE "CurrentStatus"
                END;
                """);

            migrationBuilder.Sql("""
                UPDATE "WorkflowTransitions"
                SET "FromState" = CASE "FromState"
                    WHEN 'Identified' THEN 'Created'
                    WHEN 'Outreach' THEN 'OutreachStarted'
                    WHEN 'Completed' THEN 'Bound'
                    ELSE "FromState"
                END,
                "ToState" = CASE "ToState"
                    WHEN 'Identified' THEN 'Created'
                    WHEN 'Outreach' THEN 'OutreachStarted'
                    WHEN 'Completed' THEN 'Bound'
                    ELSE "ToState"
                END
                WHERE "WorkflowType" = 'Renewal';
                """);

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Completed");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Identified");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Outreach");

            migrationBuilder.AlterColumn<string>(
                name: "CurrentStatus",
                table: "Renewals",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Created",
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldDefaultValue: "Identified");

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "InReview",
                columns: new[] { "Description", "DisplayOrder" },
                values: new object[] { "Under underwriter review for renewal terms", (short)6 });

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Lost",
                columns: new[] { "Description", "DisplayOrder" },
                values: new object[] { "Lost to competitor", (short)12 });

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Quoted",
                columns: new[] { "Description", "DisplayOrder" },
                values: new object[] { "Renewal quote issued", (short)7 });

            migrationBuilder.InsertData(
                table: "ReferenceRenewalStatuses",
                columns: new[] { "Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal" },
                values: new object[,]
                {
                    { "BindRequested", "decision", "Renewal bind requested", "Bind Requested", (short)9, false },
                    { "Bound", "won", "Policy renewed and bound", "Bound", (short)10, true },
                    { "Created", "intake", "Renewal record created from expiring policy", "Created", (short)1, false },
                    { "DataReview", "triage", "Coverage and account data review before outreach", "Data Review", (short)3, false },
                    { "Early", "intake", "In early renewal window (90-120 days out)", "Early", (short)2, false },
                    { "Expired", "lost", "Renewal workflow expired before completion", "Expired", (short)15, true },
                    { "Lapsed", "lost", "Policy expired without renewal", "Lapsed", (short)13, true },
                    { "Negotiation", "decision", "Actively negotiating renewal terms", "Negotiation", (short)8, false },
                    { "NotRenewed", "lost", "Renewal closed without binding", "Not Renewed", (short)11, true },
                    { "OutreachStarted", "waiting", "Active broker/account outreach begun", "Outreach Started", (short)4, false },
                    { "WaitingOnBroker", "waiting", "Awaiting broker response or required renewal information", "Waiting on Broker", (short)5, false },
                    { "Withdrawn", "lost", "Renewal withdrawn by broker or insured", "Withdrawn", (short)14, true }
                });

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("30efe68f-9e5c-4e7f-9191-e68ee0f8eb26"),
                columns: new[] { "Status", "TargetDays", "WarningDays" },
                values: new object[] { "Created", 3, 1 });

            migrationBuilder.UpdateData(
                table: "WorkflowSlaThresholds",
                keyColumn: "Id",
                keyValue: new Guid("bb695667-05cf-43dd-a89c-c05e4747967c"),
                column: "Status",
                value: "OutreachStarted");

            migrationBuilder.InsertData(
                table: "WorkflowSlaThresholds",
                columns: new[] { "Id", "CreatedAt", "EntityType", "Status", "TargetDays", "UpdatedAt", "WarningDays" },
                values: new object[,]
                {
                    { new Guid("0e5f31e6-af58-4e30-8ea0-f2d6f862994e"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "Early", 30, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 7 },
                    { new Guid("2a620479-fc25-4a25-b0c5-1dce00a3693a"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "WaitingOnBroker", 10, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 5 },
                    { new Guid("d7fe40cd-c9a5-4fd5-b09c-47f10ff0f20f"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "Negotiation", 21, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 7 },
                    { new Guid("f0f6f093-7e6e-45f5-ac84-76510ddfe371"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "DataReview", 5, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 2 },
                    { new Guid("fdf17afe-4182-46e4-bf8b-3079e74b3579"), new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), "renewal", "BindRequested", 5, new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Utc), 2 }
                });
        }
    }
}
