using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class BrokerInsightScorecardQueryValidator : AbstractValidator<BrokerInsightScorecardQuery>
{
    public BrokerInsightScorecardQueryValidator()
    {
        RuleFor(q => q.PeriodStart).LessThanOrEqualTo(q => q.PeriodEnd);
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 200);
        RuleFor(q => q.LineOfBusiness).MaximumLength(80);
        RuleFor(q => q.Region).MaximumLength(80);
    }
}

public class BrokerInsightTrendQueryValidator : AbstractValidator<BrokerInsightTrendQuery>
{
    public BrokerInsightTrendQueryValidator()
    {
        RuleFor(q => q.MetricKey).Must(k => BrokerInsightQueryDefaults.MetricKeys.Contains(k))
            .WithMessage("MetricKey is not supported.");
        RuleFor(q => q.Bucket).Must(b => BrokerInsightQueryDefaults.Buckets.Contains(b))
            .WithMessage("Bucket is not supported.");
        RuleFor(q => q.PeriodStart).LessThanOrEqualTo(q => q.PeriodEnd);
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 200);
    }
}

public class BrokerInsightBenchmarkQueryValidator : AbstractValidator<BrokerInsightBenchmarkQuery>
{
    public BrokerInsightBenchmarkQueryValidator()
    {
        RuleFor(q => q.PeerSet).Must(p => BrokerInsightQueryDefaults.PeerSets.Contains(p))
            .WithMessage("PeerSet is not supported.");
        RuleFor(q => q.PeriodStart).LessThanOrEqualTo(q => q.PeriodEnd);
    }
}

public class BrokerInsightSnapshotQueryValidator : AbstractValidator<BrokerInsightSnapshotQuery>
{
    public BrokerInsightSnapshotQueryValidator()
    {
        RuleFor(q => q.PeriodStart).LessThanOrEqualTo(q => q.PeriodEnd);
    }
}
