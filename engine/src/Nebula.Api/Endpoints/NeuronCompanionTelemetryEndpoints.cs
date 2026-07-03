using System.Security.Claims;
using Nebula.Api.Helpers;
using Nebula.Api.Models;
using Nebula.Api.Services;

namespace Nebula.Api.Endpoints;

public static class NeuronCompanionTelemetryEndpoints
{
    public static IEndpointRouteBuilder MapNeuronCompanionTelemetryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/internal/telemetry")
            .WithTags("NeuronCompanion")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        group.MapPost("/neuron-companion", Ingest);

        return app;
    }

    internal static IResult Ingest(
        NeuronCompanionTelemetryRequest request,
        NeuronCompanionTelemetryService telemetry,
        HttpContext httpContext)
    {
        // Telemetry identity is the OIDC subject (`sub`) — the same value Neuron forwards as
        // user_id. It is treated as a string (not a Guid): the browser/Neuron never has the
        // internal UserProfile.Id, and typing it as a Guid is what dropped every F0035 event.
        var subject = httpContext.User.FindFirstValue("sub")
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? string.Empty;

        var validation = telemetry.Validate(request, subject);
        if (!validation.IsValid)
        {
            return validation.IsForbidden && !validation.HasNonForbiddenErrors
                ? ProblemDetailsHelper.AuthorizationForbidden(TraceId(httpContext))
                : ProblemDetailsHelper.NeuronCompanionTelemetryValidationError(validation.Errors);
        }

        telemetry.WriteAcceptedEvents(request, subject, TraceId(httpContext));
        return Results.Accepted();
    }

    private static string TraceId(HttpContext httpContext) =>
        System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier;
}
