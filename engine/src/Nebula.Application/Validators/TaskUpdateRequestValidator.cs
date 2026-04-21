using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class TaskUpdateRequestValidator : AbstractValidator<TaskUpdateRequestDto>
{
    private static readonly string[] ValidStatuses = ["Open", "InProgress", "Done"];
    private static readonly string[] ValidPriorities = ["Low", "Normal", "High", "Urgent"];

    public TaskUpdateRequestValidator()
    {
        RuleFor(x => x.Title).MaximumLength(255).When(x => x.Title is not null);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);

        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s!))
            .When(x => x.Status is not null)
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");

        RuleFor(x => x.Priority)
            .Must(p => ValidPriorities.Contains(p!))
            .When(x => x.Priority is not null)
            .WithMessage($"Priority must be one of: {string.Join(", ", ValidPriorities)}.");
    }
}
