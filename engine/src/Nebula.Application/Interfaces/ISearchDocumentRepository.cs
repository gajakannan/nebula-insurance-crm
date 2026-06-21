using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

/// <summary>Result of a permission-filtered search: page items, total, and facets — all
/// computed AFTER the source-visibility predicate so hidden records never leak via counts.</summary>
public sealed record SearchQueryResult(
    IReadOnlyList<SearchDocument> Items,
    int TotalCount,
    GlobalSearchFacetsDto Facets);

public interface ISearchDocumentRepository
{
    Task<SearchQueryResult> SearchAsync(GlobalSearchQuery query, ProjectionVisibility visibility, CancellationToken ct);
    Task UpsertManyAsync(IReadOnlyList<SearchDocument> documents, CancellationToken ct);
    Task<int> CountAsync(CancellationToken ct);
}
