# Feature 027: Intelligent CSV Import

## Overview

Implement a flexible CSV import system that allows users to import transactions from bank exports and other financial sources. The system supports user-defined column mappings, enabling users to configure how CSV columns map to transaction fields. Mappings are saved per-source for reuse, and the import includes preview, validation, and duplicate detection.

**Key Integration:** Imported transactions automatically pass through the **auto-categorization rules engine** (Feature 024), ensuring consistent categorization based on user-defined rules. If a CSV includes a category column, that explicit mapping takes precedence; otherwise, the categorization rules evaluate the transaction description/amount to assign categories. Future versions will include AI-assisted column mapping suggestions.

## Problem Statement

Users receive transaction data from various sources (banks, credit cards, financial apps) in CSV format. Each source uses different column names, date formats, and conventions.

### Current State

- No built-in CSV import functionality
- Users must manually enter transactions
- Bulk data entry is time-consuming and error-prone
- Switching from another budgeting tool requires manual migration
- Bank exports vary widely in format and structure

### Target State

- Import transactions directly from CSV files
- Configure custom column mappings for each source
- Save and reuse mappings for recurring imports
- Preview imports before committing
- Detect and handle duplicates intelligently
- Support various date and amount formats
- Validate data before import
- Future: AI suggests column mappings automatically

---

## User Stories

### File Upload

#### US-027-001: Upload CSV File
**As a** user  
**I want to** upload a CSV file for import  
**So that** I can bring in transactions from external sources

**Acceptance Criteria:**
- [ ] Upload button/drop zone on import page
- [ ] Accepts .csv and .txt files
- [ ] Shows file name and row count after upload
- [ ] Parses CSV and shows column headers
- [ ] Handles various delimiters (comma, semicolon, tab)
- [ ] Supports files with or without header row
- [ ] Shows error for invalid/unparseable files
- [ ] Maximum file size: 10MB

#### US-027-002: Select Target Account
**As a** user  
**I want to** select which account to import transactions into  
**So that** transactions are associated with the correct account

**Acceptance Criteria:**
- [ ] Dropdown to select target account
- [ ] Shows account name and type
- [ ] Required before proceeding to mapping
- [ ] Can change account and re-preview

### Column Mapping

#### US-027-003: Map CSV Columns to Transaction Fields
**As a** user  
**I want to** map CSV columns to transaction fields  
**So that** the system knows how to interpret my data

**Acceptance Criteria:**
- [ ] Shows all CSV columns with sample data (first 3-5 rows)
- [ ] Dropdown for each column to select target field
- [ ] Target fields: Date, Description, Amount, Category (optional), Ignore
- [ ] Visual indicator for required fields (Date, Description, Amount)
- [ ] Shows warning if required fields not mapped
- [ ] Can map multiple columns to same field (e.g., combine two description columns)

**Mappable Fields:**
| Field | Required | Notes |
|-------|----------|-------|
| Date | Yes | Transaction date |
| Description | Yes | Transaction description/memo |
| Amount | Yes* | Transaction amount (single column) |
| Debit Amount | Yes* | Expense amount (if split columns) |
| Credit Amount | Yes* | Income amount (if split columns) |
| Category | No | Maps to existing categories |
| Reference/ID | No | External reference number |
| Ignore | N/A | Skip this column |

*Either Amount OR (Debit + Credit) required

#### US-027-004: Configure Amount Handling
**As a** user  
**I want to** specify how amounts are formatted  
**So that** positive/negative values are interpreted correctly

**Acceptance Criteria:**
- [ ] Option: Negative values are expenses (default)
- [ ] Option: Positive values are expenses
- [ ] Option: Separate debit/credit columns
- [ ] Preview shows interpreted sign for each row
- [ ] Handles parentheses for negatives: (100.00)
- [ ] Handles currency symbols: $, €, £
- [ ] Handles thousand separators: 1,000.00 or 1.000,00

#### US-027-005: Configure Date Format
**As a** user  
**I want to** specify the date format used in my CSV  
**So that** dates are parsed correctly

**Acceptance Criteria:**
- [ ] Auto-detect common formats (with confidence indicator)
- [ ] Manual override with format selector
- [ ] Common formats: MM/DD/YYYY, DD/MM/YYYY, YYYY-MM-DD, etc.
- [ ] Preview shows parsed dates
- [ ] Error indicator for unparseable dates

### Saved Mappings

