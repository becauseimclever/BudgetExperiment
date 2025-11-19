namespace BudgetExperiment.Application.CsvImport.Models;

using BudgetExperiment.Domain;

/// <summary>
/// Represents a transaction prepared for commit after preview, possibly with user edits
/// and a flag to force import even if detected as a duplicate.
/// </summary>
public sealed record CommitTransaction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommitTransaction"/> class.
    /// Parameterless constructor primarily for serialization.
    /// </summary>
    public CommitTransaction()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommitTransaction"/> class.
    /// </summary>
    /// <param name="rowNumber">The CSV row number (2-based including header).</param>
    /// <param name="date">The transaction date.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="amount">The transaction amount (positive value).</param>
    /// <param name="transactionType">The transaction type (Income or Expense).</param>
    /// <param name="category">Optional category for the transaction.</param>
    /// <param name="forceImport">Whether to import even if a duplicate is detected.</param>
    public CommitTransaction(int rowNumber, DateOnly date, string description, decimal amount, TransactionType transactionType, string? category, bool forceImport)
    {
        this.RowNumber = rowNumber;
        this.Date = date;
        this.Description = description;
        this.Amount = amount;
        this.TransactionType = transactionType;
        this.Category = category;
        this.ForceImport = forceImport;
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
    /// Gets the transaction amount (positive value).
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
    /// Gets a value indicating whether to import even if a duplicate is detected.
    /// </summary>
    public bool ForceImport { get; init; }
}
