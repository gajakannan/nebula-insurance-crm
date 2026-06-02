using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nebula.Api.Models;

public sealed record SessionContinuityTelemetryRequest(
    [property: JsonPropertyName("events")] IReadOnlyList<SessionContinuityEventDto>? Events);

public sealed record SessionContinuityEventDto(
    [property: JsonPropertyName("event_name")] string? EventName,
    [property: JsonPropertyName("event_version")] int EventVersion,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("user_id")] string? UserId,
    [property: JsonPropertyName("session_id")] string? SessionId,
    [property: JsonPropertyName("payload")] Dictionary<string, JsonElement>? Payload);

public static class SessionContinuityEventNames
{
    public const string SilentRenewalSuccess = "silent-renewal-success";
    public const string SilentRenewalFail = "silent-renewal-fail";
    public const string ForcedRedirect = "forced-redirect";
    public const string IdleWarningShown = "idle-warning-shown";
    public const string IdleWarningAccepted = "idle-warning-accepted";
    public const string IdleWarningDismissed = "idle-warning-dismissed";
    public const string AuthClassifierFallback = "auth-classifier-fallback";
    public const string AuthClassifierConflict = "auth-classifier-conflict";
    public const string FormSnapshotSkipped = "form-snapshot-skipped";
}
