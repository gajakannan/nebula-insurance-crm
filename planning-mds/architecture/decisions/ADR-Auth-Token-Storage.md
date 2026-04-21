# ADR-002: Authentication Token Storage (Frontend)

**Status:** Accepted (updated 2026-03-01 — authentik replaces authentik; token storage strategy unchanged)

**Date:** 2026-01-29

**Deciders:** Architecture Team, Security Team

**Technical Story:** Part of Phase 0 security architecture. See [ADR-006](ADR-006-authentik-idp-migration.md) for IdP migration details.

---

## Context and Problem Statement

The React frontend receives JWT access tokens and refresh tokens from authentik after successful authentication. These tokens grant access to protected API endpoints and must be stored client-side for the duration of the user session. The storage mechanism must balance security (preventing XSS/CSRF attacks), user experience (persistent login), and compliance with insurance industry security standards.

**Key Questions:**
- Where should we store JWT access tokens and refresh tokens in the browser?
- How do we protect tokens from Cross-Site Scripting (XSS) attacks?
- Should tokens persist across browser sessions or be session-only?
- How do we handle token refresh without degrading user experience?

---

## Decision Drivers

- **Security (Primary):** Prevent token theft via XSS, CSRF, or browser storage attacks
- **XSS Protection:** Tokens must not be accessible to JavaScript if compromised by XSS
- **User Experience:** Minimize login friction, support "remember me" functionality
- **Compliance:** Insurance industry requires protection of authentication credentials
- **Browser Support:** Solution must work across modern browsers
- **Mobile/Desktop:** Consider future Electron or mobile app requirements
- **Token Refresh:** Seamless token refresh without user interaction
- **OWASP Compliance:** Follow OWASP Top 10 security best practices

---

## Considered Options

1. **HttpOnly Cookies** - Tokens stored in secure, httpOnly, SameSite cookies
2. **LocalStorage** - Tokens stored in browser localStorage (persistent)
3. **SessionStorage** - Tokens stored in browser sessionStorage (tab-scoped)
4. **Memory-Only (React State)** - Tokens stored only in application memory
5. **Hybrid (Memory + HttpOnly Refresh)** - Access token in memory, refresh token in httpOnly cookie

---

## Decision Outcome

**Chosen option:** **Hybrid Approach: Memory + HttpOnly Refresh Token**

We will store tokens using a two-tier strategy:
- **Access Token:** Stored in React application memory (component state/context) - **never persisted**
- **Refresh Token:** Stored in secure, httpOnly, SameSite=Strict cookie - **persistent**

### Architecture Details:

**Access Token (In-Memory):**
- Retrieved from authentik during login
- Stored in React Context or Zustand state (in-memory only)
- Included in API requests via `Authorization: Bearer <token>` header
- Lost on page refresh → triggers automatic refresh flow
- Short TTL (5-15 minutes)

**Refresh Token (HttpOnly Cookie):**
- Set by backend after authentik token exchange
- Cookie attributes: `httpOnly=true`, `secure=true`, `SameSite=Strict`
- Not accessible to JavaScript (XSS-proof)
- Used automatically by browser when calling refresh endpoint
- Longer TTL (7 days with sliding expiration)

**Token Refresh Flow:**
1. On app load or access token expiry, frontend calls `/auth/refresh`
2. Browser automatically sends refresh token cookie
3. Backend validates refresh token with authentik
4. Backend returns new access token in response body
5. Frontend stores new access token in memory
6. Backend rotates refresh token (new httpOnly cookie)

### Justification:
- **Maximum XSS Protection:** Access tokens in memory cannot be stolen by malicious scripts
- **CSRF Protection:** Refresh token cookie uses SameSite=Strict; backend validates CSRF token
- **OWASP Compliance:** Follows OWASP A02:2021 (Cryptographic Failures) and A07:2021 (Identification and Authentication Failures)
- **Secure by Default:** Even if XSS vulnerability exists, tokens are not exposed
- **Good UX:** Refresh token cookie enables seamless re-authentication after page refresh
- **Industry Standard:** Used by Auth0, Okta, and recommended by OAuth 2.0 security best practices

---

## Consequences

### Positive:
- **XSS Resilient:** Access token in memory is inaccessible to JavaScript XSS attacks
- **CSRF Resilient:** HttpOnly + SameSite cookies prevent CSRF token theft
- **Secure Persistence:** Refresh token allows persistent login without exposing access token
- **Token Rotation:** Backend can enforce refresh token rotation on every use
- **Compliance:** Meets insurance industry security requirements for credential storage
- **Revocation:** Refresh tokens can be revoked server-side (logout, suspicious activity)
- **Short-Lived Access:** Access tokens have minimal blast radius if intercepted (5-15 min TTL)

### Negative:
- **Page Refresh Overhead:** Each page refresh triggers token refresh call (mitigated by fast refresh endpoint)
- **Backend Dependency:** Requires backend `/auth/refresh` endpoint to exchange cookies for tokens
- **Cookie Management:** Backend must manage httpOnly cookie lifecycle (rotation, expiration)
- **CORS Complexity:** Requires proper CORS configuration for cookie credentials (`credentials: 'include'`)
- **Subdomain Scope:** Cookies are domain-scoped; multi-domain support requires additional configuration

### Neutral:
- **State Loss on Refresh:** Access token is lost on page refresh (intentional security feature)
- **No LocalStorage:** Cannot inspect tokens easily in DevTools (debugging requires network inspection)
- **Backend Responsibility:** Token refresh logic lives on backend (proper separation of concerns)

