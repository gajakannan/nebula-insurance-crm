using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class AccountMergeValidator : AbstractValidator<AccountMergeRequestDto>
{
    public AccountMergeValidator()
    {
        RuleFor(x => x.SurvivorAccountId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
