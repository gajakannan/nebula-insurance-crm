using Microsoft.EntityFrameworkCore;
using Nebula.Domain.Entities;
using Nebula.Domain.Workflow;

namespace Nebula.Infrastructure.Persistence;

public static class DevSeedData
{
    private const int BrokerSeedCount = 240;
    private const int SubmissionSeedCount = 320;
    private const int RenewalSeedCount = 320;
    private const int PolicySeedCount = 480;
    private const int AccountSeedCount = 420;

    private static readonly string[] States = ["CA", "TX", "NY", "FL", "WA", "IL", "GA", "NC", "AZ", "CO", "NJ", "PA"];
    private static readonly string[] Regions = ["West", "Central", "East", "South"];
    private static readonly string[] Cities = ["Los Angeles", "Dallas", "New York", "Miami", "Seattle", "Chicago", "Atlanta", "Charlotte", "Phoenix", "Denver", "Newark", "Philadelphia"];
    private static readonly string[] Industries = ["Manufacturing", "Healthcare", "Construction", "Retail", "Logistics", "Technology", "Hospitality", "Agriculture"];
    private static readonly string[] BrokerRoles = ["Primary", "Producer", "Account Manager", "Service", "Accounting"];
    private static readonly string[] AccountContactRoles = ["Risk Manager", "Controller", "Operations Lead", "Owner", "Finance", "HR"];
    private static readonly string[] TaskPriorities = ["Low", "Normal", "High", "Urgent"];
    private static readonly string[] LineOfBusinessCodes = LineOfBusinessCatalog.Definitions.Select(definition => definition.Code).ToArray();

    private static readonly Dictionary<string, string[]> SubmissionNextStates = new(StringComparer.Ordinal)
    {
        ["Received"] = ["Triaging"],
        ["Triaging"] = ["WaitingOnBroker", "ReadyForUWReview"],
        ["WaitingOnBroker"] = ["ReadyForUWReview"],
        ["ReadyForUWReview"] = ["InReview"],
        ["InReview"] = ["Quoted", "Declined"],
        ["Quoted"] = ["BindRequested", "Declined", "Withdrawn"],
        ["BindRequested"] = ["Bound", "Withdrawn"],
    };

    private static readonly Dictionary<string, string[]> RenewalNextStates = new(StringComparer.Ordinal)
    {
        ["Identified"] = ["Outreach"],
        ["Outreach"] = ["InReview"],
        ["InReview"] = ["Quoted", "Lost"],
        ["Quoted"] = ["Completed", "Lost"],
    };

