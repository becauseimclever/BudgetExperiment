# Feature 063: Secure File Upload Hardening
> **Status:** Planning

## Overview

This feature hardens the CSV import functionality by moving all file parsing to the client side (Blazor WebAssembly) and eliminating server-side file uploads entirely. By parsing CSV files in the browser and only transmitting structured transaction data to the server, we eliminate entire classes of file upload vulnerabilities including malicious file execution, path traversal, and resource exhaustion attacks.

## Problem Statement

### Current State

The current import flow uploads raw CSV files to the server:

1. **Client**: User selects a CSV file → file content is sent to `POST /api/v1/import/parse`
2. **Server**: Receives file, parses CSV, returns structured data
3. **Client**: User maps columns and previews
4. **Server**: Receives mapped data via `POST /api/v1/import/execute` for final import

Current security measures:
- ✅ File size limit (10 MB)
- ✅ Authentication required
- ✅ No file persistence to disk
- ⚠️ No server-side file extension validation
- ⚠️ No content-type verification
- ⚠️ No magic byte validation
- ⚠️ No CSV injection sanitization

### Target State

Shift to client-side parsing with data-only API:

1. **Client**: User selects CSV file → **parsed entirely in browser** → structured preview displayed
2. **Client**: User maps columns, previews data
3. **Client → Server**: Only structured transaction DTOs sent to `POST /api/v1/import/execute`

Benefits:
- **Eliminates file upload attack surface** - No file bytes ever reach the server
- **Simpler server code** - Remove parsing infrastructure from API
- **Faster UX** - No network round-trip for parsing; instant preview
- **Offline-capable** - Parsing works without network connectivity
- **Reduced server load** - CSV parsing CPU/memory moved to client

---

## Security Analysis

### Threats Addressed

| Threat | Severity | Before | After |
|--------|----------|--------|-------|
| Malicious executable disguised as CSV | High | Vulnerable | **Eliminated** |
| Path traversal via filename | Medium | Low risk (no disk write) | **Eliminated** |
| DoS via large/malformed file | Medium | 10MB limit helps | **Eliminated** (client bears cost) |
| CSV injection (`=`, `@`, `+`, `-` formulas) | Medium | Vulnerable | **Mitigated** (sanitize on client before display) |
| Memory exhaustion from huge rows | Low | Possible | **Mitigated** (browser limits) |
| Content-type spoofing | Low | No validation | **Eliminated** |

### Remaining Considerations

- **Data validation**: Server must still validate incoming transaction DTOs (amounts, dates, etc.)
- **Rate limiting**: Protect `/api/v1/import/execute` from abuse
- **Row limits**: Consider maximum transactions per import request
- **Input sanitization**: Server validates all string fields for length/content

---

## User Stories

### File Import Security

#### US-063-001: Client-Side CSV Parsing
**As a** user importing transactions  
**I want** my CSV file to be parsed locally in my browser  
**So that** my files never leave my device until I explicitly import

**Acceptance Criteria:**
- [ ] CSV parsing happens entirely in Blazor WebAssembly
- [ ] Preview displays immediately without network requests
- [ ] No file content is transmitted during parse/preview phases
- [ ] Only structured transaction data is sent on final import

#### US-063-002: Immediate Feedback
**As a** user importing transactions  
**I want** instant feedback when I select a file  
**So that** I can quickly verify I selected the correct file

**Acceptance Criteria:**
- [ ] Headers and preview rows display within 500ms of file selection
- [ ] Large files (up to 10MB) parse within 3 seconds
- [ ] Progress indicator shown for files taking >1 second

#### US-063-003: CSV Injection Protection
**As a** user viewing imported data  
**I want** potentially dangerous formula cells to be sanitized  
**So that** I'm protected from CSV injection attacks if I export data

**Acceptance Criteria:**
- [ ] Cells starting with `=`, `@`, `+`, `-`, `\t`, `\r` are prefixed with `'`
- [ ] Sanitization happens during parsing before display
- [ ] Original values preserved for transaction storage (descriptions, etc.)

---

## Technical Design

### Architecture Changes

