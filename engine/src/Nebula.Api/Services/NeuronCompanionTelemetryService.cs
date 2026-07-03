using Nebula.Api.Models;

namespace Nebula.Api.Services;

public sealed class NeuronCompanionTelemetryValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public bool IsForbidden { get; set; }
    public bool HasNonForbiddenErrors { get; private set; }
    public Dictionary<string, string[]> Errors { get; } = new();

    public void Add(string path, string message, bool forbidden = false)
    {
        if (!forbidden)
            HasNonForbiddenErrors = true;

        Errors[path] = Errors.TryGetValue(path, out var existing)
            ? [.. existing, message]
            : [message];
    }
}

/// <summary>
/// F0038-S0008 — validates + records Neuron companion telemetry. Modeled on the F0035
/// SessionContinuity ingest (ADR-024): the telemetry identity is the OIDC subject, and an
/// event's <c>user_id</c> must match the authenticated caller (a client cannot log another
/// user's telemetry). Fire-and-forget: accepted events are written to Serilog and never
/// touch a CRM table.
/// </summary>
public sealed class NeuronCompanionTelemetryService
{
    private const int MaxEvents = 50;

    private static readonly HashSet<string> EventNames =
    [
        NeuronCompanionEventNames.NeedsAttentionSurfaced,
        NeuronCompanionEventNames.DraftReady,
        NeuronCompanionEventNames.CompanionDailyActive,
        NeuronCompanionEventNames.AttentionRenewalActioned,
        NeuronCompanionEventNames.DraftGenerated,
        NeuronCompanionEventNames.MockSent,
    ];

    private static readonly HashSet<string> Personas =
        new(StringComparer.Ordinal) { "underwriter", "distribution" };

    private readonly ILogger _logger;

    public NeuronCompanionTelemetryService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("Nebula.Neuron.Companion");
    }

    public NeuronCompanionTelemetryValidationResult Validate(
        NeuronCompanionTelemetryRequest request,
        string currentUserSubject)
    {
        var result = new NeuronCompanionTelemetryValidationResult();
        var events = request.Events;

        if (events is null || events.Count == 0)
        {
            result.Add("events", "At least one telemetry event is required.");
            return result;
        }

        if (events.Count > MaxEvents)
            result.Add("events", $"No more than {MaxEvents} telemetry events are accepted per request.");

        for (var i = 0; i < events.Count; i++)
            ValidateEvent(events[i], i, currentUserSubject, result);

        return result;
    }

    public void WriteAcceptedEvents(
        NeuronCompanionTelemetryRequest request,
        string currentUserSubject,
        string traceId)
    {
        if (request.Events is null)
            return;

        foreach (var e in request.Events)
        {
            // PII boundary: the DTO is a closed shape — only the metric name, correlation
            // ids, and persona are logged. No draft body / raw prompt / credential can be
            // carried here (there is no free-form payload).
            _logger.LogInformation(
                "Neuron companion event accepted {EventName} {EventVersion} {EventTimestamp} {UserId} {ThreadId} {RenewalId} {Persona} {TraceId}",
                e.EventName,
                e.EventVersion,
                e.Timestamp,
                currentUserSubject,
                e.ThreadId,
                e.RenewalId,
                e.Persona,
                traceId);
        }
    }

    private static void ValidateEvent(
        NeuronCompanionTelemetryEventDto e,
        int index,
        string currentUserSubject,
        NeuronCompanionTelemetryValidationResult result)
    {
        var prefix = $"events[{index}]";

        if (string.IsNullOrWhiteSpace(e.EventName) || !EventNames.Contains(e.EventName))
        {
            result.Add($"{prefix}.event_name", "Event name is not in the Neuron companion event registry.");
            return;
        }

        if (e.EventVersion < 1)
            result.Add($"{prefix}.event_version", "Event version must be at least 1.");

        if (string.IsNullOrWhiteSpace(e.UserId))
            result.Add($"{prefix}.user_id", "User ID is required.");
        else if (!string.Equals(e.UserId, currentUserSubject, StringComparison.Ordinal))
        {
            result.IsForbidden = true;
            result.Add(
                $"{prefix}.user_id",
                "Telemetry user_id must match the authenticated user.",
                forbidden: true);
        }

        // Conditional requireds mirror the schema's allOf: the two primary timestamps must
        // correlate per renewal + thread; DAU must carry the renewal-owner persona.
        if (e.EventName is NeuronCompanionEventNames.NeedsAttentionSurfaced
            or NeuronCompanionEventNames.DraftReady)
        {
            if (string.IsNullOrWhiteSpace(e.RenewalId))
                result.Add($"{prefix}.renewal_id", "renewal_id is required to correlate the baseline metric.");
            if (string.IsNullOrWhiteSpace(e.ThreadId))
                result.Add($"{prefix}.thread_id", "thread_id is required to correlate the baseline metric.");
        }

        if (e.EventName == NeuronCompanionEventNames.CompanionDailyActive
            && string.IsNullOrWhiteSpace(e.Persona))
        {
            result.Add($"{prefix}.persona", "persona is required for companion-daily-active.");
        }

        if (!string.IsNullOrWhiteSpace(e.Persona) && !Personas.Contains(e.Persona))
            result.Add($"{prefix}.persona", "persona must be 'underwriter' or 'distribution'.");
    }
}