    public static async Task SeedDevDataAsync(AppDbContext db)
    {
        await EnsureReferenceStatusesAsync(db);

        // F0009: Idempotently ensure the dev broker tenant link exists even when data was
        // already seeded before F0009 migration added the BrokerTenantId column.
        await EnsureDevBrokerTenantIdAsync(db);
        await EnsureCarrierRefsAsync(db);

        if (await db.Submissions.AnyAsync()) return; // app data already seeded

        var rng = new Random(20260226);
        var now = DateTime.UtcNow;

        var userProfiles = BuildUserProfiles(now);
        db.UserProfiles.AddRange(userProfiles);

        var userIds = userProfiles.Select(u => u.Id).ToArray();
        var userNameById = userProfiles.ToDictionary(u => u.Id, u => u.DisplayName);

        var mgas = new[]
        {
            new MGA { Name = "Acme MGA", ExternalCode = "ACME-001", Status = "Active", CreatedAt = now, UpdatedAt = now, CreatedByUserId = userIds[0], UpdatedByUserId = userIds[0] },
            new MGA { Name = "Orion Specialty MGA", ExternalCode = "ORION-002", Status = "Active", CreatedAt = now, UpdatedAt = now, CreatedByUserId = userIds[1], UpdatedByUserId = userIds[1] },
        };
        db.MGAs.AddRange(mgas);
        await db.SaveChangesAsync();

        var programs = new[]
        {
            new Nebula.Domain.Entities.Program { Name = "General Liability", ProgramCode = "GL-001", MgaId = mgas[0].Id, ManagedByUserId = userIds[0], CreatedAt = now, UpdatedAt = now, CreatedByUserId = userIds[0], UpdatedByUserId = userIds[0] },
            new Nebula.Domain.Entities.Program { Name = "Excess Liability", ProgramCode = "XS-002", MgaId = mgas[0].Id, ManagedByUserId = userIds[2], CreatedAt = now, UpdatedAt = now, CreatedByUserId = userIds[2], UpdatedByUserId = userIds[2] },
            new Nebula.Domain.Entities.Program { Name = "Property CAT", ProgramCode = "PROP-003", MgaId = mgas[1].Id, ManagedByUserId = userIds[1], CreatedAt = now, UpdatedAt = now, CreatedByUserId = userIds[1], UpdatedByUserId = userIds[1] },
        };
        db.Programs.AddRange(programs);
        await db.SaveChangesAsync();

        var accounts = BuildAccounts(AccountSeedCount, now, rng, userIds);
        db.Accounts.AddRange(accounts);
        await db.SaveChangesAsync();

        var brokers = BuildBrokers(BrokerSeedCount, now, rng, userIds, mgas, programs);
        // F0009 §9: Link the first Active broker to BrokerUser dev tenant for test identity validation.
        var devBroker = brokers.FirstOrDefault(b => b.Status == "Active") ?? brokers[0];
        devBroker.BrokerTenantId = BrokerUserDevTenantId;
        db.Brokers.AddRange(brokers);
        await db.SaveChangesAsync();

        AssignAccountRelationships(accounts, brokers, userIds, rng, now);
        db.AccountContacts.AddRange(BuildAccountContacts(accounts, now, rng, userIds));
        await db.SaveChangesAsync();

        db.BrokerRegions.AddRange(BuildBrokerRegions(brokers, rng));
        db.Contacts.AddRange(BuildContacts(brokers, now, rng, userIds));
        var carriers = await db.CarrierRefs.OrderBy(carrier => carrier.Name).ToListAsync();
        var policies = BuildPolicies(PolicySeedCount, now, rng, userIds, accounts, brokers, carriers);
        db.Policies.AddRange(policies);
        db.PolicyVersions.AddRange(BuildPolicyVersions(policies, now, userIds[0]));
        db.PolicyCoverageLines.AddRange(BuildPolicyCoverageLines(policies, now, userIds[0]));
        await db.SaveChangesAsync();

        var submissions = new List<Submission>(SubmissionSeedCount);
        var renewals = new List<Renewal>(RenewalSeedCount);
        var transitions = new List<WorkflowTransition>(SubmissionSeedCount * 6 + RenewalSeedCount * 5);
        var timelineEvents = new List<ActivityTimelineEvent>(400);

        var boundSubmissionIds = new List<Guid>();
        var renewalPolicies = policies.OrderBy(_ => rng.Next()).Take(RenewalSeedCount).ToList();
        var boundPolicies = policies
            .Where(policy => renewalPolicies.All(selected => selected.Id != policy.Id))
            .ToList();

        for (var i = 0; i < SubmissionSeedCount; i++)
        {
            var account = accounts[rng.Next(accounts.Count)];
            var broker = brokers[rng.Next(brokers.Count)];
            var assignedTo = userIds[rng.Next(userIds.Length)];
            var path = GenerateWorkflowPath(
                rng,
                "Received",
                SubmissionNextStates,
                OpportunityStatusCatalog.SubmissionTerminalStatusCodes,
                chooseSubmissionTerminal: true);

            // ~40% of submissions stay in an active (non-terminal) stage so the
            // pipeline timeline has realistic in-progress data for dashboard visuals.
            if (path.Count > 2 && rng.NextDouble() < 0.40)
            {
                // Truncate at a random active stage (including index 0 = Received).
                var cutoff = rng.Next(0, path.Count - 1);
                while (cutoff < path.Count &&
                       OpportunityStatusCatalog.SubmissionTerminalStatusCodes.Contains(path[cutoff]))
                    cutoff--;
                if (cutoff >= 0)
                    path = path.GetRange(0, cutoff + 1);
            }

            var createdAt = now.AddDays(-rng.Next(Math.Max(path.Count * 10, 21), 365)).AddHours(-rng.Next(0, 24));

            var updatedAt = createdAt;
            for (var step = 1; step < path.Count; step++)
            {
                var occurredAt = updatedAt.AddDays(RandomStepDays(rng, path[step - 1], path[step]))
                    .AddHours(rng.Next(0, 8));
                updatedAt = occurredAt;
                transitions.Add(new WorkflowTransition
                {
                    WorkflowType = "Submission",
                    EntityId = Guid.Empty, // patched after entity instantiation
                    FromState = path[step - 1],
                    ToState = path[step],
                    Reason = BuildTransitionReason(rng, "Submission", path[step - 1], path[step]),
                    ActorUserId = assignedTo,
                    OccurredAt = occurredAt,
                });
            }

            var submission = new Submission
            {
                AccountId = account.Id,
                BrokerId = broker.Id,
                ProgramId = rng.NextDouble() < 0.7 ? programs[rng.Next(programs.Length)].Id : null,
                LineOfBusiness = rng.NextDouble() < 0.1 ? null : LineOfBusinessCodes[rng.Next(LineOfBusinessCodes.Length)],
                CurrentStatus = path[^1],
                EffectiveDate = now.AddDays(rng.Next(-30, 180)),
                ExpirationDate = now.AddDays(rng.Next(180, 540)),
                PremiumEstimate = Math.Round((decimal)(rng.Next(12_000, 250_000) + rng.NextDouble()), 2),
                Description = rng.NextDouble() < 0.45 ? "Seeded submission intake record for development workflows." : null,
                AssignedToUserId = assignedTo,
                AccountDisplayNameAtLink = account.StableDisplayName,
                AccountStatusAtRead = account.Status,
                AccountSurvivorId = account.MergedIntoAccountId,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                CreatedByUserId = assignedTo,
                UpdatedByUserId = assignedTo,
            };
            submissions.Add(submission);

            if (submission.CurrentStatus == "Bound")
                boundSubmissionIds.Add(submission.Id);

            PatchRecentTransitionsEntityId(transitions, submission.Id);

            if (i < 120 || rng.NextDouble() < 0.25)
            {
                timelineEvents.Add(new ActivityTimelineEvent
                {
                    EntityType = "Submission",
                    EntityId = submission.Id,
                    EventType = "SubmissionCreated",
                    EventDescription = $"Submission created for {account.Name}",
                    ActorUserId = assignedTo,
                    ActorDisplayName = userNameById[assignedTo],
                    OccurredAt = createdAt,
                });
            }
        }

        db.Submissions.AddRange(submissions);
        await db.SaveChangesAsync();

        for (var i = 0; i < RenewalSeedCount; i++)
        {
            var policy = renewalPolicies[i];
            var account = policy.Account;
            var broker = policy.Broker;
            var assignedTo = userIds[rng.Next(userIds.Length)];
            var path = GenerateWorkflowPath(
                rng,
                "Identified",
                RenewalNextStates,
                OpportunityStatusCatalog.RenewalTerminalStatusCodes,
                chooseSubmissionTerminal: false);

            // ~40% of renewals stay in an active pipeline stage (same as submissions).
            if (path.Count > 2 && rng.NextDouble() < 0.40)
            {
                var cutoff = rng.Next(0, path.Count - 1);
                while (cutoff < path.Count &&
                       OpportunityStatusCatalog.RenewalTerminalStatusCodes.Contains(path[cutoff]))
                    cutoff--;
                if (cutoff >= 0)
                    path = path.GetRange(0, cutoff + 1);
            }

            var createdAt = now.AddDays(-rng.Next(Math.Max(path.Count * 10, 30), 365)).AddHours(-rng.Next(0, 24));

            var updatedAt = createdAt;
            for (var step = 1; step < path.Count; step++)
            {
                var occurredAt = updatedAt.AddDays(RandomStepDays(rng, path[step - 1], path[step]))
                    .AddHours(rng.Next(0, 8));
                updatedAt = occurredAt;
                transitions.Add(new WorkflowTransition
                {
                    WorkflowType = "Renewal",
                    EntityId = Guid.Empty,
                    FromState = path[step - 1],
                    ToState = path[step],
                    Reason = BuildTransitionReason(rng, "Renewal", path[step - 1], path[step]),
                    ActorUserId = assignedTo,
                    OccurredAt = occurredAt,
                });
            }

            var renewal = new Renewal
            {
                AccountId = account.Id,
                BrokerId = broker.Id,
                PolicyId = policy.Id,
                Policy = policy,
                LineOfBusiness = policy.LineOfBusiness,
                CurrentStatus = path[^1],
                PolicyExpirationDate = policy.ExpirationDate,
                AssignedToUserId = assignedTo,
                AccountDisplayNameAtLink = account.StableDisplayName,
                AccountStatusAtRead = account.Status,
                AccountSurvivorId = account.MergedIntoAccountId,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                CreatedByUserId = assignedTo,
                UpdatedByUserId = assignedTo,
            };
            renewal.TargetOutreachDate = renewal.PolicyExpirationDate.AddDays(-GetRenewalTargetDays(renewal.LineOfBusiness));
            if (renewal.CurrentStatus == "Completed")
            {
                renewal.BoundPolicyId = boundPolicies.Count > 0
                    ? boundPolicies[rng.Next(boundPolicies.Count)].Id
                    : null;
                renewal.RenewalSubmissionId = boundSubmissionIds.Count > 0 && rng.NextDouble() < 0.55
                    ? boundSubmissionIds[rng.Next(boundSubmissionIds.Count)]
                    : null;
            }

            if (renewal.CurrentStatus == "Lost")
            {
                renewal.LostReasonCode = "CompetitiveLoss";
            }
            renewals.Add(renewal);

            PatchRecentTransitionsEntityId(transitions, renewal.Id);

            if (i < 120 || rng.NextDouble() < 0.25)
            {
                timelineEvents.Add(new ActivityTimelineEvent
                {
                    EntityType = "Renewal",
                    EntityId = renewal.Id,
                    EventType = "RenewalCreated",
                    EventDescription = $"Renewal created from policy {policy.PolicyNumber}",
                    ActorUserId = assignedTo,
                    ActorDisplayName = userNameById[assignedTo],
                    OccurredAt = createdAt,
                });
            }
        }

        ApplySeededAccountLifecycleFixtures(accounts, submissions, renewals, policies, now, userIds[0]);

        db.Renewals.AddRange(renewals);
        db.WorkflowTransitions.AddRange(transitions);

        var tasks = BuildTasks(now, rng, userIds, submissions, renewals, brokers);
        db.Tasks.AddRange(tasks);

        timelineEvents.AddRange(BuildBrokerTimelineEvents(now, rng, userNameById, brokers));
        timelineEvents.AddRange(BuildTransitionTimelineEvents(rng, userNameById, transitions));
        db.ActivityTimelineEvents.AddRange(timelineEvents.OrderByDescending(e => e.OccurredAt).Take(500));

        await db.SaveChangesAsync();
    }

