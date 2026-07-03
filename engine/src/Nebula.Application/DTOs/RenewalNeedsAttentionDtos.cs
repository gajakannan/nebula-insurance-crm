using System.Text.Json.Serialization;

namespace Nebula.Application.DTOs;

// F0038-S0003 — the Neuron Renewals-zone "needs attention" read.
// Snake_case JSON per renewal-needs-attention-item.schema.json (the companion-facing
// contract Neuron consumes). NOTE: this deviates from the engine's usual camelCase
// convention; it conforms to the architect-authored schema the OpenAPI references.
public record RenewalNeedsAttentionItemDto(
    [property: JsonPropertyName("renewal_id")] string RenewalId,
    [property: JsonPropertyName("account_name")] string AccountName,
    [property: JsonPropertyName("expiry_date")] string ExpiryDate,
    [property: JsonPropertyName("days_to_expiry")] int DaysToExpiry,
    [property: JsonPropertyName("workflow_state")] string WorkflowState,
    [property: JsonPropertyName("last_broker_contact_at")] string? LastBrokerContactAt,
    [property: JsonPropertyName("no_contact_flag")] bool NoContactFlag,
    [property: JsonPropertyName("broker_name")] string BrokerName,
    [property: JsonPropertyName("can_draft_outreach")] bool CanDraftOutreach);

public record RenewalNeedsAttentionResponse(
    [property: JsonPropertyName("data")] IReadOnlyList<RenewalNeedsAttentionItemDto> Data);

// Repository -> service row: the scoped renewal's needs-attention fields plus its most-
// recent broker-contact timestamp (derived from timeline events; null when none recorded).
public record RenewalNeedsAttentionRow(
    Guid RenewalId,
    string AccountName,
    DateTime PolicyExpirationDate,
    string WorkflowState,
    string BrokerName,
    DateTime? LastBrokerContactAt);
