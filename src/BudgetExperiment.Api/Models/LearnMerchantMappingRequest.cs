// <copyright file="LearnMerchantMappingRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api.Models;

/// <summary>
/// Request to learn a merchant mapping.
/// </summary>
public sealed record LearnMerchantMappingRequest
{
    /// <summary>
    /// Gets the transaction description to learn from.
    /// </summary>
    public required string Description
    {
        get; init;
    }

    /// <summary>
    /// Gets the category ID to map to.
    /// </summary>
    public required Guid CategoryId
    {
        get; init;
    }
}
