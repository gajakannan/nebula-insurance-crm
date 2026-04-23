using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class PolicyCreateRequestValidator : AbstractValidator<PolicyCreateRequestDto>
{
    public PolicyCreateRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.BrokerOfRecordId).NotEmpty();
        RuleFor(x => x.LineOfBusiness)
            .NotEmpty()
            .Must(LineOfBusinessValidation.IsValid)
            .WithMessage(LineOfBusinessValidation.ErrorMessage);
        RuleFor(x => x.CarrierId).NotEmpty();
        RuleFor(x => x.EffectiveDate).NotEmpty();
        RuleFor(x => x.ExpirationDate)
            .GreaterThan(x => x.EffectiveDate)
            .WithMessage("ExpirationDate must be after EffectiveDate.");
        RuleFor(x => x.TotalPremium).GreaterThanOrEqualTo(0).When(x => x.TotalPremium.HasValue);
        RuleFor(x => x.PremiumCurrency).Must(value => value is null or "USD").WithMessage("PremiumCurrency must be USD.");
        RuleFor(x => x.ImportMode).Must(value => value is null or "manual" or "csv-import")
            .WithMessage("ImportMode must be manual or csv-import.");
        RuleForEach(x => x.Coverages).SetValidator(new PolicyCoverageInputValidator());
    }
}

public class PolicyUpdateRequestValidator : AbstractValidator<PolicyUpdateRequestDto>
{
    public PolicyUpdateRequestValidator()
    {
        RuleFor(x => x.LineOfBusiness)
            .Must(LineOfBusinessValidation.IsValid)
            .When(x => x.LineOfBusiness is not null)
            .WithMessage(LineOfBusinessValidation.ErrorMessage);
        RuleFor(x => x.TotalPremium).GreaterThanOrEqualTo(0).When(x => x.TotalPremium.HasValue);
        RuleFor(x => x.ExpirationDate)
            .GreaterThan(x => x.EffectiveDate)
            .When(x => x.EffectiveDate.HasValue && x.ExpirationDate.HasValue)
            .WithMessage("ExpirationDate must be after EffectiveDate.");
    }
}

public class PolicyFromBindRequestValidator : AbstractValidator<PolicyFromBindRequestDto>
{
    public PolicyFromBindRequestValidator()
    {
        RuleFor(x => x.SubmissionId).NotEmpty();
        RuleFor(x => x.QuoteId).NotEmpty();
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.BrokerOfRecordId).NotEmpty();
        RuleFor(x => x.LineOfBusiness)
            .NotEmpty()
            .Must(LineOfBusinessValidation.IsValid)
            .WithMessage(LineOfBusinessValidation.ErrorMessage);
        RuleFor(x => x.CarrierId).NotEmpty();
        RuleFor(x => x.EffectiveDate).NotEmpty();
        RuleFor(x => x.ExpirationDate)
            .GreaterThan(x => x.EffectiveDate)
            .WithMessage("ExpirationDate must be after EffectiveDate.");
        RuleFor(x => x.TotalPremium).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PremiumCurrency).Equal("USD").WithMessage("PremiumCurrency must be USD.");
        RuleFor(x => x.Coverages).NotEmpty();
        RuleForEach(x => x.Coverages).SetValidator(new PolicyCoverageInputValidator());
    }
}

public class PolicyEndorsementRequestValidator : AbstractValidator<PolicyEndorsementRequestDto>
{
    private static readonly HashSet<string> ReasonCodes = new(StringComparer.Ordinal)
    {
        "CoverageIncrease",
        "CoverageDecrease",
        "CoverageAdded",
        "CoverageRemoved",
        "PremiumAdjustment",
        "NamedInsuredChange",
        "AddressChange",
        "DeductibleChange",
        "OtherAdministrative",
    };

    public PolicyEndorsementRequestValidator()
    {
        RuleFor(x => x.EndorsementReasonCode).Must(ReasonCodes.Contains)
            .WithMessage("EndorsementReasonCode is not supported.");
        RuleFor(x => x.EffectiveDate).NotEmpty();
        RuleFor(x => x.PremiumDelta).GreaterThanOrEqualTo(decimal.MinValue).When(x => x.PremiumDelta.HasValue);
        RuleFor(x => x.Coverages).NotEmpty();
        RuleForEach(x => x.Coverages).SetValidator(new PolicyCoverageInputValidator());
    }
}

public class PolicyCancelRequestValidator : AbstractValidator<PolicyCancelRequestDto>
{
    private static readonly HashSet<string> ReasonCodes = new(StringComparer.Ordinal)
    {
        "NonPayment",
        "InsuredRequest",
        "UnderwritingDecision",
        "MaterialMisrepresentation",
        "CoverageNoLongerNeeded",
        "CarrierWithdrawal",
        "Other",
    };

    public PolicyCancelRequestValidator()
    {
        RuleFor(x => x.CancellationReasonCode).Must(ReasonCodes.Contains)
            .WithMessage("CancellationReasonCode is not supported.");
        RuleFor(x => x.CancellationReasonDetail)
            .NotEmpty()
            .When(x => x.CancellationReasonCode == "Other")
            .WithMessage("CancellationReasonDetail is required when CancellationReasonCode is Other.");
        RuleFor(x => x.CancellationEffectiveDate).NotEmpty();
    }
}

public class PolicyReinstateRequestValidator : AbstractValidator<PolicyReinstateRequestDto>
{
    private static readonly HashSet<string> ReasonCodes = new(StringComparer.Ordinal)
    {
        "InsuredPaidOutstandingPremium",
        "CancellationInError",
        "AgreementReached",
        "Other",
    };

    public PolicyReinstateRequestValidator()
    {
        RuleFor(x => x.ReinstatementReason).Must(ReasonCodes.Contains)
            .WithMessage("ReinstatementReason is not supported.");
        RuleFor(x => x.ReinstatementDetail)
            .NotEmpty()
            .When(x => x.ReinstatementReason == "Other")
            .WithMessage("ReinstatementDetail is required when ReinstatementReason is Other.");
    }
}

public class PolicyCoverageInputValidator : AbstractValidator<PolicyCoverageInputDto>
{
    public PolicyCoverageInputValidator()
    {
        RuleFor(x => x.CoverageCode).NotEmpty().MaximumLength(40);
        RuleFor(x => x.CoverageName).MaximumLength(200);
        RuleFor(x => x.Limit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Deductible).GreaterThanOrEqualTo(0).When(x => x.Deductible.HasValue);
        RuleFor(x => x.Premium).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ExposureBasis).MaximumLength(40);
        RuleFor(x => x.ExposureQuantity).GreaterThanOrEqualTo(0).When(x => x.ExposureQuantity.HasValue);
    }
}
