// <copyright file="ApplyRulesRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request for bulk applying categorization rules.
/// </summary>
public sealed class ApplyRulesRequest
{
    /// <summary>
    /// Gets or sets the transaction IDs to process. If null, all uncategorized transactions are processed.
    /// </summary>
    public IEnumerable<Guid>? TransactionIds
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to overwrite existing categories.
    /// </summary>
    public bool OverwriteExisting
    {
        get; set;
    }
}
