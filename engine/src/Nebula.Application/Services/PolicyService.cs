using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class PolicyService(
    IPolicyRepository policyRepo,
    IWorkflowTransitionRepository transitionRepo,
    ITimelineRepository timelineRepo,
    IWorkflowSlaThresholdRepository workflowSlaThresholdRepo,
    BrokerScopeResolver scopeResolver,
    IUnitOfWork unitOfWork)
{
    private const string Pending = "Pending";
    private const string Issued = "Issued";
    private const string Cancelled = "Cancelled";
    private const string Expired = "Expired";
    private const string WorkflowType = "PolicyLifecycle";
    private const string EntityType = "Policy";

    public async Task<PaginatedResult<PolicyListItemDto>> ListAsync(
        PolicyListQuery query,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var policies = await policyRepo.ListAsync(query, brokerScopeId, ct);
        var items = new List<PolicyListItemDto>(policies.Data.Count);

        foreach (var policy in policies.Data)
        {
            items.Add(await MapListItemAsync(policy, ct));
        }

        return new PaginatedResult<PolicyListItemDto>(items, policies.Page, policies.PageSize, policies.TotalCount);
    }

    public async Task<PolicyDto?> GetByIdAsync(Guid id, ICurrentUserService user, CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var policy = await policyRepo.GetAccessibleByIdAsync(id, user, brokerScopeId, ct);
        return policy is null ? null : await MapDtoAsync(policy, user, ct);
    }

    public async Task<PolicySummaryDto?> GetSummaryAsync(Guid id, ICurrentUserService user, CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        var policy = await policyRepo.GetAccessibleByIdAsync(id, user, brokerScopeId, ct);
        return policy is null ? null : await MapSummaryAsync(policy, user, ct);
    }

    public async Task<bool> ExistsAccessibleAsync(Guid id, ICurrentUserService user, CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        return await policyRepo.GetAccessibleByIdAsync(id, user, brokerScopeId, ct) is not null;
    }

    public async Task<(PolicyDto? Dto, string? ErrorCode)> CreateAsync(
        PolicyCreateRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var account = await policyRepo.GetAccountByIdAsync(dto.AccountId, ct);
        if (account is null)
            return (null, "invalid_account");

        var broker = await policyRepo.GetBrokerByIdAsync(dto.BrokerOfRecordId, ct);
        if (broker is null)
            return (null, "invalid_broker");

        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        if (!CanCreateForScope(account, broker, user, brokerScopeId))
            return (null, "out_of_scope");

        var carrier = await policyRepo.GetCarrierByIdAsync(dto.CarrierId, ct);
        if (carrier is null)
            return (null, "invalid_carrier");

        if (dto.ProducerUserId.HasValue && !await policyRepo.ProducerExistsAsync(dto.ProducerUserId.Value, ct))
            return (null, "invalid_producer");

        if (dto.PredecessorPolicyId.HasValue)
        {
            var predecessor = await policyRepo.GetByIdWithRelationsAsync(dto.PredecessorPolicyId.Value, ct);
            if (predecessor is null || predecessor.AccountId != dto.AccountId || predecessor.CurrentStatus is not (Expired or Cancelled))
                return (null, "invalid_predecessor");
        }

        var now = DateTime.UtcNow;
        var coverages = NormalizeCoverages(dto.Coverages);
        var totalPremium = dto.TotalPremium ?? coverages.Sum(coverage => coverage.Premium);
        var isImport = string.Equals(dto.ImportMode, "csv-import", StringComparison.Ordinal);
        var policy = new Policy
        {
            PolicyNumber = await GeneratePolicyNumberAsync(dto.LineOfBusiness, now.Year, ct),
            AccountId = dto.AccountId,
            BrokerId = dto.BrokerOfRecordId,
            CarrierId = dto.CarrierId,
            LineOfBusiness = dto.LineOfBusiness,
            EffectiveDate = dto.EffectiveDate.Date,
            ExpirationDate = dto.ExpirationDate.Date,
            TotalPremium = totalPremium,
            PremiumCurrency = dto.PremiumCurrency ?? "USD",
            CurrentStatus = isImport ? Issued : Pending,
            IssuedAt = isImport ? now : null,
            BoundAt = isImport ? now : null,
            PredecessorPolicyId = dto.PredecessorPolicyId,
            ProducerUserId = dto.ProducerUserId,
            ImportSource = isImport ? "csv-import" : "manual",
            ExternalPolicyReference = dto.ExternalPolicyReference,
            AccountDisplayNameAtLink = account.StableDisplayName,
            AccountStatusAtRead = account.Status,
            AccountSurvivorId = account.MergedIntoAccountId,
            CreatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedAt = now,
            UpdatedByUserId = user.UserId,
        };

        var version = BuildVersion(policy, "IssuedInitial", null, coverages, null, now, user.UserId);
        policy.CurrentVersionId = version.Id;

        await policyRepo.AddAsync(policy, ct);
        await policyRepo.AddVersionAsync(version, ct);
        await policyRepo.AddCoverageLinesAsync(BuildCoverageLines(policy, version, coverages, now, user.UserId), ct);
        await AddTransitionAndTimelineAsync(policy, null, policy.CurrentStatus, "PolicyCreated", $"Policy {policy.PolicyNumber} created", user, now, ct);
        await unitOfWork.CommitAsync(ct);

        var created = await policyRepo.GetByIdWithRelationsAsync(policy.Id, ct)
            ?? throw new InvalidOperationException("Created policy could not be reloaded.");
        return (await MapDtoAsync(created, user, ct), null);
    }

    public async Task<PolicyImportResultDto> ImportAsync(
        PolicyImportRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var accepted = new List<PolicyDto>();
        var rejected = new List<PolicyImportRejectedRowDto>();

        for (var i = 0; i < dto.Policies.Count; i++)
        {
            var request = dto.Policies[i] with { ImportMode = "csv-import" };
            var (created, error) = await CreateAsync(request, user, ct);
            if (error is null && created is not null)
                accepted.Add(created);
            else
                rejected.Add(new PolicyImportRejectedRowDto(i, error ?? "unknown_error", $"Policy row {i} was rejected."));
        }

        return new PolicyImportResultDto(accepted, rejected);
    }

    public async Task<(PolicyDto? Dto, string? ErrorCode)> UpdateAsync(
        Guid id,
        PolicyUpdateRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var policy = await GetAccessibleByIdForUpdateAsync(id, user, ct);
        if (policy is null)
            return (null, "not_found");

        if (policy.RowVersion != expectedRowVersion)
            return (null, "precondition_failed");

        var materialChange =
            (dto.LineOfBusiness is not null && dto.LineOfBusiness != policy.LineOfBusiness)
            || (dto.CarrierId.HasValue && dto.CarrierId.Value != policy.CarrierId)
            || (dto.EffectiveDate.HasValue && dto.EffectiveDate.Value.Date != policy.EffectiveDate.Date)
            || (dto.ExpirationDate.HasValue && dto.ExpirationDate.Value.Date != policy.ExpirationDate.Date)
            || (dto.TotalPremium.HasValue && dto.TotalPremium.Value != policy.TotalPremium);

        if (policy.CurrentStatus != Pending && materialChange)
            return (null, "must_use_endorse");

        if (dto.CarrierId.HasValue && await policyRepo.GetCarrierByIdAsync(dto.CarrierId.Value, ct) is null)
            return (null, "invalid_carrier");

        if (dto.ProducerUserId.HasValue && !await policyRepo.ProducerExistsAsync(dto.ProducerUserId.Value, ct))
            return (null, "invalid_producer");

        policy.LineOfBusiness = dto.LineOfBusiness ?? policy.LineOfBusiness;
        policy.CarrierId = dto.CarrierId ?? policy.CarrierId;
        policy.EffectiveDate = dto.EffectiveDate?.Date ?? policy.EffectiveDate;
        policy.ExpirationDate = dto.ExpirationDate?.Date ?? policy.ExpirationDate;
        policy.TotalPremium = dto.TotalPremium ?? policy.TotalPremium;
        policy.ProducerUserId = dto.ProducerUserId ?? policy.ProducerUserId;
        policy.ExternalPolicyReference = dto.ExternalPolicyReference ?? policy.ExternalPolicyReference;
        policy.UpdatedAt = DateTime.UtcNow;
        policy.UpdatedByUserId = user.UserId;

        try
        {
            await unitOfWork.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (null, "precondition_failed");
        }

        var updated = await policyRepo.GetByIdWithRelationsAsync(id, ct)
            ?? throw new InvalidOperationException("Updated policy could not be reloaded.");
        return (await MapDtoAsync(updated, user, ct), null);
    }

    public async Task<(PolicyDto? Dto, string? ErrorCode)> IssueAsync(
        Guid id,
        PolicyIssueRequestDto? dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var policy = await GetAccessibleByIdForUpdateAsync(id, user, ct);
        if (policy is null)
            return (null, "not_found");
        if (policy.RowVersion != expectedRowVersion)
            return (null, "precondition_failed");
        if (policy.CurrentStatus != Pending)
            return (null, "invalid_transition");

        var now = DateTime.UtcNow;
        policy.CurrentStatus = Issued;
        policy.IssuedAt = dto?.IssuedAt ?? now;
        policy.BoundAt = policy.IssuedAt;
        policy.UpdatedAt = now;
        policy.UpdatedByUserId = user.UserId;

        await AddTransitionAndTimelineAsync(policy, Pending, Issued, "PolicyIssued", $"Policy {policy.PolicyNumber} issued", user, now, ct);
        await CommitPolicyChangeAsync(ct);

        var updated = await policyRepo.GetByIdWithRelationsAsync(id, ct)
            ?? throw new InvalidOperationException("Issued policy could not be reloaded.");
        return (await MapDtoAsync(updated, user, ct), null);
    }

    public async Task<(PolicyEndorsementDto? Dto, string? ErrorCode)> EndorseAsync(
        Guid id,
        PolicyEndorsementRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var policy = await GetAccessibleByIdForUpdateAsync(id, user, ct);
        if (policy is null)
            return (null, "not_found");
        if (policy.RowVersion != expectedRowVersion)
            return (null, "precondition_failed");
        if (policy.CurrentStatus != Issued)
            return (null, "invalid_transition");
        if (dto.EffectiveDate.Date < policy.EffectiveDate.Date || dto.EffectiveDate.Date > policy.ExpirationDate.Date)
            return (null, "invalid_effective_date");

        var now = DateTime.UtcNow;
        var coverages = NormalizeCoverages(dto.Coverages);
        var previousPremium = policy.TotalPremium;
        var newPremium = coverages.Sum(coverage => coverage.Premium);
        var premiumDelta = dto.PremiumDelta ?? newPremium - previousPremium;
        var endorsement = new PolicyEndorsement
        {
            PolicyId = policy.Id,
            EndorsementNumber = await policyRepo.CountEndorsementsAsync(policy.Id, ct) + 1,
            EndorsementReasonCode = dto.EndorsementReasonCode,
            EndorsementReasonDetail = dto.EndorsementReasonDetail,
            EffectiveDate = dto.EffectiveDate.Date,
            PremiumDelta = premiumDelta,
            PremiumCurrency = policy.PremiumCurrency,
            CreatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedAt = now,
            UpdatedByUserId = user.UserId,
        };
        var version = await BuildNextVersionAsync(policy, "Endorsement", endorsement.Id, coverages, premiumDelta, now, user.UserId, ct);
        endorsement.PolicyVersionId = version.Id;
        policy.CurrentVersionId = version.Id;
        policy.TotalPremium = newPremium;
        policy.UpdatedAt = now;
        policy.UpdatedByUserId = user.UserId;

        await policyRepo.SetCoverageCurrentAsync(policy.Id, version.Id, ct);
        await policyRepo.AddVersionAsync(version, ct);
        await policyRepo.AddEndorsementAsync(endorsement, ct);
        await policyRepo.AddCoverageLinesAsync(BuildCoverageLines(policy, version, coverages, now, user.UserId), ct);
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = EntityType,
            EntityId = policy.Id,
            EventType = "PolicyEndorsed",
            EventDescription = $"Endorsement {endorsement.EndorsementNumber} applied to policy {policy.PolicyNumber}",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                endorsement.Id,
                endorsement.EndorsementNumber,
                endorsement.EndorsementReasonCode,
                endorsement.EffectiveDate,
                premiumDelta,
                policyVersionId = version.Id,
            }),
        }, ct);
        await CommitPolicyChangeAsync(ct);

        return (MapEndorsement(endorsement, version.VersionNumber), null);
    }

    public async Task<(PolicyDto? Dto, string? ErrorCode)> CancelAsync(
        Guid id,
        PolicyCancelRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var policy = await GetAccessibleByIdForUpdateAsync(id, user, ct);
        if (policy is null)
            return (null, "not_found");
        if (policy.RowVersion != expectedRowVersion)
            return (null, "precondition_failed");
        if (policy.CurrentStatus != Issued)
            return (null, "invalid_transition");
        if (dto.CancellationEffectiveDate.Date < policy.EffectiveDate.Date || dto.CancellationEffectiveDate.Date > policy.ExpirationDate.Date)
            return (null, "invalid_effective_date");

        var now = DateTime.UtcNow;
        var threshold = await workflowSlaThresholdRepo.GetThresholdAsync("policy", "ReinstatementWindow", policy.LineOfBusiness, ct);
        var reinstatementWindowDays = threshold?.TargetDays ?? 30;
        policy.CurrentStatus = Cancelled;
        policy.CancelledAt = now;
        policy.CancellationEffectiveDate = dto.CancellationEffectiveDate.Date;
        policy.CancellationReasonCode = dto.CancellationReasonCode;
        policy.CancellationReasonDetail = dto.CancellationReasonDetail;
        policy.ReinstatementDeadline = dto.CancellationEffectiveDate.Date.AddDays(reinstatementWindowDays);
        policy.UpdatedAt = now;
        policy.UpdatedByUserId = user.UserId;

        await AddTransitionAndTimelineAsync(policy, Issued, Cancelled, "PolicyCancelled", $"Policy {policy.PolicyNumber} cancelled", user, now, ct);
        await CommitPolicyChangeAsync(ct);

        var updated = await policyRepo.GetByIdWithRelationsAsync(id, ct)
            ?? throw new InvalidOperationException("Cancelled policy could not be reloaded.");
        return (await MapDtoAsync(updated, user, ct), null);
    }

    public async Task<(PolicyDto? Dto, string? ErrorCode)> ReinstateAsync(
        Guid id,
        PolicyReinstateRequestDto dto,
        uint expectedRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var policy = await GetAccessibleByIdForUpdateAsync(id, user, ct);
        if (policy is null)
            return (null, "not_found");
        if (policy.RowVersion != expectedRowVersion)
            return (null, "precondition_failed");
        if (policy.CurrentStatus != Cancelled)
            return (null, "invalid_transition");
        if (!policy.ReinstatementDeadline.HasValue || policy.ReinstatementDeadline.Value.Date < DateTime.UtcNow.Date)
            return (null, "reinstatement_window_expired");

        var now = DateTime.UtcNow;
        var currentCoverage = await policyRepo.ListCurrentCoverageLinesAsync(policy.Id, ct);
        var coverages = currentCoverage.Select(line => new PolicyCoverageInputDto(
            line.CoverageCode,
            line.CoverageName,
            line.Limit,
            line.Deductible,
            line.Premium,
            line.ExposureBasis,
            line.ExposureQuantity)).ToList();
        var version = await BuildNextVersionAsync(policy, "Reinstatement", null, coverages, null, now, user.UserId, ct);
        policy.CurrentVersionId = version.Id;
        policy.CurrentStatus = Issued;
        policy.CancelledAt = null;
        policy.CancellationEffectiveDate = null;
        policy.CancellationReasonCode = null;
        policy.CancellationReasonDetail = null;
        policy.ReinstatementDeadline = null;
        policy.UpdatedAt = now;
        policy.UpdatedByUserId = user.UserId;

        await policyRepo.SetCoverageCurrentAsync(policy.Id, version.Id, ct);
        await policyRepo.AddVersionAsync(version, ct);
        await policyRepo.AddCoverageLinesAsync(BuildCoverageLines(policy, version, coverages, now, user.UserId), ct);
        await AddTransitionAndTimelineAsync(policy, Cancelled, Issued, "PolicyReinstated", $"Policy {policy.PolicyNumber} reinstated", user, now, ct);
        await CommitPolicyChangeAsync(ct);

        var updated = await policyRepo.GetByIdWithRelationsAsync(id, ct)
            ?? throw new InvalidOperationException("Reinstated policy could not be reloaded.");
        return (await MapDtoAsync(updated, user, ct), null);
    }

    public async Task<PaginatedResult<PolicyVersionDto>?> ListVersionsAsync(Guid policyId, int page, int pageSize, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await ExistsAccessibleAsync(policyId, user, ct))
            return null;
        var versions = await policyRepo.ListVersionsAsync(policyId, page, pageSize, ct);
        return new PaginatedResult<PolicyVersionDto>(versions.Data.Select(MapVersion).ToList(), versions.Page, versions.PageSize, versions.TotalCount);
    }

    public async Task<PolicyVersionDto?> GetVersionAsync(Guid policyId, Guid versionId, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await ExistsAccessibleAsync(policyId, user, ct))
            return null;
        var version = await policyRepo.GetVersionAsync(policyId, versionId, ct);
        return version is null ? null : MapVersion(version);
    }

    public async Task<PaginatedResult<PolicyEndorsementDto>?> ListEndorsementsAsync(Guid policyId, int page, int pageSize, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await ExistsAccessibleAsync(policyId, user, ct))
            return null;
        var endorsements = await policyRepo.ListEndorsementsAsync(policyId, page, pageSize, ct);
        return new PaginatedResult<PolicyEndorsementDto>(
            endorsements.Data.Select(endorsement => MapEndorsement(endorsement, endorsement.PolicyVersion.VersionNumber)).ToList(),
            endorsements.Page,
            endorsements.PageSize,
            endorsements.TotalCount);
    }

    public async Task<IReadOnlyList<PolicyCoverageLineDto>?> ListCurrentCoverageLinesAsync(Guid policyId, ICurrentUserService user, CancellationToken ct = default)
    {
        if (!await ExistsAccessibleAsync(policyId, user, ct))
            return null;
        return (await policyRepo.ListCurrentCoverageLinesAsync(policyId, ct)).Select(MapCoverageLine).ToList();
    }

    public async Task<PolicyAccountSummaryDto?> GetAccountSummaryAsync(Guid accountId, ICurrentUserService user, CancellationToken ct = default)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        return await policyRepo.GetAccountSummaryAsync(accountId, user, brokerScopeId, ct);
    }

    public async Task<int> ExpireIssuedPoliciesAsync(DateTime utcNow, int maxBatchSize = 250, CancellationToken ct = default)
    {
        var systemUser = SystemPolicyUser.Instance;
        var policies = await policyRepo.ListIssuedPoliciesExpiredBeforeAsync(utcNow.Date, maxBatchSize, ct);
        if (policies.Count == 0)
            return 0;

        foreach (var policy in policies)
        {
            policy.CurrentStatus = Expired;
            policy.ExpiredAt = utcNow;
            policy.UpdatedAt = utcNow;
            policy.UpdatedByUserId = systemUser.UserId;

            await AddTransitionAndTimelineAsync(
                policy,
                Issued,
                Expired,
                "PolicyExpired",
                $"Policy {policy.PolicyNumber} expired",
                systemUser,
                utcNow,
                ct);
        }

        await CommitPolicyChangeAsync(ct);
        return policies.Count;
    }

    private async Task<Guid?> ResolveBrokerScopeAsync(ICurrentUserService user, CancellationToken ct)
    {
        if (!HasRole(user, "BrokerUser"))
            return null;

        return await scopeResolver.ResolveAsync(user, ct);
    }

    private async Task<Policy?> GetAccessibleByIdForUpdateAsync(Guid id, ICurrentUserService user, CancellationToken ct)
    {
        var brokerScopeId = await ResolveBrokerScopeAsync(user, ct);
        if (await policyRepo.GetAccessibleByIdAsync(id, user, brokerScopeId, ct) is null)
            return null;

        return await policyRepo.GetByIdForUpdateAsync(id, ct);
    }

    private static bool CanCreateForScope(Account account, Broker broker, ICurrentUserService user, Guid? brokerScopeId)
    {
        if (HasRole(user, "Admin") || HasRole(user, "Underwriter"))
            return true;

        var callerRegions = NormalizeRegions(user.Regions);
        if ((HasRole(user, "DistributionUser") || HasRole(user, "DistributionManager"))
            && !string.IsNullOrWhiteSpace(account.Region)
            && callerRegions.Contains(account.Region))
            return true;

        if (HasRole(user, "RelationshipManager") && broker.ManagedByUserId == user.UserId)
            return true;

        return HasRole(user, "BrokerUser") && brokerScopeId.HasValue && broker.Id == brokerScopeId.Value;
    }

    private static bool HasRole(ICurrentUserService user, string role) =>
        user.Roles.Any(existingRole => string.Equals(existingRole, role, StringComparison.OrdinalIgnoreCase));

    private static HashSet<string> NormalizeRegions(IReadOnlyList<string> regions) =>
        regions
            .Where(region => !string.IsNullOrWhiteSpace(region))
            .Select(region => region.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private async Task<string> GeneratePolicyNumberAsync(string lineOfBusiness, int year, CancellationToken ct)
    {
        var prefix = lineOfBusiness.Length <= 4
            ? lineOfBusiness.ToUpperInvariant()
            : new string(lineOfBusiness.Where(char.IsUpper).Take(4).ToArray());
        if (string.IsNullOrWhiteSpace(prefix))
            prefix = lineOfBusiness[..Math.Min(4, lineOfBusiness.Length)].ToUpperInvariant();

        var sequence = await policyRepo.CountPoliciesForYearAsync(year, ct) + 1;
        return $"NEB-{prefix}-{year}-{sequence:D6}";
    }

    private static List<PolicyCoverageInputDto> NormalizeCoverages(IReadOnlyList<PolicyCoverageInputDto>? coverages) =>
        coverages?.ToList() ?? [];

    private async Task AddTransitionAndTimelineAsync(
        Policy policy,
        string? fromState,
        string toState,
        string eventType,
        string eventDescription,
        ICurrentUserService user,
        DateTime occurredAt,
        CancellationToken ct)
    {
        await transitionRepo.AddAsync(new WorkflowTransition
        {
            WorkflowType = WorkflowType,
            EntityId = policy.Id,
            FromState = fromState,
            ToState = toState,
            ActorUserId = user.UserId,
            OccurredAt = occurredAt,
        }, ct);

        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = EntityType,
            EntityId = policy.Id,
            EventType = eventType,
            EventDescription = eventDescription,
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = occurredAt,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                policy.Id,
                policy.PolicyNumber,
                fromState,
                toState,
            }),
        }, ct);
    }

    private async Task CommitPolicyChangeAsync(CancellationToken ct)
    {
        try
        {
            await unitOfWork.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
    }

    private async Task<PolicyListItemDto> MapListItemAsync(Policy policy, CancellationToken ct)
    {
        var versionCount = await policyRepo.CountVersionsAsync(policy.Id, ct);
        var endorsementCount = await policyRepo.CountEndorsementsAsync(policy.Id, ct);
        var openRenewalCount = await policyRepo.CountOpenRenewalsAsync(policy.Id, ct);

        return new PolicyListItemDto(
            policy.Id,
            policy.PolicyNumber,
            policy.AccountId,
            policy.AccountDisplayNameAtLink,
            policy.AccountStatusAtRead,
            policy.AccountSurvivorId,
            policy.BrokerId,
            policy.Broker?.LegalName,
            policy.CarrierId,
            policy.Carrier?.Name,
            policy.LineOfBusiness,
            policy.CurrentStatus,
            policy.EffectiveDate,
            policy.ExpirationDate,
            policy.TotalPremium,
            policy.PremiumCurrency,
            policy.ProducerUserId,
            policy.Producer?.DisplayName,
            Math.Max(versionCount, 1),
            endorsementCount,
            openRenewalCount > 0,
            policy.ReinstatementDeadline,
            policy.RowVersion.ToString());
    }

    private async Task<PolicyDto> MapDtoAsync(Policy policy, ICurrentUserService user, CancellationToken ct)
    {
        var versionCount = await policyRepo.CountVersionsAsync(policy.Id, ct);
        var successor = await policyRepo.GetSuccessorPolicyAsync(policy.Id, ct);

        return new PolicyDto(
            policy.Id,
            policy.AccountId,
            policy.BrokerId,
            policy.PolicyNumber,
            policy.LineOfBusiness,
            policy.CarrierId,
            policy.Carrier?.Name,
            policy.CurrentStatus,
            policy.EffectiveDate,
            policy.ExpirationDate,
            policy.BoundAt,
            policy.IssuedAt,
            policy.CancelledAt,
            policy.CancellationEffectiveDate,
            policy.CancellationReasonCode,
            policy.CancellationReasonDetail,
            policy.ReinstatementDeadline,
            policy.ExpiredAt,
            policy.PredecessorPolicyId,
            successor?.Id,
            policy.CurrentVersionId,
            Math.Max(versionCount, 1),
            Math.Max(versionCount, 1),
            policy.TotalPremium,
            policy.PremiumCurrency,
            policy.ProducerUserId,
            policy.Producer?.DisplayName,
            policy.ImportSource,
            policy.ExternalPolicyReference,
            policy.AccountDisplayNameAtLink,
            policy.AccountStatusAtRead,
            policy.AccountSurvivorId,
            AvailableTransitions(policy, user),
            policy.RowVersion.ToString(),
            policy.CreatedAt,
            policy.CreatedByUserId,
            policy.UpdatedAt,
            policy.UpdatedByUserId);
    }

    private async Task<PolicySummaryDto> MapSummaryAsync(Policy policy, ICurrentUserService user, CancellationToken ct)
    {
        var versionCount = await policyRepo.CountVersionsAsync(policy.Id, ct);
        var endorsementCount = await policyRepo.CountEndorsementsAsync(policy.Id, ct);
        var coverageCount = await policyRepo.CountCurrentCoverageLinesAsync(policy.Id, ct);
        var openRenewalCount = await policyRepo.CountOpenRenewalsAsync(policy.Id, ct);
        var successor = await policyRepo.GetSuccessorPolicyAsync(policy.Id, ct);

        return new PolicySummaryDto(
            policy.Id,
            policy.PolicyNumber,
            policy.AccountId,
            policy.AccountDisplayNameAtLink,
            policy.AccountStatusAtRead,
            policy.AccountSurvivorId,
            policy.BrokerId,
            policy.Broker?.LegalName,
            policy.CarrierId,
            policy.Carrier?.Name,
            policy.LineOfBusiness,
            policy.CurrentStatus,
            policy.EffectiveDate,
            policy.ExpirationDate,
            policy.BoundAt,
            policy.CancelledAt,
            policy.CancellationEffectiveDate,
            policy.CancellationReasonCode,
            policy.ReinstatementDeadline,
            policy.ExpiredAt,
            policy.PredecessorPolicyId,
            policy.PredecessorPolicy?.PolicyNumber,
            successor?.Id,
            successor?.PolicyNumber,
            policy.TotalPremium,
            policy.PremiumCurrency,
            policy.CurrentVersionId,
            Math.Max(versionCount, 1),
            Math.Max(versionCount, 1),
            endorsementCount,
            coverageCount,
            openRenewalCount,
            policy.ProducerUserId,
            policy.Producer?.DisplayName,
            policy.ImportSource,
            policy.ExternalPolicyReference,
            AvailableTransitions(policy, user),
            policy.RowVersion.ToString(),
            policy.CreatedAt,
            policy.CreatedByUserId,
            policy.UpdatedAt,
            policy.UpdatedByUserId);
    }

    private static PolicyVersion BuildVersion(
        Policy policy,
        string reason,
        Guid? endorsementId,
        IReadOnlyList<PolicyCoverageInputDto> coverages,
        decimal? premiumDelta,
        DateTime now,
        Guid actorUserId)
    {
        var totalPremium = coverages.Count == 0 ? policy.TotalPremium : coverages.Sum(coverage => coverage.Premium);
        var versionNumber = policy.CurrentVersionId.HasValue ? 2 : 1;
        return new PolicyVersion
        {
            PolicyId = policy.Id,
            VersionNumber = versionNumber,
            VersionReason = reason,
            EndorsementId = endorsementId,
            EffectiveDate = policy.EffectiveDate,
            ExpirationDate = policy.ExpirationDate,
            TotalPremium = totalPremium,
            PremiumCurrency = policy.PremiumCurrency,
            ProfileSnapshotJson = JsonSerializer.Serialize(new
            {
                policy.AccountId,
                brokerOfRecordId = policy.BrokerId,
                policy.CarrierId,
                policy.ProducerUserId,
            }),
            CoverageSnapshotJson = JsonSerializer.Serialize(coverages),
            PremiumSnapshotJson = JsonSerializer.Serialize(new
            {
                totalPremium,
                policy.PremiumCurrency,
                premiumDelta,
            }),
            CreatedAt = now,
            CreatedByUserId = actorUserId,
            UpdatedAt = now,
            UpdatedByUserId = actorUserId,
        };
    }

    private async Task<PolicyVersion> BuildNextVersionAsync(
        Policy policy,
        string reason,
        Guid? endorsementId,
        IReadOnlyList<PolicyCoverageInputDto> coverages,
        decimal? premiumDelta,
        DateTime now,
        Guid actorUserId,
        CancellationToken ct)
    {
        var version = BuildVersion(policy, reason, endorsementId, coverages, premiumDelta, now, actorUserId);
        version.VersionNumber = await policyRepo.CountVersionsAsync(policy.Id, ct) + 1;
        return version;
    }

    private IEnumerable<PolicyCoverageLine> BuildCoverageLines(
        Policy policy,
        PolicyVersion version,
        IReadOnlyList<PolicyCoverageInputDto> coverages,
        DateTime now,
        Guid actorUserId) =>
        coverages.Select(coverage => new PolicyCoverageLine
        {
            PolicyId = policy.Id,
            PolicyVersionId = version.Id,
            VersionNumber = version.VersionNumber,
            CoverageCode = coverage.CoverageCode,
            CoverageName = coverage.CoverageName,
            Limit = coverage.Limit,
            Deductible = coverage.Deductible,
            Premium = coverage.Premium,
            PremiumCurrency = policy.PremiumCurrency,
            ExposureBasis = coverage.ExposureBasis,
            ExposureQuantity = coverage.ExposureQuantity,
            IsCurrent = true,
            CreatedAt = now,
            CreatedByUserId = actorUserId,
            UpdatedAt = now,
            UpdatedByUserId = actorUserId,
        });

    private static PolicyVersionDto MapVersion(PolicyVersion version) => new(
        version.Id,
        version.PolicyId,
        version.VersionNumber,
        version.VersionReason,
        version.EndorsementId,
        version.EffectiveDate,
        version.ExpirationDate,
        version.TotalPremium,
        version.PremiumCurrency,
        DeserializeJson(version.ProfileSnapshotJson),
        DeserializeJson(version.CoverageSnapshotJson),
        DeserializeJson(version.PremiumSnapshotJson),
        version.CreatedAt,
        version.CreatedByUserId);

    private static PolicyEndorsementDto MapEndorsement(PolicyEndorsement endorsement, int? versionNumber) => new(
        endorsement.Id,
        endorsement.PolicyId,
        endorsement.EndorsementNumber,
        endorsement.PolicyVersionId,
        versionNumber,
        endorsement.EndorsementReasonCode,
        endorsement.EndorsementReasonDetail,
        endorsement.EffectiveDate,
        endorsement.PremiumDelta,
        endorsement.PremiumCurrency,
        endorsement.CreatedAt,
        endorsement.CreatedByUserId);

    private static PolicyCoverageLineDto MapCoverageLine(PolicyCoverageLine line) => new(
        line.Id,
        line.PolicyId,
        line.PolicyVersionId,
        line.VersionNumber,
        line.CoverageCode,
        line.CoverageName,
        line.Limit,
        line.Deductible,
        line.Premium,
        line.PremiumCurrency,
        line.ExposureBasis,
        line.ExposureQuantity,
        line.IsCurrent,
        line.CreatedAt);

    private static object? DeserializeJson(string json) =>
        JsonSerializer.Deserialize<JsonElement>(json);

    private static IReadOnlyList<string> AvailableTransitions(Policy policy, ICurrentUserService user)
    {
        var transitions = new List<string>();
        var canIssue = HasRole(user.Roles, "Underwriter") || HasRole(user.Roles, "Admin");
        var canEndorse = canIssue;
        var canCancel = canIssue || HasRole(user.Roles, "DistributionManager");

        if (policy.CurrentStatus == Pending && canIssue)
            transitions.Add("Issue");
        if (policy.CurrentStatus == Issued && canEndorse)
            transitions.Add("Endorse");
        if (policy.CurrentStatus == Issued && canCancel)
            transitions.Add("Cancel");
        if (policy.CurrentStatus == Cancelled && canIssue && policy.ReinstatementDeadline >= DateTime.UtcNow.Date)
            transitions.Add("Reinstate");

        return transitions;
    }

    private static bool HasRole(IReadOnlyList<string> roles, string role) =>
        roles.Any(existingRole => string.Equals(existingRole, role, StringComparison.OrdinalIgnoreCase));

    private sealed class SystemPolicyUser : ICurrentUserService
    {
        public static readonly SystemPolicyUser Instance = new();

        private SystemPolicyUser() { }

        public Guid UserId => Guid.Empty;
        public string DisplayName => "System";
        public IReadOnlyList<string> Roles => ["Admin"];
        public IReadOnlyList<string> Regions => [];
        public string? BrokerTenantId => null;
    }
}