#### US-027-006: Save Column Mapping as Template
**As a** user  
**I want to** save my column mapping configuration  
**So that** I can reuse it for future imports from the same source

**Acceptance Criteria:**
- [ ] "Save Mapping" button after configuring
- [ ] Name the mapping (e.g., "Chase Checking Export")
- [ ] Saves: column mappings, date format, amount handling
- [ ] Mappings stored per-user
- [ ] Can update existing mapping

#### US-027-007: Apply Saved Mapping
**As a** user  
**I want to** apply a previously saved mapping  
**So that** I don't have to reconfigure for recurring imports

**Acceptance Criteria:**
- [ ] Dropdown to select saved mapping after file upload
- [ ] Auto-matches columns by header name
- [ ] Shows warning if columns don't match
- [ ] Can modify applied mapping before import
- [ ] Option to update saved mapping with changes

#### US-027-008: Manage Saved Mappings
**As a** user  
**I want to** view and manage my saved mappings  
**So that** I can organize and clean up old configurations

**Acceptance Criteria:**
- [ ] List of saved mappings in settings or import page
- [ ] Shows mapping name, last used date, column count
- [ ] Can rename mappings
- [ ] Can delete mappings
- [ ] Can duplicate a mapping

### Import Preview

#### US-027-009: Preview Import Before Committing
**As a** user  
**I want to** preview how transactions will be imported  
**So that** I can verify the mapping is correct before committing

**Acceptance Criteria:**
- [ ] Shows table of transactions to be imported
- [ ] Columns: Date, Description, Amount, Category, Status
- [ ] Status indicates: Valid, Warning, Error, Duplicate
- [ ] Can sort and filter preview
- [ ] Shows total count and amount summary
- [ ] Shows breakdown: new, duplicates, errors
- [ ] Can select/deselect individual rows for import

#### US-027-010: Handle Validation Errors
**As a** user  
**I want to** see and fix validation errors before import  
**So that** I don't import bad data

**Acceptance Criteria:**
- [ ] Rows with errors highlighted
- [ ] Error message shown on hover/click
- [ ] Common errors: invalid date, missing amount, unparseable number
- [ ] Can edit values inline in preview
- [ ] Can exclude error rows from import
- [ ] Cannot proceed if required field errors exist

### Duplicate Detection

#### US-027-011: Detect Duplicate Transactions
**As a** user  
**I want to** be warned about potential duplicate imports  
**So that** I don't create duplicate entries

**Acceptance Criteria:**
- [ ] Checks against existing transactions in target account
- [ ] Duplicate criteria: same date + similar amount + similar description
- [ ] Fuzzy matching for description (not exact)
- [ ] Shows matched existing transaction for comparison
- [ ] Options: Import anyway, Skip, Skip all duplicates
- [ ] Duplicates marked in preview with link to existing

#### US-027-012: Configure Duplicate Detection Sensitivity
**As a** user  
**I want to** adjust duplicate detection settings  
**So that** I get the right balance of caution vs. convenience

**Acceptance Criteria:**
- [ ] Date window: exact match, ±1 day, ±3 days
- [ ] Amount tolerance: exact, ±$0.01, ±1%
- [ ] Description matching: exact, contains, fuzzy
- [ ] Settings saved per mapping template
- [ ] Quick toggle: strict/normal/lenient

### Import Execution

#### US-027-013: Execute Import
**As a** user  
**I want to** commit the import after reviewing  
**So that** transactions are created in my account

**Acceptance Criteria:**
- [ ] "Import" button enabled only when valid rows exist
- [ ] Shows confirmation with count and total amount
- [ ] Progress indicator during import
- [ ] Creates transactions in target account
- [ ] Applies explicit category mappings from CSV (if column mapped)
- [ ] Runs auto-categorization rules on uncategorized transactions (Feature 024 integration)
- [ ] Auto-categorization respects rule priority order
- [ ] Shows success summary with count and categorization breakdown
- [ ] Summary shows: X auto-categorized, Y with CSV category, Z uncategorized

#### US-027-015: Auto-Categorization During Import
**As a** user  
**I want** imported transactions to be automatically categorized by my rules  
**So that** I don't have to manually categorize each import

**Acceptance Criteria:**
- [ ] Categorization rules engine runs on each imported transaction
- [ ] Rules evaluated in priority order
- [ ] Matching conditions: description contains/starts with/equals, amount range
- [ ] First matching rule assigns category
- [ ] CSV-provided category overrides auto-categorization (explicit > inferred)
- [ ] Preview shows which transactions will be auto-categorized
- [ ] Preview shows matched rule name for each auto-categorized row
- [ ] User can see category source: "CSV", "Rule: [rule name]", or "Uncategorized"

