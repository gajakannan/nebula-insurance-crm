using Microsoft.EntityFrameworkCore;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class SearchDocumentRepository : ISearchDocumentRepository
{
    private readonly AppDbContext _db;

    public SearchDocumentRepository(AppDbContext db) => _db = db;

    public async Task<SearchQueryResult> SearchAsync(GlobalSearchQuery query, ProjectionVisibility visibility, CancellationToken ct)
    {
        // Source-visibility predicate FIRST — every count/facet/row below is computed on this filtered set.
        var filtered = ApplyVisibility(_db.SearchDocuments.AsNoTracking(), visibility);

        // Text match (PostgreSQL ILIKE backed by the SearchText trigram GIN index).
        var text = query.Q.Trim();
        filtered = filtered.Where(d => EF.Functions.ILike(d.SearchText, $"%{text}%"));

        // Structured filters.
        if (query.ObjectTypes.Count > 0)
        {
            var types = query.ObjectTypes.ToList();
            filtered = filtered.Where(d => types.Contains(d.ObjectType));
        }
        if (!string.IsNullOrWhiteSpace(query.Status))
            filtered = filtered.Where(d => d.Status == query.Status);
        if (query.OwnerUserId.HasValue)
            filtered = filtered.Where(d => d.OwnerUserId == query.OwnerUserId);
        if (!string.IsNullOrWhiteSpace(query.Region))
            filtered = filtered.Where(d => d.Region == query.Region);
        if (!string.IsNullOrWhiteSpace(query.LineOfBusiness))
            filtered = filtered.Where(d => d.LineOfBusiness == query.LineOfBusiness);

        var total = await filtered.CountAsync(ct);

        var objectTypeBuckets = await filtered
            .GroupBy(d => d.ObjectType)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .OrderByDescending(f => f.Count)
            .ToListAsync(ct);
        var ownerBuckets = await filtered.Where(d => d.OwnerUserId != null)
            .GroupBy(d => new { d.OwnerUserId, d.OwnerDisplayName })
            .Select(g => new { g.Key.OwnerUserId, g.Key.OwnerDisplayName, Count = g.Count() })
            .OrderByDescending(f => f.Count)
            .Take(25)
            .ToListAsync(ct);
        var statusBuckets = await filtered.Where(d => d.Status != null)
            .GroupBy(d => d.Status)
            .Select(g => new { Key = g.Key!, Count = g.Count() })
            .OrderByDescending(f => f.Count)
            .ToListAsync(ct);
        var regionBuckets = await filtered.Where(d => d.Region != null)
            .GroupBy(d => d.Region)
            .Select(g => new { Key = g.Key!, Count = g.Count() })
            .OrderByDescending(f => f.Count)
            .ToListAsync(ct);
        var lineOfBusinessBuckets = await filtered.Where(d => d.LineOfBusiness != null)
            .GroupBy(d => d.LineOfBusiness)
            .Select(g => new { Key = g.Key!, Count = g.Count() })
            .OrderByDescending(f => f.Count)
            .ToListAsync(ct);

        var facets = new GlobalSearchFacetsDto(
            ObjectTypes: objectTypeBuckets.Select(b => new FacetBucketDto(b.Key, b.Key, b.Count)).ToList(),
            Owners: ownerBuckets.Select(b => new FacetBucketDto(b.OwnerUserId!.Value.ToString(), b.OwnerDisplayName, b.Count)).ToList(),
            Statuses: statusBuckets.Select(b => new FacetBucketDto(b.Key, b.Key, b.Count)).ToList(),
            Regions: regionBuckets.Select(b => new FacetBucketDto(b.Key, b.Key, b.Count)).ToList(),
            LinesOfBusiness: lineOfBusinessBuckets.Select(b => new FacetBucketDto(b.Key, b.Key, b.Count)).ToList());

        var ordered = query.Sort.ToLowerInvariant() switch
        {
            "title" => filtered.OrderBy(d => d.Title).ThenByDescending(d => d.SourceUpdatedAt),
            // "relevance" and "updated" both fall back to recency (ILIKE has no native rank score).
            _ => filtered.OrderByDescending(d => d.SourceUpdatedAt).ThenBy(d => d.Title),
        };

        var items = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new SearchQueryResult(items, total, facets);
    }

    public async Task UpsertManyAsync(IReadOnlyList<SearchDocument> documents, CancellationToken ct)
    {
        if (documents.Count == 0) return;

        foreach (var incoming in documents)
        {
            var existing = await _db.SearchDocuments
                .FirstOrDefaultAsync(d => d.ObjectType == incoming.ObjectType && d.ObjectId == incoming.ObjectId, ct);

            if (existing is null)
            {
                _db.SearchDocuments.Add(incoming);
            }
            else
            {
                existing.TargetUrl = incoming.TargetUrl;
                existing.Title = incoming.Title;
                existing.Subtitle = incoming.Subtitle;
                existing.Status = incoming.Status;
                existing.OwnerUserId = incoming.OwnerUserId;
                existing.OwnerDisplayName = incoming.OwnerDisplayName;
                existing.AccountId = incoming.AccountId;
                existing.BrokerId = incoming.BrokerId;
                existing.PolicyId = incoming.PolicyId;
                existing.SubmissionId = incoming.SubmissionId;
                existing.RenewalId = incoming.RenewalId;
                existing.TaskId = incoming.TaskId;
                existing.LineOfBusiness = incoming.LineOfBusiness;
                existing.Region = incoming.Region;
                existing.ProgramId = incoming.ProgramId;
                existing.TerritoryId = incoming.TerritoryId;
                existing.SearchText = incoming.SearchText;
                existing.MatchedFieldHintsJson = incoming.MatchedFieldHintsJson;
                existing.SourceUpdatedAt = incoming.SourceUpdatedAt;
                existing.IndexedAt = incoming.IndexedAt;
                existing.LastProjectionError = incoming.LastProjectionError;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountAsync(CancellationToken ct) => _db.SearchDocuments.CountAsync(ct);

    private static IQueryable<SearchDocument> ApplyVisibility(IQueryable<SearchDocument> q, ProjectionVisibility v)
    {
        if (v.SeeAll) return q;
        var regions = v.Regions.ToList();
        var uid = v.UserId;
        return q.Where(d => d.OwnerUserId == uid || (d.Region != null && regions.Contains(d.Region)));
    }
}
