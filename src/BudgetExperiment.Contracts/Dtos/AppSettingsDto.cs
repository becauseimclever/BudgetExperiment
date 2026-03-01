// <copyright file="AppSettingsDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for returning application settings.
/// </summary>
public sealed class AppSettingsDto
{
    /// <summary>
    /// Gets or sets a value indicating whether past-due recurring items
    /// are automatically realized without requiring manual confirmation.
    /// </summary>
    public bool AutoRealizePastDueItems { get; set; }

    /// <summary>
    /// Gets or sets how many days back to look for past-due items.
    /// </summary>
    public int PastDueLookbackDays { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether location data features are enabled.
    /// </summary>
    public bool EnableLocationData { get; set; }
}
