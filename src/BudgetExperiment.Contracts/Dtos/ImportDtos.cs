// <copyright file="ImportDtos.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request to preview an import with mapping configuration.
/// </summary>
public sealed record ImportPreviewRequest
{
    /// <summary>
    /// Gets the target account ID for the import.
    /// </summary>
    public Guid AccountId { get; init; }

    /// <summary>
    /// Gets the raw CSV rows (without header if HasHeaderRow was true).
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];

    /// <summary>
    /// Gets the column mappings defining how CSV columns map to transaction fields.
    /// </summary>
    public IReadOnlyList<ColumnMappingDto> Mappings { get; init; } = [];

    /// <summary>
    /// Gets the date format to use for parsing dates (e.g., "MM/dd/yyyy").
    /// </summary>
    public string DateFormat { get; init; } = "MM/dd/yyyy";

    /// <summary>
    /// Gets how amounts should be interpreted.
    /// </summary>
    public AmountParseMode AmountMode { get; init; } = AmountParseMode.NegativeIsExpense;

    /// <summary>
    /// Gets the settings for duplicate detection.
    /// </summary>
    public DuplicateDetectionSettingsDto DuplicateSettings { get; init; } = new();

    /// <summary>
    /// Gets a value indicating whether to check for recurring transaction matches.
    /// </summary>
    public bool CheckRecurringMatches { get; init; } = true;

    /// <summary>
    /// Gets the number of rows to skip at the beginning of the file.
    /// </summary>
    public int RowsToSkip { get; init; }

    /// <summary>
    /// Gets the debit/credit indicator settings.
    /// </summary>
    public DebitCreditIndicatorSettingsDto? IndicatorSettings { get; init; }
}

/// <summary>
/// DTO for column mapping configuration.
/// </summary>
public sealed record ColumnMappingDto
{
    /// <summary>
    /// Gets the zero-based column index in the CSV.
    /// </summary>
    public int ColumnIndex { get; init; }

    /// <summary>
    /// Gets the column header name (if available).
    /// </summary>
    public string? ColumnHeader { get; init; }

    /// <summary>
    /// Gets the target field this column maps to.
    /// </summary>
    public ImportField TargetField { get; init; }

    /// <summary>
    /// Gets the optional date format override for this column.
    /// </summary>
    public string? DateFormat { get; init; }
}

/// <summary>
/// DTO for duplicate detection settings.
/// </summary>
public sealed record DuplicateDetectionSettingsDto
{
    /// <summary>
    /// Gets a value indicating whether duplicate detection is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the number of days to look back for duplicates.
    /// </summary>
    public int LookbackDays { get; init; } = 30;

    /// <summary>
    /// Gets how descriptions should be matched for duplicate detection.
    /// </summary>
    public DescriptionMatchMode DescriptionMatch { get; init; } = DescriptionMatchMode.Exact;
}

/// <summary>
/// DTO for debit/credit indicator settings.
/// </summary>
public sealed record DebitCreditIndicatorSettingsDto
{
    /// <summary>
    /// Gets the column index of the indicator (-1 if disabled).
    /// </summary>
    public int ColumnIndex { get; init; } = -1;

    /// <summary>
    /// Gets the comma-separated debit indicator values.
    /// </summary>
    public string DebitIndicators { get; init; } = string.Empty;

    /// <summary>
    /// Gets the comma-separated credit indicator values.
    /// </summary>
    public string CreditIndicators { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether matching is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; init; }
}

/// <summary>
/// Result of an import preview operation.
/// </summary>
public sealed record ImportPreviewResult
{
    /// <summary>
    /// Gets the preview rows with validation results.
    /// </summary>
    public IReadOnlyList<ImportPreviewRow> Rows { get; init; } = [];

    /// <summary>
    /// Gets the count of valid rows ready for import.
    /// </summary>
    public int ValidCount { get; init; }

    /// <summary>
    /// Gets the count of rows with warnings.
    /// </summary>
    public int WarningCount { get; init; }

    /// <summary>
    /// Gets the count of rows with errors.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Gets the count of duplicate rows.
    /// </summary>
    public int DuplicateCount { get; init; }

    /// <summary>
    /// Gets the total amount of valid transactions.
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// Gets the count of rows that would be auto-categorized by rules.
    /// </summary>
    public int AutoCategorizedCount { get; init; }
}

/// <summary>
/// A single row from the import preview.
/// </summary>
public sealed record ImportPreviewRow
{
    /// <summary>
    /// Gets the row index from the original CSV (1-based for display).
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Gets the parsed transaction date.
    /// </summary>
    public DateOnly? Date { get; init; }

    /// <summary>
    /// Gets the transaction description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the parsed amount.
    /// </summary>
    public decimal? Amount { get; init; }

    /// <summary>
    /// Gets the category name (from CSV or auto-categorization).
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets the category ID if matched.
    /// </summary>
    public Guid? CategoryId { get; init; }

