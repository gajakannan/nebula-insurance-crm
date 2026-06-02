using System.Text.Json;
using Nebula.Api.Models;

namespace Nebula.Api.Services;

public sealed class SessionTelemetryValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public bool IsForbidden { get; set; }
    public bool HasNonForbiddenErrors { get; private set; }
    public Dictionary<string, string[]> Errors { get; } = new();

    public void Add(string path, string message, bool forbidden = false)
    {
        if (!forbidden)
            HasNonForbiddenErrors = true;

        if (Errors.TryGetValue(path, out var existing))
        {
            Errors[path] = [.. existing, message];
            return;
        }

        Errors[path] = [message];
    }
}

public sealed class SessionContinuityTelemetryService
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    private static readonly HashSet<string> EventNames =
    [
        SessionContinuityEventNames.SilentRenewalSuccess,
        SessionContinuityEventNames.SilentRenewalFail,
        SessionContinuityEventNames.ForcedRedirect,
        SessionContinuityEventNames.IdleWarningShown,
        SessionContinuityEventNames.IdleWarningAccepted,
        SessionContinuityEventNames.IdleWarningDismissed,
        SessionContinuityEventNames.AuthClassifierFallback,
        SessionContinuityEventNames.AuthClassifierConflict,
        SessionContinuityEventNames.FormSnapshotSkipped,
    ];

    private static readonly HashSet<string> ForbiddenPayloadKeys = new(KeyComparer)
    {
        "email",
        "name",
        "ip",
        "access_token",
        "refresh_token",
        "id_token",
        "broker_tenant_id",
        "roles",
        "claims",
        "form_values",
        "query",
    };

    private static readonly Dictionary<string, HashSet<string>> AllowedPayloadKeys = new(KeyComparer)
    {
        [SessionContinuityEventNames.SilentRenewalSuccess] = Keys("coalesced_request_count", "renewal_duration_ms", "original_request_count_after_retry"),
        [SessionContinuityEventNames.SilentRenewalFail] = Keys("cause", "coalesced_request_count"),
        [SessionContinuityEventNames.ForcedRedirect] = Keys("cause", "route_at_redirect"),
        [SessionContinuityEventNames.IdleWarningShown] = Keys("route_at_warning"),
        [SessionContinuityEventNames.IdleWarningAccepted] = Keys("time_remaining_ms"),
        [SessionContinuityEventNames.IdleWarningDismissed] = Keys("dismissal_action"),
        [SessionContinuityEventNames.AuthClassifierFallback] = Keys("endpoint_route", "response_status"),
        [SessionContinuityEventNames.AuthClassifierConflict] = Keys("endpoint_route", "www_authenticate_class", "problem_details_type"),
        [SessionContinuityEventNames.FormSnapshotSkipped] = Keys("cause", "route"),
    };

    private static readonly Dictionary<string, HashSet<string>> RequiredPayloadKeys = new(KeyComparer)
    {
        [SessionContinuityEventNames.SilentRenewalSuccess] = Keys("coalesced_request_count", "renewal_duration_ms"),
        [SessionContinuityEventNames.SilentRenewalFail] = Keys("cause"),
        [SessionContinuityEventNames.ForcedRedirect] = Keys("cause"),
        [SessionContinuityEventNames.IdleWarningAccepted] = Keys("time_remaining_ms"),
        [SessionContinuityEventNames.IdleWarningDismissed] = Keys("dismissal_action"),
        [SessionContinuityEventNames.FormSnapshotSkipped] = Keys("cause"),
    };

    private readonly ILogger _logger;

    public SessionContinuityTelemetryService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("Nebula.Session.Continuity");
    }

    public Task<SessionTelemetryValidationResult> ValidateAsync(
        SessionContinuityTelemetryRequest request,
        string currentUserSubject,
        CancellationToken ct)
    {
        _ = ct;
        var result = new SessionTelemetryValidationResult();
        var events = request.Events;

        if (events is null || events.Count == 0)
        {
            result.Add("events", "At least one telemetry event is required.");
            return Task.FromResult(result);
        }

        if (events.Count > 10)
            result.Add("events", "No more than 10 telemetry events are accepted per request.");

        for (var i = 0; i < events.Count; i++)
            ValidateEvent(events[i], i, currentUserSubject, result);

        return Task.FromResult(result);
    }

    public void WriteAcceptedEvents(
        SessionContinuityTelemetryRequest request,
        string currentUserSubject,
        string traceId)
    {
        if (request.Events is null)
            return;

        foreach (var item in request.Events)
        {
            _logger.LogInformation(
                "Session continuity event accepted {EventName} {EventVersion} {EventTimestamp} {UserId} {SessionId} {TraceId} {Payload}",
                item.EventName,
                item.EventVersion,
                item.Timestamp,
                currentUserSubject,
                item.SessionId,
                traceId,
                item.Payload is null ? null : PayloadForLog(item.Payload));
        }
    }

    private static void ValidateEvent(
        SessionContinuityEventDto item,
        int index,
        string currentUserSubject,
        SessionTelemetryValidationResult result)
    {
        var prefix = $"events[{index}]";
        if (string.IsNullOrWhiteSpace(item.EventName) || !EventNames.Contains(item.EventName))
        {
            result.Add($"{prefix}.event_name", "Event name is not in the session continuity event registry.");
            return;
        }

        if (item.EventVersion < 1)
            result.Add($"{prefix}.event_version", "Event version must be at least 1.");

        if (string.IsNullOrWhiteSpace(item.UserId))
            result.Add($"{prefix}.user_id", "User ID is required.");
        else if (!string.Equals(item.UserId, currentUserSubject, StringComparison.Ordinal))
        {
            result.IsForbidden = true;
            result.Add(
                $"{prefix}.user_id",
                "Telemetry user_id must match the authenticated user.",
                forbidden: true);
        }

        if (string.IsNullOrWhiteSpace(item.SessionId))
            result.Add($"{prefix}.session_id", "Session ID is required.");
        else if (item.SessionId.Length > 128)
            result.Add($"{prefix}.session_id", "Session ID must be 128 characters or fewer.");

        ValidatePayload(item.EventName, item.Payload, prefix, result);
    }

    private static void ValidatePayload(
        string eventName,
        Dictionary<string, JsonElement>? payload,
        string prefix,
        SessionTelemetryValidationResult result)
    {
        var allowed = AllowedPayloadKeys[eventName];
        var required = RequiredPayloadKeys.GetValueOrDefault(eventName) ?? [];
        payload ??= [];

        foreach (var requiredKey in required)
        {
            if (!payload.ContainsKey(requiredKey))
                result.Add($"{prefix}.payload.{requiredKey}", "Required payload key is missing.");
        }

        foreach (var (key, value) in payload)
        {
            if (ForbiddenPayloadKeys.Contains(key))
            {
                result.Add($"{prefix}.payload.{key}", "Payload key is forbidden by the session telemetry PII boundary.");
                continue;
            }

            if (!allowed.Contains(key))
            {
                result.Add($"{prefix}.payload.{key}", "Payload key is not allowed for this event type.");
                continue;
            }

            if (key.Contains("route", StringComparison.OrdinalIgnoreCase) && value.ValueKind == JsonValueKind.String)
            {
                var route = value.GetString();
                if (!string.IsNullOrEmpty(route) && route.Contains('?', StringComparison.Ordinal))
                    result.Add($"{prefix}.payload.{key}", "Route payload values must not include query strings.");
            }
        }
    }

    private static Dictionary<string, object?> PayloadForLog(Dictionary<string, JsonElement> payload)
    {
        var values = new Dictionary<string, object?>(KeyComparer);
        foreach (var (key, value) in payload)
        {
            values[key] = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number when value.TryGetInt64(out var number) => number,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => value.ToString(),
            };
        }

        return values;
    }

    private static HashSet<string> Keys(params string[] keys) => new(keys, KeyComparer);
}
