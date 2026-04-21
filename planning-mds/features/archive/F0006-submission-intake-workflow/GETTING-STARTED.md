# F0006 — Submission Intake Workflow — Getting Started

## Prerequisites

- [ ] Read the [PRD](./PRD.md) and understand the scope boundary (F0006 ends at ReadyForUWReview; F0019 owns downstream)
- [ ] Review upstream features already implemented: F0002 (Broker), F0009 (Auth), F0003/F0004 (Task Center)
- [ ] Ensure F0016 (Account) entity exists at minimum as a stub (Id, Name, Region)
- [ ] Review ADR-011 (workflow state machines) and existing Casbin policy.csv §2.3 (submission policies)
- [ ] Review the submission workflow states in BLUEPRINT §4.3

## Services to Run

```bash
# Full local stack
docker compose up -d postgres authentik
dotnet run --project engine/src/Nebula.Api
cd experience && pnpm dev
```

## Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `Authentication__Authority` | authentik OIDC issuer | `http://localhost:9000/application/o/nebula/` |
| `Authentication__Audience` | JWT audience claim | `nebula` |
| `ConnectionStrings__Default` | PostgreSQL connection | See docker-compose |

## Seed Data

- **ReferenceSubmissionStatus**: Seed intake states (Received, Triaging, WaitingOnBroker, ReadyForUWReview) plus downstream states (InReview, Quoted, BindRequested, Bound, Declined, Withdrawn) with terminal flags and display metadata
- **Stale thresholds**: Seed configurable thresholds in `WorkflowSlaThresholds` using the existing schema (`EntityType="submission"`): Received=48h, Triaging=48h, WaitingOnBroker=72h
- **Accounts**: At least one test account with Region set
- **Brokers**: At least one test broker with matching BrokerRegion
- **Users**: lisa.wong (DistributionUser), john.miller (Underwriter), akadmin (Admin)

## How to Verify

1. Authenticate as `lisa.wong` (DistributionUser)
2. Create a submission linked to a test account and broker
3. Verify submission appears in pipeline list in Received status
4. Open submission detail — verify completeness panel shows field status
5. Transition to Triaging → verify timeline event and status badge update
6. Assign to `john.miller` (Underwriter)
7. Use the detail page edit action to fill required fields (LOB, etc.) → verify completeness passes
8. Transition to ReadyForUWReview → verify completeness guard enforced
9. Authenticate as `john.miller` → verify submission visible in ReadyForUWReview
10. Verify stale flag appears on a submission left in Received for > 48 hours

## Key Files

| Layer | Path | Purpose |
|-------|------|---------|
| Backend | `engine/src/Nebula.Api/Endpoints/SubmissionEndpoints.cs` | Submission HTTP routes |
| Backend | `engine/src/Nebula.Application/Services/SubmissionService.cs` | Submission business logic |
| Backend | `engine/src/Nebula.Application/Services/WorkflowStateMachine.cs` | Submission workflow rules |
| Backend | `engine/src/Nebula.Domain/Entities/Submission.cs` | Submission entity |
| Backend | `engine/src/Nebula.Infrastructure/Repositories/SubmissionRepository.cs` | Submission data access |
| Backend | `engine/src/Nebula.Infrastructure/Persistence/Configurations/WorkflowSlaThresholdConfiguration.cs` | Current SLA threshold seed/config source |
| Frontend | `experience/src/features/submissions/` | Proposed feature slice for list, detail, edit, assignment, and timeline UI |
| Planning | `planning-mds/security/policies/policy.csv` | Casbin ABAC policies §2.3 |

## Notes

- Region alignment (Account.Region in broker's BrokerRegion set) is enforced on create — test with mismatched regions to verify the `region_mismatch` error
- Document completeness (F0020) is a soft dependency — if F0020 is not deployed, document checks are skipped gracefully
- The stale threshold clock uses the last WorkflowTransition.OccurredAt, not the submission's UpdatedAt
