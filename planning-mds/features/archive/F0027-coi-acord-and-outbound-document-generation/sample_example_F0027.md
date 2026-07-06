# Sample Examples — F0027 COI, ACORD & Outbound Document Generation

This file records sample behavior and verification notes for F0027 using the `nandini-nebula-agents` harness discipline.

## Screenshot Verification

The screenshot from `2026-07-03 1:27 PM` is a correct F0027 empty-template state.

What is visible:

- The source record is a Renewal page with the shared Documents panel.
- The F0027 Generate Document controls render inside the Documents panel.
- `Family` defaults to `COI`.
- `Classification` defaults to `Confidential`.
- The template selector is empty.
- The UI shows: `No published COI templates are available.`
- No documents are attached yet.

This state is expected when the current environment has no published outbound COI template available to the logged-in user.

## Will Another Contributor See The Same?

Yes, if they run the same branch with the same local runtime state.

Expected fresh contributor behavior:

- The F0027 panel appears on parent record document panels.
- If no published outbound template exists, the panel shows the same "No published ... templates are available" message.
- Preview and Issue cannot complete until a template is selected.

Important portability caveat:

- The frontend filters templates by tags: `coi`, `acord`, `proposal`, or `outbound:<family>`.
- The backend additionally requires template sidecar metadata:
  - `artifactFamily`: `coi`, `acord`, or `proposal`
  - `outboundStatus`: `published`
- The current generic template upload UI sends classification and tags, but does not populate the F0027 outbound metadata fields. A contributor who only uploads a template through the generic template library may still see the empty generated-template selector until metadata is seeded or patched.

## Minimum Published Template Shape

A generated-document template must resolve to a sidecar with this effective shape:

```json
{
  "documentId": "doc_01HYA7K3M9P2Q4R6S8TVWXYZAB",
  "parent": {
    "type": "template",
    "id": "00000000-0000-0000-0000-000000000000"
  },
  "classification": "confidential",
  "type": "template",
  "tags": ["coi", "outbound:coi"],
  "metadata": {
    "artifactFamily": "coi",
    "outboundStatus": "published"
  },
  "versions": [
    {
      "n": 1,
      "fileName": "coi-template.pdf",
      "status": "available"
    }
  ]
}
```

With only tags and no metadata, the frontend may list the template candidate, but the backend will reject preview/issue with `invalid_artifact_family` or `template_not_published`.

## Sample Preview Request

Endpoint:

```http
POST /outbound-documents/preview
```

Body:

```json
{
  "parent": {
    "type": "submission",
    "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
  },
  "artifactFamily": "coi",
  "templateDocumentId": "doc_01HYA7K3M9P2Q4R6S8TVWXYZAB",
  "templateVersion": null,
  "classification": "confidential",
  "regeneratedFromDocumentId": null,
  "regenerationReason": null
}
```

Expected successful response shape:

```json
{
  "artifactFamily": "coi",
  "templateDocumentId": "doc_01HYA7K3M9P2Q4R6S8TVWXYZAB",
  "templateVersion": 1,
  "sourceSnapshotHash": "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
  "status": "ready",
  "previewUrl": null,
  "expiresAtUtc": "2026-07-03T08:00:00Z",
  "mergeDiagnostics": [
    {
      "field": "parent",
      "status": "ok",
      "detail": null
    }
  ]
}
```

Code-observed note:

- `OutboundDocumentGenerationService.PreviewAsync` currently returns `status = "blocked"` when merge diagnostics contain missing data.
- The shared preview schema currently enumerates `ready`, `missing_merge_data`, and `render_failed`.
- Treat this as a contract follow-up before relying on `blocked` in external integrations.

## Sample Issue Request

Endpoint:

```http
POST /outbound-documents/issue
```

Body:

```json
{
  "parent": {
    "type": "submission",
    "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
  },
  "artifactFamily": "coi",
  "templateDocumentId": "doc_01HYA7K3M9P2Q4R6S8TVWXYZAB",
  "templateVersion": null,
  "classification": "confidential",
  "regeneratedFromDocumentId": null,
  "regenerationReason": null
}
```

Expected successful response shape:

```json
{
  "documentId": "doc_01HYB8M4N6P7Q8R9S0TAVWXYZB",
  "artifactFamily": "coi",
  "templateDocumentId": "doc_01HYA7K3M9P2Q4R6S8TVWXYZAB",
  "templateVersion": 1,
  "issuedAtUtc": "2026-07-03T08:01:00Z",
  "issuedByUserId": "11111111-1111-1111-1111-111111111111",
  "sourceSnapshotHash": "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
  "regeneratedFromDocumentId": null,
  "downloadUrl": "/documents/doc_01HYB8M4N6P7Q8R9S0TAVWXYZB/versions/latest/binary"
}
```

Expected persistence behavior:

- Creates a parent-linked document sidecar with `type = generated-document`.
- Writes the generated PDF as an available document version.
- Stores generated-document metadata with family, template, template version, source snapshot hash, and regeneration linkage.
- Adds a generated issue event and source-record timeline event.

## Sample Regenerate Request

Endpoint:

```http
POST /outbound-documents/doc_01HYB8M4N6P7Q8R9S0TAVWXYZB/regenerate
```

Body:

```json
{
  "parent": {
    "type": "submission",
    "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
  },
  "artifactFamily": "coi",
  "templateDocumentId": "doc_01HYA7K3M9P2Q4R6S8TVWXYZAB",
  "templateVersion": null,
  "classification": "confidential",
  "regeneratedFromDocumentId": null,
  "regenerationReason": "Updated insured details"
}
```

Expected behavior:

- The prior generated document remains unchanged.
- A new generated document is created.
- The new response includes `regeneratedFromDocumentId` pointing to the prior document.

## Verification Performed

The following focused checks passed locally:

```bash
dotnet test engine/tests/Nebula.Tests/Nebula.Tests.csproj --filter OutboundDocumentGenerationServiceTests --no-build
pnpm --dir experience exec vitest run src/features/documents/tests/ParentDocumentsPanel.test.tsx --reporter=dot
python3 scripts/kg/lookup.py --untested feature:F0027
```

Results:

- Backend F0027 service tests: `3 passed / 0 failed`
- Frontend document panel test: `1 passed / 0 failed`
- KG untested check: `untested_count = 0`

## Practical Manual Test Path

1. Start the local app.
2. Open a parent record page: account, policy, submission, or renewal.
3. Confirm the Documents panel shows the F0027 generated-document controls.
4. If no published template exists, confirm the empty-template message appears.
5. Seed or patch a template sidecar with:
   - tag `coi` or `outbound:coi`
   - metadata `artifactFamily = coi`
   - metadata `outboundStatus = published`
   - latest version status `available`
6. Reopen the parent record page.
7. Select the template, run Preview, then Issue.
8. Confirm the issued artifact appears in the Documents list and shows generated-artifact provenance on detail.

## F0027 Changes Reflected In The Product

- Added outbound generation endpoints: preview, issue, and regenerate.
- Added generated-document request/preview/issue DTOs and schemas.
- Added outbound generation service and template governance service.
- Added deterministic PDF renderer boundary and merge-data assembler boundary.
- Added repository support for generated available documents.
- Added generated-document metadata schema and taxonomy entry.
- Added document-panel UI controls for COI, ACORD, and Proposal generation.
- Added generated-artifact provenance display on document detail.
- Added F0027 authorization policy rows for outbound template/document actions.
- Archived F0027 planning docs and recorded harness evidence under run `2026-07-02-b9316621`.
