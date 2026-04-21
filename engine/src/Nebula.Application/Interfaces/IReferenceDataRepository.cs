using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IReferenceDataRepository
{
    Task<IReadOnlyList<Account>> GetAccountsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MGA>> GetMgasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Nebula.Domain.Entities.Program>> GetProgramsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ReferenceSubmissionStatus>> GetSubmissionStatusesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ReferenceRenewalStatus>> GetRenewalStatusesAsync(CancellationToken ct = default);
    Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken ct = default);
    Task<Policy?> GetPolicyByIdAsync(Guid id, CancellationToken ct = default);
    Task<Nebula.Domain.Entities.Program?> GetProgramByIdAsync(Guid id, CancellationToken ct = default);
}
