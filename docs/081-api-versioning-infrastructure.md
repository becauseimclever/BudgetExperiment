# Feature 081: API Versioning Infrastructure
> **Status:** Planning
> **Priority:** Medium (API design / future-proofing)
> **Estimated Effort:** Medium (2-3 days)
> **Dependencies:** None

## Overview

The coding standard (§9, §20) requires runtime API versioning with URL segments (`/api/v{version}/{resource}`), `[ApiVersion]` attributes, `api-supported-versions` response headers, and deprecation support. An audit found that while controllers use `/api/v1/...` route strings, the versioning is **purely cosmetic** — there's no `Asp.Versioning.Mvc` package, no `AddApiVersioning()` call, and no runtime version negotiation.

## Problem Statement

### Current State

- **25 of 26 controllers** use hardcoded `[Route("api/v1/...")]` — version is a literal string, not a parameter
- **1 controller** (`VersionController`) uses `[Route("api/[controller]")]` — no version segment at all
- **1 controller** (`SuggestionsController`) uses non-standard `api/v1/ai/[controller]` sub-path
- **No** `[ApiVersion]` attributes anywhere
- **No** `AddApiVersioning()` in `Program.cs`
- **No** `api-supported-versions` response header
- **No** deprecation machinery

### Target State

- `Asp.Versioning.Mvc` and `Asp.Versioning.Mvc.ApiExplorer` NuGet packages installed
- `AddApiVersioning()` configured in `Program.cs` with URL segment reader
- All controllers decorated with `[ApiVersion("1.0")]`
- Routes use `[Route("api/v{version:apiVersion}/[controller]")]`
- `api-supported-versions` header automatically included in responses
- `VersionController` brought under versioned routing
- `SuggestionsController` route normalized
- OpenAPI spec reflects versioning

---

## User Stories

### US-081-001: Add API Versioning Package and Configuration
**As a** developer
**I want to** runtime API versioning enabled
**So that** future API versions can coexist with v1 without breaking existing clients.

**Acceptance Criteria:**
- [ ] `Asp.Versioning.Mvc` and `Asp.Versioning.Mvc.ApiExplorer` NuGet packages added
- [ ] `AddApiVersioning()` configured in `Program.cs`
- [ ] Default version set to `1.0`
- [ ] URL segment version reader configured
- [ ] `api-supported-versions` header present in responses
- [ ] Assume version when unspecified option enabled for backward compatibility

### US-081-002: Annotate All Controllers with ApiVersion
**As a** developer
**I want to** all controllers decorated with `[ApiVersion("1.0")]`
**So that** the versioning system knows which version each controller supports.

**Acceptance Criteria:**
- [ ] All 26 controllers have `[ApiVersion("1.0")]` attribute
- [ ] Routes updated to `[Route("api/v{version:apiVersion}/[controller]")]`
- [ ] `VersionController` has versioned route
- [ ] `SuggestionsController` route normalized to standard pattern
- [ ] OpenAPI spec updated to reflect versioning
- [ ] Existing client calls continue to work

---

## Technical Design

### NuGet Packages

```bash
dotnet add src/BudgetExperiment.Api/BudgetExperiment.Api.csproj package Asp.Versioning.Mvc
dotnet add src/BudgetExperiment.Api/BudgetExperiment.Api.csproj package Asp.Versioning.Mvc.ApiExplorer
```

### Program.cs Configuration

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true; // adds api-supported-versions header
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

### Controller Pattern

```csharp
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class AccountsController : ControllerBase
```

### SuggestionsController Normalization

Currently: `[Route("api/v1/ai/[controller]")]`
Options:
- Rename to `[Route("api/v{version:apiVersion}/suggestions")]` — standard pattern
- Or keep AI prefix: `[Route("api/v{version:apiVersion}/ai-suggestions")]` — single resource name

### Client Impact

The Blazor Client currently uses hardcoded API paths like `api/v1/accounts`. Since `AssumeDefaultVersionWhenUnspecified = true`, existing paths continue to work. The route template `v{version:apiVersion}` matches the existing `v1` literal.

---

## Implementation Plan

### Phase 1: Add Versioning Infrastructure

**Objective:** Install packages and configure versioning in `Program.cs`.

**Tasks:**
- [ ] Add NuGet packages
- [ ] Configure `AddApiVersioning()` in `Program.cs`
- [ ] Verify build

### Phase 2: Annotate Controllers

**Objective:** Add `[ApiVersion]` and update routes on all controllers.

**Tasks:**
- [ ] Add `[ApiVersion("1.0")]` to all 26 controllers
- [ ] Update `[Route]` to use `v{version:apiVersion}` template
- [ ] Normalize `SuggestionsController` route
- [ ] Fix `VersionController` to include version segment
- [ ] Verify all endpoints accessible at existing URLs
- [ ] Check `api-supported-versions` header in responses

### Phase 3: Update OpenAPI and Tests

**Objective:** Ensure documentation and tests reflect versioning.

**Tasks:**
- [ ] Verify OpenAPI spec shows versioned endpoints
- [ ] Update API integration tests if route patterns changed
- [ ] Verify Scalar UI works with versioned routes
- [ ] Verify Client still works end-to-end

**Commit:**
```bash
git commit -m "feat(api): add runtime API versioning with Asp.Versioning

- Install Asp.Versioning.Mvc and ApiExplorer packages
- Configure URL segment versioning with default v1.0
- Annotate all 26 controllers with [ApiVersion(\"1.0\")]
- Routes use v{version:apiVersion} template
- api-supported-versions header automatically included
- Backward compatible — existing v1 URLs still work

Refs: #081"
```

---

## Testing Strategy

### Unit Tests
- [ ] Verify versioning configuration doesn't break existing endpoints

### Integration Tests
- [ ] API tests with `WebApplicationFactory` pass with versioned routes
- [ ] Response headers include `api-supported-versions: 1.0`

### Manual Testing
- [ ] All existing Client functionality works
- [ ] Scalar UI shows versioned endpoints
- [ ] `/api/v1/accounts` still resolves

---

## Risk Assessment

- **Low risk**: Adding versioning with `AssumeDefaultVersionWhenUnspecified = true` makes this backward compatible.
- **Route matching**: Test carefully that `v{version:apiVersion}` matches existing `v1` segments.
- **OpenAPI**: May need adjustment for versioned API explorer grouping.

---

## References

- Coding standard §9: "REST endpoints: `/api/v{version}/{resource}`. Version with URL segment (start at v1)."
- Coding standard §20: "Start at v1. Provide `api-supported-versions` header."
- [Asp.Versioning documentation](https://github.com/dotnet/aspnet-api-versioning)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-26 | Initial draft from codebase audit | @copilot |
