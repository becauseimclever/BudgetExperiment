# Feature 033: CSV Import Skip Header Rows Bug Fix
> **Status:** ✅ Complete

> **Status:** ✅ Complete  
> **Type:** Bug Fix  
> **Severity:** High  
> **Started:** 2026-01-22  
> **Completed:** 2026-01-23

## Overview

Fix the CSV import functionality to properly support skipping metadata rows that appear before the actual header row in bank CSV exports. Currently, the "Skip Rows" feature only skips data rows after the header is already parsed, which doesn't help with CSVs that have metadata rows before the actual column headers.

## Problem Statement

### Current Behavior (Bug)

When importing a CSV file that has metadata rows before the actual header row (like Bank of America exports), the system:

1. **Parses the wrong header row**: The CSV parser uses the first row as the header, which may be metadata (e.g., "Description,,Summary Amt.") instead of the actual data headers (e.g., "Date,Description,Amount,Running Bal.")
2. **Detects wrong column count**: The metadata header row may have a different number of columns than the actual data
3. **Displays wrong sample values**: The mapping screen shows metadata as sample data values
4. **RowsToSkip doesn't fix the issue**: The current "Skip Rows After Header" setting only skips data rows during the preview step, but the header has already been incorrectly determined

### Example: Bank of America CSV Structure

```csv
Description,,Summary Amt.                      <- Row 1: METADATA HEADER (currently used as header)
Beginning balance as of 10/01/2025,,"357.05"  <- Row 2: Metadata
Total credits,,"4,528.07"                     <- Row 3: Metadata
Total debits,,"-4,882.79"                     <- Row 4: Metadata  
Ending balance as of 11/14/2025,,"2.33"       <- Row 5: Metadata
                                               <- Row 6: Empty line
Date,Description,Amount,Running Bal.          <- Row 7: ACTUAL HEADER (should be used)
10/01/2025,Beginning balance as of 10/01/2025,,"357.05" <- Row 8+: Actual data
...
```

### Expected Behavior (After Fix)

1. User uploads CSV - initial parse shows raw data with first row as header (unchanged)
2. User notices headers are wrong and can adjust "Rows to Skip Before Header" setting in mapping step
3. When user changes skip rows, the client re-parses the CSV with the correct header row
4. The parse endpoint accepts an optional `rowsToSkip` parameter
5. Column mappings, sample values, and preview all reflect the correct structure
6. Saved mappings include the skip rows setting for future imports of the same format

### Root Cause Analysis

The `ICsvParserService.ParseAsync()` method does not accept a `rowsToSkip` parameter. The skip rows setting is currently only applied:

1. During `ImportService.PreviewAsync()` - only skips data rows, not the header
2. The UI label says "Skip Rows After Header" which is misleading
3. There is no way to re-parse the CSV with a different header row

**Affected Files:**
- `CsvParserService.cs` - No `rowsToSkip` parameter
- `ICsvParserService.cs` - Interface lacks overload
- `ImportController.cs` - Parse endpoint doesn't accept skip rows
- `ImportApiService.cs` - Client doesn't support re-parsing
- `Import.razor` - Mapping step doesn't trigger re-parse when skip rows changes

---

## User Stories

### US-033-001: Parse CSV with Skip Rows
**As a** user  
**I want to** specify how many rows to skip before the header when parsing a CSV  
**So that** I can correctly import files that have metadata rows at the beginning

**Acceptance Criteria:**
- [x] Parse endpoint accepts optional `rowsToSkip` query parameter
- [x] Parser skips specified number of rows before treating next row as header
- [x] Remaining rows after header are parsed as data rows
- [x] Returns correct column count based on actual header row

### US-033-002: Re-parse on Skip Rows Change
**As a** user  
**I want** the CSV to be re-parsed when I change the "Skip Rows" setting in the mapping step  
**So that** I can see the correct columns and sample values

**Acceptance Criteria:**
- [x] Changing skip rows value triggers re-parse via API
- [x] Column mappings reset to auto-detected values for new headers
- [x] Sample values update to show actual transaction data
- [x] Preview reflects the new parse structure
- [x] Loading indicator shown during re-parse

### US-033-003: Rename Skip Rows Label
**As a** user  
**I want** the skip rows setting to be clearly labeled  
**So that** I understand it skips rows before the header, not after

**Acceptance Criteria:**
- [x] Label changed from "Skip Rows After Header" to "Rows Before Header" or "Skip Metadata Rows"
- [x] Helper text updated to explain: "Number of rows to skip before the column headers"

### US-033-004: Validate Skip Rows Doesn't Exceed Rows
**As a** user  
**I want** meaningful feedback if I set skip rows too high  
**So that** I don't accidentally skip all data

