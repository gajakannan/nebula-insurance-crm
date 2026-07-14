using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public Task CommitAsync(CancellationToken ct = default) => SaveChangesAsync(ct);

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountContact> AccountContacts => Set<AccountContact>();
    public DbSet<AccountRelationshipHistory> AccountRelationshipHistory => Set<AccountRelationshipHistory>();
    public DbSet<MGA> MGAs => Set<MGA>();
    public DbSet<Program> Programs => Set<Program>();
    public DbSet<Broker> Brokers => Set<Broker>();
    public DbSet<BrokerRegion> BrokerRegions => Set<BrokerRegion>();
    public DbSet<DistributionNode> DistributionNodes => Set<DistributionNode>();
    public DbSet<ProducerOwnership> ProducerOwnership => Set<ProducerOwnership>();
    public DbSet<Territory> Territories => Set<Territory>();
    public DbSet<TerritoryAssignment> TerritoryAssignments => Set<TerritoryAssignment>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SubmissionQuotePacket> SubmissionQuotePackets => Set<SubmissionQuotePacket>();
    public DbSet<SubmissionApprovalDecision> SubmissionApprovalDecisions => Set<SubmissionApprovalDecision>();
    public DbSet<SubmissionBindHandoff> SubmissionBindHandoffs => Set<SubmissionBindHandoff>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<CarrierMarket> CarrierMarkets => Set<CarrierMarket>();
    public DbSet<CarrierMarketContact> CarrierMarketContacts => Set<CarrierMarketContact>();
    public DbSet<CarrierAppetiteNote> CarrierAppetiteNotes => Set<CarrierAppetiteNote>();
    public DbSet<CarrierAppointment> CarrierAppointments => Set<CarrierAppointment>();
    public DbSet<CarrierMarketActivityLink> CarrierMarketActivityLinks => Set<CarrierMarketActivityLink>();
    public DbSet<CarrierRef> CarrierRefs => Set<CarrierRef>();
    public DbSet<LobProduct> LobProducts => Set<LobProduct>();
    public DbSet<LobProductVersion> LobProductVersions => Set<LobProductVersion>();
    public DbSet<LobSchemaBundle> LobSchemaBundles => Set<LobSchemaBundle>();
    public DbSet<LobBundleActivationEvent> LobBundleActivationEvents => Set<LobBundleActivationEvent>();
    public DbSet<PolicyVersion> PolicyVersions => Set<PolicyVersion>();
    public DbSet<PolicyEndorsement> PolicyEndorsements => Set<PolicyEndorsement>();
    public DbSet<PolicyCoverageLine> PolicyCoverageLines => Set<PolicyCoverageLine>();
    public DbSet<Renewal> Renewals => Set<Renewal>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<CommunicationEvent> CommunicationEvents => Set<CommunicationEvent>();
    public DbSet<CommunicationLink> CommunicationLinks => Set<CommunicationLink>();
    public DbSet<CommunicationParticipant> CommunicationParticipants => Set<CommunicationParticipant>();
    public DbSet<CommunicationCorrection> CommunicationCorrections => Set<CommunicationCorrection>();
    public DbSet<CommunicationFollowUpTaskLink> CommunicationFollowUpTaskLinks => Set<CommunicationFollowUpTaskLink>();
    public DbSet<ServiceCase> ServiceCases => Set<ServiceCase>();
    public DbSet<ServiceCaseClaimReference> ServiceCaseClaimReferences => Set<ServiceCaseClaimReference>();
    public DbSet<ServiceCaseCommunicationLink> ServiceCaseCommunicationLinks => Set<ServiceCaseCommunicationLink>();
    public DbSet<ServiceCaseTaskLink> ServiceCaseTaskLinks => Set<ServiceCaseTaskLink>();
    public DbSet<ServiceCaseTransition> ServiceCaseTransitions => Set<ServiceCaseTransition>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<ActivityTimelineEvent> ActivityTimelineEvents => Set<ActivityTimelineEvent>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<WorkflowSlaThreshold> WorkflowSlaThresholds => Set<WorkflowSlaThreshold>();
    public DbSet<ReferenceTaskStatus> ReferenceTaskStatuses => Set<ReferenceTaskStatus>();
    public DbSet<ReferenceSubmissionStatus> ReferenceSubmissionStatuses => Set<ReferenceSubmissionStatus>();
    public DbSet<ReferenceRenewalStatus> ReferenceRenewalStatuses => Set<ReferenceRenewalStatus>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<WorkQueue> WorkQueues => Set<WorkQueue>();
    public DbSet<WorkQueueMember> WorkQueueMembers => Set<WorkQueueMember>();
    public DbSet<AssignmentRule> AssignmentRules => Set<AssignmentRule>();
    public DbSet<CoverageWindow> CoverageWindows => Set<CoverageWindow>();
    public DbSet<QueueWorkItem> QueueWorkItems => Set<QueueWorkItem>();
    public DbSet<RoutingDecisionLog> RoutingDecisionLogs => Set<RoutingDecisionLog>();
    public DbSet<ConfigurationDomain> ConfigurationDomains => Set<ConfigurationDomain>();
    public DbSet<ConfigurationDraft> ConfigurationDrafts => Set<ConfigurationDraft>();
    public DbSet<ConfigurationValidationResult> ConfigurationValidationResults => Set<ConfigurationValidationResult>();
    public DbSet<PublishedOperationalConfigurationSet> PublishedOperationalConfigurationSets => Set<PublishedOperationalConfigurationSet>();
    public DbSet<ConfigurationRefreshStatus> ConfigurationRefreshStatuses => Set<ConfigurationRefreshStatus>();
    public DbSet<ConfigurationAuditEvent> ConfigurationAuditEvents => Set<ConfigurationAuditEvent>();

    // F0023 — SearchReporting read-side module
    public DbSet<SearchDocument> SearchDocuments => Set<SearchDocument>();
    public DbSet<SavedView> SavedViews => Set<SavedView>();
    public DbSet<SavedViewAuditEvent> SavedViewAuditEvents => Set<SavedViewAuditEvent>();
    public DbSet<OperationalReportProjection> OperationalReportProjections => Set<OperationalReportProjection>();
    public DbSet<BrokerInsightProjection> BrokerInsightProjections => Set<BrokerInsightProjection>();
    public DbSet<CommissionSchedule> CommissionSchedules => Set<CommissionSchedule>();
    public DbSet<ProducerSplitAssignment> ProducerSplitAssignments => Set<ProducerSplitAssignment>();
    public DbSet<ProducerSplitParticipant> ProducerSplitParticipants => Set<ProducerSplitParticipant>();
    public DbSet<ExpectedCommission> ExpectedCommissions => Set<ExpectedCommission>();
    public DbSet<CommissionAdjustment> CommissionAdjustments => Set<CommissionAdjustment>();
    public DbSet<RevenueAttributionProjection> RevenueAttributionProjections => Set<RevenueAttributionProjection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
