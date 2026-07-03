using System.Text.Json.Serialization;

namespace Nebula.Application.DTOs;

// F0038-S0003 — per-renewal drill context for the Neuron companion. camelCase (matches
// the nebula-api.yaml companion-context response). additionalProperties:false there, so
// only these fields are emitted; the optional contactName is omitted when unknown.
public record RenewalCompanionContextDto(
    string RenewalId,
    string AccountName,
    string WorkflowState,
    string? BrokerName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ContactName,
    string? ExpiryDate,
    bool CanDraftOutreach,
    IReadOnlyList<TimelineEventDto> Timeline);
