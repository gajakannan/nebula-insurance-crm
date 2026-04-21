using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class AccountContactRequestValidator : AbstractValidator<AccountContactRequestDto>
{
    public AccountContactRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50);
    }
}
