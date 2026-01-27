# Easier Deployment for New Users: Optional Postgres, Auth Off, and Flexible Auth Providers
> **Status:** 🗒️ Planning

## Problem

New users face friction when deploying BudgetExperiment due to the need for external Postgres setup and mandatory Authentik configuration. This increases onboarding time and complexity, especially for evaluation or small family use.

## Goals

- **Lower the barrier to entry** for new users and demo environments.
- Provide a **one-command deployment** option that includes all required services.
- Allow **authentication to be disabled** for local/demo use, defaulting all users to the "family" scope.
- Support **alternative OIDC/OAuth providers** (e.g., Google, Microsoft) in addition to Authentik.

## Proposed Solution

### 1. Optional Postgres Container
- Provide a `docker-compose.demo.yml` (or similar) that includes:
  - BudgetExperiment API + Client
  - Postgres database (pre-configured, persistent volume)
- Document usage: `docker compose -f docker-compose.demo.yml up`
- Make clear this is for demo/dev only; production should use a managed/external Postgres.

### 2. Auth Off Mode
- Add configuration option (env var or appsettings): `Authentication:Mode = None`
- When set, API disables all authentication/authorization middleware:
  - All users are treated as authenticated and assigned the "family" scope/role.
  - UI hides login/logout/profile features.
- Document security implications (not for production).

### 3. Flexible OIDC/OAuth Provider Support

### 4. Easier Configuration via .env File
- Support loading configuration from a `.env` file in the root directory (alongside `docker-compose` files).
- Environment variables in `.env` override values in `appsettings.json` and are automatically picked up by the container.
- Document common settings (e.g., `ConnectionStrings__AppDb`, `Authentication__Mode`, `Authentication__Provider`, etc.) and how to set them in `.env`.
- Example usage:
  ```env
  # .env file example
  ConnectionStrings__AppDb=Host=postgres;Port=5432;Database=budget;Username=postgres;Password=postgres
  Authentication__Mode=None
  Authentication__Provider=Google
  Authentication__Google__ClientId=your-client-id
  Authentication__Google__ClientSecret=your-client-secret
  ```
- Update documentation to recommend `.env` for all container-based deployments, making configuration changes easy without editing files inside the container.
- Refactor authentication config to allow:
  - Authentik (default)
  - Other OIDC-compliant providers (Google, Microsoft, etc.)
- Expose provider selection and required settings in `appsettings.json` and docs.
- Document how to configure each supported provider.

## Acceptance Criteria

- [ ] New compose file spins up API, Client, and Postgres with one command.
- [ ] Auth can be disabled via config; all users get family scope.
- [ ] Auth provider can be switched via config; docs for Authentik, Google, Microsoft.
- [ ] Docs updated for all new options, .env usage, and security notes.

## Out of Scope
- Production hardening for included Postgres (demo/dev only).
- SSO/SCIM/advanced user management.

## Migration/Upgrade Notes
- Existing deployments are unaffected unless new compose/config is used.
- No breaking changes to current Authentik setup.

---

*Created: 2026-01-27*
