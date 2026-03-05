// <copyright file="RecurrencePatternFactory.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Creates <see cref="RecurrencePatternValue"/> instances from frequency parameters.
/// Shared between recurring transaction and transfer services.
/// </summary>
public static class RecurrencePatternFactory
{
    /// <summary>
    /// Creates a recurrence pattern from the specified frequency parameters.
    /// </summary>
    /// <param name="frequency">The recurrence frequency string.</param>
    /// <param name="interval">The interval between recurrences.</param>
    /// <param name="dayOfMonth">The day of month for monthly or yearly patterns.</param>
    /// <param name="dayOfWeek">The day of week for weekly or biweekly patterns.</param>
    /// <param name="monthOfYear">The month of year for yearly patterns.</param>
    /// <returns>A configured recurrence pattern value.</returns>
    /// <exception cref="DomainException">Thrown when the frequency or day of week is invalid.</exception>
    public static RecurrencePatternValue Create(
        string frequency,
        int interval,
        int? dayOfMonth,
        string? dayOfWeek,
        int? monthOfYear)
    {
        if (!Enum.TryParse<RecurrenceFrequency>(frequency, ignoreCase: true, out var freq))
        {
            throw new DomainException($"Invalid frequency: {frequency}");
        }

        return freq switch
        {
            RecurrenceFrequency.Daily => RecurrencePatternValue.CreateDaily(interval),
            RecurrenceFrequency.Weekly => RecurrencePatternValue.CreateWeekly(interval, ParseDayOfWeek(dayOfWeek)),
            RecurrenceFrequency.BiWeekly => RecurrencePatternValue.CreateBiWeekly(ParseDayOfWeek(dayOfWeek)),
            RecurrenceFrequency.Monthly => RecurrencePatternValue.CreateMonthly(interval, dayOfMonth ?? 1),
            RecurrenceFrequency.Quarterly => RecurrencePatternValue.CreateQuarterly(dayOfMonth ?? 1),
            RecurrenceFrequency.Yearly => RecurrencePatternValue.CreateYearly(dayOfMonth ?? 1, monthOfYear ?? 1),
            _ => throw new DomainException($"Unsupported frequency: {frequency}"),
        };
    }

    /// <summary>
    /// Parses a string day of week into a <see cref="DayOfWeek"/> value.
    /// </summary>
    /// <param name="dayOfWeek">The day of week string to parse.</param>
    /// <returns>The parsed day of week.</returns>
    /// <exception cref="DomainException">Thrown when the day of week is null, empty, or invalid.</exception>
    public static DayOfWeek ParseDayOfWeek(string? dayOfWeek)
    {
        if (string.IsNullOrWhiteSpace(dayOfWeek))
        {
            throw new DomainException("Day of week is required for weekly/biweekly patterns.");
        }

        if (!Enum.TryParse<DayOfWeek>(dayOfWeek, ignoreCase: true, out var dow))
        {
            throw new DomainException($"Invalid day of week: {dayOfWeek}");
        }

        return dow;
    }
}
