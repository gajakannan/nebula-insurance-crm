using System.Text.Json;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;
using Shouldly;

namespace Nebula.Tests.Unit.LobAttributes;

public class LobAttributeServiceTests
{
    private static readonly Guid ProductVersionId = LobSchemaDefaults.CyberProductVersionId;

    [Fact]
    public async Task ValidateAndSerializeAsync_accepts_valid_cyber_envelope()
    {
        var service = new LobAttributeService(new FakeLobSchemaRepository());
        var envelope = CyberEnvelope(
            """
            {
              "revenueBand": "10-50M",
              "recordsHeld": 250000,
              "controls": {
                "mfaEnabled": true,
                "mfaMaturity": "Implemented",
                "edrEnabled": true,
                "backupEnabled": true,
                "trainingFrequency": "Quarterly"
              },
              "requestedLimit": { "amountMinor": 500000000, "currency": "USD" },
              "requestedRetention": { "amountMinor": 5000000, "currency": "USD" }
            }
            """);

        var result = await service.ValidateAndSerializeAsync(envelope, "Cyber");

        result.IsValid.ShouldBeTrue();
        result.LobProductVersionId.ShouldBe(ProductVersionId);
        result.AttributesJson.ShouldNotBeNull();
    }

    [Fact]
    public async Task ValidateAndSerializeAsync_requires_mfa_for_high_record_count()
    {
        var service = new LobAttributeService(new FakeLobSchemaRepository());
        var envelope = CyberEnvelope(
            """
            {
              "revenueBand": "50-250M",
              "recordsHeld": 1000000,
              "controls": {
                "mfaEnabled": false,
                "edrEnabled": true,
                "backupEnabled": true,
                "trainingFrequency": "Quarterly"
              },
              "requestedLimit": { "amountMinor": 500000000, "currency": "USD" },
              "requestedRetention": { "amountMinor": 5000000, "currency": "USD" }
            }
            """);

        var result = await service.ValidateAndSerializeAsync(envelope, "Cyber");

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Code == "mfa_required_for_high_record_count");
    }

    [Fact]
    public async Task ValidateAndSerializeAsync_enforces_minimum_retention_rule()
    {
        var service = new LobAttributeService(new FakeLobSchemaRepository());
        var envelope = CyberEnvelope(
            """
            {
              "revenueBand": "50-250M",
              "recordsHeld": 100,
              "controls": {
                "mfaEnabled": true,
                "mfaMaturity": "Implemented",
                "edrEnabled": true,
                "backupEnabled": true,
                "trainingFrequency": "Quarterly"
              },
              "requestedLimit": { "amountMinor": 500000000, "currency": "USD" },
              "requestedRetention": { "amountMinor": 1000, "currency": "USD" }
            }
            """);

        var result = await service.ValidateAndSerializeAsync(envelope, "Cyber");

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.Code == "minimum_retention_not_met");
    }

    [Fact]
    public async Task ValidateAndSerializeAsync_pins_default_legacy_version_when_attributes_are_not_supplied()
    {
        var service = new LobAttributeService(new FakeLobSchemaRepository());

        var result = await service.ValidateAndSerializeAsync(null, "GeneralLiability");

        result.IsValid.ShouldBeTrue();
        result.RequiredAttributesJson.ShouldBe(LobSchemaDefaults.EmptyAttributesJson);
        result.RequiredLobProductVersionId.ShouldBe(LobSchemaDefaults.LegacyProductVersionIds["GeneralLiability"]);
    }

    [Fact]
    public void Deserialize_returns_null_for_legacy_empty_attributes_json()
    {
        var service = new LobAttributeService(new FakeLobSchemaRepository());

        service.Deserialize("{}").ShouldBeNull();
    }

    [Fact]
    public void Deserialize_returns_null_for_incomplete_envelopes()
    {
        var service = new LobAttributeService(new FakeLobSchemaRepository());

        service.Deserialize("""{"productKey":"cyber","productVersion":"1.0.0","schemaVersion":"1.0.0"}""")
            .ShouldBeNull();
    }

    [Fact]
    public void Deserialize_returns_valid_envelopes()
    {
        var service = new LobAttributeService(new FakeLobSchemaRepository());
        var envelope = CyberEnvelope(
            """
            {
              "revenueBand": "10-50M",
              "recordsHeld": 250000,
              "controls": {
                "mfaEnabled": true,
                "mfaMaturity": "Implemented",
                "edrEnabled": true,
                "backupEnabled": true,
                "trainingFrequency": "Quarterly"
              },
              "requestedLimit": { "amountMinor": 500000000, "currency": "USD" },
              "requestedRetention": { "amountMinor": 5000000, "currency": "USD" }
            }
            """);

        var result = service.Deserialize(JsonSerializer.Serialize(envelope, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        result.ShouldNotBeNull();
        result.ProductKey.ShouldBe("cyber");
    }

    private static LobAttributeEnvelopeDto CyberEnvelope(string attributesJson) => new(
        "cyber",
        "1.0.0",
        "1.0.0",
        "Cyber",
        JsonSerializer.Deserialize<JsonElement>(attributesJson));

    private sealed class FakeLobSchemaRepository : ILobSchemaRepository
    {
        private static readonly LobSchemaBundle Bundle = new()
        {
            Id = Guid.Parse("34000000-0000-0000-0000-000000000201"),
            LobProductVersionId = ProductVersionId,
            SchemaVersion = "1.0.0",
            Status = "Active",
            ContentHash = "sha256:test",
            LobProductVersion = new LobProductVersion
            {
                Id = ProductVersionId,
                Version = "1.0.0",
                Status = "Active",
                LobProduct = new LobProduct
                {
                    ProductKey = "cyber",
                    LineOfBusiness = "Cyber",
                    Name = "Cyber Liability",
                    Status = "Active",
                },
            },
        };

        public Task<IReadOnlyList<LobSchemaBundle>> ListBundlesAsync(
            string? productKey,
            string? lineOfBusiness,
            bool activeOnly,
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<LobSchemaBundle>>([Bundle]);

        public Task<LobSchemaBundle?> GetBundleByIdAsync(Guid bundleId, bool track, CancellationToken ct = default) =>
            Task.FromResult<LobSchemaBundle?>(bundleId == Bundle.Id ? Bundle : null);

        public Task<LobSchemaBundle?> GetBundleByProductVersionIdAsync(Guid productVersionId, bool track, CancellationToken ct = default) =>
            Task.FromResult<LobSchemaBundle?>(productVersionId == ProductVersionId ? Bundle : null);

        public Task<LobSchemaBundle?> GetActiveBundleAsync(
            string productKey,
            string productVersion,
            string schemaVersion,
            string? lineOfBusiness,
            CancellationToken ct = default) =>
            Task.FromResult<LobSchemaBundle?>(
                productKey == "cyber"
                && productVersion == "1.0.0"
                && schemaVersion == "1.0.0"
                && (lineOfBusiness is null or "Cyber")
                    ? Bundle
                    : null);

        public Task DeactivateActiveBundlesAsync(
            Guid productVersionId,
            Guid exceptBundleId,
            DateTime now,
            Guid actorUserId,
            CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task AddActivationEventAsync(LobBundleActivationEvent activationEvent, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