    /// <summary>
    /// Gets the source of the category assignment.
    /// </summary>
    public CategorySource CategorySource { get; init; }

    /// <summary>
    /// Gets the name of the matched auto-categorization rule (if any).
    /// </summary>
    public string? MatchedRuleName { get; init; }

    /// <summary>
    /// Gets the matched rule ID for tracking.
    /// </summary>
    public Guid? MatchedRuleId { get; init; }

    /// <summary>
    /// Gets the external reference/ID from the CSV.
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Gets the validation status of this row.
    /// </summary>
    public ImportRowStatus Status { get; init; }

    /// <summary>
    /// Gets a message describing the status (error message, warning, etc.).
    /// </summary>
    public string? StatusMessage { get; init; }

    /// <summary>
    /// Gets the ID of the existing transaction this row duplicates.
    /// </summary>
    public Guid? DuplicateOfTransactionId { get; init; }

    /// <summary>
    /// Gets a value indicating whether this row is selected for import.
    /// </summary>
    public bool IsSelected { get; init; } = true;

    /// <summary>
    /// Gets the potential recurring transaction match for this row (if any).
    /// </summary>
    public ImportRecurringMatchPreview? RecurringMatch { get; init; }
}

/// <summary>
/// Preview of a potential recurring transaction match during import.
/// </summary>
public sealed record ImportRecurringMatchPreview
{
    /// <summary>
    /// Gets the recurring transaction ID.
    /// </summary>
    public Guid RecurringTransactionId { get; init; }

    /// <summary>
    /// Gets the recurring transaction description.
    /// </summary>
    public string RecurringDescription { get; init; } = string.Empty;

    /// <summary>
    /// Gets the instance date that would be matched.
    /// </summary>
    public DateOnly InstanceDate { get; init; }

    /// <summary>
    /// Gets the expected amount from the recurring transaction.
    /// </summary>
    public decimal ExpectedAmount { get; init; }

    /// <summary>
    /// Gets the confidence score (0.0 to 1.0).
    /// </summary>
    public decimal ConfidenceScore { get; init; }

    /// <summary>
    /// Gets the confidence level (High, Medium, Low).
    /// </summary>
    public string ConfidenceLevel { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this match would be auto-applied.
    /// </summary>
    public bool WouldAutoMatch { get; init; }
}

/// <summary>
/// Request to execute an import.
/// </summary>
public sealed record ImportExecuteRequest
{
    /// <summary>
    /// Gets the target account ID.
    /// </summary>
    public Guid AccountId { get; init; }

    /// <summary>
    /// Gets the optional saved mapping ID used for this import.
    /// </summary>
    public Guid? MappingId { get; init; }

    /// <summary>
    /// Gets the original file name.
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the transactions to import (from preview, possibly with user modifications).
    /// </summary>
    public IReadOnlyList<ImportTransactionData> Transactions { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether to run reconciliation matching after import.
    /// When enabled, imported transactions will be matched against expected recurring transaction instances.
    /// </summary>
    public bool RunReconciliation { get; init; }
}

/// <summary>
/// Data for a single transaction to be imported.
/// </summary>
public sealed record ImportTransactionData
{
    /// <summary>
    /// Gets the transaction date.
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// Gets the transaction description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the transaction amount.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Gets the category ID to assign.
    /// </summary>
    public Guid? CategoryId { get; init; }

    /// <summary>
    /// Gets the external reference from CSV.
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Gets the source of the category (for tracking).
    /// </summary>
    public CategorySource CategorySource { get; init; }

    /// <summary>
    /// Gets the matched rule ID (if auto-categorized).
    /// </summary>
    public Guid? MatchedRuleId { get; init; }
}

/// <summary>
/// Result of an import execution.
/// </summary>
public sealed record ImportResult
{
    /// <summary>
    /// Gets the created import batch ID.
    /// </summary>
    public Guid BatchId { get; init; }

    /// <summary>
    /// Gets the count of successfully imported transactions.
    /// </summary>
    public int ImportedCount { get; init; }

    /// <summary>
    /// Gets the count of skipped transactions.
    /// </summary>
    public int SkippedCount { get; init; }

    /// <summary>
    /// Gets the count of failed transactions.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Gets the IDs of created transactions.
    /// </summary>
    public IReadOnlyList<Guid> CreatedTransactionIds { get; init; } = [];

    /// <summary>
    /// Gets the count of transactions auto-categorized by rules.
    /// </summary>
    public int AutoCategorizedCount { get; init; }

    /// <summary>
    /// Gets the count of transactions categorized from CSV column.
    /// </summary>
    public int CsvCategorizedCount { get; init; }

    /// <summary>
    /// Gets the count of uncategorized transactions.
    /// </summary>
    public int UncategorizedCount { get; init; }

