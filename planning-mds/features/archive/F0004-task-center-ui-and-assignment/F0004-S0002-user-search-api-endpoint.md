# F0004-S0002: User Search API Endpoint

**Story ID:** F0004-S0002
**Feature:** F0004 — Task Center UI + Manager Assignment
**Title:** User search API for assignee picker
**Priority:** High
**Phase:** Phase 1

## User Story

**As a** Distribution Manager or Admin
**I want** to search for internal users by name or email
**So that** I can select a user to assign or reassign a task to

## Context & Background

Task assignment requires a user lookup mechanism. UserProfile records exist for all authenticated users (created on first login via claims normalization, F0005). This endpoint exposes a search interface over UserProfile for the assignee picker component.

## Acceptance Criteria

**Happy Path:**
- **Given** an authenticated DistributionManager or Admin
- **When** they call `GET /users?q=lisa`
- **Then** the API returns users matching "lisa" in DisplayName or Email (case-insensitive substring), limited to active users by default

**Minimum Query Length:**
- **Given** `q=l` (1 character)
- **When** the request is made
- **Then** the API returns HTTP 400 with ProblemDetails (code: `validation_error`, detail: "Search query must be at least 2 characters")

**Active-Only Default:**
- **Given** `GET /users?q=john` with no `activeOnly` parameter
- **When** the request is made
- **Then** only users where `IsActive = true` are returned

**Include Inactive:**
- **Given** `GET /users?q=john&activeOnly=false`
- **When** the request is made
- **Then** both active and inactive users are returned, with `isActive` flag in response

**Result Limit:**
- **Given** more than 20 matching users
- **When** the request is made with default limit
- **Then** only the first 20 are returned (sorted by DisplayName ascending)

**Authorization — Internal Roles:**
- **Given** any authenticated internal role (DistributionUser, Underwriter, RelationshipManager, ProgramManager, DistributionManager, Admin)
- **When** they call `GET /users?q=...`
- **Then** access is granted (all internal roles need user display name resolution)

**Authorization — External Denied:**
- **Given** ExternalUser or BrokerUser
- **When** they call `GET /users?q=...`
- **Then** HTTP 403

**Edge Cases:**
- No matches: returns `{"users": []}`
- Special characters in query: URL-encoded, treated as literal search characters
- Query matches email domain (e.g., `@nebula`): returns all users at that domain
- limit > 50: clamped to 50

## Data Requirements

**Response schema:**
```json
{
  "users": [
    {
      "userId": "uuid",
      "displayName": "string",
      "email": "string",
      "roles": ["string"],
      "isActive": true
    }
  ]
}
```

## Role-Based Visibility

- All internal roles: search access
- ExternalUser, BrokerUser: 403

## Non-Functional Expectations

- Performance: 200ms p95
- Security: Does not expose sensitive user data (no IdpSubject, no IdpIssuer)
- Rate limiting: Consider rate limit if deployed (not required for Phase 1)

## Dependencies

- UserProfile table (F0005)
- Index on UserProfile.DisplayName for text search

## Implementation Guidance (Architect)

### Query Pattern
```sql
SELECT UserId, DisplayName, Email, RolesJson, IsActive
FROM UserProfile
WHERE (DisplayName ILIKE '%' || @query || '%' OR Email ILIKE '%' || @query || '%')
  AND (@activeOnly = false OR IsActive = true)
ORDER BY DisplayName ASC
LIMIT @limit
```

### New Index
```sql
CREATE INDEX IX_UserProfile_DisplayName ON UserProfile (DisplayName);
-- Consider GIN trigram index for better ILIKE performance if user count grows
```

### Endpoint
- `app.MapGet("/users", SearchUsers).WithTags("Users").RequireAuthorization()`

### Casbin
- Resource: `user`, Action: `search`, Condition: `true` (all internal roles)

## Definition of Done

- [ ] Acceptance criteria met
- [ ] Search returns correct results for DisplayName and Email matches
- [ ] Active-only filtering works
- [ ] No sensitive data exposed (IdpSubject, IdpIssuer excluded)
- [ ] ProblemDetails for validation errors
- [ ] Performance within budget (200ms p95)
- [ ] Tests pass (unit, integration, authorization)
