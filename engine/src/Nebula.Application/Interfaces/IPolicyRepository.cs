using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IPolicyRepository
{
    Task<PaginatedResult<Policy>> ListAsync(PolicyListQuery query, Guid? brokerScopeId, CancellationToken ct = default);
    Task<Policy?> GetAccessibleByIdAsync(Guid id, ICurrentUserService user, Guid? brokerScopeId, CancellationToken ct = default);
    Task<Policy?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<Policy?> GetByIdWithRelationsAsync(Guid id, CancellationToken ct = default);
    Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken ct = default);
    Task<Broker?> GetBrokerByIdAsync(Guid id, CancellationToken ct = default);
    Task<CarrierRef?> GetCarrierByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> AccountExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> BrokerExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> ProducerExistsAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Policy policy, CancellationToken ct = default);
    Task AddVersionAsync(PolicyVersion version, CancellationToken ct = default);
    Task AddEndorsementAsync(PolicyEndorsement endorsement, CancellationToken ct = default);
    Task AddCoverageLinesAsync(IEnumerable<PolicyCoverageLine> coverageLines, CancellationToken ct = default);
    Task SetCoverageCurrentAsync(Guid policyId, Guid policyVersionId, CancellationToken ct = default);
    Task<PaginatedResult<PolicyVersion>> ListVersionsAsync(Guid policyId, int page, int pageSize, CancellationToken ct = default);
    Task<PolicyVersion?> GetVersionAsync(Guid policyId, Guid versionId, CancellationToken ct = default);
    Task<PaginatedResult<PolicyEndorsement>> ListEndorsementsAsync(Guid policyId, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<PolicyCoverageLine>> ListCurrentCoverageLinesAsync(Guid policyId, CancellationToken ct = default);
    Task<PolicyAccountSummaryDto?> GetAccountSummaryAsync(Guid accountId, ICurrentUserService user, Guid? brokerScopeId, CancellationToken ct = default);
    Task<int> CountPoliciesForYearAsync(int year, CancellationToken ct = default);
    Task<int> CountVersionsAsync(Guid policyId, CancellationToken ct = default);
    Task<int> CountEndorsementsAsync(Guid policyId, CancellationToken ct = default);
    Task<int> CountCurrentCoverageLinesAsync(Guid policyId, CancellationToken ct = default);
    Task<int> CountOpenRenewalsAsync(Guid policyId, CancellationToken ct = default);
    Task<Policy?> GetSuccessorPolicyAsync(Guid policyId, CancellationToken ct = default);
    Task<IReadOnlyList<Policy>> ListIssuedPoliciesExpiredBeforeAsync(DateTime today, int maxBatchSize, CancellationToken ct = default);
}
