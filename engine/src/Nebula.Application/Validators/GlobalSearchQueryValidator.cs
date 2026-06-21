using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class GlobalSearchQueryValidator : AbstractValidator<GlobalSearchQuery>
{
    private static readonly string[] AllowedObjectTypes =
        ["Account", "Broker", "MGA", "Program", "Policy", "Submission", "Renewal", "Task"];

    private static readonly string[] AllowedSorts = ["relevance", "updated", "title"];

    public GlobalSearchQueryValidator()
    {
        // q length >= 2 after trim is enforced at the endpoint (search_query_too_short, 400);
        // here we bound the upper length and the structured filters.
        RuleFor(x => x.Q).MaximumLength(200);

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleForEach(x => x.ObjectTypes)
            .Must(t => AllowedObjectTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Unsupported objectType filter.");

        RuleFor(x => x.Sort)
            .Must(s => AllowedSorts.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Sort must be one of: relevance, updated, title.");

        RuleFor(x => x.Status).MaximumLength(60);
        RuleFor(x => x.Region).MaximumLength(120);
        RuleFor(x => x.LineOfBusiness).MaximumLength(120);
    }
}
