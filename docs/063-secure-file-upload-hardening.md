# Feature 063: Secure File Upload Hardening
> **Status:** Planning

## Overview

Harden CSV import by moving all file parsing to the client (Blazor WebAssembly) and eliminating server-side file uploads. Each vertical slice below delivers a tested, deployable increment. Slices are ordered so that every commit leaves the app fully functional — no broken intermediate states.

## Problem Statement

### Current State

The import flow uploads raw CSV files to the server:

1. **Client**: User selects CSV → raw file bytes sent to `POST /api/v1/import/parse` as `multipart/form-data`
2. **Server**: `ImportController.ParseAsync` → `CsvParserService.ParseAsync` reads entire file into memory, parses, returns headers + rows
3. **Client**: User maps columns, calls `POST /api/v1/import/preview` with parsed rows + mappings
4. **Client**: User confirms, calls `POST /api/v1/import/execute` with structured transaction DTOs

**Current security posture:**
- ✅ File size limit (10 MB via `[RequestSizeLimit]`)
- ✅ Authentication required (`[Authorize]` on controller)
- ✅ No file persistence to disk
- ⚠️ No file extension validation
- ⚠️ No content-type verification
- ⚠️ No CSV injection sanitization
- ⚠️ No request body size limits on preview/execute endpoints
- ⚠️ No max-transactions-per-import limit
- ⚠️ Entire file loaded into memory as string (not streamed)

### Target State

```
┌─────────────────────────────────┐     Transaction DTOs    ┌─────────────────┐
│         Blazor Client           │ ─────────────────────▶ │   API Server    │
│  ┌───────────┐  ┌────────────┐  │                        │  (Validate +    │
│  │ File API  │→│ CSV Parser │→ Preview UI               │   Store only)   │
│  └───────────┘  └────────────┘  │                        │                 │
└─────────────────────────────────┘                        └─────────────────┘
```

**Benefits:**
- Eliminates file upload attack surface entirely — no file bytes reach the server
- Faster UX — instant preview with no network round-trip for parsing
- Simpler server — remove parsing infrastructure from API/Application layers
- Reduced server load — CSV parsing CPU/memory moved to client
- Offline-capable parsing — works without network connectivity

---

## Security Analysis

### Threats Addressed

| Threat | Severity | Before | After |
|--------|----------|--------|-------|
| Malicious executable disguised as CSV | High | Vulnerable | **Eliminated** |
| Path traversal via filename | Medium | Low risk (no disk write) | **Eliminated** |
| DoS via large/malformed file | Medium | 10 MB limit helps | **Eliminated** (client bears cost) |
| CSV injection (`=`, `@`, `+`, `-` formulas) | Medium | Vulnerable | **Mitigated** (Slice 2) |
| Memory exhaustion from huge rows | Low | Possible | **Mitigated** (browser limits) |
| Content-type spoofing | Low | No validation | **Eliminated** |
| DoS via oversized JSON body (preview/execute) | Medium | No limit | **Mitigated** (Slice 4) |
| Mass-insert abuse on execute | Medium | No row limit | **Mitigated** (Slice 4) |

---

## Vertical Slices

Each slice follows TDD (RED → GREEN → REFACTOR), crosses the layers needed, and results in a single deployable commit. Slices are independently valuable but designed to be done in order.

---

### Slice 1 — Client-Side CSV Parsing + Import Page Integration

> **Goal:** User selects a CSV file → preview appears instantly in the browser with zero server calls for parsing.

**Layers touched:** Client (service + UI)  
**Depends on:** Nothing  
**Risk:** Largest slice — ports core parsing logic and rewires the primary import flow

#### What to build

| Layer | File | Change |
|-------|------|--------|
| Client | `Services/ICsvParserService.cs` | New interface mirroring `Application.Import.ICsvParserService` |
| Client | `Services/CsvParserService.cs` | Port full parsing logic from `Application.Import.CsvParserService` (BOM removal, auto-delimiter detection, quote-aware splitting, skipRows) |
| Client | `Models/CsvParseResult.cs` | New client-side result model (headers, rows, delimiter, totalLines) |
| Client | `Pages/Import.razor` | Replace `ImportApi.ParseCsvAsync()` call with injected `ICsvParserService`. Read file via `IBrowserFile.OpenReadStream()` and parse locally. Add loading spinner for files taking >1 s. |
| Client | `Program.cs` | Register `ICsvParserService` → `CsvParserService` in DI |
| Client Tests | `CsvParserServiceTests.cs` | Port/adapt the ~28 existing tests from `Application.Tests/CsvParserServiceTests.cs` |

