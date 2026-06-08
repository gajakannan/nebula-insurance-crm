using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class DistributionNodeParentRequestValidator : AbstractValidator<DistributionNodeParentRequestDto>
{
    public DistributionNodeParentRequestValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => x.Note is not null);
    }
}
