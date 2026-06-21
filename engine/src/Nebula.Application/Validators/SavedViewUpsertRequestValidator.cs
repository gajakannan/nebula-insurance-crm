using System.Text.Json;
using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class SavedViewUpsertRequestValidator : AbstractValidator<SavedViewUpsertRequestDto>
{
    private static readonly string[] AllowedViewTypes = ["Search", "WorkloadReport", "WorkflowAgingReport"];
    private static readonly string[] AllowedVisibility = ["Personal", "Team"];

    public SavedViewUpsertRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);

        RuleFor(x => x.ViewType)
            .Must(v => AllowedViewTypes.Contains(v, StringComparer.Ordinal))
            .WithMessage("ViewType must be Search, WorkloadReport, or WorkflowAgingReport.");

        RuleFor(x => x.Visibility)
            .Must(v => AllowedVisibility.Contains(v, StringComparer.Ordinal))
            .WithMessage("Visibility must be Personal or Team.");

        // Criteria must be a structured JSON object (server-validated; never opaque text).
        RuleFor(x => x.Criteria)
            .Must(c => c.ValueKind == JsonValueKind.Object)
            .WithMessage("Criteria must be a JSON object.");

        // Team-scope presence + administered-scope authorization are enforced in the service
        // (saved_view_scope_required / saved_view_scope_denied) so they return their own codes.
    }
}
