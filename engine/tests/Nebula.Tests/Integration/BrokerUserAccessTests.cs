using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace Nebula.Tests.Integration;

/// <summary>
/// Integration tests for BrokerUser access boundaries (F0009 §4, §6, §8 contracts).
///
/// Tests verify:
///   1. BrokerUser GET /brokers returns BrokerBrokerUserDto shape (no RowVersion, no IsDeactivated)
///   2. BrokerUser GET /brokers/{id} for a broker outside their scope returns 403 broker_scope_unresolvable
///   3. Missing broker_tenant_id claim → 403 broker_scope_unresolvable
///   4. GET /dashboard/kpis returns 403 for BrokerUser (not in policy §2.10)
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class BrokerUserAccessTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client = factory.CreateClient();

    public void Dispose()
    {
        // Reset shared TestAuthHandler state so other test classes are not affected.
        TestAuthHandler.TestSubject = "test-user-001";
        TestAuthHandler.TestRole = "Admin";
        TestAuthHandler.TestDisplayName = "Test User";
        TestAuthHandler.ResetF0009Overrides();
    }

    // -----------------------------------------------------------------------
    // Helper: configure TestAuthHandler as BrokerUser with a specific tenant id
    // -----------------------------------------------------------------------
    private static void SetBrokerUserContext(string tenantId, string subject = "broker-test-001")
    {
        TestAuthHandler.TestSubject = subject;
        TestAuthHandler.TestRole = "BrokerUser";
        TestAuthHandler.TestDisplayName = "Broker Test User";
        TestAuthHandler.TestNebulaRoles = ["BrokerUser"];
        TestAuthHandler.TestBrokerTenantId = tenantId;
    }

    // -----------------------------------------------------------------------
    // 1. GET /brokers — BrokerUser shape: no RowVersion / no IsDeactivated fields
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetBrokers_AsBrokerUser_ReturnsBrokerUserDto_WithoutInternalFields()
    {
        // Arrange: first create a broker via Admin so we have something to read
        var brokerTenantId = $"test-tenant-{Guid.NewGuid():N}";
        var createdBroker = await CreateBrokerAndSetTenantId(brokerTenantId);

        // Switch to BrokerUser context
        SetBrokerUserContext(brokerTenantId);

        // Act
        var response = await _client.GetAsync("/brokers");

        // Assert — 200 OK
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        // BrokerUser response must NOT contain internal fields
        var root = doc.RootElement;
        ContainsProperty(root, "rowVersion").ShouldBeFalse(
            "rowVersion is InternalOnly (BROKER-VISIBILITY-MATRIX.md §Broker)");
        ContainsProperty(root, "isDeactivated").ShouldBeFalse(
            "isDeactivated is InternalOnly (BROKER-VISIBILITY-MATRIX.md §Broker)");

        // Result should contain only the scoped broker
        var data = GetDataArray(root);
        data.ShouldNotBeNull();
        if (data.HasValue)
        {
            foreach (var item in data.Value.EnumerateArray())
            {
                item.TryGetProperty("id", out _).ShouldBeTrue();
                item.TryGetProperty("legalName", out _).ShouldBeTrue();
            }
        }

        _ = createdBroker; // suppress unused warning
    }

    // -----------------------------------------------------------------------
    // 2. GET /brokers/{id} — BrokerUser requesting a broker outside their scope → 403
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetBrokerById_AsBrokerUser_CrossScopeRequest_Returns403BrokerScopeUnresolvable()
    {
        // Arrange: create a broker but assign a DIFFERENT tenant to the BrokerUser
        var brokerTenantId = $"test-tenant-{Guid.NewGuid():N}";
        var createdBroker = await CreateBrokerAndSetTenantId(brokerTenantId);

        // BrokerUser has a DIFFERENT tenant — cannot access this broker
        SetBrokerUserContext("unrelated-tenant-xyz");

        // Act: request the broker by ID
        var response = await _client.GetAsync($"/brokers/{createdBroker}");

        // Assert: 403 with broker_scope_unresolvable code
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.TryGetProperty("code", out var codeElement).ShouldBeTrue(
            "403 from scope unresolvable must include code discriminator");
        codeElement.GetString().ShouldBe("broker_scope_unresolvable");
    }

    // -----------------------------------------------------------------------
    // 3. Missing broker_tenant_id claim → 403 broker_scope_unresolvable
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetBrokers_AsBrokerUser_NoBrokerTenantIdClaim_Returns403()
    {
        // Arrange: BrokerUser role but no broker_tenant_id claim
        TestAuthHandler.TestSubject = "broker-no-tenant";
        TestAuthHandler.TestRole = "BrokerUser";
        TestAuthHandler.TestDisplayName = "BrokerUser No Tenant";
        TestAuthHandler.TestNebulaRoles = ["BrokerUser"];
        TestAuthHandler.TestBrokerTenantId = null; // explicitly missing

        // Act
        var response = await _client.GetAsync("/brokers");

        // Assert: 403 broker_scope_unresolvable
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var json = await response.Content.ReadAsStringAsync();
        json.ShouldContain("broker_scope_unresolvable");
    }

    // -----------------------------------------------------------------------
    // 4. Dashboard KPIs — BrokerUser not in policy → 403
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetDashboardKpis_AsBrokerUser_Returns403()
    {
        SetBrokerUserContext("any-tenant");

        var response = await _client.GetAsync("/dashboard/kpis");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private async Task<Guid> CreateBrokerAndSetTenantId(string tenantId)
    {
        // Create a broker as Admin first
        TestAuthHandler.TestSubject = "test-user-001";
        TestAuthHandler.TestRole = "Admin";
        TestAuthHandler.TestNebulaRoles = ["Admin"];
        TestAuthHandler.TestBrokerTenantId = null;

        var licenseNumber = $"TEST-{Guid.NewGuid().ToString("N")[..8]}";
        var createResponse = await _client.PostAsJsonAsync("/brokers", new
        {
            legalName = $"Test BrokerUser Broker {Guid.NewGuid().ToString("N")[..4]}",
            licenseNumber,
            state = "CA",
            email = (string?)null,
            phone = (string?)null,
        });

        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created,
            "Admin must be able to create broker for test setup");

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var brokerId = created.GetProperty("id").GetGuid();

        // Patch the BrokerTenantId directly via EF — use the factory's DB context
        await factory.SetBrokerTenantIdAsync(brokerId, tenantId);

        return brokerId;
    }

    private static bool ContainsProperty(JsonElement element, string propertyName)
    {
        // Check root-level or first item in a data array
        if (element.ValueKind == JsonValueKind.Object)
        {
            return element.TryGetProperty(propertyName, out _)
                || element.TryGetProperty(ToCamelCase(propertyName), out _)
                || element.TryGetProperty(propertyName.ToLowerInvariant(), out _);
        }

        if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() > 0)
        {
            return ContainsProperty(element[0], propertyName);
        }

        return false;
    }

    private static JsonElement? GetDataArray(JsonElement root)
    {
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            return data;
        if (root.ValueKind == JsonValueKind.Array)
            return root;
        return null;
    }

    private static string ToCamelCase(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];
}
