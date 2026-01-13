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
}

/// <summary>
/// DTO for updating application settings.
/// </summary>
public sealed class AppSettingsUpdateDto
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable auto-realize.
    /// Null means no change.
    /// </summary>
    public bool? AutoRealizePastDueItems { get; set; }

    /// <summary>
    /// Gets or sets the number of days to look back for past-due items.
    /// Null means no change.
    /// </summary>
    public int? PastDueLookbackDays { get; set; }
}
