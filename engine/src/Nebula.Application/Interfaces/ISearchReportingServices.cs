using Nebula.Application.Common;
using Nebula.Application.DTOs;

namespace Nebula.Application.Interfaces;

public interface ISearchService
{
    Task<GlobalSearchResponseDto> SearchAsync(GlobalSearchQuery query, ICurrentUserService user, CancellationToken ct);
}

public interface ISavedViewService
{
    Task<PaginatedResult<SavedViewDto>> ListAsync(SavedViewListQuery query, ICurrentUserService user, CancellationToken ct);
    Task<SavedViewDto?> GetAsync(Guid savedViewId, ICurrentUserService user, CancellationToken ct);
    Task<(SavedViewDto? Result, string? Error)> CreateAsync(SavedViewUpsertRequestDto request, ICurrentUserService user, CancellationToken ct);
    Task<(SavedViewDto? Result, string? Error)> UpdateAsync(Guid savedViewId, SavedViewUpsertRequestDto request, uint expectedRowVersion, ICurrentUserService user, CancellationToken ct);
    Task<string?> ArchiveAsync(Guid savedViewId, uint expectedRowVersion, ICurrentUserService user, CancellationToken ct);
    Task<(SavedViewDto? Result, string? Error)> SetDefaultAsync(Guid savedViewId, uint expectedRowVersion, ICurrentUserService user, CancellationToken ct);
}

public interface IOperationalReportService
{
    Task<OperationalWorkloadReportDto> GetWorkloadAsync(OperationalReportQuery query, ICurrentUserService user, CancellationToken ct);
    Task<WorkflowAgingReportDto> GetWorkflowAgingAsync(OperationalReportQuery query, ICurrentUserService user, CancellationToken ct);
}

public sealed record ProjectionBackfillResult(int SearchDocuments, int ReportProjections, int Errors, DateTimeOffset CompletedAt);

public interface ISearchProjectionService
{
    Task<ProjectionBackfillResult> BackfillAsync(DateTimeOffset startedAt, CancellationToken ct);
}
