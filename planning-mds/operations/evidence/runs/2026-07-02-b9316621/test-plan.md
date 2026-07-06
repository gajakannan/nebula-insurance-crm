# Test Plan — F0027-coi-acord-and-outbound-document-generation run 2026-07-02-b9316621

## Story-to-AC Mapping

| Story | AC | Lane | Test ID | Owner |
|-------|----|------|---------|-------|
| S0001 | Published outbound template required | Unit | `IssueAsync_RejectsUnpublishedTemplate` | Backend Developer |
| S0002 | Preview before issue | Unit | `PreviewAsync_ReturnsReadyPreviewForPublishedTemplate` | Backend Developer |
| S0003 | Explicit issue creates available artifact | Unit | `IssueAsync_CreatesAvailableGeneratedDocument` | Backend Developer |
| S0003 | Panel exposes preview then issue | Component | `ParentDocumentsPanel.test.tsx` | Frontend Developer |
| S0004 | Regenerate produces a new generated document | Unit/API | Recommended before G6 | Quality Engineer |
| S0005 | Shared rendering boundary supports proposal family | Build/contract | API/frontend build | Backend + Frontend |

## Test Strategy

- Unit tests: engine service tests for preview, issue, and template-publish rejection.
- Component tests: existing parent document panel test exercises mounted document panel.
- Integration tests: recommended before G6 for endpoint auth/status mapping and sidecar persistence.
- API tests: recommended before G6 for `/outbound-documents/preview`, `/issue`, and `/{documentId}/regenerate`.
- Accessibility tests: no new modal/dialog; form controls use existing `Select` and button primitives.

## Developer-vs-QE Test Ownership

Developer-owned: backend unit tests, frontend build, component smoke, theme guard.
QE-owned before G6: endpoint integration tests, regenerate assertion, evidence completeness review.

## Test Data / Fixtures

- Backend unit tests use in-memory fake document repository and template sidecar fixtures.
- Frontend component test uses existing MSW document mock state.
- Personas: Admin for template governance and document issue path.

## Happy / Edge / Error / Auth / Accessibility / Regression Cases

- Happy: published COI template previews and issues.
- Edge: unpublished template rejects without generated write.
- Error: sandbox-denied `dotnet test` was classified as runtime/tooling and rerun unchanged outside sandbox.
- Auth: outbound policy enforced in service through `IAuthorizationService`.
- Accessibility: existing semantic form controls and buttons retained.
- Regression: existing parent document panel rendering remains green.

## Risks And Mitigations

- Integration coverage is not complete at G2. Mitigation: required before G6 candidate validation.
- Production rendering fidelity is not final. Mitigation: renderer boundary isolates future replacement.

## Recommendations

- [medium] Add endpoint integration coverage before G6 — owner: Quality Engineer; follow-up: F0027-G6-api-tests

## Result

PASS WITH RECOMMENDATIONS
