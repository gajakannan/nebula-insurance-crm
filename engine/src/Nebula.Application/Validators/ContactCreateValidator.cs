using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class ContactCreateValidator : AbstractValidator<ContactCreateDto>
{
    public ContactCreateValidator()
    {
        RuleFor(x => x.BrokerId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^\+?[1-9]\d{7,14}$")
            .WithMessage("Phone must be a valid international phone number.");
    }
}
