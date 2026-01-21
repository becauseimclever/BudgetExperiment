# Feature 030: CSV Import Enhancements - Skip Rows & Debit/Credit Indicators

> **Status:** 🚧 In Progress  
> **Branch:** `feature/030-csv-import-enhancements`  
> **Started:** 2026-01-20

## Overview

Enhance the existing CSV import functionality (Feature 027) to handle two common bank export variations:

1. **Skip Header Rows**: Banks often prepend metadata rows (account info, date range, starting balance) before the actual transaction data. Users need the ability to skip a configurable number of rows before the data begins.

2. **Debit/Credit Indicator Column**: Some banks export all amounts as positive numbers in a single column, with a separate column indicating whether the transaction is a debit (expense) or credit (income). Users need to map this indicator column and configure which values represent debits vs credits.

These enhancements improve import compatibility with a wider variety of bank export formats, reducing manual data cleanup.

## Problem Statement

### Current State

- The CSV import assumes data starts at row 1 (or row 2 if headers are present)
- Amount handling supports: negative-is-expense, positive-is-expense, separate debit/credit amount columns, absolute expense, absolute income
- No support for banks that add metadata rows before the header/data
- No support for banks that use a single amount column with a separate debit/credit indicator

### Target State

- Users can specify how many rows to skip before the header/data begins
- Users can map a debit/credit indicator column and configure the indicator values
- Import mappings save these new settings for reuse
- Preview accurately reflects skip rows and indicator column handling

---

## User Stories

### Skip Rows

#### US-030-001: Configure Rows to Skip
**As a** user  
**I want to** specify how many rows to skip at the beginning of my CSV file  
**So that** I can import files that have metadata rows before the actual data

**Acceptance Criteria:**
- [ ] Numeric input field to specify rows to skip (0-100, default 0)
- [ ] Preview updates to show data starting from the specified row
- [ ] Skipped rows are displayed separately (collapsible) for reference
- [ ] Setting is saved with the import mapping template
- [ ] Handles files where skip rows exceed total rows gracefully

#### US-030-002: Auto-Detect Skippable Rows
**As a** user  
**I want** the system to suggest how many rows to skip  
**So that** I don't have to manually count metadata rows

**Acceptance Criteria:**
- [ ] System analyzes first N rows for patterns indicating metadata
- [ ] Detection heuristics: fewer columns than expected, non-tabular structure, common keywords (Account, Balance, Date Range, etc.)
- [ ] Shows suggestion with confidence indicator
- [ ] User can accept suggestion or override
- [ ] Auto-detection runs on file upload before mapping

### Debit/Credit Indicator Column

#### US-030-003: Map Debit/Credit Indicator Column
**As a** user  
**I want to** map a column that indicates whether a transaction is a debit or credit  
**So that** I can correctly import amounts from banks that use this format

**Acceptance Criteria:**
- [ ] New target field option: "Debit/Credit Indicator"
- [ ] When selected, prompts for indicator value configuration
- [ ] Works with a single Amount column (required when using indicator)
- [ ] Shows warning if indicator mapped without Amount column
- [ ] Shows warning if Amount not mapped when indicator is used

#### US-030-004: Configure Indicator Values
**As a** user  
**I want to** specify which indicator values represent debits vs credits  
**So that** amounts are correctly signed during import

**Acceptance Criteria:**
- [ ] Text input for debit indicator values (comma-separated, e.g., "Debit,DR,D")
- [ ] Text input for credit indicator values (comma-separated, e.g., "Credit,CR,C")
- [ ] Case-insensitive matching by default with toggle for case-sensitive
- [ ] Preview shows sample indicator values from data for reference
- [ ] Error if a row's indicator doesn't match configured values
- [ ] Settings saved with import mapping template

#### US-030-005: Preview Indicator-Based Amounts
**As a** user  
**I want to** see how amounts will be interpreted based on the indicator column  
**So that** I can verify the configuration is correct before importing

**Acceptance Criteria:**
- [ ] Preview shows original amount, indicator value, and resulting signed amount
- [ ] Expenses shown as negative, income as positive
- [ ] Rows with unrecognized indicators highlighted as warnings
- [ ] Total debit/credit summary shown

