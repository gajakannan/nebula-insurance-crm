using System.Text.Json;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class SearchService : ISearchService
{
    private readonly ISearchDocumentRepository _repo;

    public SearchService(ISearchDocumentRepository repo) => _repo = repo;

    public async Task<GlobalSearchResponseDto> SearchAsync(GlobalSearchQuery query, ICurrentUserService user, CancellationToken ct)
    {
        var visibility = ProjectionVisibilityResolver.For(user);
        var result = await _repo.SearchAsync(query, visibility, ct);

        var data = result.Items.Select(Map).ToList();
        var totalPages = result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)query.PageSize);

        return new GlobalSearchResponseDto(
            Data: data,
            Facets: result.Facets,
            Page: query.Page,
            PageSize: query.PageSize,
            TotalCount: result.TotalCount,
            TotalPages: totalPages,
            QueryEcho: query.Q,
            GeneratedAt: DateTimeOffset.UtcNow);
    }

    internal static GlobalSearchResultDto Map(SearchDocument d) => new(
        ObjectType: d.ObjectType,
        ObjectId: d.ObjectId,
        Title: d.Title,
        Subtitle: d.Subtitle,
        Status: d.Status,
        OwnerUserId: d.OwnerUserId,
        OwnerDisplayName: d.OwnerDisplayName,
        LineOfBusiness: d.LineOfBusiness,
        Region: d.Region,
        MatchedFields: ParseHints(d.MatchedFieldHintsJson),
        Snippet: BuildSnippet(d),
        TargetUrl: d.TargetUrl,
        Score: 1.0m,
        LastUpdatedAt: d.SourceUpdatedAt,
        IndexedAt: d.IndexedAt);

    private static IReadOnlyList<string> ParseHints(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? BuildSnippet(SearchDocument d)
    {
        if (!string.IsNullOrWhiteSpace(d.Subtitle)) return d.Subtitle;
        if (string.IsNullOrWhiteSpace(d.SearchText)) return null;
        var text = d.SearchText.Trim();
        return text.Length <= 160 ? text : text[..160] + "…";
    }
}
