using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class CommunicationEventCreateRequestValidator : AbstractValidator<CommunicationEventCreateRequestDto>
{
    private static readonly string[] Types = ["Note", "Call", "Meeting", "EmailReference"];
    private static readonly string[] Directions = ["Inbound", "Outbound", "Internal"];
    private static readonly string[] EntityTypes = ["Broker", "Account", "Submission", "Policy", "Renewal", "Task"];

    public CommunicationEventCreateRequestValidator()
    {
        RuleFor(x => x.Type).NotEmpty().Must(t => Types.Contains(t)).WithMessage("Unsupported communication type.");
        RuleFor(x => x.Direction).Must(d => d is null || Directions.Contains(d)).WithMessage("Unsupported direction.");
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Body).MaximumLength(8000);
        RuleFor(x => x.OccurredAt).NotEmpty();
        RuleFor(x => x.Links).NotEmpty();
        RuleFor(x => x.Links.Count(l => l.IsPrimary)).Equal(1).WithMessage("Exactly one primary link is required.");
        RuleForEach(x => x.Links).ChildRules(link =>
        {
            link.RuleFor(x => x.EntityType).NotEmpty().Must(t => EntityTypes.Contains(t)).WithMessage("Unsupported linked entity type.");
            link.RuleFor(x => x.EntityId).NotEmpty();
        });
        RuleForEach(x => x.Participants).ChildRules(participant =>
        {
            participant.RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
            participant.RuleFor(x => x.Email).MaximumLength(320);
            participant.RuleFor(x => x.ParticipantType).NotEmpty().MaximumLength(50);
            participant.RuleFor(x => x.Role).MaximumLength(100);
        });
        RuleFor(x => x.EmailReference).NotNull().When(x => x.Type == "EmailReference");
        RuleFor(x => x.EmailReference!.MessageId).NotEmpty().When(x => x.Type == "EmailReference");
        RuleFor(x => x.FollowUp).SetValidator(new CommunicationEventFollowUpRequestValidator()!).When(x => x.FollowUp is not null);
    }
}

public class CommunicationEventFollowUpRequestValidator : AbstractValidator<CommunicationEventFollowUpRequestDto>
{
    private static readonly string[] EntityTypes = ["Broker", "Account", "Submission", "Policy", "Renewal", "Task"];

    public CommunicationEventFollowUpRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Priority).Must(p => p is null || p is "Low" or "Normal" or "High").WithMessage("Priority must be Low, Normal, or High.");
        RuleFor(x => x.AssignedToUserId).NotEmpty();
        RuleFor(x => x.LinkedEntityType).NotEmpty().Must(t => EntityTypes.Contains(t)).WithMessage("Unsupported linked entity type.");
        RuleFor(x => x.LinkedEntityId).NotEmpty();
    }
}

public class CommunicationEventCorrectionRequestValidator : AbstractValidator<CommunicationEventCorrectionRequestDto>
{
    public CommunicationEventCorrectionRequestValidator()
    {
        RuleFor(x => x.Action).NotEmpty().Must(a => a is "Correct" or "Redact").WithMessage("Action must be Correct or Redact.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Summary).MaximumLength(500);
        RuleFor(x => x.Body).MaximumLength(8000);
        RuleFor(x => x)
            .Must(x => x.Action != "Correct" || x.Summary is not null || x.Body is not null || x.Links is not null || x.Participants is not null)
            .WithMessage("At least one correction field is required.");
    }
}
