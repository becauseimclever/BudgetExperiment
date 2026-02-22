# Authentication Providers Guide

BudgetExperiment supports multiple authentication modes. Choose the option that best fits your deployment:

| Mode | Use Case | Setup Time |
|------|----------|------------|
| **None** (auth off) | Demo, single-user, family | 0 min |
| **Authentik** (default) | Self-hosted identity, production | 15–30 min |
| **Google** | Small teams with Google Workspace | 10 min |
| **Microsoft Entra ID** | Enterprise / Azure AD | 15 min |
| **Generic OIDC** | Keycloak, Auth0, Okta, etc. | 10–20 min |

---

## Quick Reference

All authentication settings are configured via environment variables or `appsettings.json`. The two key variables are:

```bash
Authentication__Mode=OIDC      # "None" or "OIDC"
Authentication__Provider=Authentik  # "Authentik", "Google", "Microsoft", "OIDC"
```

When `Mode=None`, no provider configuration is needed. When `Mode=OIDC`, configure the provider section that matches your `Provider` value.

---

## Auth-Off Mode (No Authentication)

The simplest option — no identity provider needed. All requests are treated as a single "Family" user.

```bash
Authentication__Mode=None
```

### When to use

- Evaluating BudgetExperiment locally
- Single-user or family use behind a private network
- Demo deployments

### Security warning

> ⚠️ **Never expose an auth-off instance to the public internet.** All data is accessible without credentials.

The application displays a banner and logs a warning at startup when authentication is disabled.

### Docker Compose (demo mode)

```bash
docker compose -f docker-compose.demo.yml up -d
```

This uses auth-off by default. See [DEPLOY-QUICKSTART.md](../DEPLOY-QUICKSTART.md) for details.

---

## Authentik (Default Provider)

