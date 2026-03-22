// <copyright file="MerchantMappingResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Result of a merchant mapping lookup.
/// </summary>
public readonly record struct MerchantMappingResult
{
    /// <summary>
    /// Gets the matched pattern.
    /// </summary>
    public required string Pattern
    {
        get; init;
    }

    /// <summary>
    /// Gets the category name.
    /// </summary>
    public required string Category
    {
        get; init;
    }

    /// <summary>
    /// Gets the suggested icon.
    /// </summary>
    public required string Icon
    {
        get; init;
    }

    /// <summary>
    /// Gets a value indicating whether this is a learned mapping.
    /// </summary>
    public required bool IsLearned
    {
        get; init;
    }

    /// <summary>
    /// Gets the category ID if this is a learned mapping.
    /// </summary>
    public Guid? CategoryId
    {
        get; init;
    }
}
