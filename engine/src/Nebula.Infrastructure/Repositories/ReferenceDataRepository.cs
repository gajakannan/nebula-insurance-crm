using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class ReferenceDataRepository(AppDbContext db, IMemoryCache cache) : IReferenceDataRepository
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<Account>> GetAccountsAsync(CancellationToken ct = default) =>
        await cache.GetOrCreateAsync("ref:accounts", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return (IReadOnlyList<Account>)await db.Accounts.OrderBy(a => a.Name).ToListAsync(ct);
        }) ?? [];

    public async Task<IReadOnlyList<MGA>> GetMgasAsync(CancellationToken ct = default) =>
        await cache.GetOrCreateAsync("ref:mgas", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return (IReadOnlyList<MGA>)await db.MGAs.OrderBy(m => m.Name).ToListAsync(ct);
        }) ?? [];

    public async Task<IReadOnlyList<Nebula.Domain.Entities.Program>> GetProgramsAsync(CancellationToken ct = default) =>
        await cache.GetOrCreateAsync("ref:programs", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return (IReadOnlyList<Nebula.Domain.Entities.Program>)await db.Programs.OrderBy(p => p.Name).ToListAsync(ct);
        }) ?? [];

    public async Task<IReadOnlyList<ReferenceSubmissionStatus>> GetSubmissionStatusesAsync(CancellationToken ct = default) =>
        await cache.GetOrCreateAsync("ref:submissionStatuses", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return (IReadOnlyList<ReferenceSubmissionStatus>)await db.ReferenceSubmissionStatuses
                .OrderBy(s => s.DisplayOrder).ToListAsync(ct);
        }) ?? [];

    public async Task<IReadOnlyList<ReferenceRenewalStatus>> GetRenewalStatusesAsync(CancellationToken ct = default) =>
        await cache.GetOrCreateAsync("ref:renewalStatuses", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return (IReadOnlyList<ReferenceRenewalStatus>)await db.ReferenceRenewalStatuses
                .OrderBy(s => s.DisplayOrder).ToListAsync(ct);
        }) ?? [];

    public async Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Accounts.FirstOrDefaultAsync(account => account.Id == id, ct);

    public async Task<Policy?> GetPolicyByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Policies
            .Include(policy => policy.Account)
            .Include(policy => policy.Broker)
            .FirstOrDefaultAsync(policy => policy.Id == id, ct);

    public async Task<Nebula.Domain.Entities.Program?> GetProgramByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Programs.FirstOrDefaultAsync(program => program.Id == id, ct);
}
