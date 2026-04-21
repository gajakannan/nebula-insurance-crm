using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class BrokerRepository(AppDbContext db) : IBrokerRepository
{
    public async Task<Broker?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Brokers.Include(b => b.BrokerRegions).FirstOrDefaultAsync(b => b.Id == id, ct);

    // IgnoreQueryFilters bypasses the IsDeleted global filter — Admin/reactivation use only (F0002-S0008).
    public async Task<Broker?> GetByIdIncludingDeactivatedAsync(Guid id, CancellationToken ct = default) =>
        await db.Brokers.IgnoreQueryFilters().Include(b => b.BrokerRegions).FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<PaginatedResult<Broker>> ListAsync(
        string? search, string? statusFilter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Brokers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter))
            query = query.Where(b => b.Status == statusFilter);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            query = query.Where(b =>
                EF.Functions.ILike(b.LegalName, $"%{trimmed}%") ||
                b.LicenseNumber == trimmed);
        }

        var totalCount = await query.CountAsync(ct);
        var data = await query
            .OrderBy(b => b.LegalName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResult<Broker>(data, page, pageSize, totalCount);
    }

    public Task AddAsync(Broker broker, CancellationToken ct = default)
    {
        db.Brokers.Add(broker);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Broker broker, CancellationToken ct = default) => Task.CompletedTask;

    public async Task<bool> ExistsByLicenseAsync(string licenseNumber, CancellationToken ct = default) =>
        await db.Brokers.IgnoreQueryFilters()
            .AnyAsync(b => b.LicenseNumber == licenseNumber, ct);

    public async Task<Guid?> GetIdByBrokerTenantIdAsync(string brokerTenantId, CancellationToken ct = default)
    {
        // Applies the global IsDeleted query filter — deactivated brokers do not resolve scope.
        // Return null for zero or multiple matches so the service layer throws BrokerScopeUnresolvableException.
        var ids = await db.Brokers
            .Where(b => b.BrokerTenantId == brokerTenantId)
            .Select(b => b.Id)
            .Take(2) // early-out: we only care whether count is 0, 1, or >1
            .ToListAsync(ct);

        return ids.Count == 1 ? ids[0] : null;
    }

    public async Task<bool> HasActiveSubmissionsOrRenewalsAsync(Guid brokerId, CancellationToken ct = default)
    {
        var terminalSubmission = await db.ReferenceSubmissionStatuses
            .Where(s => s.IsTerminal)
            .Select(s => s.Code)
            .ToListAsync(ct);
        var terminalRenewal = await db.ReferenceRenewalStatuses
            .Where(s => s.IsTerminal)
            .Select(s => s.Code)
            .ToListAsync(ct);

        var hasSubmissions = await db.Submissions
            .AnyAsync(s => s.BrokerId == brokerId && !terminalSubmission.Contains(s.CurrentStatus), ct);
        if (hasSubmissions) return true;

        return await db.Renewals
            .AnyAsync(r => r.BrokerId == brokerId && !terminalRenewal.Contains(r.CurrentStatus), ct);
    }
}
