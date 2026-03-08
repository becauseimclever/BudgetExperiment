# Feature 093: Fix CSV Import Negative Amount Parsing
> **Status:** Done

## Overview

CSV import fails to parse negative amounts (e.g., `-10.05`) due to the CSV sanitization pipeline not being properly integrated with the import parsing pipeline. Users see `Could not parse amount: ''-10.05'` errors during import preview, preventing valid transactions with negative values from being imported.

## Problem Statement

### Current State

The CSV import pipeline has two safety mechanisms that conflict:

1. **CsvSanitizer.SanitizeForDisplay** ([CsvSanitizer.cs](../src/BudgetExperiment.Client/Services/CsvSanitizer.cs)) — Prefixes cell values starting with formula-trigger characters (`=`, `@`, `+`, `-`, `\t`, `\r`) with a single quote `'` to prevent spreadsheet formula injection. This converts `-10.05` → `'-10.05`.

2. **CsvSanitizer.UnsanitizeForParsing** ([CsvSanitizer.cs](../src/BudgetExperiment.Client/Services/CsvSanitizer.cs)) — Designed to strip the sanitization prefix before numeric/date parsing. This method exists and is tested, but is **never called in production code**.

**Data flow showing the bug:**

```
CSV File: "-10.05"
    ↓ CsvParserService.ParseCsvLine()
    ↓ CsvSanitizer.SanitizeForDisplay("-10.05") → "'-10.05"
    ↓ Stored in CsvParseResult.Rows (sanitized)
    ↓ Displayed in CsvPreviewTable (safe for display ✓)
    ↓ Import.razor sends sanitized rows to API  ← BUG: no unsanitization
    ↓ ImportRowProcessor.ParseAmountValue("'-10.05")
    ↓ decimal.TryParse fails on leading apostrophe
    → Error: "Could not parse amount: ''-10.05'"
```

### Target State

Negative amounts (and values starting with `+`) in CSV files are correctly parsed during import. The sanitization remains in place for safe display, but values are unsanitized before being sent to the backend for processing.

---

## User Stories

### CSV Import with Negative Values

#### US-093-001: Import CSV with Negative Expense Amounts
**As a** budget user  
**I want to** import a CSV file containing negative amounts (e.g., `-10.05`)  
**So that** expenses are correctly recorded without parsing errors

**Acceptance Criteria:**
- [x] Negative amounts like `-10.05` are parsed correctly during import preview
- [x] No "Could not parse amount" error for valid negative values
- [x] Transactions are created with the correct negative amount
- [x] Formula injection protection (sanitization for display) is preserved

#### US-093-002: Import CSV with Positive-Prefixed Amounts
**As a** budget user  
**I want to** import a CSV file containing amounts prefixed with `+` (e.g., `+250.00`)  
**So that** income values are correctly recorded

**Acceptance Criteria:**
- [x] Positive-prefixed amounts like `+250.00` are parsed correctly
- [x] No parsing errors for `+`-prefixed values

---

## Technical Design

### Root Cause Analysis

| Step | Location | What Happens |
|------|----------|--------------|
| 1 | [CsvParserService.cs:238,251](../src/BudgetExperiment.Client/Services/CsvParserService.cs) | `SanitizeForDisplay()` prefixes `-` and `+` values with `'` |
| 2 | [Import.razor:651](../src/BudgetExperiment.Client/Pages/Import.razor) | Sanitized rows sent directly to API (no unsanitization) |
| 3 | [ImportRowProcessor.cs:189](../src/BudgetExperiment.Application/Import/ImportRowProcessor.cs) | `ParseAmountValue()` receives `'` -prefixed string, fails to parse |

### Fix Options

**Option A (Recommended): Unsanitize on the client before sending to API**

The client already has `CsvSanitizer.UnsanitizeForParsing()`. Call it on amount (and potentially date) values before submitting the import request. This keeps the backend clean and the sanitization concern fully within the client layer.

- Unsanitize row values in `Import.razor` when building the `ImportPreviewRequest`
- Display continues to use sanitized values for safety
- Backend `ParseAmountValue` remains unchanged

**Option B: Unsanitize on the server in ParseAmountValue**

Add unsanitization logic to `ParseAmountValue()` in the Application layer. This is a defense-in-depth approach but leaks the client's sanitization concern into the backend.