    /// <summary>
    /// Gets the count of transactions matched to recurring transactions.
    /// </summary>
    public int ReconciliationMatchCount { get; init; }

    /// <summary>
    /// Gets the count of high-confidence auto-matched transactions.
    /// </summary>
    public int AutoMatchedCount { get; init; }

    /// <summary>
    /// Gets the count of transactions with pending match suggestions for review.
    /// </summary>
    public int PendingMatchCount { get; init; }

    /// <summary>
    /// Gets the reconciliation match suggestions (if reconciliation was performed).
    /// </summary>
    public IReadOnlyList<ReconciliationMatchDto> MatchSuggestions { get; init; } = [];
}

/// <summary>
/// DTO for import batch history.
/// </summary>
public sealed record ImportBatchDto
{
    /// <summary>
    /// Gets the batch ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the account ID the batch was imported to.
    /// </summary>
    public Guid AccountId { get; init; }

    /// <summary>
    /// Gets the account name.
    /// </summary>
    public string AccountName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the original file name.
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of transactions imported.
    /// </summary>
    public int TransactionCount { get; init; }

    /// <summary>
    /// Gets the batch status.
    /// </summary>
    public ImportBatchStatus Status { get; init; }

    /// <summary>
    /// Gets the import timestamp.
    /// </summary>
    public DateTime ImportedAtUtc { get; init; }

    /// <summary>
    /// Gets the mapping ID used (if any).
    /// </summary>
    public Guid? MappingId { get; init; }

    /// <summary>
    /// Gets the mapping name used (if any).
    /// </summary>
    public string? MappingName { get; init; }
}

/// <summary>
/// DTO for saved import mapping.
/// </summary>
public sealed record ImportMappingDto
{
    /// <summary>
    /// Gets the mapping ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the mapping name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the column mappings.
    /// </summary>
    public IReadOnlyList<ColumnMappingDto> ColumnMappings { get; init; } = [];

    /// <summary>
    /// Gets the default date format for this mapping.
    /// </summary>
    public string? DateFormat { get; init; }

    /// <summary>
    /// Gets the default amount parse mode.
    /// </summary>
    public AmountParseMode? AmountMode { get; init; }

    /// <summary>
    /// Gets the duplicate detection settings.
    /// </summary>
    public DuplicateDetectionSettingsDto? DuplicateSettings { get; init; }

    /// <summary>
    /// Gets the number of rows to skip at the beginning of the file.
    /// </summary>
    public int RowsToSkip { get; init; }

    /// <summary>
    /// Gets the debit/credit indicator settings.
    /// </summary>
    public DebitCreditIndicatorSettingsDto? IndicatorSettings { get; init; }

    /// <summary>
    /// Gets when the mapping was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets when the mapping was last updated.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; init; }
}

/// <summary>
/// Request to create a new import mapping.
/// </summary>
public sealed record CreateImportMappingRequest
{
    /// <summary>
    /// Gets the mapping name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the column mappings.
    /// </summary>
    public IReadOnlyList<ColumnMappingDto> ColumnMappings { get; init; } = [];

    /// <summary>
    /// Gets the optional default date format.
    /// </summary>
    public string? DateFormat { get; init; }

    /// <summary>
    /// Gets the optional default amount parse mode.
    /// </summary>
    public AmountParseMode? AmountMode { get; init; }

    /// <summary>
    /// Gets the optional duplicate detection settings.
    /// </summary>
    public DuplicateDetectionSettingsDto? DuplicateSettings { get; init; }

    /// <summary>
    /// Gets the number of rows to skip at the beginning of the file.
    /// </summary>
    public int RowsToSkip { get; init; }

    /// <summary>
    /// Gets the optional debit/credit indicator settings.
    /// </summary>
    public DebitCreditIndicatorSettingsDto? IndicatorSettings { get; init; }
}

/// <summary>
/// Request to update an import mapping.
/// </summary>
public sealed record UpdateImportMappingRequest
{
    /// <summary>
    /// Gets the new name (if changing).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the new column mappings (if changing).
    /// </summary>
    public IReadOnlyList<ColumnMappingDto>? ColumnMappings { get; init; }

    /// <summary>
    /// Gets the new date format (if changing).
    /// </summary>
    public string? DateFormat { get; init; }

    /// <summary>
    /// Gets the new amount parse mode (if changing).
    /// </summary>
    public AmountParseMode? AmountMode { get; init; }

    /// <summary>
    /// Gets the new duplicate detection settings (if changing).
    /// </summary>
    public DuplicateDetectionSettingsDto? DuplicateSettings { get; init; }

    /// <summary>
    /// Gets the number of rows to skip at the beginning of the file (if changing).
    /// </summary>
    public int? RowsToSkip { get; init; }

    /// <summary>
    /// Gets the new debit/credit indicator settings (if changing).
    /// </summary>
    public DebitCreditIndicatorSettingsDto? IndicatorSettings { get; init; }
}