#### US-027-016: Review Auto-Categorization in Preview
**As a** user  
**I want to** see auto-categorization results in the preview  
**So that** I can verify my rules are working before import

**Acceptance Criteria:**
- [ ] Preview runs categorization rules without saving
- [ ] Shows predicted category for each row
- [ ] Indicates category source (CSV column, rule name, none)
- [ ] Can override predicted category before import
- [ ] Color coding: green (CSV explicit), blue (rule matched), gray (uncategorized)

#### US-027-014: View Import History
**As a** user  
**I want to** see a history of my imports  
**So that** I can track and potentially undo bulk imports

**Acceptance Criteria:**
- [ ] List of past imports with date, file name, count
- [ ] Shows source mapping used
- [ ] Can view transactions from a specific import
- [ ] Can undo/delete an entire import batch
- [ ] Import batch ID stored on transactions

---

## Technical Design

### Architecture Overview

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  Blazor Client  │────▶│   API Endpoints  │────▶│  Import Service │
│  (Import UI)    │     │  (ImportController)│    │  (Business Logic)│
└─────────────────┘     └──────────────────┘     └─────────────────┘
                                                          │
                        ┌─────────────────────────────────┼─────────────────┐
                        ▼                                 ▼                 ▼
                ┌───────────────┐              ┌──────────────────┐  ┌────────────┐
                │ CsvParser     │              │ ImportMapping    │  │ Transaction│
                │ (parse file)  │              │ Repository       │  │ Repository │
                └───────────────┘              └──────────────────┘  └────────────┘
```

### Domain Model

#### ImportMapping Entity

```csharp
public sealed class ImportMapping
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? LastUsedAtUtc { get; private set; }

    // Configuration
    public IReadOnlyList<ColumnMapping> ColumnMappings { get; private set; } = Array.Empty<ColumnMapping>();
    public string DateFormat { get; private set; } = "MM/dd/yyyy";
    public AmountParseMode AmountMode { get; private set; } = AmountParseMode.NegativeIsExpense;
    public DuplicateDetectionSettings DuplicateSettings { get; private set; } = new();

    public static ImportMapping Create(Guid userId, string name, IReadOnlyList<ColumnMapping> mappings);
    public void Update(string name, IReadOnlyList<ColumnMapping> mappings, string dateFormat, AmountParseMode amountMode);
    public void UpdateDuplicateSettings(DuplicateDetectionSettings settings);
    public void MarkUsed();
}

public sealed record ColumnMapping
{
    public int ColumnIndex { get; init; }
    public string ColumnHeader { get; init; } = string.Empty;
    public ImportField TargetField { get; init; }
    public string? TransformExpression { get; init; }  // Future: for combining/transforming
}

public enum ImportField
{
    Ignore,
    Date,
    Description,
    Amount,
    DebitAmount,
    CreditAmount,
    Category,
    Reference
}

public enum AmountParseMode
{
    NegativeIsExpense,    // -50 = expense, +50 = income
    PositiveIsExpense,    // +50 = expense, -50 = income
    SeparateColumns       // Debit column = expense, Credit column = income
}

public sealed record DuplicateDetectionSettings
{
    public int DateWindowDays { get; init; } = 1;
    public decimal AmountTolerancePercent { get; init; } = 0;
    public DescriptionMatchMode DescriptionMode { get; init; } = DescriptionMatchMode.Fuzzy;
}

public enum DescriptionMatchMode
{
    Exact,
    Contains,
    Fuzzy
}
```

#### ImportBatch Entity

```csharp
public sealed class ImportBatch
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? MappingId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public int TotalRows { get; private set; }
    public int ImportedCount { get; private set; }
    public int SkippedCount { get; private set; }
    public int ErrorCount { get; private set; }
    public DateTime ImportedAtUtc { get; private set; }
    public ImportBatchStatus Status { get; private set; }

    public static ImportBatch Create(Guid userId, Guid accountId, string fileName, int totalRows, Guid? mappingId);
    public void Complete(int imported, int skipped, int errors);
    public void MarkDeleted();
}

public enum ImportBatchStatus
{
    Completed,
    PartiallyCompleted,
    Deleted
}
```

#### Transaction Extension

Add to existing `Transaction` entity:
```csharp
// Add to Transaction
public Guid? ImportBatchId { get; private set; }
public string? ExternalReference { get; private set; }

