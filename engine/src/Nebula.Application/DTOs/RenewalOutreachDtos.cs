using System.Text.Json.Serialization;

namespace Nebula.Application.DTOs;

// F0038-S0005/S0006 — outreach draft + mock-send. Requests are snake_case (Neuron-facing,
// per renewal-outreach-*-request.schema.json); responses are camelCase (nebula-api.yaml).
public record OutreachProvenanceDto(
    [property: JsonPropertyName("actor_user_id")] string ActorUserId,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("prompt_id")] string PromptId,
    [property: JsonPropertyName("prompt_version")] string PromptVersion,
    [property: JsonPropertyName("content_hash")] string ContentHash,
    [property: JsonPropertyName("agent_run_id")] string? AgentRunId = null);

public record RenewalOutreachDraftRequestDto(
    [property: JsonPropertyName("draft_body")] string DraftBody,
    [property: JsonPropertyName("provenance")] OutreachProvenanceDto Provenance,
    [property: JsonPropertyName("internal_only")] bool? InternalOnly = null,
    [property: JsonPropertyName("label")] string? Label = null);

public record RenewalOutreachMockSendRequestDto(
    [property: JsonPropertyName("final_draft_body")] string FinalDraftBody,
    [property: JsonPropertyName("provenance")] OutreachProvenanceDto Provenance,
    [property: JsonPropertyName("draft_timeline_event_id")] string? DraftTimelineEventId = null,
    [property: JsonPropertyName("simulate_delivery")] bool? SimulateDelivery = null);

public record RenewalOutreachDraftResponseDto(
    Guid TimelineEventId,
    Guid RenewalId,
    bool InternalOnly);

public record RenewalOutreachMockSendResponseDto(
    WorkflowTransitionRecordDto Transition,
    Guid SimulatedSendEventId);
