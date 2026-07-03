using System.Text.Json.Serialization;

namespace Nebula.Api.Models;

/// <summary>
/// F0038-S0008 — a batch of Neuron companion telemetry events (neuron-api.yaml
/// <c>ingestNeuronCompanionTelemetry</c>). Fire-and-forget: accepted events are written to
/// Serilog; failures never affect the companion flow.
/// </summary>
public sealed record NeuronCompanionTelemetryRequest(
    [property: JsonPropertyName("events")] IReadOnlyList<NeuronCompanionTelemetryEventDto>? Events);

/// <summary>
/// One companion telemetry event (neuron-companion-telemetry-event.schema.json). The shape
/// is <b>closed</b> — there is no free-form payload dictionary — so the PII boundary is
/// structural: it can only ever carry the metric name, correlation ids, and persona. A draft
/// body, raw prompt, credential, email, name, or IP has nowhere to bind and is dropped on
/// deserialize.
/// </summary>
public sealed record NeuronCompanionTelemetryEventDto(
    [property: JsonPropertyName("event_name")] string? EventName,
    [property: JsonPropertyName("event_version")] int EventVersion,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("user_id")] string? UserId,
    [property: JsonPropertyName("thread_id")] string? ThreadId,
    [property: JsonPropertyName("renewal_id")] string? RenewalId,
    [property: JsonPropertyName("persona")] string? Persona);

public static class NeuronCompanionEventNames
{
    // Primary metric (brackets the "needs-attention -> draft-ready" baseline duration).
    public const string NeedsAttentionSurfaced = "needs-attention-surfaced";
    public const string DraftReady = "draft-ready";

    // Minimal secondary v1 set (edit-distance + compliance tracking are deferred).
    public const string CompanionDailyActive = "companion-daily-active";
    public const string AttentionRenewalActioned = "attention-renewal-actioned";
    public const string DraftGenerated = "draft-generated";
    public const string MockSent = "mock-sent";
}
