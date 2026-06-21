using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IOperationalReportProjectionRepository
{
    /// <summary>Projection rows after the source-visibility predicate. The report service aggregates these
    /// (counts/bands/drilldowns) so all figures reflect authorized records only.</summary>
    Task<IReadOnlyList<OperationalReportProjection>> QueryAsync(
        OperationalReportQuery query, ProjectionVisibility visibility, CancellationToken ct);

    Task UpsertManyAsync(IReadOnlyList<OperationalReportProjection> rows, CancellationToken ct);
    Task<int> CountAsync(CancellationToken ct);
}