    // Dev seed IdP issuer (authentik local dev)
    private const string DevIdpIssuer = "http://localhost:9000/application/o/nebula/";

    // BrokerUser dev seed tenant ID — must match authentik blueprint broker001 property mapping (F0009 §9).
    internal const string BrokerUserDevTenantId = "broker001-tenant-001";

    // IdpSubject values must match authentik usernames (sub_mode: user_username in blueprint).
    // Each entry here corresponds to a provisioned authentik user; data is seeded to these UserProfile IDs
    // so that logging in as the matching authentik user surfaces seeded submissions, tasks, and renewals.
    private static List<UserProfile> BuildUserProfiles(DateTime now) =>
    [
        new UserProfile { IdpIssuer = DevIdpIssuer, IdpSubject = "dev-user-001", Email = "sarah.chen@nebula.local", DisplayName = "Sarah Chen", Department = "Distribution", RegionsJson = "[\"West\",\"Central\",\"East\",\"South\"]", RolesJson = "[\"DistributionManager\"]", CreatedAt = now, UpdatedAt = now },
        new UserProfile { IdpIssuer = DevIdpIssuer, IdpSubject = "akadmin", Email = "akadmin@nebula.local", DisplayName = "Admin User", Department = "Operations", RegionsJson = "[\"West\",\"Central\",\"East\",\"South\"]", RolesJson = "[\"Admin\"]", CreatedAt = now, UpdatedAt = now },
        new UserProfile { IdpIssuer = DevIdpIssuer, IdpSubject = "john.miller", Email = "john.miller@nebula.local", DisplayName = "John Miller", Department = "Underwriting", RegionsJson = "[\"East\"]", RolesJson = "[\"Underwriter\"]", CreatedAt = now, UpdatedAt = now },
        new UserProfile { IdpIssuer = DevIdpIssuer, IdpSubject = "lisa.wong", Email = "lisa.wong@nebula.local", DisplayName = "Lisa Wong", Department = "Distribution", RegionsJson = "[\"West\"]", RolesJson = "[\"DistributionUser\"]", CreatedAt = now, UpdatedAt = now },
        // F0009 §9: BrokerUser test identity — broker_tenant_id claim must match BrokerUserDevTenantId below.
        new UserProfile { IdpIssuer = DevIdpIssuer, IdpSubject = "broker001", Email = "broker001@example.local", DisplayName = "Broker User 001", Department = "External", RegionsJson = "[]", RolesJson = "[\"BrokerUser\"]", CreatedAt = now, UpdatedAt = now },
    ];