**Acceptance Criteria:**
- [x] Warning shown if skip rows >= total rows - 1 (need at least header + 1 data row)
- [x] Prevent re-parse if all rows would be skipped
- [x] Show remaining row count after skip

---

## Technical Design

### 1. API Changes

#### Update Parse Endpoint

```csharp
// ImportController.cs
[HttpPost("parse")]
[ProducesResponseType<CsvParseResultDto>(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[RequestSizeLimit(MaxFileSizeBytes)]
public async Task<IActionResult> ParseAsync(
    IFormFile file,
    [FromQuery] int rowsToSkip = 0,  // NEW PARAMETER
    CancellationToken cancellationToken)
{
    // Validate rowsToSkip
    if (rowsToSkip < 0 || rowsToSkip > SkipRowsSettings.MaxSkipRows)
    {
        return this.BadRequest(new ProblemDetails
        {
            Title = "Invalid Skip Rows",
            Detail = $"Rows to skip must be between 0 and {SkipRowsSettings.MaxSkipRows}.",
            Status = StatusCodes.Status400BadRequest,
        });
    }

    // ... existing file validation ...

    using var stream = file.OpenReadStream();
    var result = await this._csvParserService.ParseAsync(stream, file.FileName, rowsToSkip, cancellationToken);
    
    // ... rest of method ...
}
```

### 2. Application Layer Changes

#### Update ICsvParserService Interface

```csharp
// ICsvParserService.cs
public interface ICsvParserService
{
    /// <summary>
    /// Parses a CSV file and returns the raw data with detected settings.
    /// </summary>
    /// <param name="fileStream">The file stream to parse.</param>
    /// <param name="fileName">The name of the file (for error messages).</param>
    /// <param name="rowsToSkip">Number of rows to skip before the header row (default 0).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parse result containing headers and rows.</returns>
    Task<CsvParseResult> ParseAsync(
        Stream fileStream, 
        string fileName, 
        int rowsToSkip = 0,  // NEW PARAMETER
        CancellationToken ct = default);
}
```

#### Update CsvParserService Implementation

```csharp
// CsvParserService.cs
public async Task<CsvParseResult> ParseAsync(
    Stream fileStream, 
    string fileName, 
    int rowsToSkip = 0,
    CancellationToken ct = default)
{
    // ... existing content reading code ...

    var lines = SplitCsvLines(content);
    if (lines.Count == 0)
    {
        return CsvParseResult.CreateFailure("File is empty.");
    }

    // NEW: Skip metadata rows before header
    if (rowsToSkip >= lines.Count)
    {
        return CsvParseResult.CreateFailure(
            $"Cannot skip {rowsToSkip} rows - file only has {lines.Count} rows.");
    }

    var skippedMetadataRows = lines.Take(rowsToSkip).ToList();
    var remainingLines = lines.Skip(rowsToSkip).ToList();

    if (remainingLines.Count == 0)
    {
        return CsvParseResult.CreateFailure("No rows remaining after skip.");
    }

    // Detect delimiter from ACTUAL header row (after skip)
    var delimiter = DetectDelimiter(remainingLines[0]);

    // Parse header row (first remaining row)
    var headers = ParseCsvLine(remainingLines[0], delimiter);
    if (headers.Count == 0)
    {
        return CsvParseResult.CreateFailure("No columns detected in header row.");
    }

    // Parse data rows (skip the header)
    var rows = new List<IReadOnlyList<string>>();
    for (int i = 1; i < remainingLines.Count; i++)
    {
        // ... existing row parsing logic ...
    }

    return CsvParseResult.CreateSuccess(headers, rows, delimiter, hasHeaderRow: true, rowsToSkip);
}
```

#### Update CsvParseResult

```csharp
// ICsvParserService.cs - CsvParseResult record
public sealed record CsvParseResult
{
    // ... existing properties ...

    /// <summary>
    /// Gets the number of rows skipped before the header.
    /// </summary>
    public int RowsSkipped { get; init; }  // NEW PROPERTY

    public static CsvParseResult CreateSuccess(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        char delimiter,
        bool hasHeaderRow,
        int rowsSkipped = 0)  // NEW PARAMETER
    {
        return new CsvParseResult
        {
            Success = true,
            Headers = headers,
            Rows = rows,
            DetectedDelimiter = delimiter,
            HasHeaderRow = hasHeaderRow,
            RowCount = rows.Count,
            RowsSkipped = rowsSkipped,  // NEW
        };
    }
}
```

### 3. Client Changes

#### Update IImportApiService

