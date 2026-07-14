using Shouldly;
using Nebula.Infrastructure.Authorization;

namespace Nebula.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="CasbinAuthorizationService"/> (F0002-S0009).
/// Verifies behavioral parity with the replaced PolicyAuthorizationService
/// and validates the full policy matrix from policy.csv.
/// </summary>
public class CasbinAuthorizationServiceTests
{
    private readonly CasbinAuthorizationService _sut = new();

    // ═══════════════════════════════════════════════════════════════════════
    // §1 — Broker policy matrix
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("DistributionUser",    "broker", "create", true)]
    [InlineData("DistributionUser",    "broker", "read",   true)]
    [InlineData("DistributionUser",    "broker", "search", true)]
    [InlineData("DistributionUser",    "broker", "update", true)]
    [InlineData("DistributionUser",    "broker", "delete", true)]
    [InlineData("DistributionManager", "broker", "create", true)]
    [InlineData("DistributionManager", "broker", "read",   true)]
    [InlineData("DistributionManager", "broker", "search", true)]
    [InlineData("DistributionManager", "broker", "update", true)]
    [InlineData("DistributionManager", "broker", "delete", true)]
    [InlineData("Underwriter",         "broker", "read",   true)]
    [InlineData("Underwriter",         "broker", "search", false)]
    [InlineData("Underwriter",         "broker", "create", false)]
    [InlineData("Underwriter",         "broker", "update", false)]
    [InlineData("Underwriter",         "broker", "delete", false)]
    [InlineData("RelationshipManager", "broker", "create", true)]
    [InlineData("RelationshipManager", "broker", "read",   true)]
    [InlineData("RelationshipManager", "broker", "search", true)]
    [InlineData("RelationshipManager", "broker", "update", true)]
    [InlineData("RelationshipManager", "broker", "delete", false)]
    [InlineData("ProgramManager",      "broker", "read",   true)]
    [InlineData("ProgramManager",      "broker", "create", false)]
    [InlineData("ProgramManager",      "broker", "search", false)]
    [InlineData("Admin",               "broker", "create", true)]
    [InlineData("Admin",               "broker", "read",   true)]
    [InlineData("Admin",               "broker", "search", true)]
    [InlineData("Admin",               "broker", "update", true)]
    [InlineData("Admin",               "broker", "delete", true)]
    [InlineData("Admin",               "broker", "reactivate", true)]
    [InlineData("DistributionManager", "broker", "reactivate", true)]
    [InlineData("DistributionUser",    "broker", "reactivate", false)]
    [InlineData("RelationshipManager", "broker", "reactivate", false)]
    [InlineData("Underwriter",         "broker", "reactivate", false)]
    [InlineData("BrokerUser",          "broker", "read",   true)]
    [InlineData("BrokerUser",          "broker", "search", true)]
    [InlineData("BrokerUser",          "broker", "create", false)]
    [InlineData("BrokerUser",          "broker", "update", false)]
    [InlineData("BrokerUser",          "broker", "delete", false)]
    public async Task BrokerPolicy_MatchesPolicyCsv(string role, string resource, string action, bool expected)
    {
        var result = await _sut.AuthorizeAsync(role, resource, action);
        result.ShouldBe(expected, $"{role} should {(expected ? "be allowed" : "be denied")} {resource}:{action}");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // §2 — Contact policy matrix
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("DistributionUser",    "contact", "create", true)]
    [InlineData("DistributionUser",    "contact", "read",   true)]
    [InlineData("DistributionUser",    "contact", "update", true)]
    [InlineData("DistributionUser",    "contact", "delete", false)]
    [InlineData("DistributionManager", "contact", "create", true)]
    [InlineData("DistributionManager", "contact", "read",   true)]
    [InlineData("DistributionManager", "contact", "update", true)]
    [InlineData("DistributionManager", "contact", "delete", true)]
    [InlineData("Underwriter",         "contact", "read",   true)]
    [InlineData("Underwriter",         "contact", "create", false)]
    [InlineData("RelationshipManager", "contact", "create", true)]
    [InlineData("RelationshipManager", "contact", "read",   true)]
    [InlineData("RelationshipManager", "contact", "update", true)]
    [InlineData("RelationshipManager", "contact", "delete", false)]
    [InlineData("ProgramManager",      "contact", "read",   true)]
    [InlineData("ProgramManager",      "contact", "create", false)]
    [InlineData("Admin",               "contact", "create", true)]
    [InlineData("Admin",               "contact", "read",   true)]
    [InlineData("Admin",               "contact", "update", true)]
    [InlineData("Admin",               "contact", "delete", true)]
    [InlineData("BrokerUser",          "contact", "read",   true)]
    [InlineData("BrokerUser",          "contact", "create", false)]
    public async Task ContactPolicy_MatchesPolicyCsv(string role, string resource, string action, bool expected)
    {
        var result = await _sut.AuthorizeAsync(role, resource, action);
        result.ShouldBe(expected, $"{role} should {(expected ? "be allowed" : "be denied")} {resource}:{action}");
    }

    [Theory]
    [InlineData("DistributionUser", "carrier_market", "read", true)]
    [InlineData("DistributionUser", "carrier_market", "create", false)]
    [InlineData("DistributionManager", "carrier_market", "manage_contact", true)]
    [InlineData("Underwriter", "carrier_market", "link_activity", true)]
    [InlineData("RelationshipManager", "carrier_market", "manage_appetite", true)]
    [InlineData("ProgramManager", "carrier_market", "update", false)]
    [InlineData("Admin", "carrier_market", "manage_appointment", true)]
    [InlineData("BrokerUser", "carrier_market", "read", false)]
    [InlineData("ExternalUser", "carrier_market", "search", false)]
    public async Task CarrierMarketPolicy_MatchesPolicyCsv(string role, string resource, string action, bool expected)
    {
        var result = await _sut.AuthorizeAsync(role, resource, action);
        result.ShouldBe(expected, $"{role} should {(expected ? "be allowed" : "be denied")} {resource}:{action}");
    }

    [Theory]
    [InlineData("Admin", "read", true)]
    [InlineData("Admin", "schedule_manage", true)]
    [InlineData("Admin", "split_assign", true)]
    [InlineData("Admin", "calculate", true)]
    [InlineData("Admin", "adjustment_request", true)]
    [InlineData("Admin", "adjustment_approve", true)]
    [InlineData("Admin", "rollup_read", true)]
    [InlineData("DistributionManager", "split_assign", true)]
    [InlineData("DistributionManager", "adjustment_approve", true)]
    [InlineData("DistributionUser", "schedule_manage", false)]
    [InlineData("DistributionUser", "adjustment_request", true)]
    [InlineData("Underwriter", "read", true)]
    [InlineData("Underwriter", "rollup_read", false)]
    [InlineData("RelationshipManager", "schedule_manage", true)]
    [InlineData("RelationshipManager", "split_assign", false)]
    [InlineData("ProgramManager", "rollup_read", true)]
    [InlineData("BrokerUser", "read", false)]
    [InlineData("ExternalUser", "read", false)]
    public async Task CommissionPolicy_MatchesPolicyCsv(string role, string action, bool expected)
    {
        var result = await _sut.AuthorizeAsync(role, "commission", action);
        result.ShouldBe(expected, $"{role} should {(expected ? "be allowed" : "be denied")} commission:{action}");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // §3 — Timeline event policy matrix
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("DistributionUser",    true)]
    [InlineData("DistributionManager", true)]
    [InlineData("Underwriter",         true)]
    [InlineData("RelationshipManager", true)]
    [InlineData("ProgramManager",      true)]
    [InlineData("Admin",               true)]
    [InlineData("BrokerUser",          true)]
    [InlineData("ExternalUser",        false)]
    public async Task TimelineEventRead_MatchesPolicyCsv(string role, bool expected)
    {
        var result = await _sut.AuthorizeAsync(role, "timeline_event", "read");
        result.ShouldBe(expected, $"{role} should {(expected ? "be allowed" : "be denied")} timeline_event:read");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // §4 — Task ownership condition (r.obj.assignee == r.sub.id)
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("DistributionUser")]
    [InlineData("DistributionManager")]
    [InlineData("Underwriter")]
    [InlineData("RelationshipManager")]
    [InlineData("ProgramManager")]
    [InlineData("Admin")]
    public async Task TaskRead_MatchingAssignee_ReturnsTrue(string role)
    {
        var userId = Guid.NewGuid().ToString();
        var attrs = new Dictionary<string, object>
        {
            ["assignee"] = userId,
            ["subjectId"] = userId,
        };
        var result = await _sut.AuthorizeAsync(role, "task", "read", attrs);
        result.ShouldBeTrue($"{role} reading own task should be allowed");
    }

    [Theory]
    [InlineData("DistributionUser")]
    [InlineData("Admin")]
    public async Task TaskRead_MismatchedAssignee_ReturnsFalse(string role)
    {
        var attrs = new Dictionary<string, object>
        {
            ["assignee"] = Guid.NewGuid().ToString(),
            ["subjectId"] = Guid.NewGuid().ToString(),
        };
        var result = await _sut.AuthorizeAsync(role, "task", "read", attrs);
        result.ShouldBeFalse($"{role} reading another user's task should be denied");
    }

    [Fact]
    public async Task TaskRead_NoAttributes_ReturnsFalse()
    {
        // When no resource attributes are provided, condition-based policies
        // must deny (sentinel values prevent "" == "" → true match)
        var result = await _sut.AuthorizeAsync("Admin", "task", "read");
        result.ShouldBeFalse("task:read without attributes must deny (condition can't be evaluated)");
    }

    [Theory]
    [InlineData("DistributionUser")]
    [InlineData("Admin")]
    public async Task TaskCreate_MatchingAssignee_ReturnsTrue(string role)
    {
        var userId = Guid.NewGuid().ToString();
        var attrs = new Dictionary<string, object>
        {
            ["assignee"] = userId,
            ["subjectId"] = userId,
        };
        var result = await _sut.AuthorizeAsync(role, "task", "create", attrs);
        result.ShouldBeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // §5 — ExternalUser deny-all (implicit deny, no policy lines)
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("broker", "read")]
    [InlineData("broker", "create")]
    [InlineData("contact", "read")]
    [InlineData("timeline_event", "read")]
    [InlineData("dashboard_kpi", "read")]
    [InlineData("task", "read")]
    public async Task ExternalUser_AllResources_Denied(string resource, string action)
    {
        var result = await _sut.AuthorizeAsync("ExternalUser", resource, action);
        result.ShouldBeFalse("ExternalUser has no policy lines — implicit deny");
    }

    [Theory]
    [InlineData("DistributionUser", false)]
    [InlineData("DistributionManager", true)]
    [InlineData("Underwriter", false)]
    [InlineData("RelationshipManager", true)]
    [InlineData("ProgramManager", true)]
    [InlineData("Admin", true)]
    [InlineData("BrokerUser", false)]
    [InlineData("ExternalUser", false)]
    public async Task DistributionRollupRead_MatchesF0037PolicyCsv(string role, bool expected)
    {
        var result = await _sut.AuthorizeAsync(role, "distribution_rollup", "read");
        result.ShouldBe(expected, $"{role} should {(expected ? "be allowed" : "be denied")} distribution_rollup:read");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // §6 — Unknown role deny-by-default
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UnknownRole_DeniedByDefault()
    {
        var result = await _sut.AuthorizeAsync("NonExistentRole", "broker", "read");
        result.ShouldBeFalse("unknown roles must be denied by default");
    }

    [Fact]
    public async Task UnknownAction_DeniedByDefault()
    {
        var result = await _sut.AuthorizeAsync("Admin", "broker", "nonexistent_action");
        result.ShouldBeFalse("unknown actions must be denied by default");
    }

    [Fact]
    public async Task UnknownResource_DeniedByDefault()
    {
        var result = await _sut.AuthorizeAsync("Admin", "nonexistent_resource", "read");
        result.ShouldBeFalse("unknown resources must be denied by default");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // §7 — Dashboard policy matrix (spot checks)
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("DistributionUser",    "dashboard_kpi",      true)]
    [InlineData("Admin",               "dashboard_kpi",      true)]
    [InlineData("ExternalUser",        "dashboard_kpi",      false)]
    [InlineData("BrokerUser",          "dashboard_kpi",      false)]
    [InlineData("DistributionUser",    "dashboard_pipeline", true)]
    [InlineData("BrokerUser",          "dashboard_pipeline", false)]
    [InlineData("DistributionUser",    "dashboard_nudge",    true)]
    [InlineData("BrokerUser",          "dashboard_nudge",    true)]
    public async Task DashboardPolicy_MatchesPolicyCsv(string role, string resource, bool expected)
    {
        var result = await _sut.AuthorizeAsync(role, resource, "read");
        result.ShouldBe(expected, $"{role} should {(expected ? "be allowed" : "be denied")} {resource}:read");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // §8 — Work queue policy matrix (F0022 spot checks)
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Admin",               "read",   true)]
    [InlineData("Admin",               "manage", true)]
    [InlineData("Admin",               "assign", true)]
    [InlineData("DistributionManager", "read",   true)]
    [InlineData("DistributionManager", "manage", true)]
    [InlineData("DistributionManager", "assign", true)]
    [InlineData("ProgramManager",      "read",   true)]
    [InlineData("ProgramManager",      "manage", false)]
    [InlineData("ProgramManager",      "assign", false)]
    [InlineData("Coordinator",         "read",   false)]
    [InlineData("Coordinator",         "assign", false)]
    [InlineData("Coordinator",         "manage", false)]
    [InlineData("Underwriter",         "read",   true)]
    [InlineData("Underwriter",         "assign", false)]
    [InlineData("BrokerUser",          "read",   false)]
    public async Task WorkQueuePolicy_MatchesPolicyCsv(string role, string action, bool expected)
    {
        var result = await _sut.AuthorizeAsync(role, "queue", action);
        result.ShouldBe(expected, $"{role} should {(expected ? "be allowed" : "be denied")} queue:{action}");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // §9 — Startup validation
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_LoadsSuccessfully()
    {
        // If the constructor throws, this test fails — validates embedded resources load correctly
        var service = new CasbinAuthorizationService();
        service.ShouldNotBeNull();
    }
}
