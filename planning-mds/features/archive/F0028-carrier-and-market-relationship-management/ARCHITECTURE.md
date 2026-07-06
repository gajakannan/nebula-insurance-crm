# F0028 Architecture Contract: Carrier & Market Relationship Management

Status: Phase B approved for feature action
Owner: Architect
Last Updated: 2026-07-02

## Decision Summary

F0028 promotes the earlier `CarrierRef` seed into a CRM-managed market relationship module. It does not introduce carrier API synchronization, rating, quoting, reinsurance, or external carrier system integration.

No new ADR is required for Phase B. F0028 applies existing decisions and patterns:

- Clean Architecture layer split from `SOLUTION-PATTERNS.md`.
- Casbin ABAC and per-endpoint authorization from ADR-008.
- Immutable `ActivityTimelineEvent` emission for every mutation.
- Permission-filtered F0023 `SearchDocument` projection for market search.
- `If-Match` optimistic concurrency on all update/write sub-resources.
- RFC 7807 ProblemDetails for all non-2xx responses.

## Service Boundaries

Backend owns the source-of-truth market relationship APIs, persistence, authorization, timeline emission, and search projection updates.

Frontend owns the carrier market directory, carrier detail workspace, edit forms, client cache invalidation, reload persistence checks, and responsive layout.

Search owns read-side projection updates only. It must not become the source of truth for carrier, appetite, appointment, or underwriter relationship data.

DevOps is required during feature action because the implementation introduces runtime-bearing API/UI code and database migration evidence, even though no new runtime service or deployment configuration is planned.

Security Reviewer is required because appetite notes, appointment status, carrier strategy, and underwriter relationship context are commercially sensitive.

## Data Model

### CarrierMarket

Primary CRM-side aggregate for carrier relationship context.

Fields:

- `id`: uuid.
- `code`: stable unique short code, max 40.
- `name`: legal or market display name, max 200.
- `naicCode`: nullable string, max 10.
- `amBestRating`: nullable string, max 10.
- `status`: `Active`, `Inactive`, or `Prospect`.
- `marketType`: `Admitted`, `NonAdmitted`, `MGA`, `Wholesaler`, or `Other`.
- `relationshipOwnerUserId`: nullable uuid.
- `websiteUrl`: nullable uri.
- `generalEmail`: nullable email.
- `mainPhone`: nullable E.164-ish phone.
- `notes`: nullable string, max 2000.
- `createdAt`, `createdByUserId`, `updatedAt`, `updatedByUserId`.
- `rowVersion`: opaque concurrency token.

Rules:

- `code` is unique case-insensitively.
- Active policies and submissions may reference inactive carriers; inactive blocks new relationship mutation except reactivation by manager/admin.
- All updates require `If-Match`.

### CarrierMarketContact

Underwriter or market contact linked to one carrier market.

Fields:

- `id`, `carrierMarketId`.
- `fullName`, `title`, `email`, `phone`.
- `roles`: array of `Underwriter`, `Marketing`, `Claims`, `LossControl`, `Executive`, `Operations`, `Other`.
- `isPrimary`: boolean.
- `notes`: nullable string, max 1000.
- audit fields and `rowVersion`.

Rules:

- At most one primary contact per carrier for each role value.
- Email must be unique per carrier when present.
- Contact mutations require `If-Match` on update.

### CarrierAppetiteNote

Commercial appetite note scoped to carrier, optionally LOB, region, and risk keywords.

Fields:

- `id`, `carrierMarketId`.
- `lineOfBusiness`: nullable string.
- `region`: nullable string.
- `appetiteLevel`: `Preferred`, `Open`, `Selective`, `Restricted`, or `Closed`.
- `summary`: string, max 240.
- `detail`: nullable string, max 4000.
- `effectiveFrom`: nullable date.
- `effectiveTo`: nullable date.
- `source`: nullable string, max 200.
- `createdAt`, `createdByUserId`, `updatedAt`, `updatedByUserId`, `rowVersion`.

Rules:

- `effectiveTo` must be on or after `effectiveFrom` when both are present.
- Save emits `CarrierAppetiteNoteCreated` or `CarrierAppetiteNoteUpdated`.

### CarrierAppointment

Appointment and access context with a carrier.

Fields:

- `id`, `carrierMarketId`.
- `appointmentStatus`: `NotAppointed`, `InProgress`, `Appointed`, `Suspended`, or `Terminated`.
- `states`: array of US state codes.
- `lineOfBusiness`: nullable string.
- `appointmentNumber`: nullable string, max 80.
- `effectiveDate`: nullable date.
- `expirationDate`: nullable date.
- `notes`: nullable string, max 2000.
- audit fields and `rowVersion`.