#### Acceptance criteria

- [ ] CSV parsing happens entirely in Blazor WebAssembly (no network call during parse/preview)
- [ ] Headers and preview rows display within 500 ms of file selection for typical files (< 1 MB)
- [ ] Large files (up to 10 MB) parse within 3 seconds
- [ ] Loading indicator shown for files taking > 1 second
- [ ] Auto-delimiter detection works for `,`, `;`, `\t`, `|`
- [ ] Quote-aware parsing handles escaped quotes, multi-line quoted fields
- [ ] BOM removal works for UTF-8 with BOM files
- [ ] `rowsToSkip` parameter skips metadata rows before header
- [ ] All ported parser tests pass in Client.Tests
- [ ] Existing server parse endpoint still available (not yet removed — backward compatibility)

#### TDD sequence

1. **RED:** Write `CsvParserServiceTests` — simple CSV → returns correct headers and rows
2. **GREEN:** Implement `CsvParserService.ParseAsync` with basic comma parsing
3. **RED:** Add tests for delimiter detection, quoted fields, BOM, skipRows, empty file, single-column
4. **GREEN:** Port remaining logic from `Application.Import.CsvParserService`
5. **REFACTOR:** Clean up, ensure parity with existing server-side parser behavior
6. **Integration:** Update `Import.razor`, register DI, manual smoke test

---

### Slice 2 — CSV Injection Sanitization

> **Goal:** Cells starting with formula-trigger characters are sanitized for display, protecting users if they export/copy data.

**Layers touched:** Client (service)  
**Depends on:** Slice 1

#### What to build

| Layer | File | Change |
|-------|------|--------|
| Client | `Services/CsvSanitizer.cs` | New static helper: `SanitizeForDisplay(string value)` — prefixes `=`, `@`, `+`, `-`, `\t`, `\r` with `'` |
| Client | `Services/CsvParserService.cs` | Apply `CsvSanitizer.SanitizeForDisplay` to each cell during parsing |
| Client Tests | `CsvSanitizerTests.cs` | Unit tests for each trigger character, null/empty passthrough, non-trigger passthrough |

#### Acceptance criteria

- [ ] Cells starting with `=`, `@`, `+`, `-`, `\t`, `\r` are prefixed with `'` in parsed output
- [ ] Sanitization happens during parsing before any display
- [ ] Null/empty values pass through unchanged
- [ ] Non-trigger values pass through unchanged
- [ ] Values starting with `-` followed by digits (negative amounts like `-42.50`) are sanitized for display column but original value preserved where needed for amount parsing

#### TDD sequence

1. **RED:** Write test — cell `=SUM(A1:A2)` → `'=SUM(A1:A2)`
2. **GREEN:** Implement `CsvSanitizer.SanitizeForDisplay`
3. **RED:** Write tests for each trigger char, null, empty, non-trigger
4. **GREEN:** Handle all cases
5. **RED:** Integration test — full CSV parse with injection cells → sanitized output
6. **GREEN:** Wire sanitizer into `CsvParserService`

---

### Slice 3 — Harden Import Execute Endpoint Validation

> **Goal:** Server rejects oversized, malformed, or abusive import execute requests with clear error responses.

**Layers touched:** API (controller), Application (service/validation), API Tests  
**Depends on:** Nothing (can be done in parallel with Slices 1–2)

#### What to build

| Layer | File | Change |
|-------|------|--------|
| Application | `Import/ImportValidationConstants.cs` | New constants class: `MaxTransactionsPerImport = 5000`, `MaxDescriptionLength = 500`, `MaxCategoryLength = 100`, `MaxFutureDateDays = 365`, `MaxAmountAbsoluteValue = 99_999_999.99m` |
| Application | `Import/ImportService.cs` | Add validation in `ExecuteAsync`: check transaction count, iterate DTOs to validate field lengths, date range, amount range. Return validation errors. |
| API | `Controllers/ImportController.cs` | Add `[RequestSizeLimit]` on execute and preview endpoints. Return 400/422 for validation failures with ProblemDetails. |
| API Tests | `ImportExecuteValidationTests.cs` | Integration tests using `WebApplicationFactory` for each validation rule |
| Application Tests | `ImportServiceValidationTests.cs` | Unit tests for validation logic |

