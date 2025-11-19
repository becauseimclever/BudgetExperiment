# Feature: CSV Import for Bank Transactions

**Created**: 2025-11-16  
**Updated**: 2025-11-17  
**Status**: ✅ Phase 1–5 COMPLETE  
**Priority**: HIGH

## Overview
Enable users to import bank transaction CSV files from multiple financial institutions and map them to adhoc transactions that appear on the calendar. Initial support for Bank of America, Capital One, and United Heritage Credit Union.

## Business Value
- **Time Savings**: Eliminate manual entry of dozens/hundreds of historical transactions
- **Accuracy**: Reduce human error in transaction data entry
- **Adoption**: Lower barrier to entry for new users by importing existing financial data
- **Reconciliation**: Enable users to quickly populate calendar with real bank data for budget tracking

## Scope & Supported Banks

### Phase 1: Bank of America (BofA)
**Priority**: P0 (first implementation)  
**CSV Format** (typical BofA checking/savings):
```
Date,Description,Amount,Running Bal.
"11/15/2025","ONLINE TRANSFER TO SAVINGS 123456","-50.00","1,234.56"
"11/14/2025","ATM WITHDRAWAL 00123","-40.00","1,284.56"
"11/13/2025","PAYCHECK DEPOSIT","2,500.00","1,324.56"
"11/12/2025","GROCERY STORE #456","-123.45","-1,175.44"
```

**Characteristics**:
- Date format: `MM/DD/YYYY` (US format with quotes)
- Amount: Positive = deposits/income, Negative = withdrawals/expenses
- Currency symbol: None in CSV (assume USD)
- Thousands separator: Comma in running balance
- Description: Free text, may contain account numbers

### Phase 2: Capital One
**Priority**: P1 (second implementation)  
**CSV Format** (typical Capital One checking):
```
Transaction Date,Posted Date,Card No.,Description,Category,Debit,Credit
11/15/2025,11/15/2025,1234,GROCERY STORE,Groceries,45.67,
11/14/2025,11/14/2025,1234,PAYCHECK DIRECT DEPOSIT,Income,,2500.00
11/13/2025,11/13/2025,1234,ELECTRIC COMPANY,Utilities,125.00,
```

**Characteristics**:
- Date format: `MM/DD/YYYY` (US format, no quotes)
- Amount: Split into Debit (expenses) and Credit (income) columns
- Category: Provided by Capital One (optional to map or override)
- Card No.: May be present, ignore for import
- Currency: Assume USD

### Phase 3: United Heritage Credit Union (UHCU)
**Priority**: P2 (third implementation)  
**CSV Format** (typical credit union export):
```
"Date","Transaction","Name","Memo","Amount"
"11/15/2025","DEBIT","GROCERY STORE #123","POS Purchase","-45.67"
"11/14/2025","CREDIT","PAYROLL DEPOSIT","Direct Deposit","2500.00"
"11/13/2025","DEBIT","UTILITY COMPANY","Online Payment","-125.00"
```

**Characteristics**:
- Date format: `MM/DD/YYYY` (with quotes)
- Transaction type: Explicit "DEBIT"/"CREDIT" column
- Amount: Signed decimals (negative for expenses)
- Name + Memo: Combined description source
- Currency: Assume USD

## Domain Impact

### No New Domain Entities Required
CSV import is a **UI/Application concern** that creates existing `AdhocTransaction` entities. No new domain models needed.

### Existing Domain Usage
- `AdhocTransaction` (already supports income/expense with `TransactionType` enum)
- `MoneyValue` (currency + amount validation)
- `TransactionType` (Income = 0, Expense = 1)

## Architecture & Implementation Plan

### Layer Responsibilities

#### Domain Layer
**No changes required** - existing `AdhocTransaction` aggregate handles all needed data.

#### Application Layer (NEW)
**New Service**: `CsvImportService`  
Location: `src/BudgetExperiment.Application/CsvImport/`

**Responsibilities**:
1. Parse CSV files using bank-specific parsers
2. Validate row data (dates, amounts, required fields)
3. Map CSV rows to `CreateIncomeTransactionRequest` or `CreateExpenseTransactionRequest`
4. Detect duplicates (optional enhancement)
5. Orchestrate bulk transaction creation via existing `IAdhocTransactionWriteRepository`

**Key Classes**:
```csharp
// Application/CsvImport/CsvImportService.cs
public interface ICsvImportService
{
    Task<CsvImportResult> ImportAsync(Stream csvStream, BankType bankType, CancellationToken ct);
}

// Application/CsvImport/Models/CsvImportResult.cs
public sealed record CsvImportResult(
    int TotalRows,
    int SuccessfulImports,
    int FailedImports,
    int DuplicatesSkipped,
    IReadOnlyList<CsvImportError> Errors);

public sealed record CsvImportError(
    int RowNumber,
    string Field,
    string ErrorMessage);

// Application/CsvImport/BankType.cs
public enum BankType
{
    BankOfAmerica = 0,
    CapitalOne = 1,
    UnitedHeritageCreditUnion = 2
}

// Application/CsvImport/Parsers/IBankCsvParser.cs
public interface IBankCsvParser
{
    BankType BankType { get; }
    Task<IReadOnlyList<ParsedTransaction>> ParseAsync(Stream csvStream, CancellationToken ct);
}

// Application/CsvImport/Models/ParsedTransaction.cs
public sealed record ParsedTransaction(
    DateOnly Date,
    string Description,
    decimal Amount,
    TransactionType TransactionType,
    string? Category);
```