---

## Technical Design

### Architecture Changes

Extends existing CSV import infrastructure (Feature 027). No new projects or major architectural changes required.

### Domain Model Changes

#### New: SkipRowsSettings (Value Object)

```csharp
/// <summary>
/// Settings for skipping rows at the beginning of a CSV file.
/// </summary>
public sealed record SkipRowsSettings
{
    /// <summary>
    /// Maximum number of rows that can be skipped.
    /// </summary>
    public const int MaxSkipRows = 100;

    /// <summary>
    /// Gets the number of rows to skip before the data/header begins.
    /// </summary>
    public int RowsToSkip { get; init; }

    /// <summary>
    /// Creates skip rows settings.
    /// </summary>
    /// <param name="rowsToSkip">Number of rows to skip (0-100).</param>
    /// <returns>A new <see cref="SkipRowsSettings"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when rowsToSkip is out of range.</exception>
    public static SkipRowsSettings Create(int rowsToSkip)
    {
        if (rowsToSkip < 0 || rowsToSkip > MaxSkipRows)
        {
            throw new DomainException($"Rows to skip must be between 0 and {MaxSkipRows}.");
        }

        return new SkipRowsSettings { RowsToSkip = rowsToSkip };
    }

    /// <summary>
    /// Gets default settings (no rows skipped).
    /// </summary>
    public static SkipRowsSettings Default => new() { RowsToSkip = 0 };
}
```

#### New: DebitCreditIndicatorSettings (Value Object)

```csharp
/// <summary>
/// Settings for interpreting a debit/credit indicator column.
/// </summary>
public sealed record DebitCreditIndicatorSettings
{
    /// <summary>
    /// Gets the column index of the indicator column (-1 if not used).
    /// </summary>
    public int IndicatorColumnIndex { get; init; } = -1;

    /// <summary>
    /// Gets the values that indicate a debit (expense) transaction.
    /// </summary>
    public IReadOnlyList<string> DebitIndicators { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the values that indicate a credit (income) transaction.
    /// </summary>
    public IReadOnlyList<string> CreditIndicators { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets a value indicating whether indicator matching is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; init; }

    /// <summary>
    /// Gets a value indicating whether this indicator is enabled.
    /// </summary>
    public bool IsEnabled => IndicatorColumnIndex >= 0 
        && DebitIndicators.Count > 0 
        && CreditIndicators.Count > 0;

    /// <summary>
    /// Creates debit/credit indicator settings.
    /// </summary>
    /// <param name="columnIndex">The indicator column index.</param>
    /// <param name="debitIndicators">Values indicating debit transactions.</param>
    /// <param name="creditIndicators">Values indicating credit transactions.</param>
    /// <param name="caseSensitive">Whether matching is case-sensitive.</param>
    /// <returns>A new <see cref="DebitCreditIndicatorSettings"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static DebitCreditIndicatorSettings Create(
        int columnIndex,
        IReadOnlyList<string> debitIndicators,
        IReadOnlyList<string> creditIndicators,
        bool caseSensitive = false)
    {
        if (columnIndex < 0)
        {
            throw new DomainException("Column index must be non-negative.");
        }

        if (debitIndicators == null || debitIndicators.Count == 0)
        {
            throw new DomainException("At least one debit indicator value is required.");
        }

        if (creditIndicators == null || creditIndicators.Count == 0)
        {
            throw new DomainException("At least one credit indicator value is required.");
        }

        // Validate no overlap between debit and credit indicators
        var comparison = caseSensitive 
            ? StringComparer.Ordinal 
            : StringComparer.OrdinalIgnoreCase;
        
        var debitSet = new HashSet<string>(debitIndicators.Select(d => d.Trim()), comparison);
        var creditSet = new HashSet<string>(creditIndicators.Select(c => c.Trim()), comparison);
        
        if (debitSet.Overlaps(creditSet))
        {
            throw new DomainException("Debit and credit indicators cannot overlap.");
        }

        return new DebitCreditIndicatorSettings
        {
            IndicatorColumnIndex = columnIndex,
            DebitIndicators = debitIndicators.Select(d => d.Trim()).ToList(),
            CreditIndicators = creditIndicators.Select(c => c.Trim()).ToList(),
            CaseSensitive = caseSensitive,
        };
    }

    /// <summary>
    /// Gets disabled settings (no indicator column).
    /// </summary>
    public static DebitCreditIndicatorSettings Disabled => new();

    /// <summary>
    /// Determines the sign multiplier for an indicator value.
    /// </summary>
    /// <param name="indicatorValue">The indicator value from the CSV.</param>
    /// <returns>-1 for debit, 1 for credit, null if not matched.</returns>
    public int? GetSignMultiplier(string indicatorValue)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(indicatorValue))
        {
            return null;
        }

        var comparison = CaseSensitive 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;

        var trimmedValue = indicatorValue.Trim();

        if (DebitIndicators.Any(d => string.Equals(d, trimmedValue, comparison)))
        {
            return -1; // Debit = expense = negative
        }

        if (CreditIndicators.Any(c => string.Equals(c, trimmedValue, comparison)))
        {
            return 1; // Credit = income = positive
        }

        return null; // Unrecognized indicator
    }
}
```

