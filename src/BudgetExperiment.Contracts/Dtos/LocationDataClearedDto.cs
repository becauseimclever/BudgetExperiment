// <copyright file="LocationDataClearedDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Response DTO returned after bulk-clearing all location data.
/// </summary>
public sealed class LocationDataClearedDto
{
    /// <summary>
    /// Gets or sets the number of transactions whose location data was cleared.
    /// </summary>
    public int ClearedCount
    {
        get; set;
    }
}
