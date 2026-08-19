# ADR 0003: Custom Identity with JWT + Refresh Rotation + Permission Policies

## Status

Accepted

## Context

Phase 2 needs a serious auth model: multi-tenant membership, RBAC permissions, MFA readiness, lockout, API keys, and an audit trail. Full external IdP integration can come later; the portfolio value is in owning the security model.

## Decision

- Custom `User` / `Role` / `Permission` domain model (not ASP.NET Identity user store)
- ASP.NET Core Identity **password hasher** for PBKDF2 hashing
- JWT access tokens (short-lived) + opaque refresh tokens stored as SHA-256 hashes
- Refresh-token rotation with reuse detection (revoke family on reuse)
- Permission policies via `perm:{code}` dynamic authorization
- ABAC hook (`IResourceAuthorizationService`) for resource-level checks
- Tenant and user context resolved from JWT claims after authentication
- TOTP MFA verification path (opt-in per user)
- Auth audit entries for login/refresh/logout/lockout/MFA

## Consequences

- Clear demonstration of auth engineering without hiding behind a black-box IdP
- Later OIDC federation can mint the same claim shape
- Must protect JWT signing keys and rotate carefully in production
- Edge hardening (headers, HSTS, rate limits, key validation) is covered in ADR 0010
