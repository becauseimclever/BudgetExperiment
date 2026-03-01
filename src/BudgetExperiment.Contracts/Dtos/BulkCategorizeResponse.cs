// <copyright file="BulkCategorizeResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Response from bulk categorize operation.
/// </summary>
public sealed class BulkCategorizeResponse
{
    /// <summary>Gets or sets the total number of transactions requested.</summary>
    public int TotalRequested { get; set; }

    /// <summary>Gets or sets the number of successfully updated transactions.</summary>
    public int SuccessCount { get; set; }

    /// <summary>Gets or sets the number of failed updates.</summary>
    public int FailedCount { get; set; }

    /// <summary>Gets or sets error messages for failed updates.</summary>
    public IReadOnlyList<string> Errors { get; set; } = [];
}
