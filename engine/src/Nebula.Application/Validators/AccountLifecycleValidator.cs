using FluentValidation;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Validators;

public class AccountLifecycleValidator : AbstractValidator<AccountLifecycleRequestDto>
{
    public AccountLifecycleValidator()
    {
        RuleFor(x => x.ToState)
            .NotEmpty()
            .Must(state => state is AccountStatuses.Active or AccountStatuses.Inactive or AccountStatuses.Deleted)
            .WithMessage("toState must be Active, Inactive, or Deleted.");

        When(x => x.ToState == AccountStatuses.Deleted, () =>
        {
            RuleFor(x => x.ReasonCode).NotEmpty();
            RuleFor(x => x.ReasonDetail)
                .NotEmpty()
                .When(x => string.Equals(x.ReasonCode, "Other", StringComparison.Ordinal));
        });
    }
}
