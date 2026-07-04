# Secrets Scan - F0008 Broker Insights

Result: PASS

## Command

`rg -n "(?i)(password\\s*[=:]|secret\\s*[=:]|api[_-]?key\\s*[=:]|bearer\\s+[a-z0-9._-]+|connectionstring\\s*[=:])" engine/src/Nebula.Api/Endpoints/BrokerInsightEndpoints.cs engine/src/Nebula.Application/DTOs/BrokerInsightDtos.cs engine/src/Nebula.Application/Services/BrokerInsightService.cs engine/src/Nebula.Infrastructure/Repositories/BrokerInsightProjectionRepository.cs experience/src/features/broker-insights experience/src/pages/BrokerInsightsPage.tsx`

## Result

No matches found.
