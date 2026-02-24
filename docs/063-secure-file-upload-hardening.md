# Feature 063: Secure File Upload Hardening
> **Status:** Complete — All 6 slices delivered

## Overview

Harden CSV import by moving all file parsing to the client (Blazor WebAssembly) and eliminating server-side file uploads. Each vertical slice below delivers a tested, deployable increment. Slices are ordered so that every commit leaves the app fully functional — no broken intermediate states.

## Problem Statement

### Current State (Post Slices 1–4)

The import flow now parses CSV entirely in the browser:

1. **Client**: User selects CSV → `CsvParserService` parses locally in Blazor WASM (no server call)
2. **Client**: `CsvSanitizer` sanitizes formula-trigger characters during parsing
3. **Client**: User maps columns, calls `POST /api/v1/import/preview` with parsed rows + mappings
4. **Client**: User confirms, calls `POST /api/v1/import/execute` with structured transaction DTOs
5. **Server**: Validates execute request (transaction count, field lengths, date/amount range) → 400/422 on failure

**Current security posture:**
- ✅ No file upload — CSV parsed entirely in browser (Slice 1)
- ✅ CSV injection sanitized for display (Slice 2)
- ✅ Authentication required (`[Authorize]` on controller)
- ✅ Execute endpoint validated: max 5000 transactions, field length limits, date/amount range (Slice 3)
- ✅ `[RequestSizeLimit]` on execute (5 MB) and preview (10 MB) endpoints (Slice 3)
- ✅ Server parse endpoint removed — no file bytes reach server (Slice 4)
- ✅ Server-side `CsvParserService` and `ICsvParserService` deleted (Slice 4)
- ⚠️ No max-rows validation on preview endpoint (Slice 5 — remaining)

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
| DoS via oversized JSON body (preview/execute) | Medium | No limit | **Mitigated** (Slice 3 — `[RequestSizeLimit]`) |
| Mass-insert abuse on execute | Medium | No row limit | **Mitigated** (Slice 3 — max 5000 transactions) |

---

## Vertical Slices

Each slice follows TDD (RED → GREEN → REFACTOR), crosses the layers needed, and results in a single deployable commit. Slices are independently valuable but designed to be done in order.

---

### Slice 1 — Client-Side CSV Parsing + Import Page Integration ✅

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

- [x] CSV parsing happens entirely in Blazor WebAssembly (no network call during parse/preview)
- [x] Headers and preview rows display within 500 ms of file selection for typical files (< 1 MB)
- [x] Large files (up to 10 MB) parse within 3 seconds
- [x] Loading indicator shown for files taking > 1 second
- [x] Auto-delimiter detection works for `,`, `;`, `\t`, `|`
- [x] Quote-aware parsing handles escaped quotes, multi-line quoted fields
- [x] BOM removal works for UTF-8 with BOM files
- [x] `rowsToSkip` parameter skips metadata rows before header
- [x] All ported parser tests pass in Client.Tests (34 tests)
- [x] Server parse endpoint removed in Slice 4

#### TDD sequence

1. **RED:** Write `CsvParserServiceTests` — simple CSV → returns correct headers and rows
2. **GREEN:** Implement `CsvParserService.ParseAsync` with basic comma parsing
3. **RED:** Add tests for delimiter detection, quoted fields, BOM, skipRows, empty file, single-column
4. **GREEN:** Port remaining logic from `Application.Import.CsvParserService`
5. **REFACTOR:** Clean up, ensure parity with existing server-side parser behavior
6. **Integration:** Update `Import.razor`, register DI, manual smoke test

---

### Slice 2 — CSV Injection Sanitization ✅

> **Goal:** Cells starting with formula-trigger characters are sanitized for display, protecting users if they export/copy data.

**Layers touched:** Client (service)  
**Depends on:** Slice 1

#### What to build

| Layer | File | Change |
|-------|------|--------|
| Client | `Services/CsvSanitizer.cs` | New static helper: `SanitizeForDisplay(string value)` — prefixes `=`, `@`, `+`, `-`, `\t`, `\r` with `'`; `UnsanitizeForParsing(string value)` — strips prefix for downstream parsing |
| Client | `Services/CsvParserService.cs` | Apply `CsvSanitizer.SanitizeForDisplay` to each cell during parsing |
| Client Tests | `CsvSanitizerTests.cs` | Unit tests for each trigger character, null/empty passthrough, non-trigger passthrough |

#### Acceptance criteria

- [x] Cells starting with `=`, `@`, `+`, `-`, `\t`, `\r` are prefixed with `'` in parsed output
- [x] Sanitization happens during parsing before any display
- [x] Null/empty values pass through unchanged
- [x] Non-trigger values pass through unchanged
- [x] Values starting with `-` followed by digits (negative amounts like `-42.50`) are sanitized for display; `UnsanitizeForParsing()` recovers original value for amount parsing

#### TDD sequence

1. **RED:** Write test — cell `=SUM(A1:A2)` → `'=SUM(A1:A2)`
2. **GREEN:** Implement `CsvSanitizer.SanitizeForDisplay`
3. **RED:** Write tests for each trigger char, null, empty, non-trigger
4. **GREEN:** Handle all cases
5. **RED:** Integration test — full CSV parse with injection cells → sanitized output
6. **GREEN:** Wire sanitizer into `CsvParserService`

---

### Slice 3 — Harden Import Execute Endpoint Validation ✅

> **Goal:** Server rejects oversized, malformed, or abusive import execute requests with clear error responses.

**Layers touched:** API (controller), Application (service/validation), API Tests  
**Depends on:** Nothing (can be done in parallel with Slices 1–2)

