using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Services;

/// <summary>
/// Rebuilds the F0023 read-side projections (SearchDocument + OperationalReportProjection)
/// from the authoritative source modules. Idempotent upsert keyed by (ObjectType, ObjectId).
/// Region/owner correlation columns are denormalized so the query layer can apply
/// source-visibility filters. Per-type failures are counted, not fatal.
/// </summary>
public class SearchProjectionService : ISearchProjectionService
{
    private readonly AppDbContext _db;
    private readonly ISearchDocumentRepository _searchRepo;
    private readonly IOperationalReportProjectionRepository _reportRepo;

    public SearchProjectionService(
        AppDbContext db,
        ISearchDocumentRepository searchRepo,
        IOperationalReportProjectionRepository reportRepo)
    {
        _db = db;
        _searchRepo = searchRepo;
        _reportRepo = reportRepo;
    }

    public async Task<ProjectionBackfillResult> BackfillAsync(DateTimeOffset startedAt, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(startedAt.UtcDateTime);
        var searchDocs = new List<SearchDocument>();
        var reportRows = new List<OperationalReportProjection>();
        var errors = 0;

        // ── SearchDocuments ──────────────────────────────────────────────
        try
        {
            await foreach (var a in _db.Accounts.AsNoTracking().AsAsyncEnumerable().WithCancellation(ct))
                searchDocs.Add(NewDoc("Account", a.Id, $"/accounts/{a.Id}", a.Name, a.LegalName, a.Status,
                    a.PrimaryProducerUserId, null, a.Region, a.PrimaryLineOfBusiness, startedAt, a.UpdatedAt,
                    Text(a.Name, a.LegalName, a.TaxId, a.City), accountId: a.Id));

            await foreach (var b in _db.Brokers.AsNoTracking().AsAsyncEnumerable().WithCancellation(ct))
                searchDocs.Add(NewDoc("Broker", b.Id, $"/brokers/{b.Id}", b.LegalName, b.LicenseNumber, b.Status,
                    b.ManagedByUserId, null, b.State, null, startedAt, b.UpdatedAt,
                    Text(b.LegalName, b.LicenseNumber, b.Email), brokerId: b.Id));

            await foreach (var m in _db.MGAs.AsNoTracking().AsAsyncEnumerable().WithCancellation(ct))
                searchDocs.Add(NewDoc("MGA", m.Id, $"/mgas/{m.Id}", m.Name, m.ExternalCode, m.Status,
                    null, null, null, null, startedAt, m.UpdatedAt, Text(m.Name, m.ExternalCode)));

            await foreach (var p in _db.Programs.AsNoTracking().AsAsyncEnumerable().WithCancellation(ct))
                searchDocs.Add(NewDoc("Program", p.Id, $"/programs/{p.Id}", p.Name, p.ProgramCode, null,
                    p.ManagedByUserId, null, null, null, startedAt, p.UpdatedAt, Text(p.Name, p.ProgramCode), programId: p.Id));

            await foreach (var c in _db.CarrierMarkets.AsNoTracking().AsAsyncEnumerable().WithCancellation(ct))
                searchDocs.Add(NewDoc("CarrierMarket", c.Id, $"/carrier-markets/{c.Id}", c.Name, c.Code, c.Status,
                    c.RelationshipOwnerUserId, null, null, null, startedAt, c.UpdatedAt,
                    Text(c.Name, c.Code, c.NaicCode, c.AmBestRating, c.MarketType)));

            await foreach (var p in _db.Policies
                .AsNoTracking()
                .Include(p => p.Account)
                .Include(p => p.Producer)
                .AsAsyncEnumerable()
                .WithCancellation(ct))
                searchDocs.Add(NewDoc("Policy", p.Id, $"/policies/{p.Id}", p.PolicyNumber, p.AccountDisplayNameAtLink,
                    p.CurrentStatus, p.ProducerUserId, p.Producer?.DisplayName, p.Account.Region, p.LineOfBusiness, startedAt, p.UpdatedAt,
                    Text(p.PolicyNumber, p.AccountDisplayNameAtLink, p.ExternalPolicyReference),
                    accountId: p.AccountId, brokerId: p.BrokerId, policyId: p.Id));

            await foreach (var s in _db.Submissions
                .AsNoTracking()
                .Include(s => s.Account)
                .Include(s => s.AssignedToUser)
                .Where(s => !s.IsArchived)
                .AsAsyncEnumerable()
                .WithCancellation(ct))
                searchDocs.Add(NewDoc("Submission", s.Id, $"/submissions/{s.Id}", s.AccountDisplayNameAtLink, s.Description,
                    s.CurrentStatus, s.AssignedToUserId, s.AssignedToUser.DisplayName, s.Account.Region, s.LineOfBusiness, startedAt, s.UpdatedAt,
                    Text(s.AccountDisplayNameAtLink, s.Description, s.LineOfBusiness),
                    accountId: s.AccountId, brokerId: s.BrokerId, submissionId: s.Id, programId: s.ProgramId));

            await foreach (var r in _db.Renewals
                .AsNoTracking()
                .Include(r => r.Account)
                .Include(r => r.AssignedToUser)
                .Include(r => r.Policy)
                .AsAsyncEnumerable()
                .WithCancellation(ct))
                searchDocs.Add(NewDoc("Renewal", r.Id, $"/renewals/{r.Id}", r.AccountDisplayNameAtLink, r.LineOfBusiness,
                    r.CurrentStatus, r.AssignedToUserId, r.AssignedToUser.DisplayName, r.Account.Region, r.LineOfBusiness, startedAt, r.UpdatedAt,
                    Text(r.Policy.PolicyNumber, r.AccountDisplayNameAtLink, r.LineOfBusiness),
                    accountId: r.AccountId, brokerId: r.BrokerId, policyId: r.PolicyId, renewalId: r.Id));

            await foreach (var t in _db.Tasks.AsNoTracking().AsAsyncEnumerable().WithCancellation(ct))
                searchDocs.Add(NewDoc("Task", t.Id, $"/tasks/{t.Id}", t.Title, t.Description, t.Status,
                    t.AssignedToUserId, null, null, null, startedAt, t.UpdatedAt, Text(t.Title, t.Description), taskId: t.Id));

            await _searchRepo.UpsertManyAsync(searchDocs, ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            errors++;
        }

        // ── OperationalReportProjections (workflow-bearing, open items) ───
        try
        {
            await foreach (var s in _db.Submissions
                .AsNoTracking()
                .Include(s => s.Account)
                .Include(s => s.AssignedToUser)
                .Where(s => !s.IsArchived)
                .AsAsyncEnumerable()
                .WithCancellation(ct))
                reportRows.Add(NewReport("Submission", s.Id, $"/submissions/{s.Id}", "Submission", s.CurrentStatus,
                    s.AssignedToUserId, s.AssignedToUser.DisplayName, s.Account.Region, s.LineOfBusiness, s.UpdatedAt, today, dueDate: null,
                    startedAt, accountId: s.AccountId, brokerId: s.BrokerId, programId: s.ProgramId));

            await foreach (var r in _db.Renewals
                .AsNoTracking()
                .Include(r => r.Account)
                .Include(r => r.AssignedToUser)
                .AsAsyncEnumerable()
                .WithCancellation(ct))
                reportRows.Add(NewReport("Renewal", r.Id, $"/renewals/{r.Id}", "Renewal", r.CurrentStatus,
                    r.AssignedToUserId, r.AssignedToUser.DisplayName, r.Account.Region, r.LineOfBusiness, r.UpdatedAt, today,
                    dueDate: DateOnly.FromDateTime(r.TargetOutreachDate),
                    startedAt, accountId: r.AccountId, brokerId: r.BrokerId, policyId: r.PolicyId));

            await foreach (var t in _db.Tasks.AsNoTracking().Where(t => t.CompletedAt == null).AsAsyncEnumerable().WithCancellation(ct))
                reportRows.Add(NewReport("Task", t.Id, $"/tasks/{t.Id}", "Task", t.Status,
                    t.AssignedToUserId, null, null, null, t.UpdatedAt, today,
                    dueDate: t.DueDate.HasValue ? DateOnly.FromDateTime(t.DueDate.Value) : null,
                    startedAt));

            await _reportRepo.UpsertManyAsync(reportRows, ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            errors++;
        }

        return new ProjectionBackfillResult(searchDocs.Count, reportRows.Count, errors, DateTimeOffset.UtcNow);
    }

    private static SearchDocument NewDoc(
        string objectType, Guid objectId, string targetUrl, string title, string? subtitle, string? status,
        Guid? ownerUserId, string? ownerDisplayName, string? region, string? lob, DateTimeOffset indexedAt, DateTime sourceUpdatedAt, string searchText,
        Guid? accountId = null, Guid? brokerId = null, Guid? policyId = null, Guid? submissionId = null,
        Guid? renewalId = null, Guid? taskId = null, Guid? programId = null, Guid? territoryId = null) => new()
    {
        ObjectType = objectType,
        ObjectId = objectId,
        TargetUrl = targetUrl,
        Title = string.IsNullOrWhiteSpace(title) ? objectType : title,
        Subtitle = subtitle,
        Status = status,
        OwnerUserId = ownerUserId,
        OwnerDisplayName = ownerDisplayName,
        Region = region,
        LineOfBusiness = lob,
        AccountId = accountId,
        BrokerId = brokerId,
        PolicyId = policyId,
        SubmissionId = submissionId,
        RenewalId = renewalId,
        TaskId = taskId,
        ProgramId = programId,
        TerritoryId = territoryId,
        SearchText = searchText,
        MatchedFieldHintsJson = "[]",
        SourceUpdatedAt = ToOffset(sourceUpdatedAt),
        IndexedAt = indexedAt,
        CreatedAt = indexedAt.UtcDateTime,
        CreatedByUserId = Guid.Empty,
        UpdatedAt = indexedAt.UtcDateTime,
        UpdatedByUserId = Guid.Empty,
    };

    private static OperationalReportProjection NewReport(
        string sourceType, Guid sourceId, string targetUrl, string workflowType, string? status,
        Guid? ownerUserId, string? ownerDisplayName, string? region, string? lob, DateTime sourceUpdatedAt, DateOnly today, DateOnly? dueDate,
        DateTimeOffset projectedAt, Guid? accountId = null, Guid? brokerId = null, Guid? policyId = null,
        Guid? programId = null, Guid? territoryId = null)
    {
        var statusEntered = ToOffset(sourceUpdatedAt);
        var daysInStatus = Math.Max(0, today.DayNumber - DateOnly.FromDateTime(sourceUpdatedAt).DayNumber);
        var isOverdue = dueDate.HasValue && dueDate.Value < today;
        var isDueToday = dueDate.HasValue && dueDate.Value == today;
        var ageBand = isOverdue ? "Overdue" : daysInStatus >= 7 ? "ApproachingSla" : "OnTrack";

        return new OperationalReportProjection
        {
            SourceObjectType = sourceType,
            SourceObjectId = sourceId,
            TargetUrl = targetUrl,
            WorkflowType = workflowType,
            CurrentStatus = status,
            StatusEnteredAt = statusEntered,
            DaysInStatus = daysInStatus,
            OwnerUserId = ownerUserId,
            OwnerDisplayName = ownerDisplayName,
            DueDate = dueDate,
            IsDueToday = isDueToday,
            IsOverdue = isOverdue,
            AgeBand = ageBand,
            Region = region,
            LineOfBusiness = lob,
            AccountId = accountId,
            BrokerId = brokerId,
            PolicyId = policyId,
            ProgramId = programId,
            TerritoryId = territoryId,
            LastSourceUpdatedAt = statusEntered,
            ProjectedAt = projectedAt,
            CreatedAt = projectedAt.UtcDateTime,
            CreatedByUserId = Guid.Empty,
            UpdatedAt = projectedAt.UtcDateTime,
            UpdatedByUserId = Guid.Empty,
        };
    }

    private static string Text(params string?[] parts) =>
        string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p)));

    private static DateTimeOffset ToOffset(DateTime dt) => new(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
}
