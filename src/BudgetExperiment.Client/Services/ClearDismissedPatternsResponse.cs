// <copyright file="ClearDismissedPatternsResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Response from the clear dismissed patterns endpoint.
/// </summary>
public record ClearDismissedPatternsResponse
{
    /// <summary>
    /// Gets the number of dismissed patterns that were cleared.
    /// </summary>
    public int ClearedCount
    {
        get; init;
    }
}