```csharp
// IImportApiService.cs
Task<CsvParseResultModel?> ParseCsvAsync(
    Stream fileContent, 
    string fileName, 
    int rowsToSkip = 0);  // NEW PARAMETER
```

#### Update ImportApiService

```csharp
// ImportApiService.cs
public async Task<CsvParseResultModel?> ParseCsvAsync(
    Stream fileContent, 
    string fileName, 
    int rowsToSkip = 0)
{
    try
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileContent);
        streamContent.Headers.ContentType = 
            new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(streamContent, "file", fileName);

        // NEW: Include rowsToSkip in query string
        var url = $"api/v1/import/parse?rowsToSkip={rowsToSkip}";
        var response = await this._httpClient.PostAsync(url, content);
        
        // ... rest of method ...
    }
    // ...
}
```

#### Update Import.razor

```csharp
// Import.razor - Code section
private IBrowserFile? uploadedFile;  // NEW: Store for re-parsing
private byte[]? uploadedFileContent; // NEW: Store content for re-parsing

private async Task HandleFileSelected(IBrowserFile file)
{
    // Store file content for potential re-parsing
    uploadedFile = file;
    using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
    using var memoryStream = new MemoryStream();
    await stream.CopyToAsync(memoryStream);
    uploadedFileContent = memoryStream.ToArray();

    // Initial parse with 0 skip rows
    await ParseCsvWithSkipRows(0);
}

private async Task ParseCsvWithSkipRows(int skipRows)
{
    if (uploadedFileContent == null || uploadedFile == null) return;

    isUploading = true;
    errorMessage = null;

    try
    {
        using var memoryStream = new MemoryStream(uploadedFileContent);
        var result = await ImportApi.ParseCsvAsync(memoryStream, uploadedFile.Name, skipRows);
        
        if (result != null)
        {
            wizardState.ParseResult = result;
            wizardState.RowsToSkip = skipRows;
            InitializeColumnMappings();
            // ... rest of handling ...
        }
        else
        {
            errorMessage = "Failed to parse the CSV file.";
        }
    }
    catch (Exception ex)
    {
        errorMessage = $"Error parsing file: {ex.Message}";
    }
    finally
    {
        isUploading = false;
    }
}

// NEW: Handler for skip rows change in mapping step
private async Task HandleSkipRowsChanged(int newSkipRows)
{
    if (newSkipRows == wizardState.RowsToSkip) return;
    
    await ParseCsvWithSkipRows(newSkipRows);
}
```

#### Update SkipRowsInput Component

```razor
<!-- SkipRowsInput.razor -->
@* Update label and add change callback *@
<div class="form-group">
    <label class="form-label">Rows Before Header</label>  @* RENAMED *@
    <div class="d-flex align-items-center gap-2">
        <input type="number" 
               class="form-control" 
               style="width: 100px"
               min="0" 
               max="100"
               value="@Value"
               @onchange="HandleValueChanged" />
        <span class="text-muted">rows</span>
    </div>
    <small class="form-text text-muted">
        Number of metadata rows to skip before the column headers
    </small>
</div>

@code {
    [Parameter] public int Value { get; set; }
    [Parameter] public EventCallback<int> ValueChanged { get; set; }
    [Parameter] public EventCallback<int> OnSkipRowsChanged { get; set; }  // NEW

    private async Task HandleValueChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int newValue) && newValue >= 0 && newValue <= 100)
        {
            Value = newValue;
            await ValueChanged.InvokeAsync(newValue);
            await OnSkipRowsChanged.InvokeAsync(newValue);  // Trigger re-parse
        }
    }
}
```

### 4. DTO Updates

#### Update CsvParseResultDto

```csharp
// ImportController.cs - CsvParseResultDto
public sealed record CsvParseResultDto
{
    // ... existing properties ...
    
    /// <summary>
    /// Gets the number of rows that were skipped before the header.
    /// </summary>
    public int RowsSkipped { get; init; }  // NEW
}
```

#### Update CsvParseResultModel (Client)

```csharp
// ImportModels.cs
public sealed record CsvParseResultModel
{
    // ... existing properties ...
    
    /// <summary>
    /// Gets the number of rows that were skipped before the header.
    /// </summary>
    public int RowsSkipped { get; init; }  // NEW
}
```

---

## Implementation Plan

### Phase 1: Application Layer (TDD) ✅
1. [x] Write tests for `CsvParserService.ParseAsync` with `rowsToSkip` parameter
2. [x] Update `ICsvParserService` interface
3. [x] Implement skip rows logic in `CsvParserService`
4. [x] Update `CsvParseResult` record with `RowsSkipped` property

