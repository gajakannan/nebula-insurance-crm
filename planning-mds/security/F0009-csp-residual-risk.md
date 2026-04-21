# F0009 CSP Residual Risk — Accepted for Phase 1

Feature: F0009 Authentication and Role-Based Login
Date: 2026-03-04
Status: Accepted for Phase 1

---

## 1. style-src 'unsafe-inline' — Accepted

**Risk:** `'unsafe-inline'` in `style-src` permits inline `<style>` blocks and `style=` attributes. An XSS
payload that can inject HTML could use inline styles for visual spoofing or CSS-based data exfiltration
(e.g., attribute selectors triggering `background-image` requests).

**Why it is accepted for Phase 1:** Tailwind CSS v4 (used by this project) generates inline style attributes
for certain utility classes at runtime. Removing `'unsafe-inline'` from `style-src` would require deploying a
nonce infrastructure — generating a per-request nonce on the server, embedding it in the HTML response, and
propagating it to every `<style>` element and the Tailwind runtime. That infrastructure is out of scope for
Phase 1.

**Planned mitigation path (Phase 2):** Adopt a nonce-based `style-src` using server-side nonce injection in the
nginx configuration (e.g., `ngx_http_sub_module` or a lightweight middleware layer). The `'unsafe-inline'`
directive will be removed at that point.

---

## 2. sessionStorage PKCE State — Partially Mitigated

**Risk:** `oidc-client-ts` stores the PKCE code verifier and OIDC state parameter in `sessionStorage` during
the authorization code flow. Any script executing in the page origin can read `sessionStorage`.

**Primary mitigation:** The CSP `script-src` directive (both dev and production) restricts which scripts may
execute. In production the directive is `script-src 'self'`, which prevents injected inline scripts and
cross-origin scripts from executing. This means an XSS payload cannot execute unless it is hosted on the same
origin, which is a high bar in practice.

**Residual risk:** The mitigation is not elimination. Trusted first-party scripts (i.e., the Nebula CRM
application bundle itself, and any third-party scripts loaded from `'self'`) retain full access to
`sessionStorage`. A supply-chain compromise of a bundled dependency would have access to the PKCE verifier
before it is exchanged. This is accepted for Phase 1.

**Note on scope:** The PKCE verifier is only sensitive during the authorization code flow window (a few
seconds). After `oidc-client-ts` completes the code exchange the verifier is consumed and cleared. The
`oidcUserManager.clearStaleState()` call in the session teardown contract (section 2.1) ensures residual
artifacts are also cleared on logout and on session expiry.

---

## 3. Production connect-src — Deployer Substitution Required

**Risk:** The production CSP in `experience/nginx.conf` contains the literal placeholder
`REPLACE_WITH_OIDC_AUTHORITY` in the `connect-src` directive. If this placeholder is not substituted before
serving traffic, `oidc-client-ts` will be blocked by CSP from reaching the OIDC discovery endpoint and the
token endpoint, breaking authentication entirely.

**Action required from deployer:** Before starting the nginx container in any production or staging environment,
substitute the placeholder with the real OIDC authority URL (the value of `VITE_OIDC_AUTHORITY`). Example:

```bash
sed -i "s|REPLACE_WITH_OIDC_AUTHORITY|${VITE_OIDC_AUTHORITY}|g" /etc/nginx/nginx.conf
```

This substitution must be run in all three `location` blocks inside `experience/nginx.conf` that repeat the
`Content-Security-Policy` header (the root location, the hashed-assets location, and the `index.html`
location). The `sed` command above handles all occurrences in a single pass.

**Why the placeholder approach:** CSP headers must be static strings at build time. The OIDC authority URL is
an environment-specific runtime value and cannot be embedded at `vite build` time without baking environment
assumptions into the production artifact. Runtime substitution in the nginx config is the standard approach
for this pattern.

---

## Summary Table

| Item | Accepted? | Mitigation | Phase 2 Path |
|---|---|---|---|
| `style-src 'unsafe-inline'` | Yes — Phase 1 | Tailwind requirement; script-src blocks script execution | Nonce infrastructure |
| sessionStorage PKCE access by trusted scripts | Yes — Phase 1 | `script-src 'self'` blocks injected scripts | Consider memory-only state if oidc-client-ts supports it |
| Production connect-src placeholder | N/A — operational | Deployer must run sed substitution before serving | Automate via container entrypoint script |
