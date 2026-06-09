using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class TerritoryCreateRequestValidator : AbstractValidator<TerritoryCreateRequestDto>
{
    public TerritoryCreateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.Criteria).NotNull();
    }
}
