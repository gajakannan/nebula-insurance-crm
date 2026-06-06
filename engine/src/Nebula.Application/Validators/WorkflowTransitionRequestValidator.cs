using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class WorkflowTransitionRequestValidator : AbstractValidator<WorkflowTransitionRequestDto>
{
    public WorkflowTransitionRequestValidator()
    {
        RuleFor(x => x.ToState).NotEmpty().MaximumLength(30);

        RuleFor(x => x.ReasonCode)
            .NotEmpty()
            .When(x => IsTerminalReasonRequired(x.ToState));

        RuleFor(x => x.ReasonDetail)
            .NotEmpty()
            .When(x =>
                IsTerminalReasonRequired(x.ToState)
                && string.Equals(x.ReasonCode, "Other", StringComparison.Ordinal));

        RuleFor(x => x.ReasonDetail)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.ReasonDetail));

        RuleFor(x => x)
            .Must(x =>
                !string.Equals(x.ToState, "Completed", StringComparison.Ordinal)
                || x.BoundPolicyId.HasValue
                || x.RenewalSubmissionId.HasValue)
            .WithMessage("boundPolicyId or renewalSubmissionId is required when transitioning to Completed.");
    }

    private static bool IsTerminalReasonRequired(string toState) =>
        string.Equals(toState, "Lost", StringComparison.Ordinal)
        || string.Equals(toState, "Declined", StringComparison.Ordinal)
        || string.Equals(toState, "Withdrawn", StringComparison.Ordinal);
}