---

## Implementation Notes

### Frontend (React):

```typescript
// 1. Token Context (in-memory storage)
const AuthContext = React.createContext<{
  accessToken: string | null;
  refreshToken: () => Promise<void>;
}>({ accessToken: null, refreshToken: async () => {} });

// 2. authentik Login Flow
async function login() {
  // Redirect to authentik
  authentik.login();
  // After redirect back with auth code:
  const tokens = await authentik.tokenExchange(authCode);
  // Send tokens to backend to set httpOnly cookie
  await axios.post('/auth/token', {
    access_token: tokens.access_token,
    refresh_token: tokens.refresh_token,
  }, { withCredentials: true }); // Important: send cookies

  // Store access token in memory
  setAccessToken(tokens.access_token);
}

// 3. Token Refresh on App Load
useEffect(() => {
  refreshAccessToken(); // Call on mount
}, []);

async function refreshAccessToken() {
  try {
    const response = await axios.post('/auth/refresh', {}, {
      withCredentials: true, // Send httpOnly cookie
    });
    setAccessToken(response.data.access_token);
  } catch (error) {
    // Refresh failed → redirect to login
    redirectToLogin();
  }
}

// 4. API Interceptor (add access token to requests)
axios.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});
```

### Backend (.NET 10 Minimal APIs):

```csharp
// Program.cs - Service Configuration
var builder = WebApplication.CreateBuilder(args);

// Add CORS for frontend
builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", builder => {
        builder.WithOrigins("http://localhost:5173") // Vite dev server
               .AllowCredentials() // Required for cookies
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

// Add authentik service
builder.Services.AddScoped<IauthentikService, authentikService>();

var app = builder.Build();
app.UseCors("AllowFrontend");

// Auth Endpoints Group
var authGroup = app.MapGroup("/auth");

// 1. Token Exchange Endpoint (after authentik login)
authGroup.MapPost("/token", async (
    TokenRequest request,
    IauthentikService authentikService,
    HttpContext context) =>
{
    // Validate tokens with authentik
    var validation = await authentikService.ValidateTokens(request);

    // Set httpOnly refresh token cookie
    context.Response.Cookies.Append("refresh_token", request.RefreshToken, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = DateTimeOffset.UtcNow.AddDays(7),
    });

    return Results.Ok(new { access_token = request.AccessToken });
});

// 2. Token Refresh Endpoint
authGroup.MapPost("/refresh", async (
    IauthentikService authentikService,
    HttpContext context) =>
{
    // Read refresh token from httpOnly cookie
    var refreshToken = context.Request.Cookies["refresh_token"];
    if (string.IsNullOrEmpty(refreshToken))
        return Results.Unauthorized();

    // Exchange with authentik for new tokens
    var newTokens = await authentikService.RefreshTokens(refreshToken);

    // Rotate refresh token (set new cookie)
    context.Response.Cookies.Append("refresh_token", newTokens.RefreshToken, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = DateTimeOffset.UtcNow.AddDays(7),
    });

    return Results.Ok(new { access_token = newTokens.AccessToken });
});

// 3. Logout Endpoint
authGroup.MapPost("/logout", async (
    IauthentikService authentikService,
    HttpContext context) =>
{
    var refreshToken = context.Request.Cookies["refresh_token"];

    // Delete refresh token cookie
    context.Response.Cookies.Delete("refresh_token");

    // Optionally revoke token with authentik
    if (!string.IsNullOrEmpty(refreshToken))
    {
        await authentikService.RevokeToken(refreshToken);
    }

    return Results.Ok();
});

app.Run();
```

**Axios (Frontend):**
```typescript
axios.defaults.withCredentials = true; // Include cookies in all requests
```

---

## Security Considerations

1. **XSS Mitigation:**
   - Access token in memory is lost if script is compromised
   - Content Security Policy (CSP) headers prevent inline script injection
   - Regular dependency audits (Dependabot, npm audit)

2. **CSRF Mitigation:**
   - SameSite=Strict prevents cross-site cookie sending
   - Optional: Add CSRF token validation on refresh endpoint
   - Origin and Referer header validation

3. **Token Expiration:**
   - Access tokens: 5-15 minutes (short-lived)
   - Refresh tokens: 7 days with sliding expiration (refresh extends lifetime)
   - Idle timeout: 30 minutes (configurable in authentik)

4. **Revocation:**
   - Refresh tokens stored in database for revocation tracking
   - Logout immediately revokes refresh token
   - Suspicious activity detection can revoke all user tokens

---

## Alternatives Considered (Rejected)

### LocalStorage (Rejected)
**Reason:** Accessible to JavaScript; vulnerable to XSS attacks. OWASP explicitly warns against storing tokens in localStorage.

### SessionStorage (Rejected)
**Reason:** Still accessible to JavaScript; same XSS vulnerability as localStorage. Also, tab-scoped storage creates poor UX for multi-tab usage.

### HttpOnly Cookies for Access Token (Rejected)
**Reason:** Would require backend to proxy all API requests (coupling frontend and backend). Also, cookie size limits could be problematic for large JWTs.

### Memory-Only (No Persistence) (Rejected)
**Reason:** Poor UX; users would need to log in on every page refresh. Not acceptable for production CRM application.

---

## Related ADRs

- ADR-001: Authentication Strategy (authentik OIDC/JWT)
- ADR-003: Authorization Strategy (Casbin ABAC)
- ADR-005: CSRF Protection Strategy
