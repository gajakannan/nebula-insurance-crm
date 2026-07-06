using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class OutboundDocumentEndpoints
{
    public static IEndpointRouteBuilder MapOutboundDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/outbound-documents")
            .WithTags("OutboundDocuments")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        group.MapPost("/preview", Preview);
        group.MapPost("/issue", Issue);
        group.MapPost("/{documentId}/regenerate", Regenerate);

        return app;
    }

    private static async Task<IResult> Preview(
        GeneratedDocumentRequestDto request,
        OutboundDocumentGenerationService service,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var result = await service.PreviewAsync(request, user, ct);
        return result.Value is null ? Problem(result.ErrorCode ?? "preview_failed", result.Detail) : Results.Ok(result.Value);
    }

    private static async Task<IResult> Issue(
        GeneratedDocumentRequestDto request,
        OutboundDocumentGenerationService service,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var result = await service.IssueAsync(request, user, ct);
        return result.Value is null
            ? Problem(result.ErrorCode ?? "issue_failed", result.Detail)
            : Results.Created($"/documents/{result.Value.DocumentId}", result.Value);
    }

    private static async Task<IResult> Regenerate(
        string documentId,
        GeneratedDocumentRequestDto request,
        OutboundDocumentGenerationService service,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var result = await service.RegenerateAsync(documentId, request, user, ct);
        return result.Value is null
            ? Problem(result.ErrorCode ?? "regenerate_failed", result.Detail)
            : Results.Created($"/documents/{result.Value.DocumentId}", result.Value);
    }

    private static IResult Problem(string code, string? detail = null)
    {
        var status = code switch
        {
            "document_not_found" or "template_not_found" => StatusCodes.Status404NotFound,
            "policy_denied" or "parent_access_denied" or "classification_access_denied" or "template_access_denied" => StatusCodes.Status403Forbidden,
            "template_not_published" or "not_generated_document" or "missing_merge_data" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return Results.Problem(
            title: code.Replace('_', ' '),
            detail: detail,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = code });
    }
}
