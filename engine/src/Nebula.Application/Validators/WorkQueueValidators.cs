using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class WorkQueueUpsertRequestValidator : AbstractValidator<WorkQueueUpsertRequestDto>
{
    private static readonly string[] WorkTypes = ["Task", "Submission", "Renewal", "Mixed"];
    private static readonly string[] Statuses = ["Active", "Inactive"];

    public WorkQueueUpsertRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.WorkType).Must(x => WorkTypes.Contains(x)).WithMessage("WorkType must be Task, Submission, Renewal, or Mixed.");
        RuleFor(x => x.Status).Must(x => Statuses.Contains(x)).WithMessage("Status must be Active or Inactive.");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Members).NotNull();
        RuleForEach(x => x.Members).SetValidator(new QueueMemberUpsertRequestValidator());
        RuleFor(x => x.Members).Must((dto, members) => dto.Status != "Active" || members.Any())
            .WithMessage("Active queues require at least one member.");
    }
}

public class QueueMemberUpsertRequestValidator : AbstractValidator<QueueMemberUpsertRequestDto>
{
    private static readonly string[] Roles = ["Manager", "Member", "Backup"];

    public QueueMemberUpsertRequestValidator()
    {
        RuleFor(x => x.UserProfileId).NotEmpty();
        RuleFor(x => x.Role).Must(x => Roles.Contains(x)).WithMessage("Role must be Manager, Member, or Backup.");
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.EffectiveTo).GreaterThan(x => x.EffectiveFrom).When(x => x.EffectiveTo.HasValue);
    }
}

public class AssignmentRuleUpsertRequestValidator : AbstractValidator<AssignmentRuleUpsertRequestDto>
{
    private static readonly string[] RuleTypes = ["ManualOverride", "Coverage", "TerritoryOwnership", "WorkloadBalance", "Fallback"];
    private static readonly string[] Statuses = ["Draft", "Active", "Inactive"];

    public AssignmentRuleUpsertRequestValidator()
    {
        RuleFor(x => x.WorkQueueId).NotEmpty();
        RuleFor(x => x.RuleType).Must(x => RuleTypes.Contains(x)).WithMessage("RuleType is not supported.");
        RuleFor(x => x.Precedence).InclusiveBetween(1, 500);
        RuleFor(x => x.Status).Must(x => Statuses.Contains(x)).WithMessage("Status must be Draft, Active, or Inactive.");
        RuleFor(x => x.ConditionsJson).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.OutcomeJson).NotEmpty().MaximumLength(8000);
    }
}

public class CoverageWindowUpsertRequestValidator : AbstractValidator<CoverageWindowUpsertRequestDto>
{
    private static readonly string[] Statuses = ["Scheduled", "Active", "Expired", "Cancelled"];

    public CoverageWindowUpsertRequestValidator()
    {
        RuleFor(x => x.CoveredUserId).NotEmpty();
        RuleFor(x => x.BackupUserId).NotEmpty().NotEqual(x => x.CoveredUserId);
        RuleFor(x => x.StartsAt).NotEmpty();
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt);
        RuleFor(x => x.Status).Must(x => Statuses.Contains(x)).WithMessage("Status is not supported.");
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class QueueReassignmentRequestValidator : AbstractValidator<QueueReassignmentRequestDto>
{
    public QueueReassignmentRequestValidator()
    {
        RuleFor(x => x.AssignedToUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class QueueRebalanceRequestValidator : AbstractValidator<QueueRebalanceRequestDto>
{
    public QueueRebalanceRequestValidator()
    {
        RuleFor(x => x.Strategy).NotEmpty().MaximumLength(80);
        RuleFor(x => x.MaxItems).InclusiveBetween(1, 100).When(x => x.MaxItems.HasValue);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class QueueRouteRequestValidator : AbstractValidator<QueueRouteRequestDto>
{
    private static readonly string[] SourceTypes = ["Task", "Submission", "Renewal"];

    public QueueRouteRequestValidator()
    {
        RuleFor(x => x.SourceType).Must(x => SourceTypes.Contains(x)).WithMessage("SourceType must be Task, Submission, or Renewal.");
        RuleFor(x => x.SourceId).NotEmpty();
    }
}