    private static List<Account> BuildAccounts(int count, DateTime now, Random rng, Guid[] userIds)
    {
        var accounts = new List<Account>(count);
        for (var i = 1; i <= count; i++)
        {
            var createdBy = userIds[rng.Next(userIds.Length)];
            var region = Regions[rng.Next(Regions.Length)];
            var state = States[rng.Next(States.Length)];
            var displayName = $"{Pick(rng, CompanyPrefixes)} {Pick(rng, CompanySuffixes)} {i:D3}";
            accounts.Add(new Account
            {
                Name = displayName,
                StableDisplayName = displayName,
                LegalName = rng.NextDouble() < 0.18 ? null : $"{displayName} Holdings LLC",
                TaxId = rng.NextDouble() < 0.12 ? null : $"TIN-{i:D8}",
                Industry = Industries[rng.Next(Industries.Length)],
                PrimaryLineOfBusiness = rng.NextDouble() < 0.15 ? null : LineOfBusinessCodes[rng.Next(LineOfBusinessCodes.Length)],
                PrimaryState = state,
                Region = region,
                TerritoryCode = $"{region[..1].ToUpperInvariant()}-{((i - 1) % 12) + 1:D2}",
                Address1 = $"{100 + i} {region} Avenue",
                Address2 = rng.NextDouble() < 0.2 ? $"Suite {rng.Next(100, 900)}" : null,
                City = Cities[rng.Next(Cities.Length)],
                PostalCode = $"{90000 + (i % 10000):D5}",
                Country = "USA",
                Status = rng.NextDouble() < 0.9 ? "Active" : "Inactive",
                CreatedAt = now.AddDays(-rng.Next(30, 540)),
                UpdatedAt = now.AddDays(-rng.Next(1, 120)),
                CreatedByUserId = createdBy,
                UpdatedByUserId = createdBy,
            });
        }

        return accounts;
    }

    private static void AssignAccountRelationships(
        IReadOnlyList<Account> accounts,
        IReadOnlyList<Broker> brokers,
        Guid[] userIds,
        Random rng,
        DateTime now)
    {
        foreach (var account in accounts)
        {
            if (rng.NextDouble() < 0.82)
                account.BrokerOfRecordId = brokers[rng.Next(brokers.Count)].Id;

            if (rng.NextDouble() < 0.88)
                account.PrimaryProducerUserId = userIds[rng.Next(userIds.Length)];

            account.UpdatedAt = now.AddDays(-rng.Next(0, 45));
        }
    }

    private static IEnumerable<AccountContact> BuildAccountContacts(
        IReadOnlyList<Account> accounts,
        DateTime now,
        Random rng,
        Guid[] userIds)
    {
        var contacts = new List<AccountContact>(accounts.Count * 2);
        foreach (var account in accounts)
        {
            var contactCount = rng.NextDouble() < 0.55 ? 1 : 2;
            for (var i = 0; i < contactCount; i++)
            {
                var createdBy = userIds[rng.Next(userIds.Length)];
                var first = Pick(rng, FirstNames);
                var last = Pick(rng, LastNames);
                var createdAt = now.AddDays(-rng.Next(5, 540));
                var updatedAt = createdAt.AddDays(rng.Next(0, 120));
                if (updatedAt > now)
                    updatedAt = now;
                contacts.Add(new AccountContact
                {
                    AccountId = account.Id,
                    FullName = $"{first} {last}",
                    Role = AccountContactRoles[rng.Next(AccountContactRoles.Length)],
                    Email = $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}.{account.Id.ToString("N")[..6]}{i}@example.local",
                    Phone = $"+1-{rng.Next(200, 999)}-{rng.Next(200, 999)}-{rng.Next(1000, 9999)}",
                    IsPrimary = i == 0,
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt,
                    CreatedByUserId = createdBy,
                    UpdatedByUserId = createdBy,
                });
            }
        }