#### What to build

| Layer | File | Change |
|-------|------|--------|
| Application | `Import/ImportValidationConstants.cs` | New constants class: `MaxTransactionsPerImport = 5000`, `MaxDescriptionLength = 500`, `MaxCategoryLength = 100`, `MaxFutureDateDays = 365`, `MaxAmountAbsoluteValue = 99_999_999.99m` |
| Application | `Import/ImportExecuteRequestValidator.cs` | New static validator: `Validate(ImportExecuteRequest)` returns `ImportValidationResult` with `IsBadRequest` flag for 400 vs 422 distinction |
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

- [x] `POST /api/v1/import/execute` with > 5000 transactions → 400
- [x] `POST /api/v1/import/execute` with 0 transactions → 400
- [x] Description > 500 chars → 422 with row number
- [ ] Category > 100 chars → 422 with row number (validated in constants; not yet tested at API level)
- [x] Date > 365 days in the future → 422 with row number
- [x] Amount > 99,999,999.99 → 422 with row number
- [x] `[RequestSizeLimit]` on execute (5 MB) and preview (10 MB) endpoints
- [x] All error responses use ProblemDetails format with traceId
- [x] Existing valid import flows unaffected

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

### Slice 4 — Remove Server Parse Endpoint + Cleanup ✅

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

- [x] `POST /api/v1/import/parse` returns 404/405 (endpoint removed)
- [x] OpenAPI spec no longer lists the parse endpoint
- [x] `ICsvParserService` and `CsvParserService` deleted from Application layer
- [x] `ParseCsvAsync` removed from `IImportApiService` / `ImportApiService`
- [x] `CsvParseResultDto` removed from controller
- [x] DI registration removed from `DependencyInjection.cs`
- [x] `Application.Tests/CsvParserServiceTests.cs` deleted (logic covered by Client.Tests)
- [x] `FullImportFlow_Success` test rewritten without parse step
- [x] No compilation errors — 0 warnings, 0 errors
- [x] All remaining import tests pass (preview, execute, mappings, history)

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
| API | `Controllers/ImportController.cs` | `[RequestSizeLimit]` already added in Slice 3. Add max-rows validation on `ImportPreviewRequest.Rows` (10,000 rows) |
| API Tests | `ImportPreviewValidationTests.cs` | Integration test: oversized body → 413; too many rows → 400 |

#### Acceptance criteria

- [x] `POST /api/v1/import/preview` with body > 10 MB → 413 (done in Slice 3)
- [x] `POST /api/v1/import/preview` with > 10,000 rows → 400
- [x] Normal preview requests (< 5000 rows) unaffected
- [x] Error responses use ProblemDetails format

#### TDD sequence

1. **RED:** Write test — preview with 10,001 rows → 400
2. **GREEN:** Add row count validation in controller
3. ~~**RED:** Write test — oversized body → 413~~ (already done in Slice 3)
4. ~~**GREEN:** Add `[RequestSizeLimit]`~~ (already done in Slice 3)

---

### Slice 6 — Documentation & Cleanup ✅

> **Goal:** Document the new architecture, update API docs, final polish.

**Layers touched:** Docs, code comments  
**Depends on:** All previous slices

#### Tasks

- [x] Add XML doc comments on all new public types in Client project
- [x] Update OpenAPI operation descriptions for remaining import endpoints
- [x] Remove any `// TODO` comments related to parse endpoint (none found)
- [x] Verify `CHANGELOG.md` entries for breaking change
- [x] Final code review pass — check for dead code, unused usings

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
| 1 | Unit | `Client.Tests/Services/CsvParserServiceTests.cs` (34 tests) | Valid CSV, empty file, delimiters, quotes, BOM, skipRows, single-column |
| 2 | Unit | `Client.Tests/Services/CsvSanitizerTests.cs` (33 tests) | Each trigger char, null/empty, non-trigger, `UnsanitizeForParsing` roundtrip, integration with parser |
| 3 | Unit + Integration | `Application.Tests/Services/ImportServiceValidationTests.cs` (16 tests), `Api.Tests/ImportExecuteValidationTests.cs` (7 tests) | Max transactions, field lengths, date/amount range, ProblemDetails with traceId |
| 4 | Integration | `Api.Tests/ImportControllerTests.cs` (13 tests) | Parse endpoint returns 404/405, full import flow without parse step |
| 5 | Integration | `Api.Tests/ImportPreviewValidationTests.cs` (4 tests) | Row count limit (10,000 max), boundary test (exact max → 200), normal flow, traceId in ProblemDetails |

---

## Rollback Plan

Each slice is independently revertable:
- **Slices 1–2:** Revert client parser; would need to restore server parse endpoint from git history (Slice 4 removed it)
- **Slice 3:** Revert validation rules; existing imports resume without limits
- **Slice 4:** Re-add server parse endpoint + `CsvParserService` + DI registration + `ParseCsvAsync` from git history
- **Slice 5:** Remove row-count limits from preview endpoint

Since Slice 4 has been deployed, rolling back Slices 1–2 requires also reverting Slice 4 to restore the server parse endpoint.

---

## Success Metrics

- [x] Zero file bytes transmitted during parse/preview (client-side parsing in place)
- [x] Parse latency < 500 ms for typical files (< 1 MB)
- [x] All existing import tests pass after each slice
- [x] No regressions in import functionality (E2E tests green)
- [x] Server parse endpoint fully removed and returning 404/405
- [x] Execute endpoint rejects oversized/malformed requests (Slice 3)
- [x] Preview endpoint rejects oversized row arrays (Slice 5)

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
