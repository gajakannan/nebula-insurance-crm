using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class ServiceCaseCreateRequestValidator : AbstractValidator<ServiceCaseCreateRequestDto>
{
    private static readonly string[] Types = ["Service", "ClaimSupport", "Billing", "PolicyChange", "General"];
    private static readonly string[] Priorities = ["Low", "Medium", "High", "Urgent"];

    public ServiceCaseCreateRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(8000);
        RuleFor(x => x.Type).NotEmpty().Must(t => Types.Contains(t)).WithMessage("Unsupported service case type.");
        RuleFor(x => x.Priority).NotEmpty().Must(p => Priorities.Contains(p)).WithMessage("Unsupported service case priority.");
        RuleFor(x => x.OwnerUserId).NotEmpty();
        RuleFor(x => x.DueDate).NotNull().WithMessage("Due date is required.");
        RuleFor(x => x.FollowUpSummary).MaximumLength(1000);
        RuleFor(x => x.ClaimReference).SetValidator(new ServiceCaseClaimReferenceUpdateRequestValidator()!)
            .When(x => x.ClaimReference is not null);
    }
}

public class ServiceCaseUpdateRequestValidator : AbstractValidator<ServiceCaseUpdateRequestDto>
{
    private static readonly string[] Priorities = ["Low", "Medium", "High", "Urgent"];

    public ServiceCaseUpdateRequestValidator()
    {
        RuleFor(x => x.Summary).MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(8000);
        RuleFor(x => x.Priority).Must(p => p is null || Priorities.Contains(p)).WithMessage("Unsupported service case priority.");
        RuleFor(x => x.FollowUpSummary).MaximumLength(1000);
        RuleFor(x => x.ResolutionSummary).MaximumLength(2000);
        RuleFor(x => x)
            .Must(x => x.Summary is not null
                || x.Description is not null
                || x.Priority is not null
                || x.OwnerUserId.HasValue
                || x.DueDate.HasValue
                || x.FollowUpSummary is not null
                || x.ResolutionSummary is not null)
            .WithMessage("At least one update field is required.");
    }
}

public class ServiceCaseTransitionRequestValidator : AbstractValidator<ServiceCaseTransitionRequestDto>
{
    private static readonly string[] Statuses = ["Intake", "InProgress", "Waiting", "Resolved", "Closed"];

    public ServiceCaseTransitionRequestValidator()
    {
        RuleFor(x => x.ToStatus).NotEmpty().Must(s => Statuses.Contains(s)).WithMessage("Unsupported service case status.");
        RuleFor(x => x.ReasonCode).MaximumLength(80);
        RuleFor(x => x.Note).MaximumLength(1000);
        RuleFor(x => x.ResolutionSummary).MaximumLength(2000);
        RuleFor(x => x)
            .Must(x => x.ToStatus != "Waiting" || !string.IsNullOrWhiteSpace(x.Note) || !string.IsNullOrWhiteSpace(x.ReasonCode))
            .WithMessage("Waiting status requires a reason or note.");
    }
}

public class ServiceCaseClaimReferenceUpdateRequestValidator : AbstractValidator<ServiceCaseClaimReferenceUpdateRequestDto>
{
    public ServiceCaseClaimReferenceUpdateRequestValidator()
    {
        RuleFor(x => x.CarrierClaimNumber).MaximumLength(100);
        RuleFor(x => x.DateOfLoss)
            .Must(date => !date.HasValue || date.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of loss cannot be in the future.");
        RuleFor(x => x.ClaimantDisplayName).MaximumLength(200);
        RuleFor(x => x.LossSummary).MaximumLength(2000);
        RuleFor(x => x.CarrierContactReference).MaximumLength(500);
    }
}

public class ServiceCaseCommunicationLinkRequestValidator : AbstractValidator<ServiceCaseCommunicationLinkRequestDto>
{
    public ServiceCaseCommunicationLinkRequestValidator()
    {
        RuleFor(x => x.CommunicationEventId).NotEmpty();
        RuleFor(x => x.LinkType).Must(t => t is null || t is "Context" or "Evidence" or "FollowUp")
            .WithMessage("Unsupported service case communication link type.");
    }
}

public class ServiceCaseFollowUpTaskRequestValidator : AbstractValidator<ServiceCaseFollowUpTaskRequestDto>
{
    public ServiceCaseFollowUpTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.AssignedToUserId).NotEmpty();
        RuleFor(x => x.Priority).Must(p => p is null || p is "Low" or "Normal" or "High")
            .WithMessage("Priority must be Low, Normal, or High.");
    }
}