#### Validation rules

| Field | Rule | Error code |
|-------|------|------------|
| `Transactions` collection | Max 5000 items | 400 — "Import exceeds maximum of 5000 transactions" |
| `Transactions` collection | At least 1 item | 400 — "No transactions to import" |
| `Description` | Max 500 chars | 422 — "Description exceeds 500 characters at row {n}" |
| `Category` | Max 100 chars | 422 — "Category exceeds 100 characters at row {n}" |
| `Date` | Not more than 365 days in the future | 422 — "Date is too far in the future at row {n}" |
| `Amount` | Absolute value ≤ 99,999,999.99 | 422 — "Amount out of range at row {n}" |
| Request body | Size limit (e.g., 5 MB) on execute; 10 MB on preview | 413 |

#### Acceptance criteria

- [ ] `POST /api/v1/import/execute` with > 5000 transactions → 400
- [ ] `POST /api/v1/import/execute` with 0 transactions → 400
- [ ] Description > 500 chars → 422 with row number
- [ ] Category > 100 chars → 422 with row number
- [ ] Date > 365 days in the future → 422 with row number
- [ ] Amount > 99,999,999.99 → 422 with row number
- [ ] Oversized request body → 413
- [ ] All error responses use ProblemDetails format with traceId
- [ ] Existing valid import flows unaffected

#### TDD sequence

1. **RED:** Write API test — execute with 5001 transactions → 400
2. **GREEN:** Add count check in `ImportService.ExecuteAsync` or controller
3. **RED:** Write API test — description too long → 422
4. **GREEN:** Add field validation
5. **RED:** Tests for date range, amount range, empty collection
6. **GREEN:** Implement remaining validation
7. **RED:** Test request body size limit → 413
8. **GREEN:** Add `[RequestSizeLimit]` attributes
9. **REFACTOR:** Extract constants, clean up error messages

---

### Slice 4 — Remove Server Parse Endpoint + Cleanup

> **Goal:** Eliminate the file upload attack surface by removing the deprecated `/api/v1/import/parse` endpoint and all server-side CSV parsing code.

**Layers touched:** API (controller), Application (service + interface), Client (API service), Tests  
**Depends on:** Slice 1 (client parsing must be in place)  
**Risk:** Breaking change — any external callers of `/api/v1/import/parse` will break

#### What to remove

| Layer | File | Change |
|-------|------|--------|
| API | `Controllers/ImportController.cs` | Remove `ParseAsync` action method, remove `CsvParseResultDto` inline class, remove `MaxFileSizeBytes` constant |
| Application | `Import/ICsvParserService.cs` | Delete file |
| Application | `Import/CsvParserService.cs` | Delete file |
| Application | DI registration | Remove `ICsvParserService` → `CsvParserService` registration |
| Client | `Services/IImportApiService.cs` | Remove `ParseCsvAsync` method |
| Client | `Services/ImportApiService.cs` | Remove `ParseCsvAsync` implementation |
| Application Tests | `CsvParserServiceTests.cs` | Delete file (logic now covered by Client.Tests) |
| API Tests | `ImportControllerTests.cs` | Remove tests for parse endpoint |

#### Acceptance criteria

- [ ] `POST /api/v1/import/parse` returns 404
- [ ] OpenAPI spec no longer lists the parse endpoint
- [ ] `ICsvParserService` and `CsvParserService` removed from Application layer
- [ ] `ParseCsvAsync` removed from `IImportApiService` / `ImportApiService`
- [ ] No compilation errors — no remaining references to removed types
- [ ] All remaining import tests pass (preview, execute, mappings, history)
- [ ] Import flow works end-to-end (file → client parse → preview → execute)

#### TDD sequence

1. **RED:** Write test that verifies `POST /api/v1/import/parse` returns 404 (will fail since endpoint still exists)
2. **GREEN:** Remove endpoint from controller
3. Remove server-side `CsvParserService` + interface + DI registration
4. Remove `ParseCsvAsync` from client API service
5. Delete/update tests referencing removed code
6. **VERIFY:** Full test suite green, manual smoke test of import flow