public void SetImportBatch(Guid batchId, string? externalReference = null);
```

### Service Interfaces

```csharp
public interface ICsvParserService
{
    /// <summary>
    /// Parses a CSV file and returns the raw data with detected settings.
    /// </summary>
    Task<CsvParseResult> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default);
}

public sealed record CsvParseResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> Headers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = Array.Empty<IReadOnlyList<string>>();
    public char DetectedDelimiter { get; init; }
    public bool HasHeaderRow { get; init; }
    public int RowCount { get; init; }
}

public interface IImportMappingService
{
    Task<IReadOnlyList<ImportMappingDto>> GetUserMappingsAsync(CancellationToken ct = default);
    Task<ImportMappingDto> GetMappingAsync(Guid id, CancellationToken ct = default);
    Task<ImportMappingDto> CreateMappingAsync(CreateImportMappingRequest request, CancellationToken ct = default);
    Task<ImportMappingDto> UpdateMappingAsync(Guid id, UpdateImportMappingRequest request, CancellationToken ct = default);
    Task DeleteMappingAsync(Guid id, CancellationToken ct = default);
    Task<ImportMappingDto?> SuggestMappingAsync(IReadOnlyList<string> headers, CancellationToken ct = default);
}

public interface IImportService
{
    /// <summary>
    /// Validates and previews import based on mapping configuration.
    /// </summary>
    Task<ImportPreviewResult> PreviewAsync(ImportPreviewRequest request, CancellationToken ct = default);

    /// <summary>
    /// Executes the import, creating transactions.
    /// </summary>
    Task<ImportResult> ExecuteAsync(ImportExecuteRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets import history for the user.
    /// </summary>
    Task<IReadOnlyList<ImportBatchDto>> GetImportHistoryAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes all transactions from an import batch.
    /// </summary>
    Task<int> DeleteImportBatchAsync(Guid batchId, CancellationToken ct = default);
}

public sealed record ImportPreviewRequest
{
    public Guid AccountId { get; init; }
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = Array.Empty<IReadOnlyList<string>>();
    public IReadOnlyList<ColumnMapping> Mappings { get; init; } = Array.Empty<ColumnMapping>();
    public string DateFormat { get; init; } = "MM/dd/yyyy";
    public AmountParseMode AmountMode { get; init; }
    public DuplicateDetectionSettings DuplicateSettings { get; init; } = new();
}

public sealed record ImportPreviewResult
{
    public IReadOnlyList<ImportPreviewRow> Rows { get; init; } = Array.Empty<ImportPreviewRow>();
    public int ValidCount { get; init; }
    public int WarningCount { get; init; }
    public int ErrorCount { get; init; }
    public int DuplicateCount { get; init; }
    public decimal TotalAmount { get; init; }
}

public sealed record ImportPreviewRow
{
    public int RowIndex { get; init; }
    public DateOnly? Date { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal? Amount { get; init; }
    public string? Category { get; init; }
    public Guid? CategoryId { get; init; }
    public CategorySource CategorySource { get; init; }
    public string? MatchedRuleName { get; init; }  // Rule name if auto-categorized
    public string? Reference { get; init; }
    public ImportRowStatus Status { get; init; }
    public string? StatusMessage { get; init; }
    public Guid? DuplicateOfTransactionId { get; init; }
    public bool IsSelected { get; init; } = true;
}

public enum ImportRowStatus
{
    Valid,
    Warning,
    Error,
    Duplicate
}

public enum CategorySource
{
    None,           // Uncategorized
    CsvColumn,      // Category from mapped CSV column
    AutoRule,       // Matched categorization rule
    UserOverride    // User manually set in preview
}

public sealed record ImportExecuteRequest
{
    public Guid AccountId { get; init; }
    public Guid? MappingId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public IReadOnlyList<ImportTransactionData> Transactions { get; init; } = Array.Empty<ImportTransactionData>();
}

public sealed record ImportTransactionData
{
    public DateOnly Date { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public Guid? CategoryId { get; init; }
    public string? Reference { get; init; }
}

public sealed record ImportResult
{
    public Guid BatchId { get; init; }
    public int ImportedCount { get; init; }
    public int SkippedCount { get; init; }
    public int ErrorCount { get; init; }
    public IReadOnlyList<Guid> CreatedTransactionIds { get; init; } = Array.Empty<Guid>();