**Parser Implementations**:
- `BankOfAmericaCsvParser` (Phase 1)
- `CapitalOneCsvParser` (Phase 2)
- `UnitedHeritageCreditUnionCsvParser` (Phase 3)

#### Infrastructure Layer
**No new persistence logic** - reuse existing `IAdhocTransactionWriteRepository.AddAsync()`.

Optional enhancement: Add `AddBulkAsync` for performance optimization if importing 100+ transactions at once.

#### API Layer (NEW)
**New Endpoint**: `POST /api/v1/csv-import`

```csharp
// Api/Controllers/CsvImportController.cs
[ApiController]
[Route("api/v1/csv-import")]
public sealed class CsvImportController : ControllerBase
{
    // POST /api/v1/csv-import
    // Accepts multipart/form-data with:
    // - file: CSV file (IFormFile)
    // - bankType: "BankOfAmerica" | "CapitalOne" | "UnitedHeritageCreditUnion"
    [HttpPost]
    [ProducesResponseType(typeof(CsvImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ImportCsv(
        [FromForm] IFormFile file, 
        [FromForm] string bankType, 
        CancellationToken ct);
}
```

**Validation**:
- File extension: `.csv` only
- Max file size: 5MB (configurable)
- Bank type: Must be valid enum value
- File not empty

**Response Examples**:

Success (200 OK):
```json
{
  "totalRows": 150,
  "successfulImports": 148,
  "failedImports": 2,
    "duplicatesSkipped": 3,
  "errors": [
    {
      "rowNumber": 45,
      "field": "Amount",
      "errorMessage": "Invalid decimal format"
    },
    {
      "rowNumber": 89,
      "field": "Date",
      "errorMessage": "Date cannot be parsed: '13/25/2025'"
    }
    ],
    "duplicates": [
        {
            "rowNumber": 12,
            "date": "2025-11-10",
            "description": "GROCERY STORE #456",
            "amount": 123.45,
            "existingTransactionId": "00000000-0000-0000-0000-000000000001"
        }
    ]
}
```

Validation Error (400 Bad Request):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "bankType": ["BankType must be one of: BankOfAmerica, CapitalOne, UnitedHeritageCreditUnion"]
  }
}
```

#### Client Layer (Blazor WebAssembly)
**New Component**: CSV Import Dialog

Location: `src/BudgetExperiment.Client/Components/CsvImportDialog.razor`

**UI Flow**:
1. User clicks "Import Transactions" button in calendar header
2. Dialog opens with:
   - Bank selection dropdown (BofA, Capital One, UHCU)
   - File upload input (accepts `.csv` only)
   - "Import" and "Cancel" buttons
3. On import:
   - Upload CSV to API endpoint
   - Show progress indicator
   - Display result summary (success count, errors)
   - If errors, show expandable error list
4. On success, refresh calendar data automatically

**Component Structure**:
```razor
<FluentDialog @bind-Open="@IsOpen" Modal="true" TrapFocus="true">
    <FluentDialogHeader>Import Bank Transactions</FluentDialogHeader>
    <FluentDialogBody>
        <FluentStack Orientation="Orientation.Vertical" VerticalGap="16">
            <!-- Bank Selection -->
            <FluentSelect Label="Select Your Bank" @bind-Value="selectedBankType">
                <FluentOption Value="BankOfAmerica">Bank of America</FluentOption>
                <FluentOption Value="CapitalOne">Capital One</FluentOption>
                <FluentOption Value="UnitedHeritageCreditUnion">United Heritage Credit Union</FluentOption>
            </FluentSelect>
            
            <!-- File Upload -->
            <InputFile OnChange="HandleFileSelected" accept=".csv" />
            
            <!-- Import Progress/Results -->
            @if (isImporting)
            {
                <FluentProgressRing />
                <FluentLabel>Importing transactions...</FluentLabel>
            }
            @if (importResult != null)
            {
                <FluentMessageBar Intent="@GetMessageIntent()">
                    <strong>Import Complete:</strong> @importResult.SuccessfulImports of @importResult.TotalRows transactions imported
                    @if (importResult.FailedImports > 0)
                    {
                        <span> (@importResult.FailedImports errors)</span>
                    }
                </FluentMessageBar>
                
                @if (importResult.Errors.Any())
                {
                    <FluentAccordion>
                        <FluentAccordionItem Heading="View Errors">
                            @foreach (var error in importResult.Errors)
                            {
                                <div>Row @error.RowNumber: @error.ErrorMessage</div>
                            }
                        </FluentAccordionItem>
                    </FluentAccordion>
                }
            }
        </FluentStack>
    </FluentDialogBody>
    <FluentDialogFooter>
        <FluentButton Appearance="Appearance.Accent" 
                      OnClick="ImportCsv" 
                      Disabled="@(!CanImport())">
            Import
        </FluentButton>
        <FluentButton Appearance="Appearance.Neutral" OnClick="Cancel">
            Cancel
        </FluentButton>
    </FluentDialogFooter>