```
BEFORE:
┌─────────────────┐     CSV File      ┌──────────────────┐
│  Blazor Client  │ ───────────────▶ │    API Server     │
│                 │ ◀─────────────── │  (Parse + Store)  │
│   Preview UI    │   Parsed Data    │                   │
└─────────────────┘                  └──────────────────┘

AFTER:
┌─────────────────────────────────┐     Transaction DTOs    ┌─────────────────┐
│         Blazor Client           │ ─────────────────────▶ │   API Server    │
│  ┌───────────┐  ┌────────────┐  │                        │  (Store only)   │
│  │ File API  │→│ CSV Parser │→ Preview UI               │                 │
│  └───────────┘  └────────────┘  │                        │                 │
└─────────────────────────────────┘                        └─────────────────┘
```

### Client-Side Components

#### New: `CsvParserService.cs` (Client)

```csharp
namespace BudgetExperiment.Client.Services;

/// <summary>
/// Client-side CSV parser for Blazor WebAssembly.
/// Parses files entirely in the browser without server round-trips.
/// </summary>
public interface ICsvParserService
{
    /// <summary>
    /// Parses a CSV file stream and returns structured data.
    /// </summary>
    Task<CsvParseResult> ParseAsync(Stream fileStream, string fileName, int rowsToSkip = 0);
}
```

#### Modified: `Import.razor`

- Remove call to `ImportApi.ParseCsvAsync()`
- Add injected `ICsvParserService` (client-side)
- Parse file locally using `IBrowserFile.OpenReadStream()`

### API Changes

#### Endpoints to Remove

| Method | Endpoint | Action |
|--------|----------|--------|
| ~~POST~~ | ~~`/api/v1/import/parse`~~ | **Remove** - No longer needed |

#### Endpoints to Keep (Modified)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/import/execute` | Execute import with validated transaction DTOs |
| GET | `/api/v1/import/mappings` | Get saved column mappings |
| POST | `/api/v1/import/mappings` | Save column mapping |
| PUT | `/api/v1/import/mappings/{id}` | Update column mapping |
| DELETE | `/api/v1/import/mappings/{id}` | Delete column mapping |

#### Request Validation Hardening

```csharp
public sealed class ImportExecuteRequestValidator
{
    public const int MaxTransactionsPerImport = 5000;
    public const int MaxDescriptionLength = 500;
    public const int MaxCategoryLength = 100;
    
    // Validate each transaction DTO before processing
}
```

### Code Migration

#### Move from Server to Client

| Component | From | To |
|-----------|------|-----|
| `CsvParserService.cs` | `BudgetExperiment.Application` | `BudgetExperiment.Client` |
| `CsvParseResult.cs` | `BudgetExperiment.Application` | `BudgetExperiment.Client.Models` |
| `ICsvParserService.cs` | `BudgetExperiment.Application` | `BudgetExperiment.Client.Services` |

#### Server-Side Cleanup

- Remove `ICsvParserService` and `CsvParserService` from Application layer
- Remove `ParseAsync` endpoint from `ImportController`
- Remove related unit tests (or migrate to client tests)

---

## Implementation Plan

### Phase 1: Client-Side CSV Parser

**Objective:** Port CSV parsing logic to Blazor WebAssembly client

**Tasks:**
- [ ] Create `BudgetExperiment.Client/Services/CsvParserService.cs`
- [ ] Create `BudgetExperiment.Client/Models/CsvParseResult.cs`
- [ ] Port parsing logic from `Application.Import.CsvParserService`
- [ ] Add CSV injection sanitization helper
- [ ] Write client-side unit tests (bUnit/xUnit)

**Commit:**
```bash
git commit -m "feat(client): add client-side CSV parser

- Port CsvParserService to Blazor WebAssembly
- Add CSV injection sanitization
- Client-side parsing eliminates file upload attack surface

Refs: #063"
```

---

### Phase 2: Update Import Page

**Objective:** Modify Import.razor to use client-side parsing

**Tasks:**
- [ ] Inject client-side `ICsvParserService`
- [ ] Remove `ImportApi.ParseCsvAsync()` calls
- [ ] Parse file directly from `IBrowserFile.OpenReadStream()`
- [ ] Update `HandleFileSelected` to use local parsing
- [ ] Add loading indicator for large files
- [ ] Update error handling for parse failures

