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
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountContactRepository, AccountContactRepository>();
        services.AddScoped<IAccountRelationshipHistoryRepository, AccountRelationshipHistoryRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IRenewalRepository, RenewalRepository>();
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
