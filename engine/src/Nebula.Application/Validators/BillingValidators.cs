using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class BillingInvoiceSearchQueryValidator : AbstractValidator<BillingInvoiceSearchQuery>
{
    public BillingInvoiceSearchQueryValidator()
    {
        RuleFor(x => x.Q).MaximumLength(120);
        RuleFor(x => x.Status).Must(x => x is null or "Outstanding" or "Reconciled");
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class BillingInvoiceCreateRequestValidator : AbstractValidator<BillingInvoiceCreateRequestDto>
{
    public BillingInvoiceCreateRequestValidator()
    {
        RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(80);
        RuleFor(x => x.PolicyId).NotEmpty();
        RuleFor(x => x.PolicyVersionId).NotEmpty();
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Currency).Matches("^[A-Z]{3}$");
        RuleFor(x => x.OriginalAmount).GreaterThan(0).PrecisionScale(18, 2, true);
        RuleFor(x => x).Must(x => x.DueDate >= x.InvoiceDate)
            .WithMessage("DueDate must be on or after InvoiceDate.");
    }
}

public class PaymentReceiptSearchQueryValidator : AbstractValidator<PaymentReceiptSearchQuery>
{
    public PaymentReceiptSearchQueryValidator()
    {
        RuleFor(x => x.ApplicationStatus).Must(x => x is null or "Unapplied" or "Applied");
        RuleFor(x => x.ExternalReference).MaximumLength(120);
        RuleFor(x => x.Currency).Matches("^[A-Z]{3}$").When(x => x.Currency is not null);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class PaymentReceiptCreateRequestValidator : AbstractValidator<PaymentReceiptCreateRequestDto>
{
    public PaymentReceiptCreateRequestValidator()
    {
        RuleFor(x => x.ExternalReference).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Currency).Matches("^[A-Z]{3}$");
        RuleFor(x => x.Amount).GreaterThan(0).PrecisionScale(18, 2, true);
        RuleFor(x => x.InvoiceReference).MaximumLength(80);
        RuleFor(x => x.Memo).MaximumLength(500);
    }
}

public class PaymentApplicationRequestValidator : AbstractValidator<PaymentApplicationRequestDto>
{
    public PaymentApplicationRequestValidator()
    {
        RuleFor(x => x.BillingInvoiceId).NotEmpty();
        RuleFor(x => x.PaymentReceiptId).NotEmpty();
        RuleFor(x => x.PaymentReceiptRowVersion).NotEmpty().Must(value => uint.TryParse(value, out _))
            .WithMessage("PaymentReceiptRowVersion must be an unsigned integer string.");
    }
}

public class ReconciliationExceptionSearchQueryValidator : AbstractValidator<ReconciliationExceptionSearchQuery>
{
    private static readonly string[] Types =
    [
        "MissingInvoiceReference", "InvoiceReferenceConflict", "AmountMismatch",
        "CurrencyMismatch", "DuplicateReceipt", "InvalidSourceData"
    ];

    public ReconciliationExceptionSearchQueryValidator()
    {
        RuleFor(x => x.Status).Must(x => x is null or "Open" or "Resolved");
        RuleFor(x => x.Type).Must(x => x is null || Types.Contains(x));
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class ReconciliationReferenceCorrectionRequestValidator : AbstractValidator<ReconciliationReferenceCorrectionRequestDto>
{
    public ReconciliationReferenceCorrectionRequestValidator()
    {
        RuleFor(x => x.BillingInvoiceId).NotEmpty();
        RuleFor(x => x.ResolutionCode).NotEmpty().MaximumLength(80);
        RuleFor(x => x.ResolutionNote).NotEmpty().MaximumLength(1000);
    }
}

public class BillingCorrectionRequestValidator : AbstractValidator<BillingCorrectionRequestDto>
{
    public BillingCorrectionRequestValidator()
    {
        RuleFor(x => x.ProposedOutstandingAmount).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true);
        RuleFor(x => x.CorrectionAmount).NotEqual(0).PrecisionScale(18, 2, true);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.EvidenceNote).NotEmpty().MaximumLength(1000);
    }
}

public class BillingCorrectionDecisionRequestValidator : AbstractValidator<BillingCorrectionDecisionRequestDto>
{
    public BillingCorrectionDecisionRequestValidator()
    {
        RuleFor(x => x.Decision).Must(x => x is "Approve" or "Reject");
        RuleFor(x => x.DecisionNote).NotEmpty().MaximumLength(1000);
    }
}
