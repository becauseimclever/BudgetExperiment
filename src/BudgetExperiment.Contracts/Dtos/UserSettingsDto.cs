// <copyright file="UserSettingsDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for user settings.
/// </summary>
public sealed class UserSettingsDto
{
    /// <summary>Gets or sets the user ID.</summary>
    public Guid UserId
    {
        get; set;
    }

    /// <summary>Gets or sets a value indicating whether past-due items are auto-realized.</summary>
    public bool AutoRealizePastDueItems
    {
        get; set;
    }

    /// <summary>Gets or sets the number of days to look back for past-due items.</summary>
    public int PastDueLookbackDays { get; set; } = 30;

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
    public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Sunday;

    /// <summary>Gets or sets a value indicating whether the user has completed onboarding.</summary>
    public bool IsOnboarded
    {
        get; set;
    }

    /// <summary>Gets or sets a value indicating whether the user has completed the Kakeibo category setup wizard.</summary>
    public bool HasCompletedKakeiboSetup
    {
        get; set;
    }

    /// <summary>Gets or sets a value indicating whether the spending heatmap overlay is enabled on the calendar.</summary>
    public bool ShowSpendingHeatmap { get; set; } = true;
}
