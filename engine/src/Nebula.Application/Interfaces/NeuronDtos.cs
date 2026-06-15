namespace Nebula.Application.Interfaces;

public record BrokerScorecardDto(
    Guid BrokerId, string LegalName, int WindowDays,
    int TotalSubmissions, int QuotedSubmissions, int BoundSubmissions,
    int DeclinedSubmissions, double QuoteRate, double BindRate,
    int TotalRenewals, int CompletedRenewals, int LostRenewals,
    double RetentionRate, decimal TotalPremiumEstimate,
    int ActivityCount, DateTime ComputedAt);

public record TrendPointDto(
    string PeriodLabel, int Submissions, int Bound,
    int RenewalsCompleted, decimal Premium);

public record BrokerTrendsDto(
    Guid BrokerId, int WindowDays, string Granularity,
    IReadOnlyList<TrendPointDto> Points);

public record LeaderboardEntryDto(
    int Rank, Guid BrokerId, string LegalName, string State,
    int SubmissionCount, int RenewalCount, decimal TotalPremium);

public record BrokerLeaderboardDto(
    IReadOnlyList<LeaderboardEntryDto> Entries, int TotalBrokers);