#### Update: AmountParseMode Enum

Add new value:

```csharp
/// <summary>
/// Use a separate indicator column to determine if amount is debit or credit.
/// Amount column contains absolute values; sign determined by indicator.
/// </summary>
IndicatorColumn = 5,
```

#### Update: ImportField Enum

Add new value:

```csharp
/// <summary>
/// Column indicating whether transaction is debit or credit.
/// </summary>
DebitCreditIndicator = 8,
```

#### Update: ImportMapping Entity

Add new properties:

```csharp
/// <summary>
/// Gets the skip rows settings for this mapping.
/// </summary>
public SkipRowsSettings SkipRowsSettings { get; private set; } = SkipRowsSettings.Default;

/// <summary>
/// Gets the debit/credit indicator settings for this mapping.
/// </summary>
public DebitCreditIndicatorSettings IndicatorSettings { get; private set; } = DebitCreditIndicatorSettings.Disabled;

/// <summary>
/// Updates the skip rows settings.
/// </summary>
/// <param name="settings">The new skip rows settings.</param>
public void UpdateSkipRowsSettings(SkipRowsSettings settings)
{
    ArgumentNullException.ThrowIfNull(settings);
    SkipRowsSettings = settings;
    UpdatedAtUtc = DateTime.UtcNow;
}

/// <summary>
/// Updates the debit/credit indicator settings.
/// </summary>
/// <param name="settings">The new indicator settings.</param>
public void UpdateIndicatorSettings(DebitCreditIndicatorSettings settings)
{
    ArgumentNullException.ThrowIfNull(settings);
    
    // Validate compatibility with AmountMode
    if (settings.IsEnabled && AmountMode != AmountParseMode.IndicatorColumn)
    {
        throw new DomainException("Amount mode must be IndicatorColumn when indicator settings are enabled.");
    }
    
    IndicatorSettings = settings;
    UpdatedAtUtc = DateTime.UtcNow;
}
```

### DTOs

#### CreateImportMappingDto / UpdateImportMappingDto

Add properties:

```csharp
/// <summary>
/// Gets or sets the number of rows to skip at the beginning of the file.
/// </summary>
public int RowsToSkip { get; set; }

/// <summary>
/// Gets or sets the debit/credit indicator settings.
/// </summary>
public DebitCreditIndicatorSettingsDto? IndicatorSettings { get; set; }
```

#### New: DebitCreditIndicatorSettingsDto

```csharp
/// <summary>
/// DTO for debit/credit indicator settings.
/// </summary>
public sealed record DebitCreditIndicatorSettingsDto
{
    /// <summary>
    /// Gets or sets the column index of the indicator (-1 if disabled).
    /// </summary>
    public int ColumnIndex { get; set; } = -1;

    /// <summary>
    /// Gets or sets the comma-separated debit indicator values.
    /// </summary>
    public string DebitIndicators { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the comma-separated credit indicator values.
    /// </summary>
    public string CreditIndicators { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether matching is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; }
}
```

