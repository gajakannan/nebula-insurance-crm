using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class TerritoryMemberAssignmentRequestValidator : AbstractValidator<TerritoryMemberAssignmentRequestDto>
{
    private static readonly string[] MemberTypes = ["Broker", "Producer"];

    public TerritoryMemberAssignmentRequestValidator()
    {
        RuleFor(x => x.MemberType)
            .NotEmpty()
            .Must(t => MemberTypes.Contains(t))
            .WithMessage("memberType must be 'Broker' or 'Producer'.");

        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.AssignmentReason).MaximumLength(500).When(x => x.AssignmentReason is not null);
    }
}
