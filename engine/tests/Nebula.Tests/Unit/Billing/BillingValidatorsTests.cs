using FluentValidation.TestHelper;
using Nebula.Application.DTOs;
using Nebula.Application.Validators;

namespace Nebula.Tests.Unit.Billing;

public class BillingValidatorsTests
{
    [Fact]
    public void Search_contracts_reject_unknown_states_and_out_of_range_paging()
    {
        var invoice = new BillingInvoiceSearchQueryValidator().TestValidate(
            new BillingInvoiceSearchQuery(new string('x', 121), null, null, "Unknown", null, 0, 101));
        invoice.ShouldHaveValidationErrorFor(x => x.Q);
        invoice.ShouldHaveValidationErrorFor(x => x.Status);
        invoice.ShouldHaveValidationErrorFor(x => x.Page);
        invoice.ShouldHaveValidationErrorFor(x => x.PageSize);

        var receipt = new PaymentReceiptSearchQueryValidator().TestValidate(
            new PaymentReceiptSearchQuery("Unknown", new string('x', 121), "usd", 0, 101));
        receipt.ShouldHaveValidationErrorFor(x => x.ApplicationStatus);
        receipt.ShouldHaveValidationErrorFor(x => x.ExternalReference);
        receipt.ShouldHaveValidationErrorFor(x => x.Currency);
        receipt.ShouldHaveValidationErrorFor(x => x.Page);
        receipt.ShouldHaveValidationErrorFor(x => x.PageSize);

        var exception = new ReconciliationExceptionSearchQueryValidator().TestValidate(
            new ReconciliationExceptionSearchQuery("Unknown", "Unknown", 0, 101));
        exception.ShouldHaveValidationErrorFor(x => x.Status);
        exception.ShouldHaveValidationErrorFor(x => x.Type);
        exception.ShouldHaveValidationErrorFor(x => x.Page);
        exception.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Invoice_requires_due_date_on_or_after_invoice_date()
    {
        var request = new BillingInvoiceCreateRequestDto(
            "INV-100", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "USD", 100m,
            new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 19));

        new BillingInvoiceCreateRequestValidator().TestValidate(request).ShouldHaveValidationErrorFor(x => x);
    }

    [Theory]
    [InlineData("Approve")]
    [InlineData("Reject")]
    public void Correction_decision_accepts_only_terminal_contract_values(string decision)
    {
        var result = new BillingCorrectionDecisionRequestValidator()
            .TestValidate(new BillingCorrectionDecisionRequestDto(decision, "Reviewed against the attached evidence."));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Exact_application_requires_both_ids_and_receipt_row_version()
    {
        var result = new PaymentApplicationRequestValidator()
            .TestValidate(new PaymentApplicationRequestDto(Guid.Empty, Guid.Empty, "not-a-version"));

        result.ShouldHaveValidationErrorFor(x => x.BillingInvoiceId);
        result.ShouldHaveValidationErrorFor(x => x.PaymentReceiptId);
        result.ShouldHaveValidationErrorFor(x => x.PaymentReceiptRowVersion);
    }

    [Fact]
    public void Receipt_reference_and_balance_correction_contracts_enforce_bounded_values()
    {
        var receipt = new PaymentReceiptCreateRequestValidator().TestValidate(
            new PaymentReceiptCreateRequestDto("", new DateOnly(2026, 7, 19), "usd", -1m, new string('x', 81), new string('x', 501)));
        receipt.ShouldHaveValidationErrorFor(x => x.ExternalReference);
        receipt.ShouldHaveValidationErrorFor(x => x.Currency);
        receipt.ShouldHaveValidationErrorFor(x => x.Amount);
        receipt.ShouldHaveValidationErrorFor(x => x.InvoiceReference);
        receipt.ShouldHaveValidationErrorFor(x => x.Memo);

        var reference = new ReconciliationReferenceCorrectionRequestValidator().TestValidate(
            new ReconciliationReferenceCorrectionRequestDto(Guid.Empty, "", ""));
        reference.ShouldHaveValidationErrorFor(x => x.BillingInvoiceId);
        reference.ShouldHaveValidationErrorFor(x => x.ResolutionCode);
        reference.ShouldHaveValidationErrorFor(x => x.ResolutionNote);

        var correction = new BillingCorrectionRequestValidator().TestValidate(
            new BillingCorrectionRequestDto(0m, -1m, "", ""));
        correction.ShouldHaveValidationErrorFor(x => x.CorrectionAmount);
        correction.ShouldHaveValidationErrorFor(x => x.ProposedOutstandingAmount);
        correction.ShouldHaveValidationErrorFor(x => x.Reason);
        correction.ShouldHaveValidationErrorFor(x => x.EvidenceNote);
    }
}
