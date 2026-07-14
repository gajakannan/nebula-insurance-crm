using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class CommissionSearchQueryValidator : AbstractValidator<CommissionSearchQuery>
{
    public CommissionSearchQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class CommissionScheduleUpsertValidator : AbstractValidator<CommissionScheduleUpsertDto>
{
    public CommissionScheduleUpsertValidator()
    {
        RuleFor(x => x.CarrierMarketId).NotEmpty();
        RuleFor(x => x.LineOfBusiness).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Basis).NotEmpty().MaximumLength(40);
        RuleFor(x => x.SourceNote).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.State).MaximumLength(2);
        RuleFor(x => x.ProductCode).MaximumLength(80);
        RuleFor(x => x.RatePercent).GreaterThanOrEqualTo(0).When(x => x.RatePercent.HasValue);
        RuleFor(x => x.FlatAmount).GreaterThanOrEqualTo(0).When(x => x.FlatAmount.HasValue);
        RuleFor(x => x).Must(x => x.RatePercent.HasValue || x.FlatAmount.HasValue)
            .WithMessage("RatePercent or FlatAmount is required.");
        RuleFor(x => x).Must(x => !x.EffectiveTo.HasValue || x.EffectiveTo.Value >= x.EffectiveFrom)
            .WithMessage("EffectiveTo must be on or after EffectiveFrom.");
    }
}

public class ProducerSplitAssignmentUpsertValidator : AbstractValidator<ProducerSplitAssignmentUpsertDto>
{
    public ProducerSplitAssignmentUpsertValidator()
    {
        RuleFor(x => x.PolicyId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Participants).NotEmpty();
        RuleForEach(x => x.Participants).ChildRules(p =>
        {
            p.RuleFor(x => x.ProducerId).NotEmpty();
            p.RuleFor(x => x.SplitPercent).GreaterThan(0).LessThanOrEqualTo(100);
        });
        RuleFor(x => x.Participants.Sum(p => p.SplitPercent)).Equal(100m)
            .WithMessage("Active split percentages must total 100.");
        RuleFor(x => x.Participants.Select(p => p.ProducerId).Distinct().Count()).Equal(x => x.Participants.Count)
            .WithMessage("Each producer may appear once per split assignment.");
        RuleFor(x => x).Must(x => !x.EffectiveTo.HasValue || x.EffectiveTo.Value >= x.EffectiveFrom)
            .WithMessage("EffectiveTo must be on or after EffectiveFrom.");
    }
}

public class CommissionAdjustmentRequestValidator : AbstractValidator<CommissionAdjustmentRequestDto>
{
    public CommissionAdjustmentRequestValidator()
    {
        RuleFor(x => x.Amount).NotEqual(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public class CommissionAdjustmentDecisionRequestValidator : AbstractValidator<CommissionAdjustmentDecisionRequestDto>
{
    public CommissionAdjustmentDecisionRequestValidator()
    {
        RuleFor(x => x.Decision).Must(x => x is "Approved" or "Rejected");
        RuleFor(x => x.DecisionNote).NotEmpty().MaximumLength(1000);
    }
}

public class RevenueAttributionRollupQueryValidator : AbstractValidator<RevenueAttributionRollupQuery>
{
    private static readonly string[] GroupBys = ["producer", "broker", "territory", "carrierMarket", "policyPeriod"];

    public RevenueAttributionRollupQueryValidator()
    {
        RuleFor(x => x.GroupBy).Must(x => GroupBys.Contains(x));
        RuleFor(x => x).Must(x => x.EndDate >= x.StartDate)
            .WithMessage("EndDate must be on or after StartDate.");
        RuleFor(x => x).Must(x => x.EndDate.DayNumber - x.StartDate.DayNumber <= 366)
            .WithMessage("Rollup date range cannot exceed 366 days.");
    }
}
