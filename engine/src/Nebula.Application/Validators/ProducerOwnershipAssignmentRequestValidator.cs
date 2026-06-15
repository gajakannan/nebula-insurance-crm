using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class ProducerOwnershipAssignmentRequestValidator : AbstractValidator<ProducerOwnershipAssignmentRequestDto>
{
    private static readonly string[] ScopeTypes = ["Account", "BrokerRelationship"];

    public ProducerOwnershipAssignmentRequestValidator()
    {
        RuleFor(x => x.ScopeType)
            .NotEmpty()
            .Must(s => ScopeTypes.Contains(s))
            .WithMessage("scopeType must be 'Account' or 'BrokerRelationship'.");

        RuleFor(x => x.ScopeId).NotEmpty();
        RuleFor(x => x.ProducerNodeId).NotEmpty();
        RuleFor(x => x.AssignmentReason).MaximumLength(500).When(x => x.AssignmentReason is not null);
    }
}