---

### Slice 5 — Request Body Size Limits on Preview Endpoint

> **Goal:** Protect the preview endpoint from DoS via oversized JSON payloads containing massive row arrays.

**Layers touched:** API (controller), API Tests  
**Depends on:** Nothing (can be done anytime)

#### What to build

| Layer | File | Change |
|-------|------|--------|
| API | `Controllers/ImportController.cs` | Add `[RequestSizeLimit(10 * 1024 * 1024)]` on `PreviewAsync` |
| API | `Controllers/ImportController.cs` | Add max-rows validation on `ImportPreviewRequest.Rows` (e.g., 10,000 rows) |
| API Tests | `ImportPreviewValidationTests.cs` | Integration test: oversized body → 413; too many rows → 400 |

#### Acceptance criteria

- [ ] `POST /api/v1/import/preview` with body > 10 MB → 413
- [ ] `POST /api/v1/import/preview` with > 10,000 rows → 400
- [ ] Normal preview requests (< 5000 rows) unaffected
- [ ] Error responses use ProblemDetails format

#### TDD sequence

1. **RED:** Write test — preview with 10,001 rows → 400
2. **GREEN:** Add row count validation
3. **RED:** Write test — oversized body → 413
4. **GREEN:** Add `[RequestSizeLimit]`

---

### Slice 6 — Documentation & Cleanup

> **Goal:** Document the new architecture, update API docs, final polish.

**Layers touched:** Docs, code comments  
**Depends on:** All previous slices

#### Tasks

- [ ] Add XML doc comments on all new public types in Client project
- [ ] Update OpenAPI operation descriptions for remaining import endpoints
- [ ] Remove any `// TODO` comments related to parse endpoint
- [ ] Verify `CHANGELOG.md` entries for breaking change
- [ ] Final code review pass — check for dead code, unused usings

---

## Slice Dependency Graph

```
Slice 1 (Client parser + UI)
    │
    ▼
Slice 2 (CSV injection sanitization)
    │
    ▼
Slice 4 (Remove server parse endpoint)
    │
    ▼
Slice 6 (Docs & cleanup)

Slice 3 (Harden execute validation) ──── independent ────▶ Slice 6
Slice 5 (Preview body limits)       ──── independent ────▶ Slice 6
```

Slices 3 and 5 have no dependencies on Slices 1–2 and can be done in parallel.

---

## Testing Strategy Summary

| Slice | Test type | Test location | Key scenarios |
|-------|-----------|---------------|---------------|
| 1 | Unit | `Client.Tests/CsvParserServiceTests.cs` | Valid CSV, empty file, delimiters, quotes, BOM, skipRows, single-column |
| 2 | Unit | `Client.Tests/CsvSanitizerTests.cs` | Each trigger char, null/empty, non-trigger, integration with parser |
| 3 | Unit + Integration | `Application.Tests/ImportServiceValidationTests.cs`, `Api.Tests/ImportExecuteValidationTests.cs` | Max transactions, field lengths, date/amount range, body size |
| 4 | Integration | `Api.Tests/ImportControllerTests.cs` | Parse endpoint returns 404, full import flow still works |
| 5 | Integration | `Api.Tests/ImportPreviewValidationTests.cs` | Row count limit, body size limit |

---

## Rollback Plan

Each slice is independently revertable:
- **Slices 1–2:** Revert client parser; `Import.razor` falls back to calling `ImportApi.ParseCsvAsync()` (server endpoint still exists until Slice 4)
- **Slice 3:** Revert validation rules; existing imports resume without limits
- **Slice 4:** Re-add server parse endpoint + `CsvParserService` from git history
- **Slice 5:** Remove size/row limits from preview endpoint

If Slice 4 is deployed and needs rollback, restore the server parse endpoint and temporarily add `ParseCsvAsync` back to the client API service.

---

## Success Metrics

- [ ] Zero file bytes transmitted during parse/preview (verified via browser DevTools network tab)
- [ ] Parse latency < 500 ms for typical files (< 1 MB)
- [ ] All existing import tests pass after each slice
- [ ] No regressions in import functionality (E2E tests green)
- [ ] Server parse endpoint fully removed and returning 404
- [ ] Execute and preview endpoints reject oversized/malformed requests

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
