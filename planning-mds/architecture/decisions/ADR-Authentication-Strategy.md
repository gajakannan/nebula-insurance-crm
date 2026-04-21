# ADR-001: Authentication Strategy (Keycloak — Superseded)

**Status:** Superseded by [ADR-006: authentik IdP Migration](ADR-006-authentik-idp-migration.md)

**Date:** 2026-01-29

**Deciders:** Architecture Team

**Technical Story:** Part of Phase 0 foundational architecture

---

## Context and Problem Statement

Nebula requires a robust authentication solution that supports multiple user types (Distribution & Marketing, Underwriters, Broker Relationship Managers, MGA Program Managers, and Admin), provides enterprise-grade security, and enables future extensibility for external users (MGA users with limited access). The system must handle user identity, session management, and integrate with authorization policies while maintaining compliance with insurance industry security standards.

**Key Questions:**
- How do we securely authenticate internal users across frontend and backend?
- How do we manage user sessions and token lifecycle?
- How do we support future federation or SSO requirements?
- How do we ensure the solution is maintainable and follows industry standards?

---

## Decision Drivers

- **Security:** Must support industry-standard authentication protocols (OAuth 2.0/OIDC)
- **Compliance:** Insurance industry requires audit trails and secure identity management
- **User Management:** Centralized user administration for internal staff
- **Future Extensibility:** Need to support external users (MGA portals) in future phases
- **Integration:** Must integrate seamlessly with .NET 10 Minimal API backend and React 18 frontend
- **Token Standards:** JWT tokens for stateless API authentication
- **Maintainability:** Prefer managed IAM solution over building custom auth
- **Multi-tenancy Ready:** Support for future multi-tenant scenarios

---

## Considered Options

1. **Keycloak (Open Source IAM)** - Self-hosted OIDC/OAuth 2.0 provider with realm management
2. **Azure AD B2C** - Cloud-based identity service from Microsoft
3. **Auth0** - Commercial SaaS identity platform
4. **Custom JWT Auth** - Build authentication system from scratch using .NET 10 Identity

---

## Decision Outcome

**Chosen option:** **Keycloak (OIDC/JWT)**

We will use Keycloak as our Identity and Access Management (IAM) provider implementing OpenID Connect (OIDC) for authentication with JWT bearer tokens.

### Architecture Components:
- **Keycloak Server:** Self-hosted identity provider managing users, realms, and sessions
- **Protocol:** OpenID Connect (OIDC) with OAuth 2.0 authorization code flow
- **Tokens:** JWT access tokens and refresh tokens
- **Frontend Integration:** React app redirects to Keycloak for login, receives tokens
- **Backend Integration:** .NET 10 Minimal APIs validate JWT tokens on every API request
- **User Store:** Keycloak manages user credentials and profiles
- **User Profiles:** Application maintains UserProfile entities linked to Keycloak subject (sub claim)

### Justification:
- **Industry Standard:** OIDC/OAuth 2.0 are proven, well-documented standards
- **Open Source:** No per-user licensing costs, full control over deployment
- **Feature Rich:** Built-in user management, MFA, password policies, session management
- **Extensible:** Supports custom themes, user federation, LDAP/AD integration
- **Docker Support:** Easy to deploy alongside application services
- **Casbin Integration:** Keycloak provides identity, Casbin handles authorization (clean separation)

---

## Consequences

### Positive:
- **Separation of Concerns:** Authentication (Keycloak) is decoupled from authorization (Casbin) and business logic
- **Standardized Security:** Leverages battle-tested OIDC implementation instead of custom code
- **Centralized User Management:** Single admin interface for managing all user accounts
- **Audit Trail:** Keycloak logs all authentication events (logins, logouts, failures)
- **Token-Based:** Stateless API authentication enables horizontal scaling
- **Future-Proof:** Easy to add SSO, SAML, or external identity federation later
- **Multi-Tenant Ready:** Keycloak realms can support future multi-tenancy needs
- **Developer Experience:** Standard OIDC libraries available for React and .NET 10

### Negative:
- **Infrastructure Complexity:** Adds another service to deploy and monitor (Keycloak + PostgreSQL)
- **Initial Setup:** Requires realm configuration, client setup, and integration effort
- **Self-Hosted Responsibility:** We manage Keycloak upgrades, backups, and availability (mitigated by Docker deployment)
- **Learning Curve:** Team must understand OIDC flows and Keycloak administration

### Neutral:
- **UserProfile Sync:** Application must create UserProfile records on first login using Keycloak subject as foreign key
- **Token Validation:** Backend must validate JWT signature and claims on every request (standard practice)
- **Network Dependency:** Frontend → Keycloak → Backend flow adds network hops (acceptable for security)

---

## Implementation Notes

1. **Keycloak Setup:**
   - Deploy Keycloak via Docker Compose with PostgreSQL backend
   - Create `nebula` realm for application
   - Configure OIDC client for React frontend (public client)
   - Configure API audience for .NET 10 Minimal API backend

2. **Frontend Integration:**
   - Use `keycloak-js` library for React integration
   - Implement OIDC authorization code flow with PKCE
   - Store tokens securely (see ADR-002: Auth Token Storage)
   - Auto-refresh tokens before expiration

3. **Backend Integration (.NET 10 Minimal APIs):**
   - Configure `Microsoft.AspNetCore.Authentication.JwtBearer` in Program.cs
   - Validate JWT signature using Keycloak's JWKS endpoint
   - Extract user identity from `sub` claim via endpoint filters or middleware
   - Create UserProfile on first authenticated request if not exists
   - Use endpoint authorization with `.RequireAuthorization()` on route groups
   - Apply endpoint filters for user profile synchronization

4. **Audit Integration:**
   - Log Keycloak events to ActivityTimelineEvent table
   - Track login, logout, password changes via Keycloak event listeners

---

## Related ADRs

- ADR-002: Auth Token Storage (Frontend)
- ADR-003: Authorization Strategy (Casbin ABAC)
- ADR-004: User Profile Synchronization
