using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;
using Shouldly;

namespace Nebula.Tests.Unit;

public class DocumentServiceTests
{
    [Fact]
    public async Task UploadAsync_RejectsUnsupportedExtensionWithoutRepositoryWrite()
    {
        var repo = new FakeDocumentRepository();
        var service = CreateService(repo);

        var result = await service.UploadAsync(
            new DocumentParentRefDto("submission", Guid.NewGuid()),
            [FileInput("malware.exe", "application/octet-stream", [1, 2, 3])],
            null,
            "confidential",
            new TestUser());

        result.Documents.ShouldBeEmpty();
        result.Rejected.Single().Code.ShouldBe("unsupported_type");
        repo.CreateCalls.ShouldBe(0);
    }

    [Fact]
    public async Task UploadAsync_AcceptsValidPdfAsQuarantinedAndRecordsTimeline()
    {
        var repo = new FakeDocumentRepository();
        var timeline = new FakeTimelineRepository();
        var service = CreateService(repo, timeline);

        var result = await service.UploadAsync(
            new DocumentParentRefDto("submission", Guid.NewGuid()),
            [FileInput("acord-125.pdf", "application/pdf", [1, 2, 3])],
            null,
            "confidential",
            new TestUser());

        result.Rejected.ShouldBeEmpty();
        result.Documents.Single().Status.ShouldBe("quarantined");
        repo.CreateCalls.ShouldBe(1);
        timeline.Events.Single().EventType.ShouldBe("DocumentUploaded");
    }

    [Fact]
    public async Task UploadAsync_RejectsMetadataOutsideConfiguredSchema()
    {
        var repo = new FakeDocumentRepository();
        var service = CreateService(repo);
        using var document = JsonDocument.Parse("""{"unexpected":"value"}""");

        var result = await service.UploadAsync(
            new DocumentParentRefDto("submission", Guid.NewGuid()),
            [FileInput("acord-125.pdf", "application/pdf", [1, 2, 3])],
            [new DocumentUploadFileMetadataDto(null, "acord", null, document.RootElement.Clone())],
            "confidential",
            new TestUser());

        result.Documents.ShouldBeEmpty();
        result.Rejected.Single().Code.ShouldBe("invalid_metadata");
        repo.CreateCalls.ShouldBe(0);
    }

    private static DocumentService CreateService(
        FakeDocumentRepository repository,
        FakeTimelineRepository? timeline = null,
        IDocumentClassificationGate? gate = null)
    {
        return new DocumentService(
            repository,
            new FakeDocumentConfigurationProvider(),
            gate ?? new AllowingDocumentGate(),
            timeline ?? new FakeTimelineRepository(),
            NullLogger<DocumentService>.Instance);
    }

    private static DocumentUploadFileInput FileInput(string name, string contentType, byte[] bytes)
    {
        return new DocumentUploadFileInput(
            name,
            contentType,
            bytes.Length,
            _ => Task.FromResult<Stream>(new MemoryStream(bytes)));
    }

    private sealed class TestUser : ICurrentUserService
    {
        public Guid UserId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public string? DisplayName => "Test User";
        public IReadOnlyList<string> Roles => ["Admin"];
        public IReadOnlyList<string> Regions => ["West"];
        public string? BrokerTenantId => null;
    }

    private sealed class FakeDocumentConfigurationProvider : IDocumentConfigurationProvider
    {
        public Task<DocumentConfigurationSnapshot> GetSnapshotAsync(CancellationToken ct = default)
        {
            return Task.FromResult(new DocumentConfigurationSnapshot(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "acord", "loss-run", "financials", "supplemental", "template" },
                new DocumentRetentionPolicy(10, new Dictionary<string, int>(), 60, 10, 5),
                [],
                TestMetadataSchemas()));
        }

