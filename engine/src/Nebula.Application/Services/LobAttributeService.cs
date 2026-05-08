using System.Text.Json;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Application.Services;

public class LobAttributeService(ILobSchemaRepository schemaRepository)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> CyberRevenueBands = new(StringComparer.Ordinal)
    {
        "0-10M",
        "10-50M",
        "50-250M",
        "250M+",
    };
    private static readonly HashSet<string> CyberMfaMaturity = new(StringComparer.Ordinal)
    {
        "Implemented",
        "Partial",
        "Planned",
    };
    private static readonly HashSet<string> CyberTrainingFrequency = new(StringComparer.Ordinal)
    {
        "Annual",
        "SemiAnnual",
        "Quarterly",
    };

    public async Task<LobAttributeStorageResult> ValidateAndSerializeAsync(
        LobAttributeEnvelopeDto? envelope,
        string? lineOfBusiness,
        CancellationToken ct = default)
    {
        if (envelope is null)
        {
            return new LobAttributeStorageResult(
                LobSchemaDefaults.EmptyAttributesJson,
                LobSchemaDefaults.ResolveDefaultProductVersionId(lineOfBusiness),
                []);
        }

        var errors = ValidateEnvelopeIdentity(envelope);
        if (errors.Count > 0)
            return new LobAttributeStorageResult(null, null, errors);

        if (IsUnspecifiedEmptyEnvelope(envelope) && string.IsNullOrWhiteSpace(lineOfBusiness))
        {
            return new LobAttributeStorageResult(
                LobSchemaDefaults.EmptyAttributesJson,
                LobSchemaDefaults.UnspecifiedProductVersionId,
                []);
        }

        if (string.IsNullOrWhiteSpace(lineOfBusiness))
        {
            return new LobAttributeStorageResult(null, null, [
                new LobValidationIssueDto(
                    "lob_required",
                    "$.lineOfBusiness",
                    "lineOfBusiness is required when LOB attributes are supplied.",
                    "error")
            ]);
        }

        if (!string.IsNullOrWhiteSpace(envelope.LineOfBusiness)
            && !string.Equals(envelope.LineOfBusiness, lineOfBusiness, StringComparison.Ordinal))
        {
            return new LobAttributeStorageResult(null, null, [
                new LobValidationIssueDto(
                    "lob_mismatch",
                    "$.lineOfBusiness",
                    "LOB attribute envelope lineOfBusiness does not match the record lineOfBusiness.",
                    "error")
            ]);
        }

        var bundle = await schemaRepository.GetActiveBundleAsync(
            envelope.ProductKey.Trim().ToLowerInvariant(),
            envelope.ProductVersion.Trim(),
            envelope.SchemaVersion.Trim(),
            lineOfBusiness,
            ct);

        if (bundle is null)
        {
            return new LobAttributeStorageResult(null, null, [
                new LobValidationIssueDto(
                    "lob_bundle_not_found",
                    "$",
                    "No active LOB schema bundle matches the product key, product version, schema version, and line of business.",
                    "error")
            ]);
        }

        if (!string.IsNullOrWhiteSpace(lineOfBusiness)
            && !string.Equals(bundle.LobProductVersion.LobProduct.LineOfBusiness, lineOfBusiness, StringComparison.Ordinal))
        {
            return new LobAttributeStorageResult(null, null, [
                new LobValidationIssueDto(
                    "lob_mismatch",
                    "$.lineOfBusiness",
                    "LOB attributes do not match the record lineOfBusiness.",
                    "error")
            ]);
        }

        errors.AddRange(ValidateAttributes(envelope));

        return errors.Count == 0
            ? new LobAttributeStorageResult(JsonSerializer.Serialize(envelope, JsonOptions), bundle.LobProductVersionId, [])
            : new LobAttributeStorageResult(null, null, errors);
    }

    public LobAttributeEnvelopeDto? Deserialize(string? attributesJson)
    {
        return DeserializeEnvelope(attributesJson);
    }

    public static LobAttributeEnvelopeDto? DeserializeEnvelope(string? attributesJson)
    {
        if (string.IsNullOrWhiteSpace(attributesJson))
            return null;

        try
        {
            var envelope = JsonSerializer.Deserialize<LobAttributeEnvelopeDto>(attributesJson, JsonOptions);
            return IsReadableEnvelope(envelope) ? envelope : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static List<LobValidationIssueDto> ValidateEnvelopeIdentity(LobAttributeEnvelopeDto envelope)
    {
        var errors = new List<LobValidationIssueDto>();

        AddRequiredString(errors, envelope.ProductKey, "$.productKey", "productKey");
        AddRequiredString(errors, envelope.ProductVersion, "$.productVersion", "productVersion");
        AddRequiredString(errors, envelope.SchemaVersion, "$.schemaVersion", "schemaVersion");

        if (envelope.Attributes.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new LobValidationIssueDto(
                "invalid_attributes",
                "$.attributes",
                "attributes must be a JSON object.",
                "error"));
        }

        return errors;
    }

    private static bool IsReadableEnvelope(LobAttributeEnvelopeDto? envelope) =>
        envelope is not null
        && !string.IsNullOrWhiteSpace(envelope.ProductKey)
        && !string.IsNullOrWhiteSpace(envelope.ProductVersion)
        && !string.IsNullOrWhiteSpace(envelope.SchemaVersion)
        && envelope.Attributes.ValueKind == JsonValueKind.Object;

    private static IReadOnlyList<LobValidationIssueDto> ValidateAttributes(LobAttributeEnvelopeDto envelope)
    {
        if (!string.Equals(envelope.ProductKey, "cyber", StringComparison.OrdinalIgnoreCase))
            return [];

        var errors = new List<LobValidationIssueDto>();
        var attributes = envelope.Attributes;

        var revenueBand = GetString(attributes, "revenueBand");
        if (string.IsNullOrWhiteSpace(revenueBand))
        {
            AddRequired(errors, "$.attributes.revenueBand", "revenueBand");
        }
        else if (!CyberRevenueBands.Contains(revenueBand))
        {
            errors.Add(new LobValidationIssueDto(
                "invalid_enum",
                "$.attributes.revenueBand",
                "revenueBand is not supported by the active Cyber schema bundle.",
                "error"));
        }

        var recordsHeld = GetInt64(attributes, "recordsHeld");
        if (!recordsHeld.HasValue)
            AddRequired(errors, "$.attributes.recordsHeld", "recordsHeld");
        else if (recordsHeld < 0)
            AddMinimum(errors, "$.attributes.recordsHeld", "recordsHeld", 0);

        if (!TryGetObject(attributes, "controls", out var controls))
        {
            AddRequired(errors, "$.attributes.controls", "controls");
        }
        else
        {
            var mfaEnabled = GetBoolean(controls, "mfaEnabled");
            var edrEnabled = GetBoolean(controls, "edrEnabled");
            var backupEnabled = GetBoolean(controls, "backupEnabled");
            var mfaMaturity = GetString(controls, "mfaMaturity");
            var trainingFrequency = GetString(controls, "trainingFrequency");

            if (!mfaEnabled.HasValue)
                AddRequired(errors, "$.attributes.controls.mfaEnabled", "mfaEnabled");
            if (!edrEnabled.HasValue)
                AddRequired(errors, "$.attributes.controls.edrEnabled", "edrEnabled");
            if (!backupEnabled.HasValue)
                AddRequired(errors, "$.attributes.controls.backupEnabled", "backupEnabled");

            if (mfaEnabled == true && string.IsNullOrWhiteSpace(mfaMaturity))
                AddRequired(errors, "$.attributes.controls.mfaMaturity", "mfaMaturity");
            else if (!string.IsNullOrWhiteSpace(mfaMaturity) && !CyberMfaMaturity.Contains(mfaMaturity))
                AddEnum(errors, "$.attributes.controls.mfaMaturity", "mfaMaturity");

            if (string.IsNullOrWhiteSpace(trainingFrequency))
                AddRequired(errors, "$.attributes.controls.trainingFrequency", "trainingFrequency");
            else if (!CyberTrainingFrequency.Contains(trainingFrequency))
                AddEnum(errors, "$.attributes.controls.trainingFrequency", "trainingFrequency");

            if (recordsHeld >= 1_000_000 && mfaEnabled != true)
            {
                errors.Add(new LobValidationIssueDto(
                    "mfa_required_for_high_record_count",
                    "$.attributes.controls.mfaEnabled",
                    "MFA is required when recordsHeld is at least 1,000,000.",
                    "error"));
            }
        }

        var requestedLimit = ValidateMoney(errors, attributes, "requestedLimit", requiredPositive: true);
        var requestedRetention = ValidateMoney(errors, attributes, "requestedRetention", requiredPositive: false);
        if (requestedLimit.HasValue && requestedLimit > 0 && requestedRetention.HasValue && requestedRetention < requestedLimit.Value / 100)
            errors.Add(new LobValidationIssueDto(
                "minimum_retention_not_met",
                "$.attributes.requestedRetention.amountMinor",
                "requestedRetention must be at least 1% of requestedLimit.",
                "error"));

        return errors;
    }

    private static void AddRequiredString(
        ICollection<LobValidationIssueDto> errors,
        string? value,
        string path,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            AddRequired(errors, path, field);
    }

    private static void AddRequired(ICollection<LobValidationIssueDto> errors, string path, string field) =>
        errors.Add(new LobValidationIssueDto("required", path, $"{field} is required.", "error"));

    private static void AddMinimum(ICollection<LobValidationIssueDto> errors, string path, string field, decimal minimum) =>
        errors.Add(new LobValidationIssueDto("minimum", path, $"{field} must be greater than or equal to {minimum}.", "error"));

    private static void AddEnum(ICollection<LobValidationIssueDto> errors, string path, string field) =>
        errors.Add(new LobValidationIssueDto("invalid_enum", path, $"{field} is not supported by the active Cyber schema bundle.", "error"));

    private static string? GetString(JsonElement obj, string propertyName)
    {
        return obj.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static long? GetInt64(JsonElement obj, string propertyName)
    {
        return obj.TryGetProperty(propertyName, out var property) && property.TryGetInt64(out var value)
            ? value
            : null;
    }

    private static bool? GetBoolean(JsonElement obj, string propertyName)
    {
        return obj.TryGetProperty(propertyName, out var property) && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? property.GetBoolean()
            : null;
    }

    private static bool TryGetObject(JsonElement obj, string propertyName, out JsonElement value)
    {
        if (obj.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Object)
            return true;

        value = default;
        return false;
    }

    private static long? ValidateMoney(
        ICollection<LobValidationIssueDto> errors,
        JsonElement attributes,
        string propertyName,
        bool requiredPositive)
    {
        var path = $"$.attributes.{propertyName}";
        if (!TryGetObject(attributes, propertyName, out var money))
        {
            AddRequired(errors, path, propertyName);
            return null;
        }

        var amountMinor = GetInt64(money, "amountMinor");
        var currency = GetString(money, "currency");
        if (!amountMinor.HasValue)
        {
            AddRequired(errors, $"{path}.amountMinor", "amountMinor");
        }
        else if (requiredPositive && amountMinor <= 0)
        {
            AddMinimum(errors, $"{path}.amountMinor", "amountMinor", 1);
        }
        else if (!requiredPositive && amountMinor < 0)
        {
            AddMinimum(errors, $"{path}.amountMinor", "amountMinor", 0);
        }

        if (string.IsNullOrWhiteSpace(currency))
            AddRequired(errors, $"{path}.currency", "currency");
        else if (!string.Equals(currency, "USD", StringComparison.Ordinal))
            AddEnum(errors, $"{path}.currency", "currency");

        return amountMinor;
    }

    private static bool IsUnspecifiedEmptyEnvelope(LobAttributeEnvelopeDto envelope) =>
        string.Equals(envelope.ProductKey, "_unspecified", StringComparison.Ordinal)
        && string.Equals(envelope.ProductVersion, "0.0.0", StringComparison.Ordinal)
        && string.Equals(envelope.SchemaVersion, "0.0.0", StringComparison.Ordinal)
        && envelope.Attributes.ValueKind == JsonValueKind.Object
        && !envelope.Attributes.EnumerateObject().Any();
}
