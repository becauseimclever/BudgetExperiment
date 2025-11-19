namespace BudgetExperiment.Client.Models;

using BudgetExperiment.Domain;

/// <summary>
/// DTO used by the client to commit a transaction after preview (possibly edited).
/// </summary>
public sealed class CommitTransactionDto
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

    /// <summary>Gets or sets a value indicating whether to import even if duplicate.</summary>
    public bool ForceImport { get; set; }
}
