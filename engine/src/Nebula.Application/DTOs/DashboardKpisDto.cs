namespace Nebula.Application.DTOs;

public record DashboardKpisDto(
    int ActiveBrokers,
    int OpenSubmissions,
    double? RenewalRate,
    double? AvgTurnaroundDays);