Rules:

- `expirationDate` must be on or after `effectiveDate` when both are present.
- Updating status emits `CarrierAppointmentCreated` or `CarrierAppointmentUpdated`.

### CarrierMarketActivityLink

Lightweight link from carrier context to an existing submission or policy.

Fields:

- `id`, `carrierMarketId`.
- `relatedEntityType`: `Submission` or `Policy`.
- `relatedEntityId`: uuid.
- `relationshipKind`: `Marketed`, `Quoted`, `Bound`, `Declined`, `AppointedContext`, or `GeneralReference`.
- `note`: nullable string, max 1000.
- audit fields.

Rules:

- Related submission/policy must exist and be visible to the actor.
- Link creation emits a carrier timeline event and does not mutate submission or policy workflow state.

## API Contract

All endpoints are in `planning-mds/api/nebula-api.yaml` under the `CarrierMarkets` tag.

- `GET /carrier-markets`
- `POST /carrier-markets`
- `GET /carrier-markets/{carrierMarketId}`
- `PUT /carrier-markets/{carrierMarketId}`
- `GET /carrier-markets/{carrierMarketId}/contacts`
- `POST /carrier-markets/{carrierMarketId}/contacts`
- `PUT /carrier-markets/{carrierMarketId}/contacts/{contactId}`
- `GET /carrier-markets/{carrierMarketId}/appetite-notes`
- `POST /carrier-markets/{carrierMarketId}/appetite-notes`
- `PUT /carrier-markets/{carrierMarketId}/appetite-notes/{appetiteNoteId}`
- `GET /carrier-markets/{carrierMarketId}/appointments`
- `POST /carrier-markets/{carrierMarketId}/appointments`
- `PUT /carrier-markets/{carrierMarketId}/appointments/{appointmentId}`
- `GET /carrier-markets/{carrierMarketId}/activity-links`
- `POST /carrier-markets/{carrierMarketId}/activity-links`

List endpoints are paginated. Mutations return the changed resource and create timeline evidence atomically.

## Authorization Model

New Casbin resource: `carrier_market`.

Actions:

- `read`: internal users who can view market context.
- `search`: internal users who can search carrier markets.
- `create`: RelationshipManager, DistributionManager, Admin.
- `update`: RelationshipManager, DistributionManager, Admin.
- `manage_contact`: RelationshipManager, DistributionManager, Admin.
- `manage_appetite`: RelationshipManager, DistributionManager, Admin.
- `manage_appointment`: RelationshipManager, DistributionManager, Admin.
- `link_activity`: RelationshipManager, DistributionUser, DistributionManager, Underwriter, Admin, constrained by read access to the related submission/policy.

Underwriter can read carrier market context and contacts but cannot mutate relationship intelligence. ProgramManager can read program-scoped market context only. BrokerUser and ExternalUser are denied.

## Timeline And Audit

Every successful mutation creates an immutable `ActivityTimelineEvent` with:

- `EntityType = CarrierMarket`.
- `EntityId = carrierMarketId`.
- pre-rendered `EventDescription`.
- structured `EventPayloadJson`.
- actor and timestamp from the current request context.

Required event types:

- `CarrierMarketCreated`
- `CarrierMarketUpdated`
- `CarrierMarketContactCreated`
- `CarrierMarketContactUpdated`
- `CarrierAppetiteNoteCreated`
- `CarrierAppetiteNoteUpdated`
- `CarrierAppointmentCreated`
- `CarrierAppointmentUpdated`
- `CarrierMarketActivityLinked`

Rejected mutations emit no timeline event.

## Search Binding

F0028 adds `CarrierMarket` as a `SearchDocument.objectType`. Search rows include carrier name, code, NAIC code, rating, market type, appointment status summaries, and primary contact names. Appetite details are searchable only as permission-filtered internal snippets; no external role receives carrier market rows.

## Implementation Handoff Notes

- Use a feature slice under `experience/src/features/carrierMarkets/**` and page routes for directory/detail surfaces.
- Backend uses Domain/Application/Infrastructure/API placement consistent with existing modules.
- Implement concurrency on all update endpoints with `If-Match`; stale tokens return 412 `precondition_failed`.
- Use 409 for duplicate carrier code and relationship conflicts, 422 for semantic validation failures.
- Add backend tests for authorization, duplicate code, concurrency, timeline emission, search projection updates, and activity link visibility.
- Add frontend tests for directory filtering, detail tab workflows, edit/save/cancel, reload persistence, empty states, and denied mutation controls.