### Phase 2: API Layer (TDD) ✅
5. [x] Write tests for parse endpoint with `rowsToSkip` query param
6. [x] Update `ImportController.ParseAsync` to accept `rowsToSkip`
7. [x] Update `CsvParseResultDto` with `RowsSkipped`
8. [x] Test error handling for invalid skip values

### Phase 3: Client Layer ✅
9. [x] Update `IImportApiService` interface
10. [x] Update `ImportApiService.ParseCsvAsync` with `rowsToSkip`
11. [x] Update `CsvParseResultModel` with `RowsSkipped`
12. [x] Store uploaded file content in `Import.razor` for re-parsing
13. [x] Implement `HandleSkipRowsChanged` to trigger re-parse
14. [x] Update `SkipRowsInput` component with new label and callback
15. [x] Test the complete flow

### Phase 4: Integration Testing ✅
16. [x] Test with Bank of America sample CSV (`sample data/boa.csv`)
17. [x] Test with Capital One sample CSV (no skip needed)
18. [x] Test with UHCU sample CSV (no skip needed)
19. [x] Test edge cases (skip all rows, skip more than file has)
20. [x] Update existing tests that may be affected

---

## Testing Strategy

### Unit Tests

```csharp
// CsvParserServiceTests.cs
[Fact]
public async Task ParseAsync_With_RowsToSkip_Skips_Metadata_Rows()
{
    // Arrange
    var csv = """
        Description,,Summary
        Balance,,"100.00"
        
        Date,Payee,Amount
        01/01/2025,Store A,-50.00
        """;
    
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
    
    // Act
    var result = await _service.ParseAsync(stream, "test.csv", rowsToSkip: 3);
    
    // Assert
    Assert.True(result.Success);
    Assert.Equal(3, result.RowsSkipped);
    Assert.Equal(new[] { "Date", "Payee", "Amount" }, result.Headers);
    Assert.Single(result.Rows);
}

[Fact]
public async Task ParseAsync_With_RowsToSkip_Exceeding_Total_Returns_Failure()
{
    // Arrange
    var csv = "Header\nRow1";
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
    
    // Act
    var result = await _service.ParseAsync(stream, "test.csv", rowsToSkip: 10);
    
    // Assert
    Assert.False(result.Success);
    Assert.Contains("only has 2 rows", result.ErrorMessage);
}
```

### Integration Tests

```csharp
// ImportControllerTests.cs
[Fact]
public async Task ParseAsync_With_RowsToSkip_Parameter_Returns_Correct_Headers()
{
    // Upload a CSV with metadata and verify skip rows works end-to-end
}
```

---

## Affected Components

| Component | Change Type | Description |
|-----------|-------------|-------------|
| `ICsvParserService` | Modified | Add `rowsToSkip` parameter |
| `CsvParserService` | Modified | Implement skip rows before header logic |
| `CsvParseResult` | Modified | Add `RowsSkipped` property |
| `ImportController` | Modified | Accept `rowsToSkip` query parameter |
| `CsvParseResultDto` | Modified | Add `RowsSkipped` property |
| `IImportApiService` | Modified | Add `rowsToSkip` parameter |
| `ImportApiService` | Modified | Pass `rowsToSkip` to API |
| `CsvParseResultModel` | Modified | Add `RowsSkipped` property |
| `Import.razor` | Modified | Store file for re-parse, handle skip change |
| `SkipRowsInput.razor` | Modified | Rename label, add change callback |

---

## Rollback Plan

If issues are found:
1. Revert to optional `rowsToSkip` parameter with default 0
2. API remains backward compatible (existing calls work)
3. Client falls back to single-parse behavior

---

## Success Criteria

1. [ ] Bank of America CSV imports correctly with `RowsToSkip = 6`
2. [ ] Changing skip rows in mapping step triggers re-parse with correct headers
3. [ ] Sample values in mapping step show actual transaction data
4. [ ] Saved mappings remember the skip rows setting
5. [ ] All existing tests pass
6. [ ] New tests cover the skip rows parsing logic

---

## Git Workflow

```bash
git checkout -b fix/033-csv-import-skip-header-rows
# ... implement changes ...
git commit -m "fix(import): allow skipping metadata rows before CSV header

Fixes CSV import for bank files that have metadata rows before
the actual header row (e.g., Bank of America exports).

- Add rowsToSkip parameter to CsvParserService.ParseAsync
- Update parse endpoint to accept rowsToSkip query parameter
- Client re-parses CSV when skip rows setting changes
- Rename 'Skip Rows After Header' to 'Rows Before Header'

Closes #XXX"
```

---

## Related Documentation

- [Feature 027: Intelligent CSV Import](./archive/027-csv-import.md)
- [Feature 030: CSV Import Enhancements](./archive/030-csv-import-enhancements.md)