#### ImportMappingDto

Add properties:

```csharp
/// <summary>
/// Gets or sets the number of rows to skip at the beginning of the file.
/// </summary>
public int RowsToSkip { get; set; }

/// <summary>
/// Gets or sets the debit/credit indicator settings.
/// </summary>
public DebitCreditIndicatorSettingsDto? IndicatorSettings { get; set; }
```

### Service Changes

#### CsvParserService

Update to support skip rows:

```csharp
/// <summary>
/// Parses a CSV file with the option to skip initial rows.
/// </summary>
/// <param name="content">The CSV content.</param>
/// <param name="rowsToSkip">Number of rows to skip before header/data.</param>
/// <returns>The parse result.</returns>
public CsvParseResult Parse(string content, int rowsToSkip = 0);
```

#### ImportPreviewService

Update to apply indicator column logic:

```csharp
/// <summary>
/// Interprets an amount based on indicator settings.
/// </summary>
/// <param name="rawAmount">The raw amount value from CSV.</param>
/// <param name="indicatorValue">The indicator column value.</param>
/// <param name="settings">The indicator settings.</param>
/// <returns>The signed amount, or null if parsing failed.</returns>
public decimal? InterpretAmountWithIndicator(
    string rawAmount, 
    string indicatorValue, 
    DebitCreditIndicatorSettings settings);
```

### Database Changes

#### Migration: Add SkipRows and Indicator columns to ImportMappings

```sql
ALTER TABLE "ImportMappings" ADD COLUMN "RowsToSkip" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "ImportMappings" ADD COLUMN "IndicatorColumnIndex" INTEGER NOT NULL DEFAULT -1;
ALTER TABLE "ImportMappings" ADD COLUMN "DebitIndicators" TEXT NOT NULL DEFAULT '';
ALTER TABLE "ImportMappings" ADD COLUMN "CreditIndicators" TEXT NOT NULL DEFAULT '';
ALTER TABLE "ImportMappings" ADD COLUMN "IndicatorCaseSensitive" BOOLEAN NOT NULL DEFAULT FALSE;
```

### UI Components

#### ImportMappingEditor.razor

Update to include:
- Rows to skip numeric input field
- Debit/Credit Indicator column mapping option
- Indicator value configuration fields (when indicator column mapped)

#### ImportPreview.razor

Update to:
- Show skipped rows in collapsible section
- Display indicator column values alongside amounts
- Show signed interpretation of amounts when indicator mode active

---

## Implementation Plan

### Phase 1: Domain Value Objects

**Objective:** Create the new value objects for skip rows and indicator settings.

**Tasks:**
- [ ] Write unit tests for `SkipRowsSettings` value object
- [ ] Implement `SkipRowsSettings` value object
- [ ] Write unit tests for `DebitCreditIndicatorSettings` value object
- [ ] Implement `DebitCreditIndicatorSettings` value object
- [ ] Add `IndicatorColumn` to `AmountParseMode` enum
- [ ] Add `DebitCreditIndicator` to `ImportField` enum

**Commit:**
```bash
git add .
git commit -m "feat(domain): add skip rows and indicator settings value objects

- Add SkipRowsSettings value object with validation
- Add DebitCreditIndicatorSettings value object with sign multiplier logic
- Add IndicatorColumn to AmountParseMode enum
- Add DebitCreditIndicator to ImportField enum

Refs: #030"
```

---

### Phase 2: ImportMapping Entity Updates

**Objective:** Extend ImportMapping entity with new settings properties.

**Tasks:**
- [ ] Write unit tests for ImportMapping with skip rows settings
- [ ] Write unit tests for ImportMapping with indicator settings
- [ ] Add SkipRowsSettings property to ImportMapping
- [ ] Add IndicatorSettings property to ImportMapping
- [ ] Add update methods with validation
- [ ] Ensure AmountMode/IndicatorSettings compatibility validation

**Commit:**
```bash
git add .
git commit -m "feat(domain): extend ImportMapping with skip rows and indicator settings

- Add SkipRowsSettings property with update method
- Add IndicatorSettings property with update method
- Add validation for AmountMode/IndicatorSettings compatibility
- Update entity factory method

Refs: #030"
```

