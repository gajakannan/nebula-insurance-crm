using System.Security.Cryptography;
using System.Text.Json;
using Nebula.Application.Interfaces;
using Nebula.Domain.Documents;

namespace Nebula.Infrastructure.Documents;

public sealed class YamlDocumentConfigurationProvider : IDocumentConfigurationProvider
{
    private readonly string _configurationRoot;
    private DocumentConfigurationSnapshot? _cached;
    private DateTime _lastLoadedUtc;

    public YamlDocumentConfigurationProvider()
    {
        var docRoot = DocumentPathOptions.ResolveDocumentRoot();
        _configurationRoot = Path.Combine(docRoot, "configuration");
        Directory.CreateDirectory(_configurationRoot);
        EnsureSeedFiles();
    }

    public Task<DocumentConfigurationSnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        var newest = Directory.EnumerateFiles(_configurationRoot, "*", SearchOption.AllDirectories)
            .Select(File.GetLastWriteTimeUtc)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();

        if (_cached is not null && newest <= _lastLoadedUtc)
            return Task.FromResult(_cached);

        var taxonomy = LoadTaxonomy();
        var retention = LoadRetention(taxonomy);
        var classification = LoadClassificationPolicy();
        var metadataSchemas = LoadMetadataSchemas(taxonomy);
        _cached = new DocumentConfigurationSnapshot(taxonomy, retention, classification, metadataSchemas);
        _lastLoadedUtc = DateTime.UtcNow;
        return Task.FromResult(_cached);
    }

    private HashSet<string> LoadTaxonomy()
    {
        var path = Path.Combine(_configurationRoot, "taxonomy.yaml");
        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
                values.Add(trimmed[2..].Trim());
        }

        if (values.Count == 0)
            values.UnionWith(["acord", "loss-run", "financials", "supplemental", "template"]);

        return values;
    }

    private DocumentRetentionPolicy LoadRetention(IReadOnlySet<string> taxonomy)
    {
        var path = Path.Combine(_configurationRoot, "document-retention-policies.yaml");
        var defaultDays = 10;
        var holdSeconds = 60;
        var workerTickSeconds = 10;
        var maxRetries = 5;
        var perType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var section = "";

        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            if (!raw.StartsWith(' ') && line.EndsWith(':'))
            {
                section = line.TrimEnd(':');
                continue;
            }

            var pair = SplitPair(line);
            if (pair is null)
                continue;

            var (key, value) = pair.Value;
            if (section == "perType")
            {
                if (!taxonomy.Contains(key))
                    continue;
                perType[key] = ClampRetention(ParseInt(value, defaultDays));
            }
            else if (section == "quarantine")
            {
                if (key == "holdSeconds") holdSeconds = Math.Clamp(ParseInt(value, holdSeconds), 30, 300);
                if (key == "workerTickSeconds") workerTickSeconds = Math.Clamp(ParseInt(value, workerTickSeconds), 5, 30);
                if (key == "maxRetries") maxRetries = Math.Clamp(ParseInt(value, maxRetries), 1, 20);
            }
            else if (key == "defaultRetentionDays")
            {
                defaultDays = ClampRetention(ParseInt(value, defaultDays));
            }
        }

        foreach (var type in taxonomy)
        {
            if (!perType.ContainsKey(type))
                perType[type] = type == "supplemental" ? 3 : type is "loss-run" or "financials" ? 7 : 10;
        }

        return new DocumentRetentionPolicy(defaultDays, perType, holdSeconds, workerTickSeconds, maxRetries);
    }

    private IReadOnlyList<DocumentClassificationPolicyRow> LoadClassificationPolicy()
    {
        var path = Path.Combine(_configurationRoot, "casbin-document-roles.yaml");
        var rows = new List<DocumentClassificationPolicyRow>();
        string? role = null;
        string? tier = null;
        string? op = null;
        string? verdict = null;

        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                AddPending();
                line = line[2..].Trim();
            }

            var pair = SplitPair(line);
            if (pair is null)
                continue;

            var (key, value) = pair.Value;
            if (key == "role") role = value;
            if (key == "tier") tier = value;
            if (key == "op") op = value;
            if (key == "verdict") verdict = value;
        }
        AddPending();

        return rows.Count > 0 ? rows : DefaultPolicyRows();

        void AddPending()
        {
            if (!string.IsNullOrWhiteSpace(role)
                && !string.IsNullOrWhiteSpace(tier)
                && !string.IsNullOrWhiteSpace(op)
                && !string.IsNullOrWhiteSpace(verdict))
                rows.Add(new DocumentClassificationPolicyRow(role, tier, op, verdict));

            role = tier = op = verdict = null;
        }
    }

    private DocumentMetadataSchemaRegistry LoadMetadataSchemas(IReadOnlySet<string> taxonomy)
    {
        var registryPath = Path.Combine(_configurationRoot, "metadata-schemas", "registry.yaml");
        var entries = ReadMetadataRegistry(registryPath);
        var schemas = new List<DocumentMetadataSchemaDefinition>();

        foreach (var entry in entries)
        {
            if (!taxonomy.Contains(entry.Id))
                continue;

            var schemaPath = Path.Combine(_configurationRoot, entry.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(schemaPath))
                continue;

            var json = File.ReadAllText(schemaPath);
            using var document = JsonDocument.Parse(json);
            schemas.Add(new DocumentMetadataSchemaDefinition(
                entry.Id,
                entry.Version,
                entry.Status,
                entry.IsCurrent,
                entry.RelativePath,
                $"sha256:{Hash(json)}",
                document.RootElement.Clone()));
        }

        return new DocumentMetadataSchemaRegistry(schemas);
    }

    private static IReadOnlyList<MetadataRegistryEntry> ReadMetadataRegistry(string path)
    {
        if (!File.Exists(path))
            return [];

        var currentVersions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var entries = new List<MetadataRegistryEntry>();
        string? currentId = null;
        int? version = null;
        string? file = null;
        string status = "active";

        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            if (line.StartsWith("- id:", StringComparison.Ordinal))
            {
                AddPending();
                currentId = line["- id:".Length..].Trim();
                continue;
            }

            if (line.StartsWith("- version:", StringComparison.Ordinal))
            {
                AddPending();
                version = ParseInt(line["- version:".Length..].Trim(), 1);
                status = "active";
                file = null;
                continue;
            }

            var pair = SplitPair(line);
            if (pair is null || currentId is null)
                continue;

            var (key, value) = pair.Value;
            if (key == "currentVersion")
                currentVersions[currentId] = ParseInt(value, 1);
            if (key == "version")
                version = ParseInt(value, 1);
            if (key == "file")
                file = value;
            if (key == "status")
                status = value;
        }

        AddPending();
        return entries;

        void AddPending()
        {
            if (!string.IsNullOrWhiteSpace(currentId) && version is not null && !string.IsNullOrWhiteSpace(file))
            {
                var currentVersion = currentVersions.TryGetValue(currentId, out var configuredCurrent)
                    ? configuredCurrent
                    : version.Value;
                entries.Add(new MetadataRegistryEntry(
                    currentId.Trim().ToLowerInvariant(),
                    version.Value,
                    file,
                    status,
                    version.Value == currentVersion));
            }

            version = null;
            file = null;
            status = "active";
        }
    }

    private void EnsureSeedFiles()
    {
        Directory.CreateDirectory(Path.Combine(_configurationRoot, "metadata-schemas"));

        WriteIfMissing("taxonomy.yaml", """
version: 1
types:
  - acord
  - loss-run
  - financials
  - supplemental
  - template
""");

        WriteIfMissing("document-retention-policies.yaml", """
version: 1
defaultRetentionDays: 10
perType:
  acord: 10
  loss-run: 7
  financials: 7
  supplemental: 3
  template: 10
quarantine:
  holdSeconds: 60
  workerTickSeconds: 10
  maxRetries: 5
""");

        WriteIfMissing("casbin-document-roles.yaml", BuildDefaultPolicyYaml());

        WriteIfMissing(Path.Combine("metadata-schemas", "registry.yaml"), """
version: 1
schemas:
  - id: acord
    currentVersion: 1
    versions:
      - version: 1
        file: metadata-schemas/acord.v1.schema.json
        status: active
  - id: loss-run
    currentVersion: 1
    versions:
      - version: 1
        file: metadata-schemas/loss-run.v1.schema.json
        status: active
  - id: financials
    currentVersion: 1
    versions:
      - version: 1
        file: metadata-schemas/financials.v1.schema.json
        status: active
  - id: supplemental
    currentVersion: 1
    versions:
      - version: 1
        file: metadata-schemas/supplemental.v1.schema.json
        status: active
  - id: template
    currentVersion: 1
    versions:
      - version: 1
        file: metadata-schemas/template.v1.schema.json
        status: active
""");

        WriteIfMissing(Path.Combine("metadata-schemas", "acord.v1.schema.json"), """
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://nebula.local/document-metadata/acord.v1.schema.json",
  "title": "ACORD Metadata",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "formNumber": { "type": "string", "title": "Form Number", "enum": ["25", "125", "126", "130", "140"] },
    "namedInsured": { "type": "string", "title": "Named Insured", "maxLength": 200 },
    "effectiveDate": { "type": "string", "title": "Effective Date", "format": "date" },
    "expirationDate": { "type": "string", "title": "Expiration Date", "format": "date" },
    "carrier": { "type": "string", "title": "Carrier", "maxLength": 120 }
  }
}
""");

        WriteIfMissing(Path.Combine("metadata-schemas", "loss-run.v1.schema.json"), """
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://nebula.local/document-metadata/loss-run.v1.schema.json",
  "title": "Loss Run Metadata",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "valuationDate": { "type": "string", "title": "Valuation Date", "format": "date" },
    "periodStart": { "type": "string", "title": "Period Start", "format": "date" },
    "periodEnd": { "type": "string", "title": "Period End", "format": "date" },
    "carrier": { "type": "string", "title": "Carrier", "maxLength": 120 },
    "claimCount": { "type": "integer", "title": "Claim Count", "minimum": 0 }
  }
}
""");

        WriteIfMissing(Path.Combine("metadata-schemas", "financials.v1.schema.json"), """
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://nebula.local/document-metadata/financials.v1.schema.json",
  "title": "Financial Statement Metadata",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "fiscalYear": { "type": "integer", "title": "Fiscal Year", "minimum": 1900 },
    "statementPeriodEnd": { "type": "string", "title": "Statement Period End", "format": "date" },
    "statementType": { "type": "string", "title": "Statement Type", "enum": ["audited", "reviewed", "compiled", "internal"] },
    "revenue": { "type": "number", "title": "Revenue", "minimum": 0 },
    "currency": { "type": "string", "title": "Currency", "enum": ["USD", "CAD"] }
  }
}
""");

        WriteIfMissing(Path.Combine("metadata-schemas", "supplemental.v1.schema.json"), """
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://nebula.local/document-metadata/supplemental.v1.schema.json",
  "title": "Supplemental Metadata",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "description": { "type": "string", "title": "Description", "maxLength": 300 },
    "receivedDate": { "type": "string", "title": "Received Date", "format": "date" },
    "source": { "type": "string", "title": "Source", "maxLength": 120 }
  }
}
""");

        WriteIfMissing(Path.Combine("metadata-schemas", "template.v1.schema.json"), """
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://nebula.local/document-metadata/template.v1.schema.json",
  "title": "Template Metadata",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "templateCategory": { "type": "string", "title": "Template Category", "enum": ["acord", "supplemental", "proposal", "notice"] },
    "jurisdiction": { "type": "string", "title": "Jurisdiction", "maxLength": 40 }
  }
}
""");
    }

    private void WriteIfMissing(string name, string contents)
    {
        var path = Path.Combine(_configurationRoot, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? _configurationRoot);
        if (!File.Exists(path))
            File.WriteAllText(path, contents);
    }

    private static (string Key, string Value)? SplitPair(string line)
    {
        var index = line.IndexOf(':', StringComparison.Ordinal);
        if (index <= 0)
            return null;
        return (line[..index].Trim(), line[(index + 1)..].Trim().Trim('"'));
    }

    private static int ParseInt(string value, int fallback) => int.TryParse(value, out var parsed) ? parsed : fallback;

    private static int ClampRetention(int value) => Math.Clamp(value, 1, 10);

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static IReadOnlyList<DocumentClassificationPolicyRow> DefaultPolicyRows()
    {
        return BuildDefaultRows().ToList();
    }

    private static string BuildDefaultPolicyYaml()
    {
        var lines = new List<string> { "version: 1", "policy:" };
        foreach (var row in BuildDefaultRows())
        {
            lines.Add($"  - role: {row.Role}");
            lines.Add($"    tier: {row.Tier}");
            lines.Add($"    op: {row.Operation}");
            lines.Add($"    verdict: {row.Verdict}");
        }
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static IEnumerable<DocumentClassificationPolicyRow> BuildDefaultRows()
    {
        var internalRoles = new[] { "Admin", "Underwriter", "DistributionUser", "DistributionManager", "RelationshipManager", "ProgramManager", "Coordinator" };
        var externalRoles = new[] { "BrokerUser", "MgaUser", "ExternalUser" };
        var allOps = new[] { "read", "create", "replace", "update_metadata", "download", "create:restricted", "declassify", "link" };

        foreach (var role in internalRoles)
        {
            foreach (var tier in DocumentConstants.Classifications)
            foreach (var op in allOps)
            {
                var allow = role is "Admin" or "Underwriter"
                    || !tier.Equals("restricted", StringComparison.OrdinalIgnoreCase);
                yield return new DocumentClassificationPolicyRow(role, tier, op, allow ? "allow" : "deny");
            }
        }

        foreach (var role in externalRoles)
        {
            foreach (var tier in DocumentConstants.Classifications)
            foreach (var op in allOps)
            {
                var allow = tier.Equals("public", StringComparison.OrdinalIgnoreCase)
                    && (op is "read" or "create" or "download" or "link");
                yield return new DocumentClassificationPolicyRow(role, tier, op, allow ? "allow" : "deny");
            }
        }
    }

    private sealed record MetadataRegistryEntry(string Id, int Version, string RelativePath, string Status, bool IsCurrent);
}

internal static class DocumentPathOptions
{
    public static string ResolveDocumentRoot()
    {
        var configured = Environment.GetEnvironmentVariable("NEBULA_DOCUMENT_ROOT");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return Path.Combine(Environment.CurrentDirectory, "data", "documents");
    }
}
