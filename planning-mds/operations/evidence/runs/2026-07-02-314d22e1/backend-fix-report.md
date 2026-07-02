# Backend Fix Report

## Changed Paths

- `engine/src/Nebula.Application/Interfaces/ICommunicationRepository.cs`
- `engine/src/Nebula.Infrastructure/Repositories/CommunicationRepository.cs`
- `engine/src/Nebula.Application/Services/CommunicationService.cs`

## Fix

Added `AddCorrectionAsync` to the communication repository and used it from `CorrectOrRedactAsync` after setting `CommunicationEventId`. This forces the correction audit row to be inserted as a new row rather than inferred through the loaded navigation collection.

## Validation

- `docker compose up -d --build api` rebuilt and restarted the API successfully.
- `curl -fsS http://127.0.0.1:8080/healthz` returned `Healthy`.
- Final focused F0021 E2E passed 5/5, including correction and redaction.
