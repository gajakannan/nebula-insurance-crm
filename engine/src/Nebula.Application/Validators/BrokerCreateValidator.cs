using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class BrokerCreateValidator : AbstractValidator<BrokerCreateDto>
{
    public BrokerCreateValidator()
    {
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.State).NotEmpty().Length(2).Matches("^[A-Z]{2}$")
            .WithMessage("State must be a two-letter US state code (e.g. CA, NY).");
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
        RuleFor(x => x.Phone).Matches(@"^\+?[1-9]\d{7,14}$").When(x => x.Phone is not null)
            .WithMessage("Phone must be a valid international phone number.");
    }
}
