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
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<ActivityTimelineEvent> ActivityTimelineEvents => Set<ActivityTimelineEvent>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<WorkflowSlaThreshold> WorkflowSlaThresholds => Set<WorkflowSlaThreshold>();
    public DbSet<ReferenceTaskStatus> ReferenceTaskStatuses => Set<ReferenceTaskStatus>();
    public DbSet<ReferenceSubmissionStatus> ReferenceSubmissionStatuses => Set<ReferenceSubmissionStatus>();
    public DbSet<ReferenceRenewalStatus> ReferenceRenewalStatuses => Set<ReferenceRenewalStatus>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    // F0023 — SearchReporting read-side module
    public DbSet<SearchDocument> SearchDocuments => Set<SearchDocument>();
    public DbSet<SavedView> SavedViews => Set<SavedView>();
    public DbSet<SavedViewAuditEvent> SavedViewAuditEvents => Set<SavedViewAuditEvent>();
    public DbSet<OperationalReportProjection> OperationalReportProjections => Set<OperationalReportProjection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