**Option C: Both (belt and suspenders)**

Unsanitize on the client before sending AND handle the leading apostrophe in `ParseAmountValue` as a fallback. Provides maximum resilience.

### Recommended Approach: Option A

The sanitization is a client-side display concern. The client should unsanitize values before submitting them for processing. The `UnsanitizeForParsing` method already exists and is tested — it just needs to be wired into the import submission flow.

### Key Files to Modify

| File | Change |
|------|--------|
| `src/BudgetExperiment.Client/Pages/Import.razor` | Unsanitize row cell values before building `ImportPreviewRequest` |
| `tests/BudgetExperiment.Application.Tests/Import/ImportRowProcessorTests.cs` | Add test for apostrophe-prefixed negative amounts (regression) |
| `tests/BudgetExperiment.Client.Tests/Services/CsvParserServiceTests.cs` | Verify end-to-end: sanitized values unsanitized before import |

---

## Implementation Plan

### Phase 1: Add Failing Tests (RED)

**Objective:** Write tests that demonstrate the bug and define expected behavior.

**Tasks:**
- [ ] Add `ImportRowProcessorTests` test: `ParseAmountValue` with `'-10.05` input currently returns null (documents the bug)
- [ ] Add client-side test: verify import submission unsanitizes values before sending to API
- [ ] Add test for `+`-prefixed amounts (`'+250.00`) to cover the same class of bug

**Commit:**
```bash
git add .
git commit -m "test(import): add failing tests for negative amount parsing bug

- ImportRowProcessor returns null for sanitized negative amounts
- Verify unsanitization required before import submission

Refs: #093"
```

---

### Phase 2: Fix Client-Side Unsanitization (GREEN)

**Objective:** Wire `CsvSanitizer.UnsanitizeForParsing` into the import submission path so values are cleaned before being sent to the API.

**Tasks:**
- [ ] In `Import.razor`, unsanitize cell values when building `ImportPreviewRequest.Rows`
- [ ] Ensure display preview still shows sanitized values (no regression)
- [ ] Verify all Phase 1 tests pass

**Commit:**
```bash
git add .
git commit -m "fix(import): unsanitize CSV values before sending to API

- Call CsvSanitizer.UnsanitizeForParsing on row values before import
- Fixes negative amount parsing error for values like -10.05
- Display preview retains sanitized values for formula injection safety

Refs: #093"
```

---

### Phase 3: Server-Side Defensive Parsing (Optional Hardening)

**Objective:** Add resilience to `ParseAmountValue` to handle leading apostrophes as a defense-in-depth measure, in case sanitized values ever reach the backend from another source.

**Tasks:**
- [ ] Strip leading apostrophe in `ParseAmountValue` when followed by a trigger character
- [ ] Update existing tests to verify this fallback behavior
- [ ] Evaluate if this is worth the added complexity (may skip if Option A is sufficient)

**Commit:**
```bash
git add .
git commit -m "fix(import): add defensive apostrophe handling in ParseAmountValue

- Strip leading apostrophe followed by trigger chars as fallback
- Defense-in-depth for sanitized values reaching backend

Refs: #093"
```

---

### Phase 4: Cleanup & Documentation

**Objective:** Final polish and verification.

**Tasks:**
- [ ] Run full test suite to confirm no regressions
- [ ] Verify with sample CSV files containing negative amounts (`boa.csv`, `capone.csv`, `uhcu.csv`)
- [ ] Update this document status to `Done`
- [ ] Move document to `docs/archive/`

**Commit:**
```bash
git add .
git commit -m "docs(import): complete feature 093 - negative amount parsing fix

- Update feature doc status
- Archive completed feature document

Refs: #093"
```

---

## Testing Checklist

- [x] Unit: `ParseAmountValue("'-10.05")` → `-10.05m` (after fix)
- [x] Unit: `ParseAmountValue("'+250.00")` → `250.00m` (after fix)
- [x] Unit: Import submission unsanitizes values
- [x] Unit: Display preview retains sanitized values
- [x] Integration: End-to-end CSV import with negative amounts succeeds
- [x] Regression: Formula injection protection still works (sanitized display values)
- [x] Regression: Existing CSV import tests still pass
