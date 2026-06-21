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
