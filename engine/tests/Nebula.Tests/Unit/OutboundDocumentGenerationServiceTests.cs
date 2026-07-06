using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;
using Shouldly;

namespace Nebula.Tests.Unit;

public class OutboundDocumentGenerationServiceTests
{
    [Fact]
    public async Task PreviewAsync_ReturnsReadyPreviewForPublishedTemplate()
    {
        var repo = new FakeDocumentRepository();
        var service = CreateService(repo);

        var result = await service.PreviewAsync(Request(), new TestUser());

        result.ErrorCode.ShouldBeNull();
        result.Value.ShouldNotBeNull();
        result.Value.Status.ShouldBe("ready");
        result.Value.ArtifactFamily.ShouldBe("coi");
    }

    [Fact]
    public async Task IssueAsync_CreatesAvailableGeneratedDocument()
    {
        var repo = new FakeDocumentRepository();
        var timeline = new FakeTimelineRepository();
        var service = CreateService(repo, timeline);

        var result = await service.IssueAsync(Request(), new TestUser());

        result.ErrorCode.ShouldBeNull();
        result.Value.ShouldNotBeNull();
        result.Value.DocumentId.ShouldBe("doc_generated_1");
        repo.GeneratedWrites.ShouldBe(1);
        repo.TemplateUseIncrements.ShouldBe(1);
        repo.LastGeneratedCommand!.Type.ShouldBe("generated-document");
        timeline.Events.Single().EventType.ShouldBe("OutboundDocumentIssued");
    }

    [Fact]
    public async Task IssueAsync_RejectsUnpublishedTemplate()
    {
        var repo = new FakeDocumentRepository { TemplatePublished = false };
        var service = CreateService(repo);

        var result = await service.IssueAsync(Request(), new TestUser());

        result.Value.ShouldBeNull();
        result.ErrorCode.ShouldBe("template_not_published");
        repo.GeneratedWrites.ShouldBe(0);
    }

    private static OutboundDocumentGenerationService CreateService(
        FakeDocumentRepository repo,
        FakeTimelineRepository? timeline = null)
    {
        var config = new FakeDocumentConfigurationProvider();
        var gate = new AllowingDocumentGate();
        var auth = new AllowingAuthorizationService();
        var documentService = new DocumentService(
            repo,
            config,
            gate,
            timeline ?? new FakeTimelineRepository(),
            NullLogger<DocumentService>.Instance);

        return new OutboundDocumentGenerationService(
            repo,
            config,
            gate,
            auth,
            new StaticMergeDataAssembler(),
            new StaticRenderer(),
            new OutboundTemplateGovernanceService(repo, gate, auth),
            documentService);
    }

