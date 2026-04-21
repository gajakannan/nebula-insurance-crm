using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Shouldly;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nebula.Application.DTOs;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;
using OpportunityProgram = Nebula.Domain.Entities.Program;

namespace Nebula.Tests.Integration;

[Collection(IntegrationTestCollection.Name)]
public class DashboardScopeFilteringTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string Issuer = "http://test.local/application/o/nebula/";
    private const string ScopeStatusCode = "ScopeTestStage";

    private static readonly Guid DistributionUserId = Guid.Parse("3f4f74dc-c2bf-4b9a-b0ab-7e504f4b3025");
    private static readonly Guid DistributionManagerId = Guid.Parse("db0f79eb-aade-4c26-8cc0-b88bb66ec35e");
    private static readonly Guid UnderwriterId = Guid.Parse("f92f76d7-0209-4130-8241-81acbef5dbf8");
    private static readonly Guid RelationshipManagerId = Guid.Parse("3d06707a-c30d-4a8f-b4fe-ea2724c8778d");
    private static readonly Guid ProgramManagerId = Guid.Parse("7603e11f-e67b-4200-ae5e-4ad02d5c53a2");
    private static readonly Guid OtherUserId = Guid.Parse("dad5f7d3-52a5-4a20-ab4c-f08f954cca39");
    private static readonly Guid AdminUserId = Guid.Parse("27f2b4dd-c730-4108-8382-b02684fe44ed");

    private static readonly Guid ScopeMgaId = Guid.Parse("e3637cd8-e69f-4ad7-8e12-f10d9d24d4cb");
    private static readonly Guid ScopeManagedProgramId = Guid.Parse("b27d11da-6015-45c1-b651-4f3a85dfe6c6");
    private static readonly Guid ScopeOtherProgramId = Guid.Parse("7c15ec8a-ace5-403d-b5c8-f8ed5fbe8188");
    private static readonly Guid ScopeBrokerWestId = Guid.Parse("9f39c778-c762-4230-bf2d-9f682e8594f8");
    private static readonly Guid ScopeBrokerEastId = Guid.Parse("5c9e89c4-6aa8-4a7f-b5d3-2825f7f5a9ee");
    private static readonly Guid ScopeAccountWestId = Guid.Parse("0cc6a9fc-6157-4325-b8a0-eb757168f65d");
    private static readonly Guid ScopeAccountEastId = Guid.Parse("ef4df4e6-a7e8-4adf-8ca4-5e9589965dd1");

    private static readonly Guid ScopeSubmissionDistributionUserId = Guid.Parse("0ce67d81-c157-40f8-bf72-f7f94b3d86df");
    private static readonly Guid ScopeSubmissionDistributionManagerId = Guid.Parse("a4715f6f-43b2-4df5-a7e2-8a1d6e4fc349");
    private static readonly Guid ScopeSubmissionUnderwriterId = Guid.Parse("0a8d4232-0046-4ede-90f6-8dc8ef4ec7a1");
    private static readonly Guid ScopeSubmissionRelationshipManagerId = Guid.Parse("6823f37b-39a6-4291-b39d-b1b8cd12f0d8");
    private static readonly Guid ScopeSubmissionProgramManagerId = Guid.Parse("6e6f8e0e-8f8f-4bcc-b94d-444f7bc06e97");

    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _appFactory;
    private readonly HttpClient _client;

    public DashboardScopeFilteringTests(CustomWebApplicationFactory factory)
    {
        _appFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "ScopedRole";
                        options.DefaultChallengeScheme = "ScopedRole";
                    })
                    .AddScheme<AuthenticationSchemeOptions, ScopedRoleAuthHandler>("ScopedRole", _ => { });
            });
        });

        _client = _appFactory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await using var scope = _appFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Submissions.AnyAsync(s => s.Id == ScopeSubmissionDistributionUserId))
            return;

        var now = DateTime.UtcNow;

        if (!await db.ReferenceSubmissionStatuses.AnyAsync(status => status.Code == ScopeStatusCode))
        {
            db.ReferenceSubmissionStatuses.Add(new ReferenceSubmissionStatus
            {
                Code = ScopeStatusCode,
                DisplayName = "Scope Test Stage",
                Description = "Integration scope test status",
                IsTerminal = false,
                DisplayOrder = 999,
                ColorGroup = "intake",
            });
        }

        UpsertUserProfile(db, DistributionUserId, "scope-dist-user", "DistributionUser");
        UpsertUserProfile(db, DistributionManagerId, "scope-dist-manager-user", "DistributionManager", ["West"]);
        UpsertUserProfile(db, UnderwriterId, "scope-underwriter-user", "Underwriter");
        UpsertUserProfile(db, RelationshipManagerId, "scope-relationship-manager-user", "RelationshipManager");
        UpsertUserProfile(db, ProgramManagerId, "scope-program-manager-user", "ProgramManager");
        UpsertUserProfile(db, OtherUserId, "scope-other-user", "DistributionUser");
        UpsertUserProfile(db, AdminUserId, "scope-admin-user", "Admin");

        if (!await db.MGAs.AnyAsync(mga => mga.Id == ScopeMgaId))
        {
            db.MGAs.Add(new MGA
            {
                Id = ScopeMgaId,
                Name = "Scope MGA",
                ExternalCode = "SCOPE-MGA",
                Status = "Active",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = AdminUserId,
                UpdatedByUserId = AdminUserId,
            });
        }

        if (!await db.Programs.AnyAsync(program => program.Id == ScopeManagedProgramId))
        {
            db.Programs.Add(new OpportunityProgram
            {
                Id = ScopeManagedProgramId,
                Name = "Scope Managed Program",
                ProgramCode = "SCOPE-PM",
                MgaId = ScopeMgaId,
                ManagedByUserId = ProgramManagerId,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = AdminUserId,
                UpdatedByUserId = AdminUserId,
            });
        }

        if (!await db.Programs.AnyAsync(program => program.Id == ScopeOtherProgramId))
        {
            db.Programs.Add(new OpportunityProgram
            {
                Id = ScopeOtherProgramId,
                Name = "Scope Other Program",
                ProgramCode = "SCOPE-OTHER",
                MgaId = ScopeMgaId,
                ManagedByUserId = null,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = AdminUserId,
                UpdatedByUserId = AdminUserId,
            });
        }

        if (!await db.Brokers.AnyAsync(broker => broker.Id == ScopeBrokerWestId))
        {
            db.Brokers.Add(new Broker
            {
                Id = ScopeBrokerWestId,
                LegalName = "Scope Broker West",
                LicenseNumber = "SCOPEWEST001",
                State = "CA",
                Status = "Active",
                ManagedByUserId = RelationshipManagerId,
                PrimaryProgramId = ScopeOtherProgramId,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = AdminUserId,
                UpdatedByUserId = AdminUserId,
            });
        }

        if (!await db.Brokers.AnyAsync(broker => broker.Id == ScopeBrokerEastId))
        {
            db.Brokers.Add(new Broker
            {
                Id = ScopeBrokerEastId,
                LegalName = "Scope Broker East",
                LicenseNumber = "SCOPEEAST001",
                State = "TX",
                Status = "Active",
                ManagedByUserId = null,
                PrimaryProgramId = ScopeManagedProgramId,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = AdminUserId,
                UpdatedByUserId = AdminUserId,
            });
        }

        if (!await db.BrokerRegions.AnyAsync(region => region.BrokerId == ScopeBrokerWestId && region.Region == "West"))
            db.BrokerRegions.Add(new BrokerRegion { BrokerId = ScopeBrokerWestId, Region = "West" });

        if (!await db.BrokerRegions.AnyAsync(region => region.BrokerId == ScopeBrokerEastId && region.Region == "East"))
            db.BrokerRegions.Add(new BrokerRegion { BrokerId = ScopeBrokerEastId, Region = "East" });

        if (!await db.Accounts.AnyAsync(account => account.Id == ScopeAccountWestId))
        {
            const string displayName = "Scope Account West";
            db.Accounts.Add(new Account
            {
                Id = ScopeAccountWestId,
                Name = displayName,
                StableDisplayName = displayName,
                Industry = "Manufacturing",
                PrimaryState = "CA",
                Region = "West",
                Status = "Active",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = AdminUserId,
                UpdatedByUserId = AdminUserId,
            });
        }

        if (!await db.Accounts.AnyAsync(account => account.Id == ScopeAccountEastId))
        {
            const string displayName = "Scope Account East";
            db.Accounts.Add(new Account
            {
                Id = ScopeAccountEastId,
                Name = displayName,
                StableDisplayName = displayName,
                Industry = "Technology",
                PrimaryState = "TX",
                Region = "East",
                Status = "Active",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = AdminUserId,
                UpdatedByUserId = AdminUserId,
            });
        }

        AddScopeSubmissionIfMissing(db, ScopeSubmissionDistributionUserId, ScopeAccountEastId, ScopeBrokerEastId, ScopeOtherProgramId, DistributionUserId, "Property", now.AddDays(-5));
        AddScopeSubmissionIfMissing(db, ScopeSubmissionDistributionManagerId, ScopeAccountWestId, ScopeBrokerWestId, ScopeOtherProgramId, OtherUserId, "CommercialAuto", now.AddDays(-4));
        AddScopeSubmissionIfMissing(db, ScopeSubmissionUnderwriterId, ScopeAccountEastId, ScopeBrokerEastId, ScopeOtherProgramId, UnderwriterId, "Cyber", now.AddDays(-3));
        AddScopeSubmissionIfMissing(db, ScopeSubmissionRelationshipManagerId, ScopeAccountWestId, ScopeBrokerWestId, ScopeOtherProgramId, OtherUserId, "GeneralLiability", now.AddDays(-2));
        AddScopeSubmissionIfMissing(db, ScopeSubmissionProgramManagerId, ScopeAccountEastId, ScopeBrokerEastId, ScopeManagedProgramId, OtherUserId, "WorkersComp", now.AddDays(-1));

        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("DistributionUser", "scope-dist-user", "", 1)]
    [InlineData("DistributionManager", "scope-dist-manager-user", "West", 2)]
    [InlineData("Underwriter", "scope-underwriter-user", "", 1)]
    [InlineData("RelationshipManager", "scope-relationship-manager-user", "", 2)]
    [InlineData("ProgramManager", "scope-program-manager-user", "", 1)]
    [InlineData("Admin", "scope-admin-user", "", 5)]
    public async Task GetOpportunities_FiltersScopeStageCountsByRole(
        string role,
        string subject,
        string regions,
        int expectedCount)
    {
        var response = await SendDashboardRequestAsync("/dashboard/opportunities?periodDays=365", role, subject, regions);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<DashboardOpportunitiesDto>();
        payload.ShouldNotBeNull();

        var scopeStatus = payload!.Submissions.Single(status => status.Status == ScopeStatusCode);
        scopeStatus.Count.ShouldBe(expectedCount);
    }

    [Theory]
    [InlineData("broker", "Scope Broker East")]
    [InlineData("lineOfBusiness", "Property")]
    [InlineData("brokerState", "TX")]
    public async Task GetOpportunityBreakdown_DistributionUser_DoesNotLeakOutOfScopeGroups(string groupBy, string expectedOnlyKey)
    {
        var response = await SendDashboardRequestAsync(
            $"/dashboard/opportunities/submission/{ScopeStatusCode}/breakdown?groupBy={groupBy}&periodDays=365",
            "DistributionUser",
            "scope-dist-user",
            "");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<OpportunityBreakdownDto>();
        payload.ShouldNotBeNull();
        payload!.Total.ShouldBe(1);
        payload.Groups.Count.ShouldBe(1);
        payload.Groups.Single().Key.ShouldBe(expectedOnlyKey);
    }

    private async Task<HttpResponseMessage> SendDashboardRequestAsync(string path, string role, string subject, string regions)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Test-Role", role);
        request.Headers.Add("X-Test-Sub", subject);

        if (!string.IsNullOrWhiteSpace(regions))
            request.Headers.Add("X-Test-Regions", regions);

        return await _client.SendAsync(request);
    }

    private static void AddScopeSubmissionIfMissing(
        AppDbContext db,
        Guid submissionId,
        Guid accountId,
        Guid brokerId,
        Guid? programId,
        Guid assignedToUserId,
        string lineOfBusiness,
        DateTime createdAtUtc)
    {
        if (db.Submissions.Any(submission => submission.Id == submissionId))
            return;

        var account = db.Accounts.Local.FirstOrDefault(existingAccount => existingAccount.Id == accountId)
            ?? db.Accounts.Single(existingAccount => existingAccount.Id == accountId);
        db.Submissions.Add(new Submission
        {
            Id = submissionId,
            AccountId = accountId,
            BrokerId = brokerId,
            ProgramId = programId,
            LineOfBusiness = lineOfBusiness,
            CurrentStatus = ScopeStatusCode,
            EffectiveDate = DateTime.UtcNow.Date,
            PremiumEstimate = 250000m,
            AssignedToUserId = assignedToUserId,
            AccountDisplayNameAtLink = account.StableDisplayName,
            AccountStatusAtRead = account.Status,
            AccountSurvivorId = account.MergedIntoAccountId,
            CreatedAt = createdAtUtc,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = assignedToUserId,
            UpdatedByUserId = assignedToUserId,
        });
    }

    private static void UpsertUserProfile(
        AppDbContext db,
        Guid id,
        string subject,
        string role,
        IReadOnlyList<string>? regions = null)
    {
        var existing = db.UserProfiles.FirstOrDefault(profile => profile.Id == id);
        if (existing is not null)
            return;

        var now = DateTime.UtcNow;
        var normalizedRegions = regions ?? [];
        db.UserProfiles.Add(new UserProfile
        {
            Id = id,
            IdpIssuer = Issuer,
            IdpSubject = subject,
            Email = $"{subject}@nebula.test",
            DisplayName = subject,
            Department = "Operations",
            RegionsJson = System.Text.Json.JsonSerializer.Serialize(normalizedRegions),
            RolesJson = System.Text.Json.JsonSerializer.Serialize(new[] { role }),
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    private sealed class ScopedRoleAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Request.Headers["X-Test-Role"].FirstOrDefault();
            var subject = Request.Headers["X-Test-Sub"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(subject))
                return Task.FromResult(AuthenticateResult.Fail("X-Test-Role and X-Test-Sub headers are required."));

            var claims = new List<Claim>
            {
                new("iss", Issuer),
                new("sub", subject),
                new(ClaimTypes.NameIdentifier, subject),
                new("name", subject),
                new(ClaimTypes.Name, subject),
                new("role", role),
                new(ClaimTypes.Role, role),
                new("nebula_roles", role),
            };

            var regions = Request.Headers["X-Test-Regions"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(regions))
            {
                foreach (var region in regions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    claims.Add(new Claim("regions", region));
            }

            var identity = new ClaimsIdentity(claims, "ScopedRole");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "ScopedRole");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
