// <copyright file="DeleteBatchResultModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Model for delete batch result.
/// </summary>
public sealed record DeleteBatchResultModel
{
    /// <summary>
    /// Gets the number of transactions deleted.
    /// </summary>
    public int DeletedCount
    {
        get; init;
    }
}