    private static GeneratedDocumentRequestDto Request() =>
        new(
            new DocumentParentRefDto("submission", Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
            "coi",
            "doc_template_1",
            null,
            "confidential",
            null,
            null);

    private sealed class TestUser : ICurrentUserService
    {
        public Guid UserId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public string? DisplayName => "Test User";
        public IReadOnlyList<string> Roles => ["Admin"];
        public IReadOnlyList<string> Regions => ["West"];
        public string? BrokerTenantId => null;
    }

    private sealed class AllowingAuthorizationService : IAuthorizationService
    {
        public Task<bool> AuthorizeAsync(string userRole, string resourceType, string action, IDictionary<string, object>? resourceAttributes = null) =>
            Task.FromResult(true);
    }

    private sealed class AllowingDocumentGate : IDocumentClassificationGate
    {
        public Task<DocumentAccessDecision> AuthorizeDocumentAsync(ICurrentUserService user, DocumentParentRefDto parent, string classification, string operation, CancellationToken ct = default) =>
            Task.FromResult(new DocumentAccessDecision(true, null, null, user.Roles));

        public Task<DocumentAccessDecision> AuthorizeTemplateAsync(ICurrentUserService user, string classification, string operation, CancellationToken ct = default) =>
            Task.FromResult(new DocumentAccessDecision(true, null, null, user.Roles));
    }

    private sealed class StaticMergeDataAssembler : IOutboundMergeDataAssembler
    {
        public Task<OutboundMergeContext> AssembleAsync(GeneratedDocumentRequestDto request, CancellationToken ct = default) =>
            Task.FromResult(new OutboundMergeContext(
                request.Parent,
                request.ArtifactFamily,
                new Dictionary<string, object?> { ["parentId"] = request.Parent.Id },
                [new GeneratedDocumentMergeDiagnosticDto("parent", "resolved", null)],
                "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
    }

    private sealed class StaticRenderer : IDocumentRenderer
    {
        public Task<RenderedDocumentBinary> RenderAsync(DocumentSidecarDto template, OutboundMergeContext context, CancellationToken ct = default)
        {
            var bytes = new byte[] { 1, 2, 3 };
            return Task.FromResult(new RenderedDocumentBinary(new MemoryStream(bytes), "application/pdf", "coi.pdf", bytes.Length));
        }
    }

    private sealed class FakeDocumentConfigurationProvider : IDocumentConfigurationProvider
    {
        public Task<DocumentConfigurationSnapshot> GetSnapshotAsync(CancellationToken ct = default)
        {
            using var generated = JsonDocument.Parse("""{"type":"object","additionalProperties":true}""");
            return Task.FromResult(new DocumentConfigurationSnapshot(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "generated-document", "template" },
                new DocumentRetentionPolicy(10, new Dictionary<string, int>(), 60, 10, 5),
                [],
                new DocumentMetadataSchemaRegistry([
                    new DocumentMetadataSchemaDefinition("generated-document", 1, "active", true, "generated-document.v1.schema.json", "sha256:test", generated.RootElement.Clone()),
                ])));
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
        public bool TemplatePublished { get; init; } = true;
        public int GeneratedWrites { get; private set; }
        public int TemplateUseIncrements { get; private set; }
        public DocumentGeneratedWriteCommand? LastGeneratedCommand { get; private set; }

        public Task<DocumentWriteResult> CreateQuarantinedAsync(DocumentUploadCommand command, Stream binary, CancellationToken ct = default) =>
            Task.FromResult(new DocumentWriteResult("doc_upload", 1, null, null));

        public Task<DocumentWriteResult> CreateGeneratedAvailableAsync(DocumentGeneratedWriteCommand command, Stream binary, CancellationToken ct = default)
        {
            GeneratedWrites++;
            LastGeneratedCommand = command;
            return Task.FromResult(new DocumentWriteResult("doc_generated_1", 1, null, null));
        }

        public Task<IReadOnlyList<DocumentSidecarDto>> ListParentSidecarsAsync(DocumentParentRefDto parent, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DocumentSidecarDto>>([]);

        public Task<IReadOnlyList<DocumentSidecarDto>> ListTemplateSidecarsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DocumentSidecarDto>>([]);

        public Task<DocumentSidecarDto?> FindSidecarAsync(string documentId, CancellationToken ct = default) =>
            Task.FromResult<DocumentSidecarDto?>(documentId == "doc_template_1" ? TemplateSidecar() : null);

        public Task<DocumentBinaryRead?> OpenVersionForReadAsync(string documentId, string versionRef, CancellationToken ct = default) =>
            Task.FromResult<DocumentBinaryRead?>(null);

        public Task<DocumentWriteResult> AppendReplacementAsync(string documentId, DocumentReplaceCommand command, Stream binary, CancellationToken ct = default) =>
            Task.FromResult(new DocumentWriteResult(null, null, "not_supported", null));

        public Task<DocumentSidecarDto?> UpdateMetadataAsync(string documentId, DocumentMetadataPatch patch, CancellationToken ct = default) =>
            Task.FromResult<DocumentSidecarDto?>(null);

        public Task<DocumentSidecarDto?> AppendEventAsync(string documentId, DocumentEventDto evt, CancellationToken ct = default) =>
            Task.FromResult<DocumentSidecarDto?>(null);

        public Task<DocumentSidecarDto?> IncrementTemplateUseAsync(string templateId, string newDocumentId, Guid byUserId, CancellationToken ct = default)
        {
            TemplateUseIncrements++;
            return Task.FromResult<DocumentSidecarDto?>(TemplateSidecar());
        }

        public Task<IReadOnlyList<QuarantineEntryDto>> ListPromotableQuarantineEntriesAsync(DateTime nowUtc, TimeSpan hold, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<QuarantineEntryDto>>([]);

        public Task<PromoteResult> PromoteAsync(QuarantineEntryDto entry, CancellationToken ct = default) =>
            Task.FromResult(new PromoteResult(false, null));

        public Task<IReadOnlyList<RetentionCandidateDto>> ListRetentionCandidatesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<RetentionCandidateDto>>([]);

        public Task<RetentionSweepResultDto> SweepAsync(IReadOnlyList<RetentionCandidateDto> candidates, bool dryRun, CancellationToken ct = default) =>
            Task.FromResult(new RetentionSweepResultDto(0, 0, new Dictionary<string, int>(), dryRun));

        private DocumentSidecarDto TemplateSidecar()
        {
            var metadata = JsonSerializer.SerializeToElement(new
            {
                artifactFamily = "coi",
                outboundStatus = TemplatePublished ? "published" : "draft",
            });
            return new DocumentSidecarDto(
                "doc_template_1",
                "COI Template",
                new DocumentParentRefDto("template", Guid.Empty),
                "confidential",
                "template",
                ["coi", "outbound:coi"],
                new DocumentMetadataSchemaRefDto("template", 1, "sha256:template"),
                metadata,
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                new DocumentAuditTimestampsDto(DateTime.UtcNow, DateTime.UtcNow),
                null,
                [new DocumentVersionDto(1, "coi-template.pdf", 10, "hash", TemplatePublished ? "available" : "quarantined", DateTime.UtcNow, Guid.Parse("11111111-1111-1111-1111-111111111111"), null)],
                0,
                null,
                []);
        }
    }
}
