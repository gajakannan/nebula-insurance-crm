using FluentValidation;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class CommunicationEndpoints
{
    public static IEndpointRouteBuilder MapCommunicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/communications")
            .WithTags("Communications")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        group.MapGet("/", ListCommunications);
        group.MapPost("/", CreateCommunication);
        group.MapGet("/{communicationId:guid}", GetCommunicationById);
        group.MapPost("/{communicationId:guid}/follow-up-task", CreateFollowUpTask);
        group.MapPost("/{communicationId:guid}/corrections", CorrectOrRedactCommunication);

        return app;
    }

    private static async Task<IResult> ListCommunications(
        string entityType,
        Guid entityId,
        int? page,
        int? pageSize,
        CommunicationService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var query = new CommunicationHistoryQuery(entityType, entityId, page ?? 1, Math.Min(pageSize ?? 20, 100));
        var (result, error) = await svc.ListAsync(query, user, ct);
        return MapCommunicationResult(result, error, entityId);
    }

    private static async Task<IResult> CreateCommunication(
        CommunicationEventCreateRequestDto dto,
        IValidator<CommunicationEventCreateRequestDto> validator,
        CommunicationService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(validation.ToDictionary());

        var (result, error) = await svc.CreateAsync(dto, user, ct);
        if (error is not null)
            return MapCommunicationResult(result, error, Guid.Empty);

        return Results.Created($"/communications/{result!.Id}", result);
    }

    private static async Task<IResult> GetCommunicationById(
        Guid communicationId,
        CommunicationService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var (result, error) = await svc.GetByIdAsync(communicationId, user, ct);
        return MapCommunicationResult(result, error, communicationId);
    }

    private static async Task<IResult> CreateFollowUpTask(
        Guid communicationId,
        CommunicationEventFollowUpRequestDto dto,
        IValidator<CommunicationEventFollowUpRequestDto> validator,
        CommunicationService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(validation.ToDictionary());

        var (result, error) = await svc.CreateFollowUpTaskAsync(communicationId, dto, user, ct);
        return error switch
        {
            null => Results.Created($"/tasks/{result!.Id}", result),
            "invalid_assignee" => ProblemDetailsHelper.InvalidAssignee(),
            "inactive_assignee" => ProblemDetailsHelper.InactiveAssignee(),
            "forbidden" => ProblemDetailsHelper.PolicyDenied(),
            "not_found" => ProblemDetailsHelper.NotFound("Communication", communicationId),
            _ => ProblemDetailsHelper.ValidationError(new Dictionary<string, string[]> { ["communication"] = [error] }),
        };
    }

    private static async Task<IResult> CorrectOrRedactCommunication(
        Guid communicationId,
        CommunicationEventCorrectionRequestDto dto,
        IValidator<CommunicationEventCorrectionRequestDto> validator,
        CommunicationService svc,
        ICurrentUserService user,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ProblemDetailsHelper.ValidationError(validation.ToDictionary());

        var (result, error) = await svc.CorrectOrRedactAsync(communicationId, dto, user, ct);
        return MapCommunicationResult(result, error, communicationId);
    }

    private static IResult MapCommunicationResult<T>(T? result, string? error, Guid resourceId)
    {
        return error switch
        {
            null => Results.Ok(result),
            "forbidden" => ProblemDetailsHelper.PolicyDenied(),
            "not_found" => ProblemDetailsHelper.NotFound("Communication", resourceId),
            "duplicate_link" => Results.Problem(
                title: "Duplicate communication link",
                detail: "A communication cannot contain duplicate related record links.",
                statusCode: 409,
                extensions: new Dictionary<string, object?> { ["code"] = "duplicate_link" }),
            _ => ProblemDetailsHelper.ValidationError(new Dictionary<string, string[]> { ["communication"] = [error] }),
        };
    }
}
