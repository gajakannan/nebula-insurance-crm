using Nebula.Application.DTOs;

namespace Nebula.Application.Interfaces;

public interface IAdminConfigurationDomainAdapter
{
    string DomainKey { get; }
    string ConsumerKey { get; }
    Task<string> BuildCurrentPayloadJsonAsync(CancellationToken ct);
    Task<IReadOnlyList<AdminConfigurationValidationIssueDto>> ValidatePayloadAsync(string payloadJson, CancellationToken ct);
    Task<IReadOnlyList<AdminConfigurationChangeSummaryDto>> CompareAsync(string beforeJson, string afterJson, CancellationToken ct);
}

public interface IAdminConfigurationRefreshNotifier
{
    Task<IReadOnlyList<string>> NotifyAsync(string domainKey, int publishedVersion, CancellationToken ct);
}