    // Categorization statistics
    public int AutoCategorizedCount { get; init; }
    public int CsvCategorizedCount { get; init; }
    public int UncategorizedCount { get; init; }
}
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/import/parse` | Upload and parse CSV file |
| GET | `/api/v1/import/mappings` | Get user's saved mappings |
| GET | `/api/v1/import/mappings/{id}` | Get specific mapping |
| POST | `/api/v1/import/mappings` | Create new mapping |
| PUT | `/api/v1/import/mappings/{id}` | Update mapping |
| DELETE | `/api/v1/import/mappings/{id}` | Delete mapping |
| POST | `/api/v1/import/preview` | Preview import with mapping |
| POST | `/api/v1/import/execute` | Execute import |
| GET | `/api/v1/import/history` | Get import history |
| DELETE | `/api/v1/import/batches/{id}` | Delete import batch |

### Database Changes

**New table: `ImportMappings`**

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| UserId | uuid | NOT NULL, INDEX |
| Name | varchar(200) | NOT NULL |
| ColumnMappingsJson | jsonb | NOT NULL |
| DateFormat | varchar(50) | NOT NULL |
| AmountMode | varchar(20) | NOT NULL |
| DuplicateSettingsJson | jsonb | NOT NULL |
| CreatedAtUtc | timestamp | NOT NULL |
| UpdatedAtUtc | timestamp | NOT NULL |
| LastUsedAtUtc | timestamp | NULL |

**New table: `ImportBatches`**

| Column | Type | Constraints |
|--------|------|-------------|
| Id | uuid | PK |
| UserId | uuid | NOT NULL, INDEX |
| AccountId | uuid | FK, NOT NULL |
| MappingId | uuid | FK, NULL |
| FileName | varchar(500) | NOT NULL |
| TotalRows | int | NOT NULL |
| ImportedCount | int | NOT NULL |
| SkippedCount | int | NOT NULL |
| ErrorCount | int | NOT NULL |
| ImportedAtUtc | timestamp | NOT NULL |
| Status | varchar(20) | NOT NULL |

**Modify table: `Transactions`**

| Column | Type | Constraints |
|--------|------|-------------|
| ImportBatchId | uuid | FK, NULL, INDEX |
| ExternalReference | varchar(100) | NULL |

### UI Components

#### New Pages

- `Import.razor` - Main import wizard page

#### New Components

| Component | Description |
|-----------|-------------|
| `FileUploadZone.razor` | Drag-and-drop file upload area |
| `CsvPreviewTable.razor` | Shows raw CSV data with headers |
| `ColumnMappingEditor.razor` | Configure column-to-field mappings |
| `ColumnMappingRow.razor` | Single column mapping with dropdown |
| `DateFormatSelector.razor` | Date format picker with preview |
| `AmountModeSelector.razor` | Amount handling configuration |
| `ImportPreviewTable.razor` | Preview table with validation status |
| `ImportPreviewRow.razor` | Single row with status indicators |
| `DuplicateWarningCard.razor` | Shows duplicate match details |
| `ImportSummaryCard.razor` | Summary of import results |
| `SavedMappingSelector.razor` | Dropdown to select saved mapping |
| `SaveMappingDialog.razor` | Dialog to name and save mapping |
| `ImportHistoryList.razor` | List of past imports |

#### Import Wizard Flow

```
┌─────────────────────────────────────────────────────────────────┐
│  Step 1: Upload                                                 │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                                                             ││
│  │     📁 Drop CSV file here or click to browse               ││
│  │                                                             ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│  Account: [Primary Checking ▼]                                  │
│  Saved Mapping: [-- Select or create new -- ▼]                  │
│                                                                 │
│                                          [Next: Configure ▶]   │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  Step 2: Configure Mapping                                      │
│                                                                 │
│  CSV Columns              Map To            Sample Data         │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ Post Date            [Date ▼]           01/15/2026          ││
│  │ Description          [Description ▼]    WALMART STORE #123  ││
│  │ Amount               [Amount ▼]         -45.99              ││
│  │ Balance              [Ignore ▼]         1,234.56            ││
│  │ Category             [Category ▼]       Groceries           ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│  Date Format: [MM/dd/yyyy ▼]  Detected: ✓                       │
│  Amount Mode: [Negative = Expense ▼]                            │
│                                                                 │
│  [💾 Save Mapping]                    [◀ Back] [Next: Preview ▶]│
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────┐
│  Step 3: Preview & Import                                                       │
│                                                                                 │
│  Summary: 47 valid, 2 duplicates, 1 error                                       │
│  Categories: 25 auto-categorized, 15 from CSV, 7 uncategorized                  │
│                                                                                 │
│  ☑ │ Date       │ Description          │ Amount   │ Category      │ Status     │
│  ──┼────────────┼──────────────────────┼──────────┼───────────────┼────────────│
│  ☑ │ 01/15/2026 │ WALMART STORE #123   │ -$45.99  │ 🔵 Groceries  │ ✓ Valid    │
│  ☑ │ 01/14/2026 │ AMAZON.COM           │ -$29.99  │ 🟢 Shopping   │ ✓ Valid    │
│  ☐ │ 01/14/2026 │ NETFLIX              │ -$15.99  │ 🔵 Streaming  │ ⚠ Duplicate│
│  ☐ │ 01/13/2026 │ Invalid row          │          │ ⚪ —          │ ✗ Error    │
│  ☑ │ 01/12/2026 │ UNKNOWN MERCHANT     │ -$12.00  │ ⚪ —          │ ✓ Valid    │
│  └─────────────────────────────────────────────────────────────────────────────┘│
│                                                                                 │
│  Legend: 🟢 CSV Category  🔵 Auto-Rule  ⚪ Uncategorized                         │
│                                                                                 │
│  [Skip All Duplicates] [Include All]                                            │
│                                                                                 │
│  Import 47 transactions totaling -$1,234.56                                     │
│                                                                                 │
│                                              [◀ Back] [✓ Import Now]           │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Auto-Categorization Integration

This feature integrates with the **Categorization Rules Engine** (Feature 024) to automatically categorize imported transactions.

### Integration Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Import Service                                     │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  PreviewAsync()                                                       │  │
│  │                                                                       │  │
│  │  1. Parse CSV data                                                    │  │
│  │  2. Apply column mappings                                             │  │
│  │  3. Check for CSV category column ──▶ If present, use CSV category   │  │
│  │  4. If no CSV category:                                               │  │
│  │     └──▶ ICategorizationRuleService.EvaluateAsync(description, amount)│  │
│  │          └──▶ Returns matched rule & category (or null)              │  │
│  │  5. Set CategorySource (CsvColumn | AutoRule | None)                  │  │
│  │  6. Return preview rows with category info                            │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  ExecuteAsync()                                                       │  │
│  │                                                                       │  │
│  │  1. Create transactions with category from preview                    │  │
│  │  2. Category already determined (CSV > UserOverride > AutoRule)       │  │
│  │  3. Track MatchedRuleId on transaction (analytics)                    │  │
│  │  4. Return stats: {AutoCategorized, CsvCategorized, Uncategorized}    │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Category Priority (Highest to Lowest)

| Priority | Source | Description |
|----------|--------|-------------|
| 1 | User Override | User manually changed category in preview |
| 2 | CSV Column | Category mapped from CSV file column |
| 3 | Auto Rule | Matched categorization rule |
| 4 | None | No category assigned (uncategorized) |

### Service Dependency

The `ImportService` depends on `ICategorizationRuleService`:

```csharp
public interface ICategorizationRuleService
{
    /// <summary>
    /// Evaluates all rules for a transaction and returns the best match.
    /// </summary>
    Task<RuleMatchResult?> EvaluateAsync(
        string description, 
        decimal amount, 
        Guid? accountId = null,
        CancellationToken ct = default);
}

public sealed record RuleMatchResult
{
    public Guid RuleId { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
}
```

### Preview Behavior

During preview, categorization rules are evaluated **without saving**:
- Each row shows predicted category
- Category source indicated visually (🟢 CSV, 🔵 Rule, ⚪ None)
- Matched rule name shown on hover/tooltip
- User can override any category before import

### Import Execution Behavior

During import, the final category is applied:
- Uses category from preview (respects overrides)
- Records `MatchedRuleId` on transaction if auto-categorized
- Calculates categorization statistics for result summary
- Does NOT re-evaluate rules (uses preview results)

### Existing Rule Types Supported

The import leverages all existing categorization rule types:

| Rule Type | How It Matches |
|-----------|----------------|
| Description Contains | Transaction description contains text |
| Description Starts With | Transaction description starts with text |
| Description Equals | Exact match (case-insensitive) |
| Amount Range | Amount within min/max range |
| Combined | Multiple conditions must match |

---

## Implementation Plan

### Phase 1: Domain Model & CSV Parsing ✅

**Objective:** Establish entities and basic CSV parsing

**Status:** Completed (2026-01-18)

**Tasks:**
- [x] Write unit tests for `ImportMapping` entity
- [x] Write unit tests for `ImportBatch` entity
- [x] Implement `ImportMapping` entity with factory methods
- [x] Implement `ImportBatch` entity
- [x] Add `ImportBatchId` and `ExternalReference` to `Transaction`
- [x] Create `ICsvParserService` interface
- [x] Implement `CsvParserService` with delimiter detection
- [x] Write unit tests for CSV parsing (various formats)

**Deliverables:**
- Domain: `ImportMapping`, `ImportBatch`, `ColumnMapping`, `DuplicateDetectionSettings`
- Enums: `ImportField`, `AmountParseMode`, `DescriptionMatchMode`, `ImportBatchStatus`
- Transaction: Added `ImportBatchId`, `ExternalReference`, `IsFromImport`, `SetImportBatch()`
- Application: `ICsvParserService`, `CsvParserService`, `CsvParseResult`
- Tests: 73 new tests (all passing)

---

### Phase 2: Infrastructure - Repositories & Migrations ✅

**Objective:** Implement persistence layer

**Status:** Completed (2026-01-18)

**Tasks:**
- [x] Create EF Core configuration for `ImportMapping`
- [x] Create EF Core configuration for `ImportBatch`
- [x] Update `Transaction` configuration for new columns
- [x] Add database migration
- [x] Implement `IImportMappingRepository`
- [x] Implement `IImportBatchRepository`
- [x] Write integration tests

**Deliverables:**
- Infrastructure: `ImportMappingConfiguration`, `ImportBatchConfiguration`
- Repository Interfaces: `IImportMappingRepository`, `IImportBatchRepository`
- Repository Implementations: `ImportMappingRepository`, `ImportBatchRepository`
- Migration: `AddCsvImportSupport` (ImportMappings, ImportBatches tables + Transaction columns)
- Tests: 20 integration tests (all passing)

---

### Phase 3: Import Service - Preview & Validation ✅

**Objective:** Implement preview and validation logic

**Status:** Completed (2026-01-18)

**Tasks:**
- [x] Implement `IImportMappingService`
- [x] Implement `IImportService.PreviewAsync`
- [x] Date parsing with multiple formats
- [x] Amount parsing with various formats
- [x] Duplicate detection logic
- [x] Category matching by name (from CSV column)
- [x] Integrate `ICategorizationRuleRepository` for auto-categorization
- [x] Apply categorization rules to preview rows (without saving)
- [x] Track category source (CSV, Rule, None) for each row
- [x] Write unit tests for categorization integration

**Deliverables:**
- Domain: `CategorySource`, `ImportRowStatus` enums; added `StartsWith` to `DescriptionMatchMode`; added `AbsoluteExpense`/`AbsoluteIncome` to `AmountParseMode`
- Contracts: `ImportDtos.cs` (ImportPreviewRequest, ImportPreviewResult, ImportPreviewRow, ImportExecuteRequest, etc.)
- Application: `IImportMappingService`, `ImportMappingService`, `IImportService`, `ImportService`
- Infrastructure: Added `GetForDuplicateDetectionAsync`, `GetByImportBatchAsync` to `ITransactionRepository`
- Tests: 33 new unit tests for import services (all passing)

---

### Phase 4: Import Service - Execution

**Objective:** Implement actual import execution

**Tasks:**
- [ ] Implement `IImportService.ExecuteAsync`
- [ ] Create transactions in batch
- [ ] Apply category from preview (respects source priority):
  - CSV explicit category (highest priority)
  - User override from preview
  - Auto-categorization rule match
  - Uncategorized (fallback)
- [ ] Use `ICategorizationRuleService.ApplyRulesAsync()` for rule matching
- [ ] Record matched rule ID on transaction (for analytics)
- [ ] Record import batch
- [ ] Track categorization statistics in batch result
- [ ] Implement `DeleteImportBatchAsync`
- [ ] Implement `GetImportHistoryAsync`
- [ ] Write unit tests for categorization during import

---

### Phase 5: API Endpoints

**Objective:** Expose import functionality via REST API

**Tasks:**
- [ ] Create `ImportController` with all endpoints
- [ ] File upload handling with size limits
- [ ] Request/response DTOs
- [ ] Validation and error handling
- [ ] Write API integration tests

---

### Phase 6: Blazor UI - File Upload & Mapping

**Objective:** Build first half of import wizard

**Tasks:**
- [ ] Create `Import.razor` page with wizard structure
- [ ] Implement `FileUploadZone` component
- [ ] Implement `CsvPreviewTable` component
- [ ] Implement `ColumnMappingEditor` component
- [ ] Implement `DateFormatSelector` component
- [ ] Implement `AmountModeSelector` component
- [ ] Implement `SavedMappingSelector` component
- [ ] Create `IImportApiService` client service

---

### Phase 7: Blazor UI - Preview & Execute

**Objective:** Complete import wizard

**Tasks:**
- [ ] Implement `ImportPreviewTable` component
- [ ] Implement `DuplicateWarningCard` component
- [ ] Implement row selection logic
- [ ] Implement import execution with progress
- [ ] Implement `ImportSummaryCard` component
- [ ] Add import page to navigation

---

### Phase 8: Import History & Management

**Objective:** Add history and undo capability

**Tasks:**
- [ ] Implement `ImportHistoryList` component
- [ ] Add history section to import page or settings
- [ ] Implement batch delete confirmation
- [ ] Implement saved mapping management UI

---

## Testing Strategy

### Unit Tests

- `ImportMappingTests` - Entity creation, validation
- `ImportBatchTests` - Entity state transitions
- `CsvParserServiceTests` - Various CSV formats, delimiters, edge cases
- `ImportServiceTests` - Preview validation, duplicate detection, execution
- `ImportCategorizationTests` - Auto-categorization integration:
  - Rules applied in priority order during preview
  - CSV category takes precedence over rule match
  - User override takes precedence over both
  - Uncategorized when no match
  - Correct category source tracked

### Integration Tests

- `ImportMappingRepositoryTests` - CRUD operations
- `ImportBatchRepositoryTests` - CRUD with transaction links
- `ImportControllerTests` - API endpoint tests

### Manual Testing Checklist

- [ ] Upload CSV from major banks (Chase, Bank of America, Wells Fargo)
- [ ] Upload CSV with various delimiters (comma, semicolon, tab)
- [ ] Test different date formats
- [ ] Test negative amounts in parentheses
- [ ] Test split debit/credit columns
- [ ] Create and reuse saved mapping
- [ ] Verify duplicate detection works
- [ ] Import batch and verify transactions created
- [ ] Delete import batch and verify transactions removed

**Auto-Categorization Testing:**
- [ ] Import without category column - verify rules apply
- [ ] Import with category column - verify CSV category used
- [ ] Verify rule priority respected (higher priority wins)
- [ ] Preview shows correct category source indicators (🟢/🔵/⚪)
- [ ] Import summary shows categorization breakdown
- [ ] Create rule, import CSV, verify new transactions categorized
- [ ] Import with mix of matched/unmatched - verify counts correct

---

## Security Considerations

- File uploads validated for type and size
- CSV content sanitized before display
- User can only access their own mappings and batches
- Import operations scoped to user's accounts
- No sensitive data logged during import

---

## Performance Considerations

- Stream large files instead of loading entirely in memory
- Batch insert transactions (100 at a time)
- Preview limited to first 1000 rows for large files
- Duplicate detection uses database indexes efficiently
- Consider background job for very large imports (>5000 rows)

---

## Future Enhancements (Post-MVP)

- **AI Column Mapping**: Use AI to suggest column mappings based on headers and sample data
- **AI Category Suggestions**: Suggest categories for uncategorized imports
- **Import Scheduling**: Schedule recurring imports from file drops
- **Bank Connections**: Direct import via Plaid or similar
- **Multi-account Import**: Single file with transactions for multiple accounts
- **QIF/OFX Support**: Import other financial file formats
- **Export Mappings**: Share mapping configurations
- **Bulk Edit**: Edit multiple imported transactions at once

---

## References

- [Feature 024: Auto-Categorization Rules Engine](./024-auto-categorization-rules-engine.md) - Applied during import
- [Feature 025: AI-Powered Rule Suggestions](./025-ai-rule-suggestions.md) - Future AI integration
- [Feature Template](./FEATURE-TEMPLATE.md)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-18 | Initial draft | @copilot |
| 2026-01-18 | Added auto-categorization integration (Feature 024) | @copilot |
| 2026-01-18 | Completed Phase 1: Domain entities, CSV parser service, 73 tests | @copilot |
