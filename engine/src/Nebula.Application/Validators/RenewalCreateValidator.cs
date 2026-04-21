using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class RenewalCreateValidator : AbstractValidator<RenewalCreateDto>
{
    public RenewalCreateValidator()
    {
        RuleFor(x => x.PolicyId).NotEmpty();
        RuleFor(x => x.AssignedToUserId)
            .NotEmpty()
            .When(x => x.AssignedToUserId.HasValue);
        RuleFor(x => x.LineOfBusiness)
            .Must(LineOfBusinessValidation.IsValid)
            .When(x => x.LineOfBusiness is not null)
            .WithMessage(LineOfBusinessValidation.ErrorMessage);
    }
}
