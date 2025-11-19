namespace BudgetExperiment.Client.Models;

using BudgetExperiment.Domain;

/// <summary>
/// DTO representing a preview row returned by the server for CSV import.
/// </summary>
public sealed class CsvImportPreviewRowDto
{
    /// <summary>Gets or sets the CSV row number (2-based including header row).</summary>
    public int RowNumber { get; set; }

    /// <summary>Gets or sets the transaction date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the transaction description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the transaction amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the transaction type (Income or Expense).</summary>
    public TransactionType TransactionType { get; set; }

    /// <summary>Gets or sets the optional transaction category.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets a value indicating whether this row is detected as duplicate.</summary>
    public bool IsDuplicate { get; set; }

    /// <summary>Gets or sets the existing transaction id when a duplicate is detected; otherwise null.</summary>
    public Guid? ExistingTransactionId { get; set; }
}
