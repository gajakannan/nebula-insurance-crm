using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class OperationalReportQueryValidator : AbstractValidator<OperationalReportQuery>
{
    public OperationalReportQueryValidator()
    {
        RuleFor(x => x.Region).MaximumLength(120);
        RuleFor(x => x.LineOfBusiness).MaximumLength(120);
        RuleFor(x => x.WorkflowType).MaximumLength(60);
        RuleFor(x => x.DrilldownLimit).InclusiveBetween(1, 200);
    }
}

public class DistributionRollupQueryValidator : AbstractValidator<DistributionRollupQuery>
{
    private static readonly string[] AllowedGroupBy = ["Hierarchy", "Territory", "Producer"];
    private static readonly string[] AllowedMetricFamilies = ["Production", "Workflow", "Activity"];

    public DistributionRollupQueryValidator()
    {
        RuleFor(x => x.GroupBy)
            .Must(v => AllowedGroupBy.Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithMessage("groupBy must be one of: Hierarchy, Territory, Producer.");

        RuleFor(x => x.MetricFamily)
            .Must(v => AllowedMetricFamilies.Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithMessage("metricFamily must be one of: Production, Workflow, Activity.");

        RuleFor(x => x.DrilldownLimit).InclusiveBetween(1, 200);
    }
}
