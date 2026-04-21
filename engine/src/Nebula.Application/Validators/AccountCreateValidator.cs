using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class AccountCreateValidator : AbstractValidator<AccountCreateRequestDto>
{
    public AccountCreateValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LegalName).MaximumLength(200);
        RuleFor(x => x.TaxId).MaximumLength(50);
        RuleFor(x => x.Industry).MaximumLength(100);
        RuleFor(x => x.PrimaryLineOfBusiness).MaximumLength(50);
        RuleFor(x => x.TerritoryCode).MaximumLength(50);
        RuleFor(x => x.Region).MaximumLength(50);
        RuleFor(x => x.Address1).MaximumLength(200);
        RuleFor(x => x.Address2).MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(50);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.Country).MaximumLength(50);
    }
}
