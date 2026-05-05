using System.Text.Json;

namespace Nebula.Application.Interfaces;

public interface IDocumentConfigurationProvider
{
    Task<DocumentConfigurationSnapshot> GetSnapshotAsync(CancellationToken ct = default);
}

public sealed record DocumentConfigurationSnapshot(
    IReadOnlySet<string> TaxonomyTypes,
    DocumentRetentionPolicy Retention,
    IReadOnlyList<DocumentClassificationPolicyRow> ClassificationPolicy,
    DocumentMetadataSchemaRegistry MetadataSchemas);

public sealed class DocumentMetadataSchemaRegistry
{
    private readonly IReadOnlyDictionary<string, DocumentMetadataSchemaDefinition> _current;
    private readonly IReadOnlyDictionary<string, DocumentMetadataSchemaDefinition> _versioned;

    public DocumentMetadataSchemaRegistry(IReadOnlyList<DocumentMetadataSchemaDefinition> schemas)
    {
        Schemas = schemas;
        _versioned = schemas.ToDictionary(
            schema => VersionKey(schema.Id, schema.Version),
            schema => schema,
            StringComparer.OrdinalIgnoreCase);
        _current = schemas
            .Where(schema => schema.IsCurrent)
            .GroupBy(schema => schema.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(schema => schema.Version).First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<DocumentMetadataSchemaDefinition> Schemas { get; }

    public DocumentMetadataSchemaDefinition? FindCurrent(string id) =>
        _current.TryGetValue(id, out var schema) ? schema : null;

    public DocumentMetadataSchemaDefinition? Find(string id, int version) =>
        _versioned.TryGetValue(VersionKey(id, version), out var schema) ? schema : null;

    private static string VersionKey(string id, int version) => $"{id.Trim().ToLowerInvariant()}@{version}";
}

public sealed record DocumentMetadataSchemaDefinition(
    string Id,
    int Version,
    string Status,
    bool IsCurrent,
    string RelativePath,
    string SchemaHash,
    JsonElement Schema);

public sealed record DocumentRetentionPolicy(
    int DefaultRetentionDays,
    IReadOnlyDictionary<string, int> PerType,
    int HoldSeconds,
    int WorkerTickSeconds,
    int MaxRetries);

public sealed record DocumentClassificationPolicyRow(
    string Role,
    string Tier,
    string Operation,
    string Verdict);