[Authentik](https://goauthentik.io/) is the recommended identity provider for self-hosted production deployments.

### Prerequisites

- A running Authentik instance
- An OAuth2/OpenID Provider configured in Authentik

### Authentik setup

1. In Authentik, go to **Applications → Providers → Create**.
2. Choose **OAuth2/OpenID Connect Provider**.
3. Configure:
   - **Name:** `BudgetExperiment`
   - **Authorization flow:** Select your authorization flow
   - **Client type:** `Public`
   - **Redirect URIs:** `https://your-budget-app.example.com/authentication/login-callback`
   - **Scopes:** `openid`, `profile`, `email`
4. Create an **Application** linked to this provider.
5. Note the **Application Slug** (used in the authority URL) and **Client ID**.

### Environment variables

```bash
Authentication__Mode=OIDC
Authentication__Provider=Authentik
Authentication__Authentik__Authority=https://your-authentik.example.com/application/o/budget-experiment/
Authentication__Authentik__Audience=budget-experiment
Authentication__Authentik__RequireHttpsMetadata=true
```

### Docker Compose example

```yaml
environment:
  - Authentication__Mode=OIDC
  - Authentication__Provider=Authentik
  - Authentication__Authentik__Authority=${AUTHENTIK_AUTHORITY}
  - Authentication__Authentik__Audience=${AUTHENTIK_AUDIENCE}
  - Authentication__Authentik__RequireHttpsMetadata=${AUTHENTIK_REQUIRE_HTTPS:-true}
```

### Legacy configuration

Existing deployments that set `Authentication__Authentik__Authority` without explicitly setting `Mode` or `Provider` continue to work — Authentik is the default provider and OIDC is the default mode.

The legacy `Authentication__Authentik__Enabled=false` flag still disables authentication (treated as `Mode=None`), but a deprecation warning is logged. Migrate to `Authentication__Mode=None` when convenient.

---

## Google OAuth

Use your Google Workspace or personal Google account for authentication.

### Prerequisites

- A Google Cloud project with the OAuth consent screen configured
- OAuth 2.0 credentials (Web application type)

### Google Cloud Console setup

1. Go to [Google Cloud Console → APIs & Services → Credentials](https://console.cloud.google.com/apis/credentials).
2. Click **Create Credentials → OAuth client ID**.
3. Select **Web application**.
4. Configure:
   - **Name:** `BudgetExperiment`
   - **Authorized JavaScript origins:** `https://your-budget-app.example.com`
   - **Authorized redirect URIs:** `https://your-budget-app.example.com/authentication/login-callback`
5. Note the **Client ID** and **Client Secret**.
6. Go to **OAuth consent screen** and add the following scopes: `openid`, `profile`, `email`.

### Environment variables

```bash
Authentication__Mode=OIDC
Authentication__Provider=Google
Authentication__Google__ClientId=123456789-xyz.apps.googleusercontent.com
Authentication__Google__ClientSecret=GOCSPX-xxxxx
```

### Docker Compose example

```yaml
environment:
  - Authentication__Mode=OIDC
  - Authentication__Provider=Google
  - Authentication__Google__ClientId=${GOOGLE_CLIENT_ID}
  - Authentication__Google__ClientSecret=${GOOGLE_CLIENT_SECRET}
```

### Notes

- Google's OIDC authority (`https://accounts.google.com`) is set automatically.
- The `email` claim is mapped to `preferred_username` for consistency with other providers.
- Google OAuth requires HTTPS for redirect URIs in production.

---

## Microsoft Entra ID (Azure AD)

Integrate with your organization's Microsoft identity platform.

### Prerequisites

- An Azure account with permission to register applications
- Access to the [Azure Portal](https://portal.azure.com)

### Azure app registration

1. Go to [Azure Portal → Microsoft Entra ID → App registrations](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade).
2. Click **New registration**.
3. Configure:
   - **Name:** `BudgetExperiment`
   - **Supported account types:**
     - *Single tenant* — your organization only
     - *Multitenant* — any Azure AD directory
   - **Redirect URI:** `Single-page application` → `https://your-budget-app.example.com/authentication/login-callback`
4. Note the **Application (client) ID** and **Directory (tenant) ID**.

### Environment variables

```bash
Authentication__Mode=OIDC
Authentication__Provider=Microsoft
Authentication__Microsoft__ClientId=00000000-0000-0000-0000-000000000000
Authentication__Microsoft__TenantId=your-tenant-id
```

### Tenant options

| TenantId value | Who can sign in |
|---------------|-----------------|
| `common` (default) | Any Microsoft account (personal + work/school) |
| `organizations` | Any Azure AD work/school account |
| `consumers` | Personal Microsoft accounts only |
| Specific GUID | Only accounts from that Azure AD tenant |

### Docker Compose example

```yaml
environment:
  - Authentication__Mode=OIDC
  - Authentication__Provider=Microsoft
  - Authentication__Microsoft__ClientId=${MICROSOFT_CLIENT_ID}
  - Authentication__Microsoft__TenantId=${MICROSOFT_TENANT_ID:-common}
```

### Notes

- Authority URL is computed automatically: `https://login.microsoftonline.com/{tenantId}/v2.0`
- The `email` claim is mapped to `preferred_username` for consistency.
- For confidential clients, you can also set `Authentication__Microsoft__ClientSecret`.

---

## Generic OIDC (Keycloak, Auth0, Okta, etc.)

Connect any OIDC-compliant identity provider.

### Environment variables

```bash
Authentication__Mode=OIDC
Authentication__Provider=OIDC
Authentication__Oidc__Authority=https://your-idp.example.com/realms/master
Authentication__Oidc__ClientId=budget-experiment
Authentication__Oidc__ClientSecret=your-client-secret
```

### Optional settings

```bash
# Custom audience (if different from ClientId)
Authentication__Oidc__Audience=budget-api

# Disable HTTPS metadata requirement (dev only)
Authentication__Oidc__RequireHttpsMetadata=false

# Custom scopes (array binding)
Authentication__Oidc__Scopes__0=openid
Authentication__Oidc__Scopes__1=profile
Authentication__Oidc__Scopes__2=email
Authentication__Oidc__Scopes__3=custom-scope

# Custom claim mappings (source → target)
Authentication__Oidc__ClaimMappings__name=preferred_username
Authentication__Oidc__ClaimMappings__custom_role=role
```

### Keycloak example

```bash
Authentication__Mode=OIDC
Authentication__Provider=OIDC
Authentication__Oidc__Authority=https://keycloak.example.com/realms/budget
Authentication__Oidc__ClientId=budget-experiment
Authentication__Oidc__ClientSecret=your-keycloak-secret
```

### Auth0 example

```bash
Authentication__Mode=OIDC
Authentication__Provider=OIDC
Authentication__Oidc__Authority=https://your-tenant.auth0.com/
Authentication__Oidc__ClientId=your-auth0-client-id
Authentication__Oidc__ClientSecret=your-auth0-client-secret
Authentication__Oidc__Audience=https://budget-api.example.com
```

### Okta example

```bash
Authentication__Mode=OIDC
Authentication__Provider=OIDC
Authentication__Oidc__Authority=https://your-org.okta.com/oauth2/default
Authentication__Oidc__ClientId=your-okta-client-id
Authentication__Oidc__ClientSecret=your-okta-client-secret
```

### Docker Compose example

```yaml
environment:
  - Authentication__Mode=OIDC
  - Authentication__Provider=OIDC
  - Authentication__Oidc__Authority=${OIDC_AUTHORITY}
  - Authentication__Oidc__ClientId=${OIDC_CLIENT_ID}
  - Authentication__Oidc__ClientSecret=${OIDC_CLIENT_SECRET}
  - Authentication__Oidc__Audience=${OIDC_AUDIENCE:-}
```

---

## Troubleshooting

### Application won't start — "Authority is not configured"

```
InvalidOperationException: Authentication is set to OIDC mode but no Authority is configured.
```

**Cause:** `Mode=OIDC` (the default) but no provider authority is set.

**Solutions:**
- If you want auth off: set `Authentication__Mode=None`
- If using Authentik: set `Authentication__Authentik__Authority=...`
- If using another provider: set the appropriate authority/clientId (see provider sections above)

### Login redirects fail or loop

**Common causes:**
1. **Wrong redirect URI** — The redirect URI in your identity provider must exactly match `https://your-app/authentication/login-callback`
2. **HTTP vs HTTPS mismatch** — Most providers require HTTPS redirect URIs. For local dev, set `RequireHttpsMetadata=false`.
3. **CORS issues** — Ensure `AllowedHosts` includes your domain, or set it to `*` for testing.

### "401 Unauthorized" on API calls

**In auth-off mode:** This should not happen. Verify `Authentication__Mode=None` is set (case-insensitive).

**In OIDC mode:**
1. Verify the token audience matches your configuration
2. Check that scopes include `openid`, `profile`, `email`
3. Verify the authority URL is reachable from the server

### Claims not mapping correctly

Some providers use different claim names. Use the Generic OIDC provider with custom claim mappings:

```bash
Authentication__Provider=OIDC
Authentication__Oidc__ClaimMappings__your_claim=preferred_username
```

The application needs a `preferred_username` or `name` claim for displaying the user's name, and `sub` for the user ID.

### Auth-off banner won't go away

The "Running in demo mode" banner appears when `Authentication__Mode=None`. Set `Authentication__Mode=OIDC` and configure a provider to remove it.

### Migrating from demo to production

1. Set `Authentication__Mode=OIDC` and configure your provider
2. Point `ConnectionStrings__AppDb` to a production PostgreSQL instance
3. (Optional) Reassign demo data from the family user:
   ```sql
   UPDATE "Transactions" SET "UserId" = 'your-real-user-guid'
   WHERE "UserId" = '00000000-0000-0000-0000-000000000001';
   ```

---

## Configuration Reference

### All environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `Authentication__Mode` | `OIDC` | `None` or `OIDC` |
| `Authentication__Provider` | `Authentik` | `Authentik`, `Google`, `Microsoft`, `OIDC` |
| `Authentication__Authentik__Authority` | — | Authentik OIDC provider URL |
| `Authentication__Authentik__Audience` | — | Authentik client/audience ID |
| `Authentication__Authentik__RequireHttpsMetadata` | `true` | Require HTTPS for metadata endpoint |
| `Authentication__Google__ClientId` | — | Google OAuth client ID |
| `Authentication__Google__ClientSecret` | — | Google OAuth client secret |
| `Authentication__Microsoft__ClientId` | — | Microsoft Entra ID app client ID |
| `Authentication__Microsoft__TenantId` | `common` | Azure AD tenant ID or alias |
| `Authentication__Microsoft__ClientSecret` | — | Microsoft client secret (optional) |
| `Authentication__Oidc__Authority` | — | Generic OIDC authority URL |
| `Authentication__Oidc__ClientId` | — | Generic OIDC client ID |
| `Authentication__Oidc__ClientSecret` | — | Generic OIDC client secret |
| `Authentication__Oidc__Audience` | — | Generic OIDC audience (if different from ClientId) |
| `Authentication__Oidc__RequireHttpsMetadata` | `true` | Require HTTPS for metadata endpoint |
| `Authentication__Oidc__Scopes__N` | `openid,profile,email` | Custom scopes (array) |
| `Authentication__Oidc__ClaimMappings__X` | — | Claim mappings (source=target) |
