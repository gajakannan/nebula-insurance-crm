using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class RenewalUpdateValidator : AbstractValidator<RenewalUpdateDto>
{
    public RenewalUpdateValidator()
    {
    }
}
