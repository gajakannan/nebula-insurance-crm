using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandOpportunityStatusesAndFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Submissions_CurrentStatus",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_ReferenceRenewalStatuses_DisplayOrder",
                table: "ReferenceRenewalStatuses");

            migrationBuilder.DropIndex(
                name: "IX_ReferenceSubmissionStatuses_DisplayOrder",
                table: "ReferenceSubmissionStatuses");

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Bound",
                column: "DisplayOrder",
                value: (short)10);

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "InReview",
                column: "DisplayOrder",
                value: (short)6);

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Lapsed",
                column: "DisplayOrder",
                value: (short)13);

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Lost",
                column: "DisplayOrder",
                value: (short)12);

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "OutreachStarted",
                column: "DisplayOrder",
                value: (short)4);

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Quoted",
                column: "DisplayOrder",
                value: (short)7);

            migrationBuilder.InsertData(
                table: "ReferenceRenewalStatuses",
                columns: new[] { "Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal" },
                values: new object[,]
                {
                    { "BindRequested", "decision", "Renewal bind requested", "Bind Requested", (short)9, false },
                    { "DataReview", "triage", "Coverage and account data review before outreach", "Data Review", (short)3, false },
                    { "Expired", null, "Renewal workflow expired before completion", "Expired", (short)15, true },
                    { "Negotiation", "decision", "Actively negotiating renewal terms", "Negotiation", (short)8, false },
                    { "NotRenewed", null, "Renewal closed without binding", "Not Renewed", (short)11, true },
                    { "WaitingOnBroker", "waiting", "Awaiting broker response or required renewal information", "Waiting on Broker", (short)5, false },
                    { "Withdrawn", null, "Renewal withdrawn by broker or insured", "Withdrawn", (short)14, true }
                });

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
                columns: new[] { "Description", "DisplayOrder" },
                values: new object[] { "Broker or insured withdrew submission", (short)14 });

            migrationBuilder.InsertData(
                table: "ReferenceSubmissionStatuses",
                columns: new[] { "Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal" },
                values: new object[,]
                {
                    { "Binding", "decision", "Binding and issuance processing in progress", "Binding", (short)11, false },
                    { "Expired", null, "Submission expired before disposition completed", "Expired", (short)17, true },
                    { "Lost", null, "Opportunity lost to another market or strategy change", "Lost", (short)16, true },
                    { "NotQuoted", null, "Submission closed without quote issued", "Not Quoted", (short)15, true },
                    { "QuotePreparation", "decision", "Preparing quote terms for broker", "Quote Preparation", (short)7, false },
                    { "RequoteRequested", "decision", "Broker requested revised quote terms", "Requote Requested", (short)9, false },
                    { "WaitingOnDocuments", "waiting", "Awaiting required underwriting documents", "Waiting on Documents", (short)4, false }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceRenewalStatuses_DisplayOrder",
                table: "ReferenceRenewalStatuses",
                column: "DisplayOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceSubmissionStatuses_DisplayOrder",
                table: "ReferenceSubmissionStatuses",
                column: "DisplayOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_CurrentStatus",
                table: "Submissions",
                column: "CurrentStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Submissions_CurrentStatus",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_ReferenceRenewalStatuses_DisplayOrder",
                table: "ReferenceRenewalStatuses");

            migrationBuilder.DropIndex(
                name: "IX_ReferenceSubmissionStatuses_DisplayOrder",
                table: "ReferenceSubmissionStatuses");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "BindRequested");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "DataReview");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Expired");

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
                keyValue: "WaitingOnBroker");

            migrationBuilder.DeleteData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Withdrawn");

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

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Bound",
                column: "DisplayOrder",
                value: (short)6);

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "InReview",
                column: "DisplayOrder",
                value: (short)4);

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Lapsed",
                column: "DisplayOrder",
                value: (short)8);

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Lost",
                column: "DisplayOrder",
                value: (short)7);

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "OutreachStarted",
                column: "DisplayOrder",
                value: (short)3);

            migrationBuilder.UpdateData(
                table: "ReferenceRenewalStatuses",
                keyColumn: "Code",
                keyValue: "Quoted",
                column: "DisplayOrder",
                value: (short)5);

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
                columns: new[] { "Description", "DisplayOrder" },
                values: new object[] { "Broker withdrew submission", (short)10 });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceRenewalStatuses_DisplayOrder",
                table: "ReferenceRenewalStatuses",
                column: "DisplayOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceSubmissionStatuses_DisplayOrder",
                table: "ReferenceSubmissionStatuses",
                column: "DisplayOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_CurrentStatus",
                table: "Submissions",
                column: "CurrentStatus",
                filter: "\"CurrentStatus\" NOT IN ('Bound', 'Declined', 'Withdrawn')");
        }
    }
}
