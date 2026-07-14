using Nebula.Application.Common;
using Nebula.Application.DTOs;

namespace Nebula.Application.Interfaces;

public interface IRevenueAttributionRepository
{
    Task<RevenueAttributionRollupResponseDto> GetRollupsAsync(RevenueAttributionRollupQuery query, ICurrentUserService user, Guid? brokerScopeId, CancellationToken ct = default);
}