        private static DocumentMetadataSchemaRegistry TestMetadataSchemas()
        {
            using var acord = JsonDocument.Parse("""{"type":"object","additionalProperties":false,"properties":{"formNumber":{"type":"string"}}}""");
            using var generic = JsonDocument.Parse("""{"type":"object","additionalProperties":true,"properties":{}}""");
            return new DocumentMetadataSchemaRegistry([
                new DocumentMetadataSchemaDefinition("acord", 1, "active", true, "metadata-schemas/acord.v1.schema.json", "sha256:test-acord", acord.RootElement.Clone()),
                new DocumentMetadataSchemaDefinition("loss-run", 1, "active", true, "metadata-schemas/loss-run.v1.schema.json", "sha256:test-loss", generic.RootElement.Clone()),
                new DocumentMetadataSchemaDefinition("financials", 1, "active", true, "metadata-schemas/financials.v1.schema.json", "sha256:test-financials", generic.RootElement.Clone()),
                new DocumentMetadataSchemaDefinition("supplemental", 1, "active", true, "metadata-schemas/supplemental.v1.schema.json", "sha256:test-supplemental", generic.RootElement.Clone()),
                new DocumentMetadataSchemaDefinition("template", 1, "active", true, "metadata-schemas/template.v1.schema.json", "sha256:test-template", generic.RootElement.Clone()),
            ]);
        }
    }

    private sealed class AllowingDocumentGate : IDocumentClassificationGate
    {
        public Task<DocumentAccessDecision> AuthorizeDocumentAsync(
            ICurrentUserService user,
            DocumentParentRefDto parent,
            string classification,
            string operation,
            CancellationToken ct = default)
        {
            return Task.FromResult(new DocumentAccessDecision(true, null, null, user.Roles));
        }

        public Task<DocumentAccessDecision> AuthorizeTemplateAsync(
            ICurrentUserService user,
            string classification,
            string operation,
            CancellationToken ct = default)
        {
            return Task.FromResult(new DocumentAccessDecision(true, null, null, user.Roles));
        }
    }

    private sealed class FakeTimelineRepository : ITimelineRepository
    {
        public List<ActivityTimelineEvent> Events { get; } = [];

        public Task<IReadOnlyList<ActivityTimelineEvent>> ListEventsAsync(string entityType, Guid? entityId, int limit, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ActivityTimelineEvent>>(Events);

        public Task<PaginatedResult<ActivityTimelineEvent>> ListEventsPagedAsync(string entityType, Guid? entityId, int page, int pageSize, CancellationToken ct = default) =>
            Task.FromResult(new PaginatedResult<ActivityTimelineEvent>(Events, page, pageSize, Events.Count));

        public Task<IReadOnlyList<ActivityTimelineEvent>> ListEventsForBrokerUserAsync(IReadOnlyList<Guid> brokerIds, int limit, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ActivityTimelineEvent>>(Events);

        public Task AddEventAsync(ActivityTimelineEvent evt, CancellationToken ct = default)
        {
            Events.Add(evt);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        public int CreateCalls { get; private set; }

        public Task<DocumentWriteResult> CreateQuarantinedAsync(DocumentUploadCommand command, Stream binary, CancellationToken ct = default)
        {
            CreateCalls++;
            return Task.FromResult(new DocumentWriteResult("doc_test_document", 1, null, null));
        }

        public Task<IReadOnlyList<DocumentSidecarDto>> ListParentSidecarsAsync(DocumentParentRefDto parent, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DocumentSidecarDto>>([]);

        public Task<IReadOnlyList<DocumentSidecarDto>> ListTemplateSidecarsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DocumentSidecarDto>>([]);

        public Task<DocumentSidecarDto?> FindSidecarAsync(string documentId, CancellationToken ct = default) =>
            Task.FromResult<DocumentSidecarDto?>(null);

        public Task<DocumentBinaryRead?> OpenVersionForReadAsync(string documentId, string versionRef, CancellationToken ct = default) =>
            Task.FromResult<DocumentBinaryRead?>(null);

        public Task<DocumentWriteResult> AppendReplacementAsync(string documentId, DocumentReplaceCommand command, Stream binary, CancellationToken ct = default) =>
            Task.FromResult(new DocumentWriteResult(null, null, "not_supported", null));

        public Task<DocumentSidecarDto?> UpdateMetadataAsync(string documentId, DocumentMetadataPatch patch, CancellationToken ct = default) =>
            Task.FromResult<DocumentSidecarDto?>(null);

        public Task<DocumentSidecarDto?> AppendEventAsync(string documentId, DocumentEventDto evt, CancellationToken ct = default) =>
            Task.FromResult<DocumentSidecarDto?>(null);

        public Task<DocumentSidecarDto?> IncrementTemplateUseAsync(string templateId, string newDocumentId, Guid byUserId, CancellationToken ct = default) =>
            Task.FromResult<DocumentSidecarDto?>(null);

        public Task<IReadOnlyList<QuarantineEntryDto>> ListPromotableQuarantineEntriesAsync(DateTime nowUtc, TimeSpan hold, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<QuarantineEntryDto>>([]);

        public Task<PromoteResult> PromoteAsync(QuarantineEntryDto entry, CancellationToken ct = default) =>
            Task.FromResult(new PromoteResult(false, null));

        public Task<IReadOnlyList<RetentionCandidateDto>> ListRetentionCandidatesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<RetentionCandidateDto>>([]);

        public Task<RetentionSweepResultDto> SweepAsync(IReadOnlyList<RetentionCandidateDto> candidates, bool dryRun, CancellationToken ct = default) =>
            Task.FromResult(new RetentionSweepResultDto(0, 0, new Dictionary<string, int>(), dryRun));
    }
}
