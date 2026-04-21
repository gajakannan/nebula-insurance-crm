using Shouldly;
using Nebula.Application.Common;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;

namespace Nebula.Tests.Unit.BrokerScopeResolverTests;

/// <summary>
/// Unit tests for BrokerScopeResolver (F0009 §6 contract).
///
/// Tests cover the three resolution cases:
///   1. Zero matches  → BrokerScopeUnresolvableException
///   2. Exactly one match → returns the resolved Guid
///   3. Missing broker_tenant_id claim → BrokerScopeUnresolvableException
/// </summary>
public class BrokerScopeResolverTests
{
    // -----------------------------------------------------------------------
    // Happy path: exactly one active mapping resolves successfully
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ResolveAsync_ExactlyOneMatch_ReturnsBrokerId()
    {
        var brokerId = Guid.NewGuid();
        var repo = new StubBrokerRepository(resolveResult: brokerId);
        var user = new StubCurrentUserService(brokerTenantId: "tenant-abc");
        var resolver = new BrokerScopeResolver(repo);

        var result = await resolver.ResolveAsync(user);

        result.ShouldBe(brokerId);
    }

    // -----------------------------------------------------------------------
    // Failure: zero or ambiguous matches → deny
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ResolveAsync_ZeroMatches_ThrowsBrokerScopeUnresolvableException()
    {
        var repo = new StubBrokerRepository(resolveResult: null);
        var user = new StubCurrentUserService(brokerTenantId: "tenant-no-match");
        var resolver = new BrokerScopeResolver(repo);

        await Should.ThrowAsync<BrokerScopeUnresolvableException>(async () => await resolver.ResolveAsync(user));
    }

    [Fact]
    public async Task ResolveAsync_AmbiguousMatches_ThrowsBrokerScopeUnresolvableException()
    {
        // Repository returns null when > 1 broker matches the tenant ID.
        var repo = new StubBrokerRepository(resolveResult: null);
        var user = new StubCurrentUserService(brokerTenantId: "tenant-ambiguous");
        var resolver = new BrokerScopeResolver(repo);

        await Should.ThrowAsync<BrokerScopeUnresolvableException>(async () => await resolver.ResolveAsync(user));
    }

    // -----------------------------------------------------------------------
    // Failure: missing or empty broker_tenant_id claim → deny without DB call
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ResolveAsync_NullBrokerTenantId_ThrowsBrokerScopeUnresolvableException()
    {
        var repo = new StubBrokerRepository(resolveResult: Guid.NewGuid());
        var user = new StubCurrentUserService(brokerTenantId: null);
        var resolver = new BrokerScopeResolver(repo);

        await Should.ThrowAsync<BrokerScopeUnresolvableException>(async () => await resolver.ResolveAsync(user));
    }

    [Fact]
    public async Task ResolveAsync_EmptyBrokerTenantId_ThrowsBrokerScopeUnresolvableException()
    {
        var repo = new StubBrokerRepository(resolveResult: Guid.NewGuid());
        var user = new StubCurrentUserService(brokerTenantId: "");
        var resolver = new BrokerScopeResolver(repo);

        await Should.ThrowAsync<BrokerScopeUnresolvableException>(async () => await resolver.ResolveAsync(user));
    }

    [Fact]
    public async Task ResolveAsync_NullBrokerTenantId_DoesNotCallRepository()
    {
        var repo = new StubBrokerRepository(resolveResult: Guid.NewGuid());
        var user = new StubCurrentUserService(brokerTenantId: null);
        var resolver = new BrokerScopeResolver(repo);

        try { await resolver.ResolveAsync(user); } catch { }

        repo.CallCount.ShouldBe(0, "scope guard must short-circuit before hitting the DB");
    }
}

// ---------------------------------------------------------------------------
// Test doubles (no external mocking library needed)
// ---------------------------------------------------------------------------

file class StubBrokerRepository : IBrokerRepository
{
    private readonly Guid? _resolveResult;
    public int CallCount { get; private set; }

    public StubBrokerRepository(Guid? resolveResult) => _resolveResult = resolveResult;

    public Task<Guid?> GetIdByBrokerTenantIdAsync(string brokerTenantId, CancellationToken ct = default)
    {
        CallCount++;
        return Task.FromResult(_resolveResult);
    }

    // --- Unused stubs --- //
    public Task<Broker?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Broker?>(null);
    public Task<Broker?> GetByIdIncludingDeactivatedAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Broker?>(null);
    public Task<Nebula.Application.Common.PaginatedResult<Broker>> ListAsync(string? search, string? statusFilter, int page, int pageSize, CancellationToken ct = default) => Task.FromResult(new Nebula.Application.Common.PaginatedResult<Broker>([], 1, pageSize, 0));
    public Task AddAsync(Broker broker, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateAsync(Broker broker, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> ExistsByLicenseAsync(string licenseNumber, CancellationToken ct = default) => Task.FromResult(false);
    public Task<bool> HasActiveSubmissionsOrRenewalsAsync(Guid brokerId, CancellationToken ct = default) => Task.FromResult(false);
}

file class StubCurrentUserService : Nebula.Application.Common.ICurrentUserService
{
    public StubCurrentUserService(string? brokerTenantId) => BrokerTenantId = brokerTenantId;

    public Guid UserId => Guid.NewGuid();
    public string? DisplayName => "Test User";
    public IReadOnlyList<string> Roles => [];
    public IReadOnlyList<string> Regions => [];
    public string? BrokerTenantId { get; }
}