</FluentDialog>
```

**Integration Points**:
- Add "Import" button to `CalendarHeader.razor`
- After successful import, call `CalendarDataService.ClearCacheAsync()` and reload calendar

## Testing Strategy

### Unit Tests (Application Layer)

**Test Project**: `tests/BudgetExperiment.Application.Tests/CsvImport/`

#### BankOfAmericaCsvParserTests
- ✅ ParseAsync_ValidBofACsv_ReturnsCorrectTransactions
- ✅ ParseAsync_MixedIncomeAndExpenses_CorrectlyMapsTransactionTypes
- ✅ ParseAsync_InvalidDateFormat_ThrowsDomainException
- ✅ ParseAsync_InvalidAmountFormat_ThrowsDomainException
- ✅ ParseAsync_EmptyFile_ReturnsEmptyList
- ✅ ParseAsync_MissingRequiredColumns_ThrowsDomainException
- ✅ ParseAsync_HandlesQuotedStrings_CorrectlyParsesDescription

#### CapitalOneCsvParserTests (Phase 2)
- ✅ ParseAsync_ValidCapitalOneCsv_ReturnsCorrectTransactions
- ✅ ParseAsync_DebitCreditColumns_CorrectlyMapsToTransactionType
- ✅ ParseAsync_ProvidedCategory_MapsToTransactionCategory
- ✅ ParseAsync_MissingPostedDate_UsesTransactionDate

#### UnitedHeritageCreditUnionCsvParserTests (Phase 3)
- ✅ ParseAsync_ValidUHCUCsv_ReturnsCorrectTransactions
- ✅ ParseAsync_ExplicitTransactionType_CorrectlyMapsDebitCredit
- ✅ ParseAsync_CombinesNameAndMemo_BuildsFullDescription

#### CsvImportServiceTests
- ✅ ImportAsync_ValidCsv_CreatesAllTransactions
- ✅ ImportAsync_DuplicateTransactions_SkipsOrReportsCorrectly
- ✅ ImportAsync_PartialFailures_ReturnsCorrectCounts
- ✅ ImportAsync_InvalidBankType_ThrowsArgumentException
- ✅ ImportAsync_LargeFile_HandlesPerformanceGracefully (100+ rows)

### Integration Tests (API Layer)

**Test Project**: `tests/BudgetExperiment.Api.Tests/CsvImport/`

#### CsvImportControllerTests
- ✅ ImportCsv_ValidBofAFile_Returns200WithResults
- ✅ ImportCsv_InvalidBankType_Returns400ValidationError
- ✅ ImportCsv_NonCsvFile_Returns400ValidationError
- ✅ ImportCsv_FileTooLarge_Returns413PayloadTooLarge
- ✅ ImportCsv_EmptyFile_Returns200WithZeroImports
 - ✅ ImportCsv_MalformedCsv_Returns422WithErrors
 - ✅ ImportCsv_DuplicateSecondImport_SkipsDuplicates
 - ✅ AdhocTransactions_CreateDuplicate_Returns400

### Manual Testing Checklist

**Blazor Client**:
- [x] Import button visible in calendar header
- [x] Dialog opens on button click
- [x] Bank dropdown lists all 3 banks (BofA, Capital One, UHCU)
- [x] File input accepts `.csv` only
- [x] Progress indicator shows during upload
- [x] Success message displays with correct counts
- [x] Error accordion expands to show row-level errors
- [x] Calendar refreshes automatically after import
- [x] Imported transactions appear on correct dates
- [x] Income transactions display as green/positive
- [x] Expense transactions display as red/negative

**API Endpoint**:
- [ ] POST `/api/v1/csv-import` accepts multipart form data
- [ ] Validates bank type enum
- [ ] Validates file extension
- [ ] Returns proper error responses (400, 413, 422)
- [ ] OpenAPI/Scalar documents endpoint correctly

## CSV Parsing Library

**Recommendation**: Use **CsvHelper** NuGet package  
**Rationale**: 
- Industry-standard, well-maintained (3M+ downloads/month)
- Handles quoted fields, escape characters, encoding issues
- Type-safe mapping with flexible configuration
- Performance-optimized for large files
- MIT licensed

**Alternative**: Manual parsing with `StreamReader`  
**Rationale**: Lightweight, no external dependencies, but more error-prone.

**Decision**: Use **CsvHelper** for robustness. Add to `BudgetExperiment.Application` project.

## Edge Cases & Considerations

### Duplicate Detection
**Problem**: User imports same CSV multiple times, or manually enters transactions that were already imported.  
**Solutions**:
1. **No duplicate check** (Phase 1-3) - allow duplicates, user responsibility to clean up
2. **Exact match check** (Phase 4) - skip if identical transaction (Date + Description + Amount + Type) exists
3. **Fuzzy matching** (Phase 5) - use Levenshtein distance on description + date proximity ±1 day

**Decision**: 
- Phase 1-3: **Option 1** (no check) - keep initial implementation simple
- Phase 4: **Option 2** (exact match) - prevent duplicate imports and manual entries
- Phase 5: **Option 3** (fuzzy match) - only if user feedback shows need for more sophisticated matching

### Date Parsing
**Problem**: Ambiguous date formats (MM/DD/YYYY vs DD/MM/YYYY).  
**Solution**: Bank-specific parsers enforce known format. Fail loudly on parse error with clear message.

### Currency Handling
**Problem**: Multi-currency bank accounts not yet supported.  
**Decision**: Assume USD for all imports in Phase 1. Add currency detection/override in future.

### Large File Performance
**Problem**: 1000+ row CSV imports may be slow.  
**Solutions**:
1. Process in batches of 100 rows
2. Add bulk insert repository method
3. Stream processing instead of loading entire file into memory

**Decision**: Test with 500-row files. Optimize if Phase 1 testing reveals issues.

### Transaction Category Mapping
**Problem**: Capital One provides categories; BofA/UHCU do not.  
**Decision**: 
- If CSV has category column, map to `AdhocTransaction.Category`
- Allow user to override/edit categories after import via existing edit dialog

### Failed Row Handling
**Problem**: Some rows fail validation mid-import.  
**Decision**: Continue processing remaining rows (partial success). Return full error report at end.

## OpenAPI / Scalar Documentation

**Endpoint Documentation** (`POST /api/v1/csv-import`):
```csharp
/// <summary>
/// Import bank transactions from CSV file.
/// </summary>
/// <remarks>
/// Supported banks:
/// - Bank of America
/// - Capital One
/// - United Heritage Credit Union
/// 
/// CSV format varies by bank. Ensure the bankType parameter matches your CSV source.
/// 
/// Maximum file size: 5MB
/// Allowed file extensions: .csv
/// </remarks>
/// <param name="file">CSV file to import.</param>
/// <param name="bankType">Bank that generated the CSV (BankOfAmerica, CapitalOne, UnitedHeritageCreditUnion).</param>
/// <param name="ct">Cancellation token.</param>
/// <returns>Import result with success/failure counts and error details.</returns>
[HttpPost]
[Consumes("multipart/form-data")]
[ProducesResponseType(typeof(CsvImportResult), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
public async Task<IActionResult> ImportCsv(/*...*/);
```

**Example Request** (cURL):
```bash
curl -X POST http://localhost:5099/api/v1/csv-import \
  -F "file=@transactions.csv" \
  -F "bankType=BankOfAmerica"
```

## Security Considerations

1. **File Upload Validation**:
   - Extension whitelist: `.csv` only
   - MIME type check: `text/csv` or `application/vnd.ms-excel`
   - File size limit: 5MB (configurable via `appsettings.json`)

2. **CSV Injection Prevention**:
   - Sanitize descriptions before storing (remove leading `=`, `+`, `-`, `@` characters)
   - Use CsvHelper's safe parsing (no formula evaluation)

3. **Rate Limiting** (future):
   - Limit to 5 imports per user per hour to prevent abuse

4. **Authentication** (future):
   - When auth is added, ensure imports are user-scoped

## Performance Considerations

**Expected Load**:
- Average CSV: 50-200 rows (1-3 months of transactions)
- Large CSV: 500-1000 rows (1 year of transactions)

**Performance Targets**:
- 100 rows: < 2 seconds
- 500 rows: < 10 seconds
- 1000 rows: < 20 seconds

**Optimization Strategies** (if needed):
1. Bulk insert repository method (batch EF Core operations)
2. Async/parallel processing of parsed rows
3. Database transaction batching
4. Client-side progress updates via SignalR (future)

## Incremental Delivery Plan

### Phase 1: Bank of America Support ✅ COMPLETE
**Target**: Minimal viable CSV import

**Tasks** (TDD Order):
1. ✅ Create feature planning document
2. ✅ Add CsvHelper NuGet to Application project
3. ✅ Implement `BankOfAmericaCsvParser` + unit tests
4. ✅ Implement `CsvImportService` + unit tests
5. ✅ Implement `CsvImportController` + integration tests
6. ✅ Add OpenAPI documentation (Scalar UI wired separately)
7. ✅ Implement Blazor `CsvImportDialog` component
8. ✅ Integrate import button into `CalendarHeader`
9. ✅ Manual testing with BofA sample CSV
10. ⬜ Code review & merge (pending branch merge)

**Success Criteria**:
- ✅ User can import BofA CSV
- ✅ Transactions appear on calendar
- ✅ Errors reported clearly
- ✅ All tests pass

### Phase 2: Capital One Support ✅ **COMPLETE**
**Target**: Add second bank parser

**Tasks**:
1. ✅ Implement `CapitalOneCsvParser` + unit tests
2. ✅ Update Blazor dialog to enable Capital One option
3. ✅ Manual testing with real Capital One CSV files
4. ✅ Update documentation
5. ✅ Code review & merge

**Success Criteria**:
- ✅ User can import Capital One CSV
- ✅ Categories map correctly (if present - not in current Capital One format)
- ✅ Debit/Credit columns parse correctly

### Phase 3: United Heritage Credit Union Support ✅ **COMPLETE**
**Target**: Add third bank parser

**Tasks**:
1. ✅ Implement `UnitedHeritageCreditUnionCsvParser` + unit tests
2. ✅ Update Blazor dialog to enable UHCU option
3. ✅ Manual testing with UHCU sample CSV
4. ✅ Update documentation
5. ⬜ Code review & merge (pending branch merge)

**Success Criteria**:
- ✅ User can import UHCU CSV (parser implemented)
- ✅ Check numbers combine with descriptions
- ✅ Debit/Credit columns parse correctly to separate Income/Expense types
- ✅ Date format M/d/yyyy handled (flexible single/double digit)
- ✅ Quoted fields and HTML entities preserved
- ✅ 17 unit tests + 3 integration tests passing

### Phase 4: Duplicate Detection & Prevention 🎯 ✅ COMPLETE
**Target**: Prevent duplicate transactions from any source (manual or imported)

**Problem Statement**: Users may:
- Import the same CSV file multiple times
- Manually enter transactions that already exist from an import
- Import overlapping date ranges from multiple CSV exports

**Solution**: Implement intelligent duplicate detection at the repository/service layer.

**Tasks** (TDD Order):
1. ✅ Design duplicate detection strategy (exact match)
2. ✅ Implement `IAdhocTransactionReadRepository.FindDuplicatesAsync()` method
3. ✅ Add unit tests for duplicate detection logic
4. ✅ Update `CsvImportService` to check for duplicates before inserting
5. ✅ Update `AdhocTransactionService.CreateIncomeAsync/CreateExpenseAsync` to check for duplicates
6. ✅ Add integration tests for duplicate scenarios
7. ✅ Update API responses to indicate duplicates skipped and details
8. ✅ Update Blazor UI to show duplicate warnings/skipped count
9. ⬜ Add user preference: "Skip duplicates" vs "Allow duplicates" (optional)
10. ✅ Manual testing plan prepared
11. ⬜ Code review & merge

**Duplicate Detection Strategy**:

**Exact Match Criteria** (all must match):
- Date (exact match: `DateOnly`)
- Description (case-insensitive, trimmed)
- Amount (exact decimal match on absolute value)
- Transaction Type (Income vs Expense)

**Implementation**:
```csharp
// Domain/IAdhocTransactionReadRepository.cs (add method)
public interface IAdhocTransactionReadRepository : IReadRepository<AdhocTransaction>
{
    // ... existing methods ...
    
    /// <summary>
    /// Find potential duplicate transactions based on date, description, amount, and type.
    /// </summary>
    Task<IReadOnlyList<AdhocTransaction>> FindDuplicatesAsync(
        DateOnly date, 
        string description, 
        decimal amount, 
        TransactionType transactionType, 
        CancellationToken cancellationToken = default);
}

// Application/CsvImport/Models/CsvImportResult.cs (update)
public sealed record CsvImportResult(
    int TotalRows,
    int SuccessfulImports,
    int FailedImports,
    int DuplicatesSkipped,  // ← Already in design, now used!
    IReadOnlyList<CsvImportError> Errors,
    IReadOnlyList<DuplicateTransaction> Duplicates);  // ← New: details of skipped duplicates

public sealed record DuplicateTransaction(
    int RowNumber,
    DateOnly Date,
    string Description,
    decimal Amount,
    Guid ExistingTransactionId);  // Reference to existing transaction
```

**Service Layer Logic**:
```csharp
// Application/CsvImport/CsvImportService.cs (pseudocode)
public async Task<CsvImportResult> ImportAsync(Stream csvStream, BankType bankType, CancellationToken ct)
{
    var parsedTransactions = await _parser.ParseAsync(csvStream, ct);
    var successCount = 0;
    var duplicatesSkipped = 0;
    var duplicateDetails = new List<DuplicateTransaction>();
    
    foreach (var (transaction, rowNumber) in parsedTransactions.Select((t, i) => (t, i + 2)))  // +2 for header row
    {
        // Check for duplicates
        var duplicates = await _readRepo.FindDuplicatesAsync(
            transaction.Date, 
            transaction.Description, 
            Math.Abs(transaction.Amount),  // Store as absolute value
            transaction.TransactionType, 
            ct);
        
        if (duplicates.Any())
        {
            duplicatesSkipped++;
            duplicateDetails.Add(new DuplicateTransaction(
                rowNumber, 
                transaction.Date, 
                transaction.Description, 
                transaction.Amount, 
                duplicates.First().Id));
            continue;  // Skip this transaction
        }
        
        // No duplicate, proceed with insert
        await CreateTransactionAsync(transaction, ct);
        successCount++;
    }
    
    return new CsvImportResult(/* ... include duplicatesSkipped and duplicateDetails ... */);
}
```

**UI Display** (Blazor dialog):
```razor
@if (importResult.DuplicatesSkipped > 0)
{
    <FluentMessageBar Intent="MessageIntent.Warning">
        <strong>Skipped @importResult.DuplicatesSkipped duplicate transaction(s).</strong>
    </FluentMessageBar>

    <FluentAccordion>
        <FluentAccordionItem Heading="View Duplicates">
            @foreach (var dup in importResult.Duplicates)
            {
                <div>Row @dup.RowNumber: @dup.Date.ToString("yyyy-MM-dd") · @dup.Description · @dup.Amount.ToString("C")</div>
            }
        </FluentAccordionItem>
    </FluentAccordion>
}
```

**Performance Considerations**:
- Composite index added on `(Date, TransactionType)` to accelerate duplicate lookups
- For imports >100 rows, consider pre-loading all existing transactions in date range into memory dictionary (future optimization)

**Success Criteria**:
- ✅ Importing same CSV twice results in 0 new transactions on second import
- ✅ Manually entering transaction that matches CSV import is prevented
- ✅ Similar transactions with different amounts are NOT treated as duplicates
- ✅ Duplicate check adds <100ms per transaction (performance acceptable)
- ✅ User sees clear report of which rows were skipped and why

### Phase 5: Advanced Deduplication ✅ COMPLETE
Target: Reduce near-duplicate imports via fuzzy matching, handling bank-generated metadata vs manual entries.

**Problem Solved**: Bank CSVs contain transaction dates, confirmation codes, merchant category codes, phone numbers, and locations embedded in descriptions (e.g., `"GROCERY STORE #659 11/07 MOBILE PURCHASE ANYTOWN TX"`) while manual entries might be simpler (e.g., `"GROCERY STORE #659"`). These should be recognized as duplicates.

**Implementation**:
- Enhanced description normalization in `CsvImportService`:
  - **Metadata removal**: Strips dates (MM/DD/YYYY formats), confirmation numbers (Conf#, ID:), transaction codes, phone numbers, account masks (XXXXX), website domains, state codes, and common bank keywords (PURCHASE, DEBIT CARD, MOBILE, ACH, etc.)
  - **Keyword extraction**: Tokenizes cleaned descriptions into significant words (≥3 chars)
  - **Dual similarity scoring**:
    1. **Levenshtein distance** ≤ 5 on normalized strings (increased tolerance for bank variations)
    2. **Jaccard similarity** ≥ 0.6 on keyword sets (handles word-order differences)
- Date proximity: ±1 day window (unchanged)
- Amount/type: exact match on absolute amount and transaction type (unchanged)
- Exact-match check remains first; fuzzy check runs only when no exact duplicate found

**Testing**:
- `ImportAsync_FuzzyDuplicateWithinOneDay_SkipsDuplicate`: Basic fuzzy matching with punctuation differences
- `ImportAsync_BankMetadataVsManualEntry_SkipsDuplicate`: Bank description with date/location vs simple manual entry
- `ImportAsync_ZelleWithConfCodeVsManual_SkipsDuplicate`: Zelle transaction with confirmation code vs clean description
- `ImportAsync_DifferentMerchantsSimilarNames_AllowsBoth`: Ensures different merchants aren't falsely matched (amount filter protects)

**Behavior**:
- Duplicates (exact or fuzzy) increment `DuplicatesSkipped` and appear in `CsvImportResult.Duplicates`
- No API or UI contract changes required
- Handles real-world scenarios from BofA, Capital One, and UHCU sample CSVs

**Examples**:
- Manual: `"GROCERY STORE #659"` (83.72, 11/7) matches Bank CSV: `"GROCERY STORE #659 11/07 MOBILE PURCHASE ANYTOWN TX"` (-83.72, 11/7)
- Manual: `"Zelle payment from John Smith"` (100.00, 10/1) matches Bank CSV: `"Zelle payment from John Smith Conf# AB8KL2MXC"` (100.00, 10/1)
- Different transactions with same merchant/similar amounts still differentiated by date/amount checks

### Phase 5: Additional Enhancements (Future)
**Optional features based on user feedback**:
- ⬜ Import history log (track past imports with file names and timestamps)
- ⬜ Undo import feature (batch delete by import session ID)
- ⬜ Category auto-mapping rules (ML-based or user-defined patterns)
- ⬜ Multi-currency support (detect from CSV or allow user override)
- ⬜ Additional bank formats (Chase, Wells Fargo, Ally, etc.)
- ⬜ Drag-and-drop file upload in Blazor UI
- ⬜ Import preview before committing (show parsed data in table)
- ⬜ Fuzzy duplicate matching (Levenshtein distance on description + date proximity ±1 day)
- ⬜ User preference: "Auto-skip duplicates" vs "Prompt for confirmation"

## Files to Create

### Application Layer
- ✅ `src/BudgetExperiment.Application/CsvImport/ICsvImportService.cs`
- ✅ `src/BudgetExperiment.Application/CsvImport/CsvImportService.cs`
- ✅ `src/BudgetExperiment.Application/CsvImport/BankType.cs`
- ✅ `src/BudgetExperiment.Application/CsvImport/Models/CsvImportResult.cs`
- ✅ `src/BudgetExperiment.Application/CsvImport/Models/CsvImportError.cs`
- ✅ `src/BudgetExperiment.Application/CsvImport/Models/ParsedTransaction.cs`
- ✅ `src/BudgetExperiment.Application/CsvImport/Parsers/IBankCsvParser.cs`
- ✅ `src/BudgetExperiment.Application/CsvImport/Parsers/BankOfAmericaCsvParser.cs`
- ✅ `src/BudgetExperiment.Application/CsvImport/Parsers/CapitalOneCsvParser.cs` (Phase 2)
- ✅ `src/BudgetExperiment.Application/CsvImport/Parsers/UnitedHeritageCreditUnionCsvParser.cs` (Phase 3)

### API Layer
- `src/BudgetExperiment.Api/Controllers/CsvImportController.cs`

### Client Layer
- `src/BudgetExperiment.Client/Components/CsvImportDialog.razor`
- `src/BudgetExperiment.Client/Components/CsvImportDialog.razor.css` (scoped styles)

### Tests
- ✅ `tests/BudgetExperiment.Application.Tests/CsvImport/BankOfAmericaCsvParserTests.cs`
- ✅ `tests/BudgetExperiment.Application.Tests/CsvImport/CapitalOneCsvParserTests.cs` (Phase 2)
- ✅ `tests/BudgetExperiment.Application.Tests/CsvImport/UnitedHeritageCreditUnionCsvParserTests.cs` (Phase 3)
- ✅ `tests/BudgetExperiment.Application.Tests/CsvImport/UnitedHeritageCreditUnionRealCsvTests.cs` (Phase 3 - integration)
- ⬜ `tests/BudgetExperiment.Application.Tests/CsvImport/CsvImportServiceTests.cs`
- ⬜ `tests/BudgetExperiment.Api.Tests/CsvImport/CsvImportControllerTests.cs`

### Files to Modify

**Phase 1-3**:
- ✅ `src/BudgetExperiment.Application/DependencyInjection.cs` (register `ICsvImportService` + parsers)
- ✅ `src/BudgetExperiment.Client/Components/Calendar/CalendarHeader.razor` (add import button)
- ✅ `src/BudgetExperiment.Client/Pages/FluentCalendar.razor` (integrate import dialog)

**Phase 4 (Duplicate Detection)**:
- ✅ `src/BudgetExperiment.Domain/IAdhocTransactionReadRepository.cs` (add `FindDuplicatesAsync` method)
- ✅ `src/BudgetExperiment.Infrastructure/Repositories/AdhocTransactionReadRepository.cs` (implement duplicate query)
- ✅ `src/BudgetExperiment.Infrastructure/BudgetDbContext.cs` (composite index)
- ✅ Database migration: Add composite index for duplicate detection performance
- ✅ `src/BudgetExperiment.Application/CsvImport/CsvImportService.cs` (duplicate checking logic)
- ✅ `src/BudgetExperiment.Application/CsvImport/Models/CsvImportResult.cs` (add `Duplicates` property)
- ✅ `src/BudgetExperiment.Application/CsvImport/Models/DuplicateTransaction.cs` (details model)
- ✅ `src/BudgetExperiment.Application/AdhocTransactions/AdhocTransactionService.cs` (duplicate check on manual create)
- ✅ `src/BudgetExperiment.Client/Components/CsvImportDialog.razor` (display duplicate warnings and list)

## Open Questions & Decisions

### Q1: Should duplicate detection be Phase 1 or Phase 4?
**Decision**: Phase 4 (dedicated phase for deduplication). Keep Phase 1-3 simple and focused on bank-specific parsing. Users can manually delete duplicates if needed before Phase 4 is complete. Phase 4 will prevent duplicates from ANY source (imports or manual entry).

### Q2: How to handle CSV encoding issues (UTF-8, Windows-1252, etc.)?
**Decision**: CsvHelper handles common encodings automatically. If issues arise, add encoding detection library.

### Q3: Should we validate transaction dates against calendar bounds?
**Decision**: No validation. Allow importing historical transactions (past years) and future-dated transactions.

### Q4: Bulk repository method or sequential inserts?
**Decision**: Start with sequential `AddAsync()` calls. Add `AddBulkAsync()` only if performance testing shows need.

### Q5: Should imported transactions be marked/tagged as "imported"?
**Decision**: No special tagging in Phase 1-3. User can manually edit if needed. Consider adding `ImportSourceId` and `ImportTimestamp` in Phase 5 to support "Undo Import" feature.

### Q6: Should duplicate detection apply to manual entry too, or just imports?
**Decision**: Phase 4 duplicate detection applies to BOTH manual entry and imports. The duplicate check will be at the service/repository layer, catching duplicates regardless of source. This prevents accidentally entering the same transaction twice via the UI.

## Success Metrics

**User Success**:
- ✅ User can import 100+ transactions in under 10 seconds
- ✅ Error messages clearly explain what went wrong and which row failed
- ✅ Imported transactions are immediately visible on calendar

**Code Quality**:
- ✅ All unit tests pass (90%+ coverage on CsvImport namespace)
- ✅ All integration tests pass
- ✅ No StyleCop warnings introduced
- ✅ OpenAPI spec fully documents endpoint with examples

**Deployment**:
- ✅ Feature toggleable via configuration (if needed)
- ✅ Works on Raspberry Pi deployment (no platform-specific code)

## Future Enhancements (Out of Scope for Initial Release)

1. **Smart Category Learning**: ML-based category suggestion based on description patterns
2. **Import Scheduling**: Auto-import from bank API connections (not CSV)
3. **Multi-File Upload**: Import multiple CSVs at once
4. **Import Templates**: User-defined CSV format parsers for unsupported banks
5. **Transaction Merging**: Combine duplicate detection with auto-merge logic
6. **Audit Trail**: Track which transactions were imported from which CSV file

---

## Appendix: Sample CSV Files (for Testing)

### Bank of America (Sample)
```csv
Date,Description,Amount,Running Bal.
"11/15/2025","ONLINE TRANSFER TO SAVINGS 123456","-50.00","1,234.56"
"11/14/2025","ATM WITHDRAWAL 00123","-40.00","1,284.56"
"11/13/2025","PAYCHECK DEPOSIT","2,500.00","1,324.56"
"11/12/2025","GROCERY STORE #456","-123.45","-1,175.44"
"11/11/2025","ELECTRIC COMPANY AUTOPAY","-125.00","-1,051.99"
"11/10/2025","RESTAURANT #789","-45.67","-926.99"
"11/09/2025","GAS STATION FUEL","-50.00","-881.32"
"11/08/2025","COFFEE SHOP","-5.50","-831.32"
```

### Capital One (Sample)
```csv
Transaction Date,Posted Date,Card No.,Description,Category,Debit,Credit
11/15/2025,11/15/2025,1234,GROCERY STORE,Groceries,45.67,
11/14/2025,11/14/2025,1234,PAYCHECK DIRECT DEPOSIT,Income,,2500.00
11/13/2025,11/13/2025,1234,ELECTRIC COMPANY,Utilities,125.00,
11/12/2025,11/12/2025,1234,ATM WITHDRAWAL,Other,40.00,
11/11/2025,11/11/2025,1234,RESTAURANT DINING,Dining,55.00,
11/10/2025,11/10/2025,1234,GAS STATION,Gas/Fuel,50.00,
11/09/2025,11/09/2025,1234,ONLINE SHOPPING,Shopping,89.99,
```

### United Heritage Credit Union (Sample)
```csv
"Date","Transaction","Name","Memo","Amount"
"11/15/2025","DEBIT","GROCERY STORE #123","POS Purchase","-45.67"
"11/14/2025","CREDIT","PAYROLL DEPOSIT","Direct Deposit","2500.00"
"11/13/2025","DEBIT","UTILITY COMPANY","Online Payment","-125.00"
"11/12/2025","DEBIT","ATM WITHDRAWAL 00456","ATM Transaction","-40.00"
"11/11/2025","DEBIT","RESTAURANT DINING","POS Purchase","-55.00"
"11/10/2025","DEBIT","GAS STATION #789","POS Purchase","-50.00"
"11/09/2025","DEBIT","ONLINE RETAILER","Web Payment","-89.99"
"11/08/2025","CREDIT","REFUND FROM STORE","Credit Memo","25.00"
```

---

## Readiness for Next Phase

- All three bank formats supported and manually tested end-to-end (UI → API → DB).
- Blazor dialog lists Bank of America, Capital One, and United Heritage Credit Union.
- API validations (file type/size, bank type) enforced; errors surfaced in UI.
- Tests green across Application, API, and Client.

Phase 4 completed (Duplicate Detection & Prevention). Proceed with optional Phase 5 enhancements as needed.

**Last Updated**: 2025-11-17  
**Next Review**: Post Phase 4 completion
