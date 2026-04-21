using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class SubmissionUpdateValidator : AbstractValidator<SubmissionUpdateDto>
{
    public SubmissionUpdateValidator()
    {
        RuleFor(x => x.PremiumEstimate)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PremiumEstimate.HasValue);

        RuleFor(x => x.LineOfBusiness)
            .Must(LineOfBusinessValidation.IsValid)
            .WithMessage(LineOfBusinessValidation.ErrorMessage)
            .When(x => x.LineOfBusiness is not null);
        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);
    }
}
