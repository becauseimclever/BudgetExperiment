namespace BudgetExperiment.Application.CsvImport.Models;

using BudgetExperiment.Domain;

/// <summary>
/// Represents a parsed CSV row returned during preview with duplicate detection
/// information and a reference to an existing transaction when applicable.
/// </summary>
public sealed record CsvImportPreviewRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvImportPreviewRow"/> class.
    /// Parameterless constructor primarily for serialization.
    /// </summary>
    public CsvImportPreviewRow()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvImportPreviewRow"/> class.
    /// </summary>
    /// <param name="rowNumber">The CSV row number (2-based including header).</param>
    /// <param name="date">The transaction date.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="amount">The transaction amount.</param>
    /// <param name="transactionType">The transaction type (Income or Expense).</param>
    /// <param name="category">Optional transaction category.</param>
    /// <param name="isDuplicate">Duplicate detection flag.</param>
    /// <param name="existingTransactionId">Existing transaction id if duplicate detected.</param>
    public CsvImportPreviewRow(int rowNumber, DateOnly date, string description, decimal amount, TransactionType transactionType, string? category, bool isDuplicate, Guid? existingTransactionId)
    {
        this.RowNumber = rowNumber;
        this.Date = date;
        this.Description = description;
        this.Amount = amount;
        this.TransactionType = transactionType;
        this.Category = category;
        this.IsDuplicate = isDuplicate;
        this.ExistingTransactionId = existingTransactionId;
    }

    /// <summary>
    /// Gets the CSV row number (2-based including header row).
    /// </summary>
    public int RowNumber { get; init; }

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
    /// Gets the transaction type (Income or Expense).
    /// </summary>
    public TransactionType TransactionType { get; init; }

    /// <summary>
    /// Gets the optional transaction category.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets a value indicating whether this row is detected as a duplicate.
    /// </summary>
    public bool IsDuplicate { get; init; }

    /// <summary>
    /// Gets the existing transaction id when a duplicate is detected; otherwise null.
    /// </summary>
    public Guid? ExistingTransactionId { get; init; }
}
