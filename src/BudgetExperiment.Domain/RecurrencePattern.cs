// <copyright file="RecurrencePattern.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Immutable value object representing a recurrence pattern for recurring transactions.
/// </summary>
public sealed record RecurrencePattern
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecurrencePattern"/> class.
    /// </summary>
    /// <param name="frequency">The recurrence frequency.</param>
    /// <param name="interval">The interval between occurrences.</param>
    /// <param name="dayOfMonth">The day of month for monthly/quarterly/yearly patterns.</param>
    /// <param name="dayOfWeek">The day of week for weekly/biweekly patterns.</param>
    /// <param name="monthOfYear">The month of year for yearly patterns.</param>
    private RecurrencePattern(
        RecurrenceFrequency frequency,
        int interval,
        int? dayOfMonth,
        DayOfWeek? dayOfWeek,
        int? monthOfYear)
    {
        this.Frequency = frequency;
        this.Interval = interval;
        this.DayOfMonth = dayOfMonth;
        this.DayOfWeek = dayOfWeek;
        this.MonthOfYear = monthOfYear;
    }

    /// <summary>
    /// Gets the recurrence frequency.
    /// </summary>
    public RecurrenceFrequency Frequency { get; init; }

    /// <summary>
    /// Gets the interval between occurrences (e.g., every 2 weeks).
    /// </summary>
    public int Interval { get; init; }

    /// <summary>
    /// Gets the day of month (1-31) for monthly, quarterly, or yearly patterns.
    /// </summary>
    public int? DayOfMonth { get; init; }

    /// <summary>
    /// Gets the day of week for weekly or biweekly patterns.
    /// </summary>
    public DayOfWeek? DayOfWeek { get; init; }

    /// <summary>
    /// Gets the month of year (1-12) for yearly patterns.
    /// </summary>
    public int? MonthOfYear { get; init; }

    /// <summary>
    /// Creates a daily recurrence pattern.
    /// </summary>
    /// <param name="interval">The interval in days (default 1).</param>
    /// <returns>A new daily <see cref="RecurrencePattern"/>.</returns>
    /// <exception cref="DomainException">Thrown when interval is less than 1.</exception>
    public static RecurrencePattern CreateDaily(int interval = 1)
    {
        ValidateInterval(interval);
        return new RecurrencePattern(RecurrenceFrequency.Daily, interval, null, null, null);
    }

    /// <summary>
    /// Creates a weekly recurrence pattern.
    /// </summary>
    /// <param name="interval">The interval in weeks (default 1).</param>
    /// <param name="dayOfWeek">The day of week for the occurrence.</param>
    /// <returns>A new weekly <see cref="RecurrencePattern"/>.</returns>
    /// <exception cref="DomainException">Thrown when interval is less than 1.</exception>
    public static RecurrencePattern CreateWeekly(int interval, DayOfWeek dayOfWeek)
    {
        ValidateInterval(interval);
        return new RecurrencePattern(RecurrenceFrequency.Weekly, interval, null, dayOfWeek, null);
    }

    /// <summary>
    /// Creates a biweekly (every 2 weeks) recurrence pattern.
    /// </summary>
    /// <param name="dayOfWeek">The day of week for the occurrence.</param>
    /// <returns>A new biweekly <see cref="RecurrencePattern"/>.</returns>
    public static RecurrencePattern CreateBiWeekly(DayOfWeek dayOfWeek)
    {
        return new RecurrencePattern(RecurrenceFrequency.BiWeekly, 2, null, dayOfWeek, null);
    }

    /// <summary>
    /// Creates a monthly recurrence pattern.
    /// </summary>
    /// <param name="interval">The interval in months (default 1).</param>
    /// <param name="dayOfMonth">The day of month (1-31).</param>
    /// <returns>A new monthly <see cref="RecurrencePattern"/>.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static RecurrencePattern CreateMonthly(int interval, int dayOfMonth)
    {
        ValidateInterval(interval);
        ValidateDayOfMonth(dayOfMonth);
        return new RecurrencePattern(RecurrenceFrequency.Monthly, interval, dayOfMonth, null, null);
    }

    /// <summary>
    /// Creates a quarterly (every 3 months) recurrence pattern.
    /// </summary>
    /// <param name="dayOfMonth">The day of month (1-31).</param>
    /// <returns>A new quarterly <see cref="RecurrencePattern"/>.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static RecurrencePattern CreateQuarterly(int dayOfMonth)
    {
        ValidateDayOfMonth(dayOfMonth);
        return new RecurrencePattern(RecurrenceFrequency.Quarterly, 3, dayOfMonth, null, null);
    }

    /// <summary>
    /// Creates a yearly recurrence pattern.
    /// </summary>
    /// <param name="dayOfMonth">The day of month (1-31).</param>
    /// <param name="monthOfYear">The month of year (1-12).</param>
    /// <returns>A new yearly <see cref="RecurrencePattern"/>.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static RecurrencePattern CreateYearly(int dayOfMonth, int monthOfYear)
    {
        ValidateDayOfMonth(dayOfMonth);
        ValidateMonthOfYear(monthOfYear);
        return new RecurrencePattern(RecurrenceFrequency.Yearly, 1, dayOfMonth, null, monthOfYear);
    }

    /// <summary>
    /// Calculates the next occurrence date from the given date.
    /// </summary>
    /// <param name="fromDate">The date to calculate from.</param>
    /// <returns>The next occurrence date.</returns>
    public DateOnly CalculateNextOccurrence(DateOnly fromDate)
    {
        return this.Frequency switch
        {
            RecurrenceFrequency.Daily => fromDate.AddDays(this.Interval),
            RecurrenceFrequency.Weekly => fromDate.AddDays(7 * this.Interval),
            RecurrenceFrequency.BiWeekly => fromDate.AddDays(14),
            RecurrenceFrequency.Monthly => this.CalculateNextMonthlyOccurrence(fromDate),
            RecurrenceFrequency.Quarterly => this.CalculateNextMonthlyOccurrence(fromDate),
            RecurrenceFrequency.Yearly => this.CalculateNextYearlyOccurrence(fromDate),
            _ => throw new DomainException($"Unsupported frequency: {this.Frequency}"),
        };
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return this.Frequency switch
        {
            RecurrenceFrequency.Daily when this.Interval == 1 => "Daily",
            RecurrenceFrequency.Daily => $"Every {this.Interval} days",
            RecurrenceFrequency.Weekly when this.Interval == 1 => $"Weekly on {this.DayOfWeek}",
            RecurrenceFrequency.Weekly => $"Every {this.Interval} weeks on {this.DayOfWeek}",
            RecurrenceFrequency.BiWeekly => $"Every 2 weeks on {this.DayOfWeek}",
            RecurrenceFrequency.Monthly when this.Interval == 1 => $"Monthly on day {this.DayOfMonth}",
            RecurrenceFrequency.Monthly => $"Every {this.Interval} months on day {this.DayOfMonth}",
            RecurrenceFrequency.Quarterly => $"Quarterly on day {this.DayOfMonth}",
            RecurrenceFrequency.Yearly => $"Yearly on {this.MonthOfYear}/{this.DayOfMonth}",
            _ => this.Frequency.ToString(),
        };
    }

    private static void ValidateInterval(int interval)
    {
        if (interval < 1)
        {
            throw new DomainException("Interval must be at least 1.");
        }
    }

    private static void ValidateDayOfMonth(int dayOfMonth)
    {
        if (dayOfMonth < 1 || dayOfMonth > 31)
        {
            throw new DomainException("Day of month must be between 1 and 31.");
        }
    }

    private static void ValidateMonthOfYear(int monthOfYear)
    {
        if (monthOfYear < 1 || monthOfYear > 12)
        {
            throw new DomainException("Month of year must be between 1 and 12.");
        }
    }

    private DateOnly CalculateNextMonthlyOccurrence(DateOnly fromDate)
    {
        var nextMonth = fromDate.AddMonths(this.Interval);
        var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
        var targetDay = Math.Min(this.DayOfMonth!.Value, daysInMonth);
        return new DateOnly(nextMonth.Year, nextMonth.Month, targetDay);
    }

    private DateOnly CalculateNextYearlyOccurrence(DateOnly fromDate)
    {
        var nextYear = fromDate.Year + 1;
        var daysInMonth = DateTime.DaysInMonth(nextYear, this.MonthOfYear!.Value);
        var targetDay = Math.Min(this.DayOfMonth!.Value, daysInMonth);
        return new DateOnly(nextYear, this.MonthOfYear.Value, targetDay);
    }
}
