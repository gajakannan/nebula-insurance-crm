using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class TaskCreateRequestValidator : AbstractValidator<TaskCreateRequestDto>
{
    private static readonly string[] ValidLinkedEntityTypes = ["Broker", "Account", "Submission", "Renewal"];
    private static readonly string[] ValidPriorities = ["Low", "Normal", "High", "Urgent"];

    public TaskCreateRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.AssignedToUserId).NotEmpty();

        RuleFor(x => x.Priority)
            .Must(p => ValidPriorities.Contains(p!))
            .When(x => x.Priority is not null)
            .WithMessage($"Priority must be one of: {string.Join(", ", ValidPriorities)}.");

        RuleFor(x => x.LinkedEntityType)
            .Must(t => ValidLinkedEntityTypes.Contains(t!))
            .When(x => x.LinkedEntityType is not null)
            .WithMessage($"LinkedEntityType must be one of: {string.Join(", ", ValidLinkedEntityTypes)}.");

        RuleFor(x => x.LinkedEntityId)
            .NotEmpty()
            .When(x => x.LinkedEntityType is not null)
            .WithMessage("LinkedEntityId is required when LinkedEntityType is provided.");

        RuleFor(x => x.LinkedEntityType)
            .NotEmpty()
            .When(x => x.LinkedEntityId is not null)
            .WithMessage("LinkedEntityType is required when LinkedEntityId is provided.");
    }
}
