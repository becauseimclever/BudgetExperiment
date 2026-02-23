// <copyright file="LocationSource.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Common;

/// <summary>
/// Indicates how a transaction's location data was determined.
/// </summary>
public enum LocationSource
{
    /// <summary>
    /// Location was manually entered by the user.
    /// </summary>
    Manual = 0,

    /// <summary>
    /// Location was captured from device GPS.
    /// </summary>
    Gps = 1,

    /// <summary>
    /// Location was parsed from the transaction description.
    /// </summary>
    Parsed = 2,

    /// <summary>
    /// Location was determined via reverse geocoding.
    /// </summary>
    Geocoded = 3,
}
