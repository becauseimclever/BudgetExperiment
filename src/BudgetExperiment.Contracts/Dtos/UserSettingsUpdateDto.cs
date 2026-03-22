// <copyright file="UserSettingsUpdateDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for updating user settings.
/// </summary>
public sealed class UserSettingsUpdateDto
{
    /// <summary>Gets or sets the default budget scope ("Shared", "Personal", or null for All).</summary>
    public string? DefaultScope
    {
        get; set;
    }

    /// <summary>Gets or sets a value indicating whether past-due items are auto-realized.</summary>
    public bool? AutoRealizePastDueItems
    {
        get; set;
    }

    /// <summary>Gets or sets the number of days to look back for past-due items.</summary>
    public int? PastDueLookbackDays
    {
        get; set;
    }

    /// <summary>Gets or sets the preferred currency code.</summary>
    public string? PreferredCurrency
    {
        get; set;
    }

    /// <summary>Gets or sets the user's time zone ID (IANA format).</summary>
    public string? TimeZoneId
    {
        get; set;
    }

    /// <summary>Gets or sets the user's preferred first day of the week.</summary>
    public DayOfWeek? FirstDayOfWeek
    {
        get; set;
    }
}
