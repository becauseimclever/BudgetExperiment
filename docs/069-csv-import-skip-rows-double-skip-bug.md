# Bug Fix 069: CSV Import Skip Rows Double-Skip Bug
> **Status:** ✅ Done  
> **Priority:** High  
> **Estimated Effort:** Tiny (< 1 day)  
> **Dependencies:** None

## Overview

Fix a bug where setting "rows to skip" during CSV import caused the first N data rows to be silently discarded, in addition to correctly skipping the N metadata/header lines.

## Problem Statement

### Root Cause

The `RowsToSkip` value was applied **twice** during the import flow:

1. **`CsvParserService.ParseAsync`** — correctly skips N metadata lines before the header row, then returns only parsed data rows (header excluded, metadata excluded).
2. **`ImportService.PreviewAsync`** — receives the already-trimmed data rows but redundantly skips N rows **again**, discarding actual transaction data.

### Symptom

When a user set `RowsToSkip = 6` to skip bank metadata at the top of a CSV file, the parser correctly removed the 6 metadata lines and treated the next line as the header. However, the preview service then skipped 6 more rows from the remaining data, causing the first 6 real transactions to disappear from the preview and import.

### Impact

- Users importing CSV files with metadata headers (common with Bank of America, Capital One, and similar bank exports) lost the first N transaction rows silently.
- The bug was proportional to the skip count — higher skip values caused more data loss.
- No error or warning was shown; transactions simply did not appear.

---

## Fix

### Changed Files

| File | Change |
|------|--------|
| `src/BudgetExperiment.Application/Import/ImportService.cs` | Removed redundant skip logic in `PreviewAsync`. `RowsToSkip` is now only used for display row-index offset calculation. |
| `tests/BudgetExperiment.Application.Tests/Services/ImportServiceTests.cs` | Added regression test `PreviewAsync_WithRowsToSkip_DoesNotDoubleSkipAlreadyParsedRows`; updated two existing tests that were asserting the buggy behavior. |

### Technical Detail

In `ImportService.PreviewAsync`, the following block was removed:

```csharp
// REMOVED — this was the double-skip bug
var skippedRows = new List<IReadOnlyList<string>>();
if (request.RowsToSkip > 0 && request.RowsToSkip < request.Rows.Count)
{
    skippedRows = request.Rows.Take(request.RowsToSkip).ToList();
    rowsToProcess = request.Rows.Skip(request.RowsToSkip).ToList();
}
else if (request.RowsToSkip >= request.Rows.Count)
{
    return new ImportPreviewResult();
}
```

The `RowsToSkip` value is still preserved on the request and used for row-index display offset (so row numbers shown to users correctly account for skipped metadata + header row):

```csharp
var previewRow = ProcessRow(
    request.RowsToSkip + i + 1, // Row index accounts for skipped rows + 1-based display
    ...);
```

### Data Flow (Corrected)

```
CSV File (with 6 metadata lines + header + data)
  │
  ▼
CsvParserService.ParseAsync(rowsToSkip: 6)
  → Skips 6 metadata lines
  → Treats line 7 as header
  → Returns data rows (lines 8+)
  │
  ▼
ImportService.PreviewAsync(rows: [data only], rowsToSkip: 6)
  → Processes ALL rows (no second skip)
  → Uses rowsToSkip only for row index offset display
```

---

## Testing

### TDD Approach

1. **RED:** Added `PreviewAsync_WithRowsToSkip_DoesNotDoubleSkipAlreadyParsedRows` — 7 data rows with `RowsToSkip = 6`, asserted all 7 rows processed. Failed with `Expected: 7, Actual: 1`.
2. **GREEN:** Removed redundant skip logic in `ImportService.PreviewAsync`. Test passed.
3. **REFACTOR:** Updated two existing tests (`PreviewAsync_WithSkipRows_SkipsFirstNRows` → `PreviewAsync_WithSkipRows_ProcessesAllRowsAndOffsetsRowIndex`, `PreviewAsync_WithSkipRowsExceedingTotal_ReturnsEmptyResult` → `PreviewAsync_WithHighSkipRows_StillProcessesAllDataRows`) that were asserting the buggy behavior.

### Test Coverage

- 45 ImportService tests passing
- 34 CsvParserService tests passing (unchanged, already correct)
