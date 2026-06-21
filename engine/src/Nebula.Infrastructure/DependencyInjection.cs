using Microsoft.Extensions.DependencyInjection;
using Nebula.Application.Interfaces;
using Nebula.Infrastructure.Authorization;
using Nebula.Infrastructure.Documents;
using Nebula.Infrastructure.Persistence;
using Nebula.Infrastructure.Repositories;
using Nebula.Infrastructure.Services;

namespace Nebula.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IBrokerRepository, BrokerRepository>();
        services.AddScoped<IDistributionNodeRepository, DistributionNodeRepository>();
        services.AddScoped<IProducerOwnershipRepository, ProducerOwnershipRepository>();
        services.AddScoped<ITerritoryRepository, TerritoryRepository>();
        services.AddScoped<ITerritoryAssignmentRepository, TerritoryAssignmentRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountContactRepository, AccountContactRepository>();
        services.AddScoped<IAccountRelationshipHistoryRepository, AccountRelationshipHistoryRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<ISubmissionQuotePacketRepository, SubmissionQuotePacketRepository>();
        services.AddScoped<ISubmissionApprovalDecisionRepository, SubmissionApprovalDecisionRepository>();
        services.AddScoped<ISubmissionBindHandoffRepository, SubmissionBindHandoffRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IRenewalRepository, RenewalRepository>();
        services.AddScoped<ILobSchemaRepository, LobSchemaRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<ITimelineRepository, TimelineRepository>();
        services.AddScoped<IWorkflowTransitionRepository, WorkflowTransitionRepository>();
        services.AddScoped<IWorkflowSlaThresholdRepository, WorkflowSlaThresholdRepository>();
        services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
        services.AddScoped<ISubmissionDocumentChecklistReader, UnavailableSubmissionDocumentChecklistReader>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddSingleton<IDocumentConfigurationProvider, YamlDocumentConfigurationProvider>();
        services.AddScoped<IDocumentParentAccessResolver, DocumentParentAccessResolver>();
        services.AddScoped<IDocumentClassificationGate, DocumentClassificationGate>();
        services.AddScoped<IDocumentRepository, LocalFileSystemDocumentRepository>();
        // F0023 — SearchReporting read-side module
        services.AddScoped<ISearchDocumentRepository, SearchDocumentRepository>();
        services.AddScoped<ISavedViewRepository, SavedViewRepository>();
        services.AddScoped<IOperationalReportProjectionRepository, OperationalReportProjectionRepository>();
        services.AddScoped<ISearchService, Nebula.Application.Services.SearchService>();
        services.AddScoped<ISavedViewService, Nebula.Application.Services.SavedViewService>();
        services.AddScoped<IOperationalReportService, Nebula.Application.Services.OperationalReportService>();
        services.AddScoped<ISearchProjectionService, Nebula.Infrastructure.Services.SearchProjectionService>();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddSingleton<IAuthorizationService, CasbinAuthorizationService>();
        services.AddScoped<Nebula.Application.Services.DocumentService>();
        services.AddScoped<Nebula.Application.Services.DocumentTemplateService>();
        services.AddScoped<Nebula.Application.Services.DocumentRetentionService>();
        services.AddSingleton<IQuarantineScanner, MockTimerScanner>();
        services.AddHostedService<PolicyExpirationHostedService>();
        services.AddHostedService<QuarantinePromotionWorker>();
        services.AddHostedService<DocumentRetentionHostedService>();
        services.AddMemoryCache();

        return services;
    }
}
