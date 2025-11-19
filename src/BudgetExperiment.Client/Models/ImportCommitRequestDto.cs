namespace BudgetExperiment.Client.Models;

/// <summary>
/// Wrapper request for committing import transactions.
/// </summary>
public sealed class ImportCommitRequestDto
{
    /// <summary>Gets or sets the items to commit.</summary>
    public List<CommitTransactionDto> Items { get; set; } = new();
}
