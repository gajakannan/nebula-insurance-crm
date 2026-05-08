using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class RenewalUpdateValidator : AbstractValidator<RenewalUpdateDto>
{
    public RenewalUpdateValidator()
    {
    }
}

public class RenewalLobAttributesUpdateValidator : AbstractValidator<RenewalLobAttributesUpdateDto>
{
    public RenewalLobAttributesUpdateValidator()
    {
        RuleFor(x => x.LobAttributes)
            .NotNull()
            .WithMessage("lobAttributes is required.");
    }
}