        return contacts;
    }

    private static List<Policy> BuildPolicies(
        int count,
        DateTime now,
        Random rng,
        Guid[] userIds,
        IReadOnlyList<Account> accounts,
        IReadOnlyList<Broker> brokers,
        IReadOnlyList<CarrierRef> carriers)
    {
        var policies = new List<Policy>(count);
        for (var i = 1; i <= count; i++)
        {
            var createdBy = userIds[rng.Next(userIds.Length)];
            var account = accounts[rng.Next(accounts.Count)];
            var broker = brokers[rng.Next(brokers.Count)];
            var carrier = carriers[rng.Next(carriers.Count)];
            var expirationDate = now.Date.AddDays(rng.Next(-45, 210));
            var effectiveDate = expirationDate.AddYears(-1);
            var lineOfBusiness = LineOfBusinessCodes[rng.Next(LineOfBusinessCodes.Length)];
            var status = expirationDate.Date < now.Date
                ? "Expired"
                : WeightedPick(rng, ("Issued", 85), ("Pending", 10), ("Cancelled", 5));
            var versionId = Guid.NewGuid();

            policies.Add(new Policy
            {
                PolicyNumber = $"NEB-{lineOfBusiness[..Math.Min(4, lineOfBusiness.Length)].ToUpperInvariant()}-{effectiveDate.Year}-{i:D6}",
                AccountId = account.Id,
                BrokerId = broker.Id,
                CarrierId = carrier.Id,
                LineOfBusiness = lineOfBusiness,
                EffectiveDate = effectiveDate,
                ExpirationDate = expirationDate,
                TotalPremium = Math.Round((decimal)(rng.Next(12_000, 250_000) + rng.NextDouble()), 2),
                PremiumCurrency = "USD",
                CurrentStatus = status,
                CurrentVersionId = versionId,
                IssuedAt = status is "Issued" or "Expired" ? effectiveDate.AddDays(rng.Next(0, 14)) : null,
                BoundAt = status is "Issued" or "Expired" ? effectiveDate.AddDays(rng.Next(0, 14)) : null,
                CancelledAt = status == "Cancelled" ? now.AddDays(-rng.Next(1, 30)) : null,
                CancellationEffectiveDate = status == "Cancelled" ? now.Date.AddDays(-rng.Next(1, 15)) : null,
                CancellationReasonCode = status == "Cancelled" ? "InsuredRequest" : null,
                ReinstatementDeadline = status == "Cancelled" ? now.Date.AddDays(rng.Next(1, 30)) : null,
                ExpiredAt = status == "Expired" ? expirationDate.Date.AddDays(1) : null,
                ImportSource = "manual",
                AccountDisplayNameAtLink = account.StableDisplayName,
                AccountStatusAtRead = account.Status,
                AccountSurvivorId = account.MergedIntoAccountId,
                Account = account,
                Broker = broker,
                CreatedAt = now.AddDays(-rng.Next(30, 720)),
                UpdatedAt = now.AddDays(-rng.Next(0, 120)),
                CreatedByUserId = createdBy,
                UpdatedByUserId = createdBy,
            });
        }

        return policies;
    }

    private static List<PolicyVersion> BuildPolicyVersions(IReadOnlyList<Policy> policies, DateTime now, Guid userId) =>
        policies.Select(policy => new PolicyVersion
        {
            Id = policy.CurrentVersionId!.Value,
            PolicyId = policy.Id,
            VersionNumber = 1,
            VersionReason = "IssuedInitial",
            EffectiveDate = policy.EffectiveDate,
            ExpirationDate = policy.ExpirationDate,
            TotalPremium = policy.TotalPremium,
            PremiumCurrency = policy.PremiumCurrency,
            ProfileSnapshotJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                policy.AccountId,
                brokerOfRecordId = policy.BrokerId,
                policy.CarrierId,
                policy.ProducerUserId,
            }),
            CoverageSnapshotJson = "[]",
            PremiumSnapshotJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                totalPremium = policy.TotalPremium,
                premiumCurrency = policy.PremiumCurrency,
            }),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        }).ToList();

    private static List<PolicyCoverageLine> BuildPolicyCoverageLines(IReadOnlyList<Policy> policies, DateTime now, Guid userId) =>
        policies.Select(policy => new PolicyCoverageLine
        {
            PolicyId = policy.Id,
            PolicyVersionId = policy.CurrentVersionId!.Value,
            VersionNumber = 1,
            CoverageCode = policy.LineOfBusiness,
            CoverageName = policy.LineOfBusiness,
            Limit = policy.TotalPremium * 10,
            Premium = policy.TotalPremium,
            PremiumCurrency = policy.PremiumCurrency,
            IsCurrent = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        }).ToList();

    private static void ApplySeededAccountLifecycleFixtures(
        IReadOnlyList<Account> accounts,
        IReadOnlyList<Submission> submissions,
        IReadOnlyList<Renewal> renewals,
        IReadOnlyList<Policy> policies,
        DateTime now,
        Guid actorUserId)
    {
        var accountsWithDependents = accounts
            .Where(account =>
                string.Equals(account.Status, AccountStatuses.Active, StringComparison.Ordinal)
                && (submissions.Any(submission => submission.AccountId == account.Id)
                    || renewals.Any(renewal => renewal.AccountId == account.Id)
                    || policies.Any(policy => policy.AccountId == account.Id)))
            .Take(3)
            .ToArray();

        if (accountsWithDependents.Length < 3)
            return;

        var survivor = accountsWithDependents[0];
        var merged = accountsWithDependents[1];
        var deleted = accountsWithDependents[2];

        merged.Status = AccountStatuses.Merged;
        merged.MergedIntoAccountId = survivor.Id;
        merged.RemovedAt = now.AddDays(-7);
        merged.UpdatedAt = now.AddDays(-7);
        merged.UpdatedByUserId = actorUserId;

        deleted.Status = AccountStatuses.Deleted;
        deleted.RemovedAt = now.AddDays(-3);
        deleted.DeleteReasonCode = "Duplicate";
        deleted.DeleteReasonDetail = "Dev-seeded duplicate account archived for fallback contract coverage.";
        deleted.UpdatedAt = now.AddDays(-3);
        deleted.UpdatedByUserId = actorUserId;

        ApplyFallbackState(merged, submissions, renewals, policies);
        ApplyFallbackState(deleted, submissions, renewals, policies);
    }

    private static void ApplyFallbackState(
        Account account,
        IEnumerable<Submission> submissions,
        IEnumerable<Renewal> renewals,
        IEnumerable<Policy> policies)
    {
        foreach (var submission in submissions.Where(submission => submission.AccountId == account.Id))
        {
            submission.AccountDisplayNameAtLink = account.StableDisplayName;
            submission.AccountStatusAtRead = account.Status;
            submission.AccountSurvivorId = account.MergedIntoAccountId;
        }

        foreach (var renewal in renewals.Where(renewal => renewal.AccountId == account.Id))
        {
            renewal.AccountDisplayNameAtLink = account.StableDisplayName;
            renewal.AccountStatusAtRead = account.Status;
            renewal.AccountSurvivorId = account.MergedIntoAccountId;
        }

        foreach (var policy in policies.Where(policy => policy.AccountId == account.Id))
        {
            policy.AccountDisplayNameAtLink = account.StableDisplayName;
            policy.AccountStatusAtRead = account.Status;
            policy.AccountSurvivorId = account.MergedIntoAccountId;
        }
    }

    private static List<Broker> BuildBrokers(
        int count,
        DateTime now,
        Random rng,
        Guid[] userIds,
        MGA[] mgas,
        Nebula.Domain.Entities.Program[] programs)
    {
        var brokers = new List<Broker>(count);
        for (var i = 1; i <= count; i++)
        {
            var createdBy = userIds[rng.Next(userIds.Length)];
            var state = States[rng.Next(States.Length)];
            var status = WeightedPick(rng,
                ("Active", 72),
                ("Pending", 18),
                ("Inactive", 10));

            brokers.Add(new Broker
            {
                LegalName = $"{Pick(rng, BrokerNamePrefixes)} {Pick(rng, BrokerNameSuffixes)} {i:D3}",
                LicenseNumber = $"{state}-{2024 + (i % 3)}-{i:D5}",
                State = state,
                Status = status,
                Email = $"broker{i:D3}@example.local",
                Phone = $"+1-{rng.Next(200, 999)}-{rng.Next(200, 999)}-{rng.Next(1000, 9999)}",
                ManagedByUserId = status == "Inactive" && rng.NextDouble() < 0.5 ? null : userIds[rng.Next(userIds.Length)],
                MgaId = rng.NextDouble() < 0.8 ? mgas[rng.Next(mgas.Length)].Id : null,
                PrimaryProgramId = rng.NextDouble() < 0.75 ? programs[rng.Next(programs.Length)].Id : null,
                CreatedAt = now.AddDays(-rng.Next(20, 720)),
                UpdatedAt = now.AddDays(-rng.Next(0, 120)),
                CreatedByUserId = createdBy,
                UpdatedByUserId = createdBy,
            });
        }

        return brokers;
    }

    private static IEnumerable<BrokerRegion> BuildBrokerRegions(IReadOnlyList<Broker> brokers, Random rng)
    {
        var rows = new List<BrokerRegion>(brokers.Count * 2);
        foreach (var broker in brokers)
        {
            var regionCount = rng.NextDouble() < 0.7 ? 1 : 2;
            var selected = Regions.OrderBy(_ => rng.Next()).Take(regionCount).ToList();
            foreach (var region in selected)
            {
                rows.Add(new BrokerRegion { BrokerId = broker.Id, Region = region });
            }
        }
        return rows;
    }

    private static IEnumerable<Contact> BuildContacts(IReadOnlyList<Broker> brokers, DateTime now, Random rng, Guid[] userIds)
    {
        var contacts = new List<Contact>(brokers.Count * 2);
        for (var i = 0; i < brokers.Count; i++)
        {
            var broker = brokers[i];
            var contactCount = broker.Status == "Inactive" ? 1 : (rng.NextDouble() < 0.5 ? 1 : 2);
            for (var c = 0; c < contactCount; c++)
            {
                var createdBy = userIds[rng.Next(userIds.Length)];
                var first = Pick(rng, FirstNames);
                var last = Pick(rng, LastNames);
                contacts.Add(new Contact
                {
                    BrokerId = broker.Id,
                    FullName = $"{first} {last}",
                    Email = $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}.{i:D3}{c}@example.local",
                    Phone = $"+1-{rng.Next(200, 999)}-{rng.Next(200, 999)}-{rng.Next(1000, 9999)}",
                    Role = BrokerRoles[rng.Next(BrokerRoles.Length)],
                    CreatedAt = now.AddDays(-rng.Next(10, 600)),
                    UpdatedAt = now.AddDays(-rng.Next(0, 90)),
                    CreatedByUserId = createdBy,
                    UpdatedByUserId = createdBy,
                });
            }
        }

        return contacts;
    }

    private static List<TaskItem> BuildTasks(
        DateTime now,
        Random rng,
        Guid[] userIds,
        IReadOnlyList<Submission> submissions,
        IReadOnlyList<Renewal> renewals,
        IReadOnlyList<Broker> brokers)
    {
        var tasks = new List<TaskItem>(96);
        for (var i = 0; i < 96; i++)
        {
            var assignedTo = userIds[rng.Next(userIds.Length)];
            var status = WeightedPick(rng, ("Open", 48), ("InProgress", 34), ("Done", 18));
            var dueDate = now.Date.AddDays(rng.Next(-10, 21));
            Guid? linkedId = null;
            string? linkedType = null;
            string title;

            switch (rng.Next(3))
            {
                case 0:
                    var sub = submissions[rng.Next(submissions.Count)];
                    linkedId = sub.Id;
                    linkedType = "Submission";
                    title = $"Review submission in {sub.CurrentStatus}";
                    break;
                case 1:
                    var ren = renewals[rng.Next(renewals.Count)];
                    linkedId = ren.Id;
                    linkedType = "Renewal";
                    title = $"Follow renewal {ren.CurrentStatus}";
                    break;
                default:
                    var broker = brokers[rng.Next(brokers.Count)];
                    linkedId = broker.Id;
                    linkedType = "Broker";
                    title = "Broker outreach follow-up";
                    break;
            }

            tasks.Add(new TaskItem
            {
                Title = title,
                Description = rng.NextDouble() < 0.35 ? "Generated dev task for dashboard nudges and task list coverage." : null,
                Status = status,
                Priority = TaskPriorities[rng.Next(TaskPriorities.Length)],
                DueDate = dueDate,
                AssignedToUserId = assignedTo,
                LinkedEntityType = linkedType,
                LinkedEntityId = linkedId,
                CompletedAt = status == "Done" ? dueDate.AddDays(rng.Next(-2, 2)) : null,
                CreatedAt = now.AddDays(-rng.Next(1, 45)),
                UpdatedAt = now.AddDays(-rng.Next(0, 10)),
                CreatedByUserId = assignedTo,
                UpdatedByUserId = assignedTo,
            });
        }

        return tasks;
    }

    private static IEnumerable<ActivityTimelineEvent> BuildBrokerTimelineEvents(
        DateTime now,
        Random rng,
        IReadOnlyDictionary<Guid, string> userNameById,
        IReadOnlyList<Broker> brokers)
    {
        var events = new List<ActivityTimelineEvent>(brokers.Count + 40);
        foreach (var broker in brokers.Take(160))
        {
            var actor = broker.CreatedByUserId;
            events.Add(new ActivityTimelineEvent
            {
                EntityType = "Broker",
                EntityId = broker.Id,
                EventType = "BrokerCreated",
                EventDescription = $"New broker \"{broker.LegalName}\" added",
                ActorUserId = actor,
                ActorDisplayName = userNameById.GetValueOrDefault(actor),
                OccurredAt = broker.CreatedAt,
            });

            if (broker.Status == "Inactive" && rng.NextDouble() < 0.5)
            {
                events.Add(new ActivityTimelineEvent
                {
                    EntityType = "Broker",
                    EntityId = broker.Id,
                    EventType = "BrokerStatusChanged",
                    EventDescription = $"Broker \"{broker.LegalName}\" marked Inactive",
                    ActorUserId = actor,
                    ActorDisplayName = userNameById.GetValueOrDefault(actor),
                    OccurredAt = broker.UpdatedAt,
                });
            }
        }

        return events;
    }

    private static IEnumerable<ActivityTimelineEvent> BuildTransitionTimelineEvents(
        Random rng,
        IReadOnlyDictionary<Guid, string> userNameById,
        IReadOnlyList<WorkflowTransition> transitions)
    {
        return transitions
            .Where(t => rng.NextDouble() < 0.22)
            .OrderByDescending(t => t.OccurredAt)
            .Take(220)
            .Select(t => new ActivityTimelineEvent
            {
                EntityType = t.WorkflowType,
                EntityId = t.EntityId,
                EventType = $"{t.WorkflowType}Transitioned",
                EventDescription = $"{t.WorkflowType} transitioned from {t.FromState} to {t.ToState}",
                ActorUserId = t.ActorUserId,
                ActorDisplayName = userNameById.GetValueOrDefault(t.ActorUserId),
                OccurredAt = t.OccurredAt,
            })
            .ToList();
    }

    private static void PatchRecentTransitionsEntityId(List<WorkflowTransition> transitions, Guid entityId)
    {
        for (var i = transitions.Count - 1; i >= 0 && transitions[i].EntityId == Guid.Empty; i--)
        {
            transitions[i].EntityId = entityId;
        }
    }

    private static List<string> GenerateWorkflowPath(
        Random rng,
        string start,
        IReadOnlyDictionary<string, string[]> nextStateMap,
        IReadOnlySet<string> terminalStatuses,
        bool chooseSubmissionTerminal)
    {
        var path = new List<string> { start };
        var current = start;

        for (var step = 0; step < 9; step++)
        {
            var shouldTerminate = step >= 1 && rng.NextDouble() < (step < 3 ? 0.14 : step < 5 ? 0.28 : 0.52);
            var canTerminate = chooseSubmissionTerminal || CanTerminateRenewalFromState(current);
            if (shouldTerminate && canTerminate && !terminalStatuses.Contains(current))
            {
                path.Add(chooseSubmissionTerminal
                    ? PickSubmissionTerminal(rng, current)
                    : PickRenewalTerminal(rng, current));
                break;
            }

            if (!nextStateMap.TryGetValue(current, out var nextStates) || nextStates.Length == 0)
                break;

            var next = nextStates[rng.Next(nextStates.Length)];
            path.Add(next);
            current = next;

            if (terminalStatuses.Contains(current))
                break;
        }

        if (!terminalStatuses.Contains(path[^1])
            && (chooseSubmissionTerminal || CanTerminateRenewalFromState(path[^1]))
            && rng.NextDouble() < 0.38)
        {
            path.Add(chooseSubmissionTerminal
                ? PickSubmissionTerminal(rng, path[^1])
                : PickRenewalTerminal(rng, path[^1]));
        }

        return path;
    }

    private static string PickSubmissionTerminal(Random rng, string current) => current switch
    {
        "Received" or "Triaging" or "WaitingOnBroker" => WeightedPick(rng,
            ("Withdrawn", 52), ("Declined", 48)),
        "ReadyForUWReview" or "InReview" => WeightedPick(rng,
            ("Declined", 68), ("Withdrawn", 32)),
        "Quoted" => WeightedPick(rng,
            ("Declined", 58), ("Withdrawn", 42)),
        "BindRequested" => WeightedPick(rng,
            ("Bound", 72), ("Withdrawn", 28)),
        _ => WeightedPick(rng,
            ("Bound", 52), ("Declined", 28), ("Withdrawn", 20)),
    };

    private static bool CanTerminateRenewalFromState(string current) =>
        current is "InReview" or "Quoted";

    private static string PickRenewalTerminal(Random rng, string current) => current switch
    {
        "InReview" => "Lost",
        "Quoted" => WeightedPick(rng, ("Completed", 68), ("Lost", 32)),
        _ => "Lost",
    };

    private static double RandomStepDays(Random rng, string fromState, string toState)
    {
        if (toState == "WaitingOnBroker") return rng.Next(2, 14);
        if (toState == "BindRequested") return rng.Next(1, 6);
        if (toState is "Completed" or "Lost" or "Bound" or "Declined" or "Withdrawn")
            return rng.Next(1, 10);
        return rng.Next(1, 8);
    }

    private static string? BuildTransitionReason(Random rng, string workflowType, string fromState, string toState)
    {
        if (toState == "WaitingOnBroker")
            return WeightedPick(rng, ("Need updated SOV", 35), ("Broker clarification pending", 35), ("Missing loss runs", 30));

        if (workflowType == "Submission")
        {
            return toState switch
            {
                "Declined" => WeightedPick(rng, ("Carrier appetite", 35), ("Eligibility", 25), ("Loss history", 20), ("Incomplete information", 20)),
                "Withdrawn" => WeightedPick(rng, ("Broker withdrew", 40), ("Insured withdrew", 35), ("Timing changed", 25)),
                "BindRequested" => WeightedPick(rng, ("Broker accepted quoted terms", 50), ("Bind order received", 30), ("Coverage finalized", 20)),
                _ => null,
            };
        }

        return toState switch
        {
            "Lost" => WeightedPick(rng, ("CompetitiveLoss", 45), ("NonRenewal", 30), ("PricingDeclined", 15), ("CoverageNoLongerNeeded", 10)),
            _ => null,
        };
    }

    private static int GetRenewalTargetDays(string? lineOfBusiness) => lineOfBusiness switch
    {
        "WorkersCompensation" => 120,
        "Cyber" => 60,
        _ => 90,
    };

    private static async Task EnsureReferenceStatusesAsync(AppDbContext db)
    {
        await UpsertSubmissionReferenceStatusesAsync(db);
        await UpsertRenewalReferenceStatusesAsync(db);
    }

    /// <summary>
    /// Idempotently sets BrokerTenantId on one active broker so the BrokerUser dev identity
    /// (broker001) can resolve their scope. Runs before the early-return so it applies even
    /// when data was seeded before the F0009 migration added the column.
    /// </summary>
    private static async Task EnsureDevBrokerTenantIdAsync(AppDbContext db)
    {
        var alreadyLinked = await db.Brokers
            .AnyAsync(b => b.BrokerTenantId == BrokerUserDevTenantId);
        if (alreadyLinked) return;

        var broker = await db.Brokers.FirstOrDefaultAsync(b => b.Status == "Active")
                     ?? await db.Brokers.FirstOrDefaultAsync();
        if (broker is null) return;

        broker.BrokerTenantId = BrokerUserDevTenantId;
        await db.SaveChangesAsync();
    }

    private static async Task EnsureCarrierRefsAsync(AppDbContext db)
    {
        var existing = await db.CarrierRefs
            .Select(carrier => carrier.Name)
            .ToListAsync();
        var missing = CarrierNames
            .Where(name => !existing.Contains(name, StringComparer.OrdinalIgnoreCase))
            .Select(name => new CarrierRef
            {
                Name = name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByUserId = Guid.Empty,
                UpdatedByUserId = Guid.Empty,
            })
            .ToList();

        if (missing.Count == 0)
            return;

        db.CarrierRefs.AddRange(missing);
        await db.SaveChangesAsync();
    }

    private static async Task UpsertSubmissionReferenceStatusesAsync(AppDbContext db)
    {
        var existing = await db.ReferenceSubmissionStatuses.ToDictionaryAsync(s => s.Code);
        var desired = OpportunityStatusCatalog.SubmissionStatuses;

        if (existing.Count > 0)
        {
            foreach (var row in existing.Values)
            {
                row.DisplayOrder = (short)(row.DisplayOrder + 100);
            }
            await db.SaveChangesAsync();
        }

        foreach (var status in desired)
        {
            if (!existing.TryGetValue(status.Code, out var row))
            {
                db.ReferenceSubmissionStatuses.Add(new ReferenceSubmissionStatus
                {
                    Code = status.Code,
                    DisplayName = status.DisplayName,
                    Description = status.Description,
                    IsTerminal = status.IsTerminal,
                    DisplayOrder = status.DisplayOrder,
                    ColorGroup = status.ColorGroup,
                });
                continue;
            }

            row.DisplayName = status.DisplayName;
            row.Description = status.Description;
            row.IsTerminal = status.IsTerminal;
            row.DisplayOrder = status.DisplayOrder;
            row.ColorGroup = status.ColorGroup;
        }

        await db.SaveChangesAsync();
    }

    private static async Task UpsertRenewalReferenceStatusesAsync(AppDbContext db)
    {
        var existing = await db.ReferenceRenewalStatuses.ToDictionaryAsync(s => s.Code);
        var desired = OpportunityStatusCatalog.RenewalStatuses;

        if (existing.Count > 0)
        {
            foreach (var row in existing.Values)
            {
                row.DisplayOrder = (short)(row.DisplayOrder + 100);
            }
            await db.SaveChangesAsync();
        }

        foreach (var status in desired)
        {
            if (!existing.TryGetValue(status.Code, out var row))
            {
                db.ReferenceRenewalStatuses.Add(new ReferenceRenewalStatus
                {
                    Code = status.Code,
                    DisplayName = status.DisplayName,
                    Description = status.Description,
                    IsTerminal = status.IsTerminal,
                    DisplayOrder = status.DisplayOrder,
                    ColorGroup = status.ColorGroup,
                });
                continue;
            }

            row.DisplayName = status.DisplayName;
            row.Description = status.Description;
            row.IsTerminal = status.IsTerminal;
            row.DisplayOrder = status.DisplayOrder;
            row.ColorGroup = status.ColorGroup;
        }

        await db.SaveChangesAsync();
    }

    private static T WeightedPick<T>(Random rng, params (T Item, int Weight)[] choices)
    {
        var totalWeight = choices.Sum(c => c.Weight);
        var roll = rng.Next(1, totalWeight + 1);
        var cumulative = 0;
        foreach (var (item, weight) in choices)
        {
            cumulative += weight;
            if (roll <= cumulative) return item;
        }

        return choices[^1].Item;
    }

    private static T Pick<T>(Random rng, IReadOnlyList<T> values) => values[rng.Next(values.Count)];

    private static readonly string[] CompanyPrefixes =
    [
        "Northstar", "Blue Harbor", "Summit", "Granite", "Crescent", "Atlas", "Harbor", "Redwood",
        "Beacon", "Pioneer", "Frontier", "Sterling", "Cobalt", "Highline", "Riverbend", "Oakridge"
    ];

    private static readonly string[] CompanySuffixes =
    [
        "Manufacturing", "Logistics", "Health", "Foods", "Systems", "Transport", "Builders", "Holdings",
        "Services", "Distribution", "Packaging", "Supply", "Retail", "Dynamics", "Materials", "Partners"
    ];

    private static readonly string[] BrokerNamePrefixes =
    [
        "Blue Horizon", "Northline", "Cedar", "Ironwood", "Summit", "Pinnacle", "Harbor", "Compass",
        "Crown", "Meridian", "Anchor", "Evergreen", "Stonegate", "Red River", "Skyline", "Legacy"
    ];

    private static readonly string[] BrokerNameSuffixes =
    [
        "Brokerage", "Risk Partners", "Insurance Group", "Advisors", "Placement Services", "Wholesale", "Markets", "Agency"
    ];

    private static readonly string[] CarrierNames =
    [
        "Archway Specialty", "Blue Atlas Insurance", "Summit National", "Frontier Casualty",
        "Compass Mutual", "Harbor Re", "Northstar Indemnity", "Sterling Insurance Co."
    ];

    private static readonly string[] FirstNames =
    [
        "Alex", "Jamie", "Taylor", "Jordan", "Morgan", "Casey", "Riley", "Avery", "Cameron", "Drew",
        "Parker", "Reese", "Quinn", "Sam", "Blake", "Hayden", "Logan", "Kendall", "Rowan", "Sydney"
    ];

    private static readonly string[] LastNames =
    [
        "Anderson", "Brooks", "Carter", "Diaz", "Edwards", "Foster", "Garcia", "Hayes", "Iverson", "Jenkins",
        "Kim", "Lopez", "Mitchell", "Nguyen", "Owens", "Patel", "Reed", "Sanchez", "Turner", "Young"
    ];
}
