using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class RenewalTransitionValidator : AbstractValidator<RenewalTransitionRequestDto>
{
    private static readonly string[] ValidLostReasonCodes =
        ["NonRenewal", "CompetitiveLoss", "BusinessClosed", "CoverageNoLongerNeeded", "PricingDeclined", "Other"];

    public RenewalTransitionValidator()
    {
        RuleFor(x => x.ToState)
            .NotEmpty()
            .Must(state => state is "Outreach" or "InReview" or "Quoted" or "Completed" or "Lost")
            .WithMessage("ToState must be one of: Outreach, InReview, Quoted, Completed, Lost.");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));

        RuleFor(x => x.ReasonCode)
            .NotEmpty()
            .WithMessage("reasonCode is required when transitioning to Lost.")
            .When(x => string.Equals(x.ToState, "Lost", StringComparison.Ordinal));

        RuleFor(x => x.ReasonCode)
            .Must(code => code is null || ValidLostReasonCodes.Contains(code))
            .WithMessage($"reasonCode must be one of: {string.Join(", ", ValidLostReasonCodes)}.");

        RuleFor(x => x.ReasonDetail)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.ReasonDetail));

        RuleFor(x => x.ReasonDetail)
            .NotEmpty()
            .WithMessage("reasonDetail is required when reasonCode is Other.")
            .When(x => string.Equals(x.ReasonCode, "Other", StringComparison.Ordinal));

        RuleFor(x => x)
            .Must(x =>
                !string.Equals(x.ToState, "Completed", StringComparison.Ordinal)
                || x.BoundPolicyId.HasValue
                || x.RenewalSubmissionId.HasValue)
            .WithMessage("boundPolicyId or renewalSubmissionId is required when transitioning to Completed.");
    }
}
