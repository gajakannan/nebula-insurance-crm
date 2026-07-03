using Microsoft.Extensions.Logging.Abstractions;
using Nebula.Api.Models;
using Nebula.Api.Services;
using Shouldly;

namespace Nebula.Tests.Unit.Neuron;

/// <summary>F0038-S0008 — companion telemetry ingest validation (pure; no DB/Docker).</summary>
public class NeuronCompanionTelemetryServiceTests
{
    private const string Subject = "user-abc";
    private static readonly NeuronCompanionTelemetryService Service = new(NullLoggerFactory.Instance);

    private static NeuronCompanionTelemetryRequest Req(params NeuronCompanionTelemetryEventDto[] events) =>
        new(events);

    private static NeuronCompanionTelemetryEventDto Surfaced(string userId, string? renewalId = "r1", string? threadId = "t1") =>
        new("needs-attention-surfaced", 1, DateTimeOffset.UtcNow, userId, threadId, renewalId, null);

    [Fact]
    public void ValidBatch_AllSixEventTypes_IsValid()
    {
        var req = Req(
            new("needs-attention-surfaced", 1, DateTimeOffset.UtcNow, Subject, "t1", "r1", null),
            new("draft-ready", 1, DateTimeOffset.UtcNow, Subject, "t1", "r1", null),
            new("companion-daily-active", 1, DateTimeOffset.UtcNow, Subject, null, null, "underwriter"),
            new("draft-generated", 1, DateTimeOffset.UtcNow, Subject, "t1", "r1", null),
            new("mock-sent", 1, DateTimeOffset.UtcNow, Subject, "t1", "r1", null),
            new("attention-renewal-actioned", 1, DateTimeOffset.UtcNow, Subject, "t1", "r1", null));

        Service.Validate(req, Subject).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void PrimaryTimestamps_CorrelatePerRenewalAndThread()
    {
        // The baseline metric requires both primaries to carry the same renewal_id + thread_id.
        var start = new NeuronCompanionTelemetryEventDto("needs-attention-surfaced", 1, DateTimeOffset.UtcNow, Subject, "t1", "r1", null);
        var end = new NeuronCompanionTelemetryEventDto("draft-ready", 1, DateTimeOffset.UtcNow, Subject, "t1", "r1", null);

        Service.Validate(Req(start, end), Subject).IsValid.ShouldBeTrue();
        start.RenewalId.ShouldBe(end.RenewalId);
        start.ThreadId.ShouldBe(end.ThreadId);
    }

    [Fact]
    public void UserIdMismatch_IsForbidden_NotAPlainValidationError()
    {
        // A client cannot log another user's telemetry — the endpoint maps this to 403.
        var result = Service.Validate(Req(Surfaced("someone-else")), Subject);

        result.IsValid.ShouldBeFalse();
        result.IsForbidden.ShouldBeTrue();
        result.HasNonForbiddenErrors.ShouldBeFalse();
    }

    [Fact]
    public void UnknownEventName_IsRejected()
    {
        var bad = new NeuronCompanionTelemetryEventDto("totally-made-up", 1, DateTimeOffset.UtcNow, Subject, "t1", "r1", null);

        var result = Service.Validate(Req(bad), Subject);

        result.IsValid.ShouldBeFalse();
        result.Errors.Keys.ShouldContain("events[0].event_name");
    }

    [Fact]
    public void PrimaryEvent_MissingRenewalId_IsRejected()
    {
        var result = Service.Validate(Req(Surfaced(Subject, renewalId: null)), Subject);

        result.IsValid.ShouldBeFalse();
        result.Errors.Keys.ShouldContain("events[0].renewal_id");
    }

    [Fact]
    public void DailyActive_RequiresPersona()
    {
        var noPersona = new NeuronCompanionTelemetryEventDto("companion-daily-active", 1, DateTimeOffset.UtcNow, Subject, null, null, null);

        var result = Service.Validate(Req(noPersona), Subject);

        result.IsValid.ShouldBeFalse();
        result.Errors.Keys.ShouldContain("events[0].persona");
    }

    [Fact]
    public void Persona_MustBeUnderwriterOrDistribution()
    {
        var invalid = new NeuronCompanionTelemetryEventDto("companion-daily-active", 1, DateTimeOffset.UtcNow, Subject, null, null, "ceo");
        Service.Validate(Req(invalid), Subject).IsValid.ShouldBeFalse();

        var valid = new NeuronCompanionTelemetryEventDto("companion-daily-active", 1, DateTimeOffset.UtcNow, Subject, null, null, "distribution");
        Service.Validate(Req(valid), Subject).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void EventVersion_MustBeAtLeastOne()
    {
        var bad = new NeuronCompanionTelemetryEventDto("mock-sent", 0, DateTimeOffset.UtcNow, Subject, "t1", "r1", null);

        Service.Validate(Req(bad), Subject).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void EmptyOrNullBatch_IsRejected()
    {
        Service.Validate(new NeuronCompanionTelemetryRequest([]), Subject).IsValid.ShouldBeFalse();
        Service.Validate(new NeuronCompanionTelemetryRequest(null), Subject).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void OverBatchLimit_IsRejected()
    {
        var many = Enumerable.Range(0, 51)
            .Select(_ => new NeuronCompanionTelemetryEventDto("draft-generated", 1, DateTimeOffset.UtcNow, Subject, "t1", "r1", null))
            .ToArray();

        Service.Validate(new NeuronCompanionTelemetryRequest(many), Subject).IsValid.ShouldBeFalse();
    }
}
