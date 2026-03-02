// <copyright file="DeleteBatchResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api.Models;

/// <summary>
/// Result of deleting an import batch.
/// </summary>
public sealed record DeleteBatchResult
{
    /// <summary>
    /// Gets the number of transactions deleted.
    /// </summary>
    public int DeletedCount { get; init; }
}
