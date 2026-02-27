# Feature 083: Centralize Magic Strings and Constants
> **Status:** Planning
> **Priority:** Medium (code quality / maintainability)
> **Estimated Effort:** Small-Medium (1-2 days)
> **Dependencies:** None

## Overview

The coding standard (§8) requires "Centralize constants; avoid magic strings/numbers." An audit found **~16 magic string instances** across 7 files, including duplicated claim names, reconciliation status strings, hardcoded URLs, and sort field names. Several values are duplicated across files, making them error-prone to update.

## Problem Statement

### Current State

#### Claim Name Strings (duplicated in 2+ files)
| String | Files |
|--------|-------|
| `"sub"` | `UserContext.cs`, `NoAuthHandler.cs`, `FamilyUserContext.cs`, `GenericOidcClaimMapper.cs`, etc. |
| `"preferred_username"` | `UserContext.cs`, `NoAuthHandler.cs` |
| `"email"` | `UserContext.cs` |
| `"name"` | `UserContext.cs`, `NoAuthHandler.cs` |
| `"picture"` | `UserContext.cs` |

#### Reconciliation Status Strings
| String | File | Line |
|--------|------|------|
| `"Matched"` | `ReconciliationService.cs` | ~299 |
| `"Pending"` | `ReconciliationService.cs` | ~317 |
| `"Missing"` | `ReconciliationService.cs` | ~324 |
| `"Missing"` | `ReconciliationDtos.cs` | ~200 (default value) |

#### Hardcoded URLs (duplicated 3×)
| String | Files |
|--------|-------|
| `"http://localhost:11434"` | `AppSettings.cs` (Domain), `AiDtos.cs` (Contracts), `AppSettingsConfiguration.cs` (Infrastructure) |

#### Sort Field Strings (duplicated)
| String | Files |
|--------|-------|
| `"Date"` | `TransactionRepository.cs`, `TransactionsController.cs` |

#### Export Column Names
| Strings | File |
|---------|------|
| `"Category"`, `"Amount"`, `"Currency"`, `"Percentage"`, `"Transactions"` | `ExportController.cs` |

### Target State

- All claim names centralized in a `ClaimTypes` or `ClaimConstants` class
- Reconciliation statuses use an enum or constants class
- Default Ollama URL defined once and referenced
- Sort field names centralized as constants
- Export column names centralized

---

## User Stories

### US-083-001: Centralize Claim Name Constants
**As a** developer
**I want to** claim name strings defined in one place
**So that** a typo or rename is caught at compile time, not runtime.

**Acceptance Criteria:**
- [ ] `ClaimConstants` (or similar) class created in Api project
- [ ] All hardcoded claim name strings replaced with constant references
- [ ] Constants shared between `UserContext`, `NoAuthHandler`, `FamilyUserContext`, claim mappers
- [ ] Build succeeds

### US-083-002: Centralize Reconciliation Status Values
**As a** developer
**I want to** reconciliation status values defined as constants or an enum
**So that** status comparisons are consistent and typo-proof.

**Acceptance Criteria:**
- [ ] Status values centralized (enum preferred since these map to display labels)
- [ ] `ReconciliationService` uses the centralized values
- [ ] `ReconciliationDtos` default uses the centralized value
- [ ] Build succeeds

### US-083-003: Centralize Default Configuration Values
**As a** developer
**I want to** default URLs and configuration strings defined once
**So that** changing a default doesn't require updating 3 files.

**Acceptance Criteria:**
- [ ] Default Ollama URL defined in one location
- [ ] `AppSettings`, `AiDtos`, and `AppSettingsConfiguration` reference the single source
- [ ] Sort field defaults centralized
- [ ] Build succeeds

---

## Technical Design

### Claim Constants

```csharp
// src/BudgetExperiment.Api/Authentication/ClaimConstants.cs
public static class ClaimConstants
{
    public const string Subject = "sub";
    public const string PreferredUsername = "preferred_username";
    public const string Email = "email";
    public const string Name = "name";
    public const string Picture = "picture";
}
```

### Reconciliation Status

```csharp
// src/BudgetExperiment.Contracts/Dtos/ReconciliationInstanceStatus.cs
// (or as constants if enum is too heavy)
public static class ReconciliationInstanceStatusValues
{
    public const string Matched = "Matched";
    public const string Pending = "Pending";
    public const string Missing = "Missing";
}
```

### Default Configuration

```csharp
// src/BudgetExperiment.Domain/Settings/AiDefaults.cs
public static class AiDefaults
{
    public const string DefaultOllamaUrl = "http://localhost:11434";
}
```

---

## Implementation Plan

### Phase 1: Claim Constants

**Tasks:**
- [ ] Create `ClaimConstants` class
- [ ] Replace all hardcoded claim strings in Auth files
- [ ] Verify build and tests

### Phase 2: Reconciliation Status Constants

**Tasks:**
- [ ] Create status constants or enum
- [ ] Update `ReconciliationService` and DTOs
- [ ] Verify build and tests

### Phase 3: Configuration and Sort Constants

**Tasks:**
- [ ] Centralize default Ollama URL
- [ ] Centralize sort field constants
- [ ] Centralize export column names
- [ ] Verify build and tests

**Commit:**
```bash
git commit -m "refactor: centralize magic strings into named constants

- Add ClaimConstants for OIDC claim names
- Add reconciliation status constants
- Centralize default Ollama URL (was duplicated 3x)
- Centralize sort field and export column names
- Eliminates ~16 magic string instances across 7 files

Refs: #083"
```

---

## Testing Strategy

### Unit Tests
- [ ] Constants have expected values (regression guard)
- [ ] Existing tests pass with no behavior change

### Verification
- [ ] `dotnet build` succeeds
- [ ] All authentication flows work (claim mapping unchanged)
- [ ] Reconciliation status display unchanged
- [ ] AI settings default unchanged

---

## Risk Assessment

- **Low risk**: Pure refactoring — replacing literals with constants. No behavior change.
- **Layer placement**: Claim constants live in Api (not shared). AI defaults may need to be in Domain or Contracts depending on reference direction.

---

## References

- Coding standard §8: "Centralize constants; avoid magic strings/numbers."

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-26 | Initial draft from codebase audit | @copilot |
