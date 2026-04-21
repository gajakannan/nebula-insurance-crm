using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class ContactUpdateValidator : AbstractValidator<ContactUpdateDto>
{
    public ContactUpdateValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^\+?[1-9]\d{7,14}$")
            .WithMessage("Phone must be a valid international phone number.");
    }
}
