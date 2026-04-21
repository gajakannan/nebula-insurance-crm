using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class SubmissionAssignmentValidator : AbstractValidator<SubmissionAssignmentRequestDto>
{
    public SubmissionAssignmentValidator()
    {
        RuleFor(x => x.AssignedToUserId).NotEmpty();
    }
}
