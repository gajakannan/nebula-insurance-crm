using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class RenewalAssignmentValidator : AbstractValidator<RenewalAssignmentRequestDto>
{
    public RenewalAssignmentValidator()
    {
        RuleFor(x => x.AssignedToUserId).NotEmpty();
    }
}
