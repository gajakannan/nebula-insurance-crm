using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class AccountRelationshipRequestValidator : AbstractValidator<AccountRelationshipRequestDto>
{
    public AccountRelationshipRequestValidator()
    {
        RuleFor(x => x.RelationshipType)
            .NotEmpty()
            .Must(value => value is "BrokerOfRecord" or "PrimaryProducer" or "Territory")
            .WithMessage("relationshipType must be BrokerOfRecord, PrimaryProducer, or Territory.");
        RuleFor(x => x.NewValue).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
