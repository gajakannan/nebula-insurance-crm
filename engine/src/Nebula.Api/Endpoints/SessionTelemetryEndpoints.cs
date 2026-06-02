using System.Security.Claims;
using Nebula.Api.Helpers;
using Nebula.Api.Models;
using Nebula.Api.Services;

namespace Nebula.Api.Endpoints;

public static class SessionTelemetryEndpoints
{
    public static IEndpointRouteBuilder MapSessionTelemetryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/internal/telemetry")
            .WithTags("Session Telemetry")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        group.MapPost("/session-continuity", IngestAsync);

        return app;
    }

    internal static async Task<IResult> IngestAsync(
        SessionContinuityTelemetryRequest request,
        SessionContinuityTelemetryService telemetry,
        HttpContext httpContext,
        CancellationToken ct)
    {
        // Telemetry identity is the OIDC subject (`sub`) — the same value the SPA puts in
        // `user_id`. Previously this was compared against the internal UserProfile.Id Guid
        // (which the browser never has) and the DTO typed `user_id` as a Guid, so every
        // ingest failed: a username sub could not bind to a Guid and all telemetry was
        // dropped with a 500.
        var subject = httpContext.User.FindFirstValue("sub")
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? string.Empty;

        var validation = await telemetry.ValidateAsync(request, subject, ct);
        if (!validation.IsValid)
        {
            return validation.IsForbidden && !validation.HasNonForbiddenErrors
                ? ProblemDetailsHelper.AuthorizationForbidden(TraceId(httpContext))
                : ProblemDetailsHelper.TelemetryValidationError(validation.Errors);
        }

        telemetry.WriteAcceptedEvents(request, subject, TraceId(httpContext));
        return Results.Accepted();
    }

    private static string TraceId(HttpContext httpContext) =>
        System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier;
}