---

### Phase 3: Infrastructure & Database Migration

**Objective:** Add database columns and update repository/EF configuration.

**Tasks:**
- [ ] Create EF migration for new columns
- [ ] Update ImportMapping EF configuration for owned types
- [ ] Write integration tests for persisting new settings
- [ ] Verify migration runs cleanly

**Commit:**
```bash
git add .
git commit -m "feat(infra): add database support for skip rows and indicator settings

- Create migration for ImportMappings table changes
- Update EF configuration for owned value objects
- Add integration tests for persistence

Refs: #030"
```

---

### Phase 4: Application DTOs & Mapping

**Objective:** Add DTOs and update mapping logic.

**Tasks:**
- [ ] Create `DebitCreditIndicatorSettingsDto`
- [ ] Update `ImportMappingDto` with new properties
- [ ] Update `CreateImportMappingDto` with new properties
- [ ] Update `UpdateImportMappingDto` with new properties
- [ ] Update mapping extension methods
- [ ] Write tests for DTO mapping

**Commit:**
```bash
git add .
git commit -m "feat(app): add DTOs for skip rows and indicator settings

- Add DebitCreditIndicatorSettingsDto
- Update ImportMappingDto with new properties
- Update create/update DTOs
- Update mapping extensions

Refs: #030"
```

---

### Phase 5: CsvParserService Skip Rows Support

**Objective:** Update CSV parser to respect skip rows setting.

**Tasks:**
- [ ] Write tests for parsing with skip rows
- [ ] Update `CsvParserService.Parse()` to accept rowsToSkip parameter
- [ ] Ensure skipped rows are available for display (not discarded)
- [ ] Test edge cases: skip > total rows, skip all except header, etc.

**Commit:**
```bash
git add .
git commit -m "feat(app): add skip rows support to CSV parser

- Update Parse method to accept rowsToSkip parameter
- Preserve skipped rows for UI display
- Handle edge cases gracefully

Refs: #030"
```

---

### Phase 6: Amount Interpretation with Indicator Column

**Objective:** Implement amount sign determination from indicator column.

**Tasks:**
- [ ] Write tests for `InterpretAmountWithIndicator`
- [ ] Implement amount interpretation logic in service
- [ ] Integrate with preview generation
- [ ] Handle unrecognized indicator values with warnings

**Commit:**
```bash
git add .
git commit -m "feat(app): implement indicator column amount interpretation

- Add InterpretAmountWithIndicator method
- Integrate with preview service
- Add warning handling for unrecognized indicators

Refs: #030"
```

---

### Phase 7: API Endpoint Updates

**Objective:** Update import endpoints to support new settings.

**Tasks:**
- [ ] Update import mapping endpoints for new DTO properties
- [ ] Update preview endpoint to apply skip rows
- [ ] Update preview endpoint to apply indicator interpretation
- [ ] Write API integration tests

**Commit:**
```bash
git add .
git commit -m "feat(api): update import endpoints for skip rows and indicators

- Update mapping endpoints with new DTO properties
- Update preview endpoint with skip rows support
- Update preview endpoint with indicator interpretation

Refs: #030"
```

---

### Phase 8: Client UI Updates

**Objective:** Update Blazor import UI to configure new settings.

**Tasks:**
- [ ] Add rows-to-skip input to import mapping editor
- [ ] Add indicator column dropdown to field mapping
- [ ] Add indicator value configuration fields
- [ ] Update preview to show skipped rows
- [ ] Update preview to show indicator interpretation
- [ ] Add auto-detect suggestion for skip rows (optional enhancement)

**Commit:**
```bash
git add .
git commit -m "feat(client): add UI for skip rows and indicator column settings

- Add rows-to-skip input field
- Add indicator column mapping option
- Add indicator value configuration
- Update preview display

Refs: #030"
```

---

### Phase 9: Documentation & Cleanup

**Objective:** Final polish, documentation updates, and cleanup.