**Commit:**
```bash
git commit -m "feat(import): switch to client-side CSV parsing

- Parse CSV files entirely in browser
- Remove server round-trip for parsing
- Instant preview with no network latency

Refs: #063"
```

---

### Phase 3: Harden Server-Side Import Execute

**Objective:** Strengthen validation on the import execution endpoint

**Tasks:**
- [ ] Add `MaxTransactionsPerImport` limit (5000)
- [ ] Validate all string field lengths
- [ ] Add rate limiting consideration
- [ ] Validate date ranges (not future-dated beyond reasonable limit)
- [ ] Validate amount ranges (reasonable bounds)
- [ ] Write integration tests for validation scenarios

**Commit:**
```bash
git commit -m "feat(api): harden import execute validation

- Add maximum transactions per import limit
- Validate field lengths and content
- Add date and amount range validation

Refs: #063"
```

---

### Phase 4: Remove Server-Side Parse Endpoint

**Objective:** Clean up deprecated server-side parsing code

**Tasks:**
- [ ] Remove `ParseAsync` endpoint from `ImportController`
- [ ] Remove `CsvParserService` from Application layer
- [ ] Remove `ICsvParserService` interface from Application
- [ ] Update `ImportApiService` (client) to remove `ParseCsvAsync`
- [ ] Remove/update related tests
- [ ] Update OpenAPI documentation

**Commit:**
```bash
git commit -m "refactor(api): remove deprecated parse endpoint

- Remove /api/v1/import/parse endpoint
- Remove server-side CsvParserService
- Parsing now handled entirely client-side

BREAKING CHANGE: /api/v1/import/parse endpoint removed

Refs: #063"
```

---

### Phase 5: Documentation & Cleanup

**Objective:** Final polish and documentation

**Tasks:**
- [ ] Update API documentation
- [ ] Add XML comments for new client services
- [ ] Document security improvements in README or SECURITY.md
- [ ] Remove any TODO comments
- [ ] Final code review

**Commit:**
```bash
git commit -m "docs: document secure file upload changes

- Document client-side parsing architecture
- Update security documentation
- Clean up code comments

Refs: #063"
```

---

## Testing Strategy

### Client-Side Tests

```csharp
public class CsvParserServiceTests
{
    [Fact]
    public async Task ParseAsync_ValidCsv_ReturnsHeaders()
    
    [Fact]
    public async Task ParseAsync_CsvInjection_SanitizesCells()
    
    [Fact]
    public async Task ParseAsync_EmptyFile_ReturnsError()
    
    [Fact]
    public async Task ParseAsync_SkipRows_SkipsMetadata()
}
```

### API Integration Tests

```csharp
public class ImportExecuteSecurityTests
{
    [Fact]
    public async Task Execute_TooManyTransactions_Returns400()
    
    [Fact]
    public async Task Execute_DescriptionTooLong_Returns400()
    
    [Fact]
    public async Task Execute_FutureDate_Returns400()
}
```

---

## Rollback Plan

If issues arise:
1. Re-enable server-side `ParseAsync` endpoint
2. Revert client to call `ImportApi.ParseCsvAsync()`
3. Keep both paths available temporarily with feature flag

---

## Success Metrics

- [ ] Zero file bytes transmitted during parse/preview
- [ ] Parse latency < 500ms for typical files (< 1MB)
- [ ] All existing import tests pass
- [ ] No regressions in import functionality
- [ ] Security scan shows no file upload vulnerabilities

---

## Appendix: CSV Injection Reference

Characters that trigger formula execution in spreadsheet applications:

| Character | Risk | Mitigation |
|-----------|------|------------|
| `=` | Formula execution | Prefix with `'` |
| `@` | Formula (Excel) | Prefix with `'` |
| `+` | Formula | Prefix with `'` |
| `-` | Formula | Prefix with `'` |
| `\t` | Tab injection | Prefix with `'` |
| `\r` | Carriage return injection | Prefix with `'` |

Sanitization example:
```csharp
public static string SanitizeForDisplay(string value)
{
    if (string.IsNullOrEmpty(value)) return value;
    
    char first = value[0];
    if (first is '=' or '@' or '+' or '-' or '\t' or '\r')
    {
        return "'" + value;
    }
    
    return value;
}
```
