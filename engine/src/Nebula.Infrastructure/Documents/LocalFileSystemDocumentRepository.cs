using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Documents;

namespace Nebula.Infrastructure.Documents;

public sealed class LocalFileSystemDocumentRepository : IDocumentRepository
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.Ordinal);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string _root;

    public LocalFileSystemDocumentRepository()
    {
        _root = DocumentPathOptions.ResolveDocumentRoot();
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(Path.Combine(_root, "quarantine"));
        Directory.CreateDirectory(Path.Combine(_root, "templates"));
        Directory.CreateDirectory(Path.Combine(_root, "configuration"));
    }

    public async Task<DocumentWriteResult> CreateQuarantinedAsync(
        DocumentUploadCommand command,
        Stream binary,
        CancellationToken ct = default)
    {
        var documentId = DocumentIds.NewDocumentId();
        var logicalName = MakeSafeLogicalName(command.LogicalName);
        var extension = Path.GetExtension(command.OriginalFileName).ToLowerInvariant();
        var parentDir = command.IsTemplate
            ? Path.Combine(_root, "templates", documentId)
            : ParentDirectory(command.Parent);
        Directory.CreateDirectory(parentDir);

        logicalName = DisambiguateLogicalName(parentDir, logicalName);
        var fileName = $"{logicalName}-v1{extension}";
        var quarantinePath = QuarantinePath(documentId, 1, extension);
        var hash = await CopyAndHashAsync(binary, quarantinePath, ct);
        var now = DateTime.UtcNow;
        var sidecar = new DocumentSidecarDto(
            documentId,
            logicalName,
            command.Parent,
            command.Classification,
            command.Type,
            command.Tags,
            command.MetadataSchema,
            NormalizeMetadata(command.Metadata),
            command.UploadedByUserId,
            new DocumentAuditTimestampsDto(now, now),
            command.ProvenanceSource is null ? null : new DocumentProvenanceDto(command.ProvenanceSource),
            [
                new DocumentVersionDto(
                    1,
                    fileName,
                    command.SizeBytes,
                    hash,
                    "quarantined",
                    now,
                    command.UploadedByUserId,
                    null)
            ],
            command.IsTemplate ? 0 : null,
            null,
            [
                new DocumentEventDto("uploaded", now, command.UploadedByUserId.ToString(), Version: 1)
            ]);

        await WriteSidecarAsync(SidecarPath(sidecar), sidecar, ct);
        return new DocumentWriteResult(documentId, 1, null, null);
    }

    public async Task<IReadOnlyList<DocumentSidecarDto>> ListParentSidecarsAsync(
        DocumentParentRefDto parent,
        CancellationToken ct = default)
    {
        var dir = ParentDirectory(parent);
        if (!Directory.Exists(dir))
            return [];

        return await ReadSidecarsAsync(Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly), ct);
    }

    public async Task<IReadOnlyList<DocumentSidecarDto>> ListTemplateSidecarsAsync(CancellationToken ct = default)
    {
        var dir = Path.Combine(_root, "templates");
        if (!Directory.Exists(dir))
            return [];

        return await ReadSidecarsAsync(Directory.EnumerateFiles(dir, "*.json", SearchOption.AllDirectories), ct);
    }

    public async Task<DocumentSidecarDto?> FindSidecarAsync(string documentId, CancellationToken ct = default)
    {
        if (!DocumentIds.IsDocumentId(documentId))
            return null;

        foreach (var file in Directory.EnumerateFiles(_root, "*.json", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}configuration{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;
            var sidecar = await ReadSidecarAsync(file, ct);
            if (sidecar?.DocumentId == documentId)
                return sidecar;
        }

        return null;
    }

    public async Task<DocumentBinaryRead?> OpenVersionForReadAsync(
        string documentId,
        string versionRef,
        CancellationToken ct = default)
    {
        var sidecar = await FindSidecarAsync(documentId, ct);
        if (sidecar is null)
            return null;

        var version = versionRef.Equals("latest", StringComparison.OrdinalIgnoreCase)
            ? sidecar.Versions.Where(v => v.Status == "available").OrderByDescending(v => v.N).FirstOrDefault()
            : sidecar.Versions.FirstOrDefault(v => v.N.ToString() == versionRef);

        if (version is null || version.Status != "available")
            return null;

        var path = VersionPath(sidecar, version);
        if (!File.Exists(path) || !IsUnderRoot(path))
            return null;

        return new DocumentBinaryRead(
            File.OpenRead(path),
            ContentType(version.FileName),
            version.FileName,
            version.N,
            version.SizeBytes);
    }

    public async Task<DocumentWriteResult> AppendReplacementAsync(
        string documentId,
        DocumentReplaceCommand command,
        Stream binary,
        CancellationToken ct = default)
    {
        var gate = Locks.GetOrAdd(documentId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            var sidecar = await FindSidecarAsync(documentId, ct);
            if (sidecar is null)
                return new DocumentWriteResult(null, null, "document_not_found", null);

            var latest = sidecar.Versions.OrderByDescending(v => v.N).FirstOrDefault();
            if (latest is null || latest.Status != "available")
                return new DocumentWriteResult(null, null, "version_not_available", null);

            var next = latest.N + 1;
            var extension = Path.GetExtension(command.OriginalFileName).ToLowerInvariant();
            var fileName = $"{sidecar.LogicalName}-v{next}{extension}";
            var quarantinePath = QuarantinePath(documentId, next, extension);
            var hash = await CopyAndHashAsync(binary, quarantinePath, ct);
            var now = DateTime.UtcNow;
            var versions = sidecar.Versions.Append(new DocumentVersionDto(
                next,
                fileName,
                command.SizeBytes,
                hash,
                "quarantined",
                now,
                command.UploadedByUserId,
                latest.N)).ToList();
            var events = sidecar.Events.Append(new DocumentEventDto(
                "replaced",
                now,
                command.UploadedByUserId.ToString(),
                FromVersion: latest.N,
                ToVersion: next)).ToList();
            var updated = sidecar with
            {
                AuditTimestamps = sidecar.AuditTimestamps with { UpdatedAtUtc = now },
                Versions = versions,
                Events = events,
            };
            await WriteSidecarAsync(SidecarPath(updated), updated, ct);
            return new DocumentWriteResult(documentId, next, null, null);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DocumentSidecarDto?> UpdateMetadataAsync(
        string documentId,
        DocumentMetadataPatch patch,
        CancellationToken ct = default)
    {
        var gate = Locks.GetOrAdd(documentId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            var sidecar = await FindSidecarAsync(documentId, ct);
            if (sidecar is null)
                return null;

            var changes = new Dictionary<string, object?>();
            var classification = sidecar.Classification;
            var type = sidecar.Type;
            var tags = sidecar.Tags;
            var metadataSchema = sidecar.MetadataSchema;
            var metadata = NormalizeMetadata(sidecar.Metadata);

            if (patch.Classification is not null && patch.Classification != sidecar.Classification)
            {
                changes["classification"] = new { from = sidecar.Classification, to = patch.Classification };
                classification = patch.Classification;
            }

            if (patch.Type is not null && patch.Type != sidecar.Type)
            {
                changes["type"] = new { from = sidecar.Type, to = patch.Type };
                type = patch.Type;
            }

            if (patch.Tags is not null && !patch.Tags.SequenceEqual(sidecar.Tags))
            {
                changes["tags"] = patch.Tags;
                tags = patch.Tags;
            }

            if (patch.MetadataSchema is not null
                && (patch.MetadataSchema.Id != sidecar.MetadataSchema.Id
                    || patch.MetadataSchema.Version != sidecar.MetadataSchema.Version
                    || patch.MetadataSchema.SchemaHash != sidecar.MetadataSchema.SchemaHash))
            {
                changes["metadataSchema"] = new { from = sidecar.MetadataSchema, to = patch.MetadataSchema };
                metadataSchema = patch.MetadataSchema;
            }

            if (patch.Metadata is not null && !JsonEquals(metadata, patch.Metadata.Value))
            {
                changes["metadata"] = patch.Metadata.Value;
                metadata = NormalizeMetadata(patch.Metadata.Value);
            }

            if (changes.Count == 0)
                return null;

            var now = DateTime.UtcNow;
            var kind = changes.ContainsKey("classification") ? "classified" : "metadata_edited";
            var evt = kind == "classified"
                ? new DocumentEventDto(kind, now, patch.ActorUserId.ToString(), From: sidecar.Classification, To: classification)
                : new DocumentEventDto(kind, now, patch.ActorUserId.ToString(), Changes: changes);
            var updated = sidecar with
            {
                Classification = classification,
                Type = type,
                Tags = tags,
                MetadataSchema = metadataSchema,
                Metadata = metadata,
                AuditTimestamps = sidecar.AuditTimestamps with { UpdatedAtUtc = now },
                Events = sidecar.Events.Append(evt).ToList(),
            };

            await WriteSidecarAsync(SidecarPath(updated), updated, ct);
            return updated;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DocumentSidecarDto?> AppendEventAsync(
        string documentId,
        DocumentEventDto evt,
        CancellationToken ct = default)
    {
        var gate = Locks.GetOrAdd(documentId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            var sidecar = await FindSidecarAsync(documentId, ct);
            if (sidecar is null)
                return null;
            var updated = sidecar with
            {
                AuditTimestamps = sidecar.AuditTimestamps with { UpdatedAtUtc = DateTime.UtcNow },
                Events = sidecar.Events.Append(evt).ToList(),
            };
            await WriteSidecarAsync(SidecarPath(updated), updated, ct);
            return updated;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DocumentSidecarDto?> IncrementTemplateUseAsync(
        string templateId,
        string newDocumentId,
        Guid byUserId,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var evt = new DocumentEventDto("linked", now, byUserId.ToString(), Changes: new Dictionary<string, object?> { ["documentId"] = newDocumentId });
        var sidecar = await AppendEventAsync(templateId, evt, ct);
        if (sidecar is null)
            return null;
        var updated = sidecar with { UseCount = (sidecar.UseCount ?? 0) + 1, LastUsedAt = now };
        await WriteSidecarAsync(SidecarPath(updated), updated, ct);
        return updated;
    }

    public async Task<IReadOnlyList<QuarantineEntryDto>> ListPromotableQuarantineEntriesAsync(
        DateTime nowUtc,
        TimeSpan hold,
        CancellationToken ct = default)
    {
        var entries = new List<QuarantineEntryDto>();
        foreach (var sidecar in await ReadSidecarsAsync(Directory.EnumerateFiles(_root, "*.json", SearchOption.AllDirectories), ct))
        {
            foreach (var version in sidecar.Versions.Where(v => v.Status == "quarantined" && v.UploadedAt.Add(hold) <= nowUtc))
            {
                var path = QuarantinePath(sidecar.DocumentId, version.N, Path.GetExtension(version.FileName));
                entries.Add(new QuarantineEntryDto(sidecar.DocumentId, version.N, path, version.UploadedAt));
            }
        }

        return entries;
    }

    public async Task<PromoteResult> PromoteAsync(QuarantineEntryDto entry, CancellationToken ct = default)
    {
        var gate = Locks.GetOrAdd(entry.DocumentId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            var sidecar = await FindSidecarAsync(entry.DocumentId, ct);
            if (sidecar is null)
                return new PromoteResult(false, "document_not_found");
            var version = sidecar.Versions.FirstOrDefault(v => v.N == entry.Version);
            if (version is null)
                return new PromoteResult(false, "version_not_found");
            if (version.Status == "available")
                return new PromoteResult(false, null);
            if (!File.Exists(entry.QuarantinePath))
                return await MarkPromoteFailed(sidecar, version, "quarantine_missing", ct);

            var target = VersionPath(sidecar, version);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            if (File.Exists(target))
                File.Delete(target);
            File.Move(entry.QuarantinePath, target);

            var now = DateTime.UtcNow;
            var versions = sidecar.Versions
                .Select(v => v.N == version.N ? v with { Status = "available" } : v)
                .ToList();
            var updated = sidecar with
            {
                AuditTimestamps = sidecar.AuditTimestamps with { UpdatedAtUtc = now },
                Versions = versions,
                Events = sidecar.Events.Append(new DocumentEventDto("promoted", now, DocumentConstants.SystemQuarantineWorker, Version: version.N)).ToList(),
            };
            await WriteSidecarAsync(SidecarPath(updated), updated, ct);
            return new PromoteResult(true, null);
        }
        catch (Exception ex)
        {
            return new PromoteResult(false, ex.GetType().Name);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<RetentionCandidateDto>> ListRetentionCandidatesAsync(CancellationToken ct = default)
    {
        var candidates = new List<RetentionCandidateDto>();
        foreach (var sidecar in await ReadSidecarsAsync(Directory.EnumerateFiles(_root, "*.json", SearchOption.AllDirectories), ct))
        {
            var latest = sidecar.Versions.OrderByDescending(v => v.UploadedAt).FirstOrDefault();
            if (latest is null)
                continue;
            candidates.Add(new RetentionCandidateDto(sidecar.DocumentId, sidecar.Parent, sidecar.Type, sidecar.Classification, latest.UploadedAt));
        }

        return candidates;
    }

    public async Task<RetentionSweepResultDto> SweepAsync(
        IReadOnlyList<RetentionCandidateDto> candidates,
        bool dryRun,
        CancellationToken ct = default)
    {
        var sweptByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var swept = 0;
        foreach (var candidate in candidates)
        {
            var sidecar = await FindSidecarAsync(candidate.DocumentId, ct);
            if (sidecar is null)
                continue;

            AppendSweepAudit(candidate, dryRun);
            if (!dryRun)
            {
                foreach (var version in sidecar.Versions)
                {
                    var path = VersionPath(sidecar, version);
                    if (File.Exists(path))
                        File.Delete(path);
                }
                var sidecarPath = SidecarPath(sidecar);
                if (File.Exists(sidecarPath))
                    File.Delete(sidecarPath);
            }

            swept++;
            sweptByType[candidate.Type] = sweptByType.GetValueOrDefault(candidate.Type) + 1;
        }

        return new RetentionSweepResultDto(candidates.Count, swept, sweptByType, dryRun);
    }

    private async Task<PromoteResult> MarkPromoteFailed(DocumentSidecarDto sidecar, DocumentVersionDto version, string error, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var versions = sidecar.Versions
            .Select(v => v.N == version.N ? v with { Status = "failed_promote" } : v)
            .ToList();
        var updated = sidecar with
        {
            AuditTimestamps = sidecar.AuditTimestamps with { UpdatedAtUtc = now },
            Versions = versions,
            Events = sidecar.Events.Append(new DocumentEventDto("promote_failed", now, DocumentConstants.SystemQuarantineWorker, Version: version.N, Error: error)).ToList(),
        };
        await WriteSidecarAsync(SidecarPath(updated), updated, ct);
        return new PromoteResult(false, error);
    }

    private async Task<IReadOnlyList<DocumentSidecarDto>> ReadSidecarsAsync(IEnumerable<string> files, CancellationToken ct)
    {
        var result = new List<DocumentSidecarDto>();
        foreach (var file in files)
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}configuration{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;
            var sidecar = await ReadSidecarAsync(file, ct);
            if (sidecar is not null)
                result.Add(sidecar);
        }

        return result;
    }

    private static async Task<DocumentSidecarDto?> ReadSidecarAsync(string path, CancellationToken ct)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            var sidecar = await JsonSerializer.DeserializeAsync<DocumentSidecarDto>(stream, JsonOptions, ct);
            return sidecar is null ? null : NormalizeSidecar(sidecar);
        }
        catch
        {
            return null;
        }
    }

    private static async Task WriteSidecarAsync(string path, DocumentSidecarDto sidecar, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = $"{path}.tmp";
        await using (var stream = File.Create(temp))
            await JsonSerializer.SerializeAsync(stream, sidecar, JsonOptions, ct);
        if (File.Exists(path))
            File.Delete(path);
        File.Move(temp, path);
    }

    private static DocumentSidecarDto NormalizeSidecar(DocumentSidecarDto sidecar)
    {
        var metadataSchema = sidecar.MetadataSchema
            ?? new DocumentMetadataSchemaRefDto(sidecar.Type, 1, "");
        return sidecar with
        {
            MetadataSchema = metadataSchema,
            Metadata = NormalizeMetadata(sidecar.Metadata),
        };
    }

    private static JsonElement NormalizeMetadata(JsonElement metadata) =>
        metadata.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? EmptyMetadata() : metadata.Clone();

    private static JsonElement EmptyMetadata()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }

    private static bool JsonEquals(JsonElement left, JsonElement right) =>
        JsonSerializer.Serialize(NormalizeMetadata(left), JsonOptions) == JsonSerializer.Serialize(NormalizeMetadata(right), JsonOptions);

    private string ParentDirectory(DocumentParentRefDto parent)
    {
        if (parent.Type.Equals("template", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(_root, "templates");
        return Path.Combine(_root, MakeSafeSegment(parent.Type), parent.Id.ToString("D"));
    }

    private string SidecarPath(DocumentSidecarDto sidecar)
    {
        var dir = sidecar.Parent.Type.Equals("template", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(_root, "templates", sidecar.DocumentId)
            : ParentDirectory(sidecar.Parent);
        return Path.Combine(dir, $"{sidecar.LogicalName}.json");
    }

    private string VersionPath(DocumentSidecarDto sidecar, DocumentVersionDto version)
    {
        var dir = sidecar.Parent.Type.Equals("template", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(_root, "templates", sidecar.DocumentId)
            : ParentDirectory(sidecar.Parent);
        return Path.Combine(dir, version.FileName);
    }

    private string QuarantinePath(string documentId, int version, string extension) =>
        Path.Combine(_root, "quarantine", $"{documentId}-v{version}{extension.ToLowerInvariant()}");

    private bool IsUnderRoot(string path)
    {
        var fullRoot = Path.GetFullPath(_root);
        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(fullRoot, StringComparison.Ordinal);
    }

    private void AppendSweepAudit(RetentionCandidateDto candidate, bool dryRun)
    {
        var path = Path.Combine(_root, "configuration", "retention-sweeps.jsonl");
        var row = JsonSerializer.Serialize(new
        {
            candidate.DocumentId,
            candidate.Parent,
            candidate.Type,
            candidate.Classification,
            LastUploadedAt = candidate.LastUploadedAtUtc,
            SweptAt = DateTime.UtcNow,
            ByPrincipal = DocumentConstants.SystemRetentionSweeper,
            DryRun = dryRun,
        });
        File.AppendAllText(path, row + Environment.NewLine);
    }

    private static async Task<string> CopyAndHashAsync(Stream input, string targetPath, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        await using var output = File.Create(targetPath);
        using var sha = SHA256.Create();
        var buffer = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
            sha.TransformBlock(buffer, 0, read, null, 0);
        }
        sha.TransformFinalBlock([], 0, 0);
        return Convert.ToHexString(sha.Hash!).ToLowerInvariant();
    }

    private static string ContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".csv" => "text/csv",
            _ => "application/octet-stream",
        };
    }

    private static string DisambiguateLogicalName(string directory, string logicalName)
    {
        var candidate = logicalName;
        var suffix = 2;
        while (File.Exists(Path.Combine(directory, $"{candidate}.json")))
            candidate = $"{logicalName}-{suffix++}";
        return candidate;
    }

    private static string MakeSafeLogicalName(string value)
    {
        var name = Path.GetFileNameWithoutExtension(value).Trim();
        if (string.IsNullOrWhiteSpace(name))
            name = "document";
        var chars = name.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '-').ToArray();
        var safe = new string(chars).Trim('-');
        if (string.IsNullOrWhiteSpace(safe))
            safe = "document";
        return safe[..Math.Min(safe.Length, 120)];
    }

    private static string MakeSafeSegment(string value)
    {
        if (value.Any(c => !char.IsLetterOrDigit(c) && c != '-'))
            throw new InvalidOperationException("Unsafe path segment.");
        return value.ToLowerInvariant();
    }
}