**Tasks:**
- [ ] Update API documentation / OpenAPI specs
- [ ] Add/update XML comments for public APIs
- [ ] Update Feature 027 with cross-reference to this enhancement
- [ ] Remove any TODO comments
- [ ] Final code review

**Commit:**
```bash
git add .
git commit -m "docs(import): document skip rows and indicator column features

- XML comments for public API
- Update OpenAPI spec
- Cross-reference with Feature 027

Refs: #030"
```

---

## Conventional Commit Reference

Use these commit types to ensure proper changelog generation:

| Type | When to Use | SemVer Impact | Example |
|------|-------------|---------------|---------|
| `feat` | New feature or capability | Minor | `feat(import): add skip rows setting` |
| `fix` | Bug fix | Patch | `fix(import): correct indicator matching` |
| `docs` | Documentation only | None | `docs: update import examples` |
| `style` | Formatting, no logic change | None | `style: fix indentation` |
| `refactor` | Code restructure, no feature/fix | None | `refactor(import): extract indicator logic` |
| `perf` | Performance improvement | Patch | `perf(import): optimize row skipping` |
| `test` | Adding or fixing tests | None | `test(import): add indicator settings tests` |
| `chore` | Build, CI, dependencies | None | `chore: update NuGet packages` |

### Scope Suggestions

| Scope | Description |
|-------|-------------|
| `domain` | Domain model, entities, value objects |
| `import` | CSV import specific features |
| `api` | API controllers, endpoints |
| `client` | Blazor UI components |
| `infra` | Infrastructure, database, repositories |
| `app` | Application services |

---

## Testing Strategy

### Unit Tests

- [ ] `SkipRowsSettings.Create()` validation (valid range, out of range)
- [ ] `DebitCreditIndicatorSettings.Create()` validation (required fields, no overlap)
- [ ] `DebitCreditIndicatorSettings.GetSignMultiplier()` for debit/credit/unknown values
- [ ] Case-sensitive vs case-insensitive indicator matching
- [ ] `ImportMapping` with skip rows settings update
- [ ] `ImportMapping` with indicator settings update
- [ ] `ImportMapping` AmountMode/IndicatorSettings compatibility validation
- [ ] CSV parsing with skip rows (0, 1, N rows)
- [ ] Amount interpretation with indicator column

### Integration Tests

- [ ] Persist and retrieve ImportMapping with new settings
- [ ] Import preview with skip rows applied
- [ ] Import execution with indicator column amounts
- [ ] API endpoints accept and return new DTO properties

### Manual Testing Checklist

- [ ] Upload CSV with metadata rows, configure skip rows, verify preview
- [ ] Upload CSV with indicator column, configure mappings, verify amounts
- [ ] Save mapping with new settings, verify saved correctly
- [ ] Apply saved mapping to new file, verify settings applied
- [ ] Import with indicator column, verify transaction amounts correct

---

## Migration Notes

### Database Migration

```bash
dotnet ef migrations add Feature030_SkipRowsAndIndicators --project src/BudgetExperiment.Infrastructure --startup-project src/BudgetExperiment.Api
```

### Breaking Changes

None. All new settings have safe defaults that maintain existing behavior:
- `RowsToSkip` defaults to 0 (no skip)
- `IndicatorSettings` defaults to disabled

---

## Security Considerations

- Validate skip rows value is within reasonable bounds (0-100) to prevent abuse
- Sanitize indicator value inputs to prevent injection
- No new authentication/authorization requirements

---

## Performance Considerations

- Skip rows parsing should not re-read the file; implemented as offset during initial parse
- Indicator matching uses hashset for O(1) lookup
- Consider caching parsed skip rows content if displayed in UI

---

## Future Enhancements

- Auto-detect skip rows based on content heuristics (Phase 8 optional)
- AI-assisted detection of indicator column and values
- Support for multi-value indicator columns (e.g., "Debit - Purchase")
- Regular expression matching for indicator values

---

## References

- [Feature 027: Intelligent CSV Import](./027-csv-import.md)
- [Feature 024: Auto-Categorization Rules Engine](./024-auto-categorization-rules-engine.md)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-20 | Started implementation - Phase 1 | @copilot |
| 2026-01-19 | Initial draft | @copilot |
