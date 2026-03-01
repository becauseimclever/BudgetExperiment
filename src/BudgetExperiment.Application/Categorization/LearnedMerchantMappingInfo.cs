// <copyright file="LearnedMerchantMappingInfo.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Information about a learned merchant mapping.
/// </summary>
public sealed record LearnedMerchantMappingInfo
{
    /// <summary>
    /// Gets the mapping ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the merchant pattern.
    /// </summary>
    public required string MerchantPattern { get; init; }

    /// <summary>
    /// Gets the category ID.
    /// </summary>
    public required Guid CategoryId { get; init; }

    /// <summary>
    /// Gets the category name.
    /// </summary>
    public required string CategoryName { get; init; }

    /// <summary>
    /// Gets the number of times this mapping has been learned.
    /// </summary>
    public required int LearnCount { get; init; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the last update timestamp.
    /// </summary>
    public required DateTime UpdatedAtUtc { get; init; }
}
