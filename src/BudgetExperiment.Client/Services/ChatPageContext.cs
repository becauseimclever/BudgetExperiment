// <copyright file="ChatPageContext.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Represents context information from the current page for the chat assistant.
/// </summary>
public class ChatPageContext
{
    /// <summary>
    /// Gets or sets the current account ID, if viewing an account-related page.
    /// </summary>
    public Guid? CurrentAccountId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the current account name, if viewing an account-related page.
    /// </summary>
    public string? CurrentAccountName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the current category ID, if viewing a category-related page.
    /// </summary>
    public Guid? CurrentCategoryId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the current category name, if viewing a category-related page.
    /// </summary>
    public string? CurrentCategoryName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the page type (e.g., "transactions", "transfers", "recurring").
    /// </summary>
    public string? PageType
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the year being viewed on the calendar.
    /// </summary>
    public int? CalendarViewedYear
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the month being viewed on the calendar (1-12).
    /// </summary>
    public int? CalendarViewedMonth
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the selected date on the calendar.
    /// </summary>
    public DateOnly? SelectedDate
    {
        get; set;
    }

    /// <summary>
    /// Gets a summary of the context for the AI prompt.
    /// </summary>
    /// <returns>A human-readable context summary.</returns>
    public string GetContextSummary()
    {
        var parts = new List<string>();

        if (CalendarViewedYear.HasValue && CalendarViewedMonth.HasValue)
        {
            var year = CalendarViewedYear.Value;
            var month = CalendarViewedMonth.Value;

            if (year > 0 && month >= 1 && month <= 12)
            {
                var monthName = new DateOnly(year, month, 1).ToString("MMMM yyyy");
                parts.Add($"Viewing {monthName}");
            }
        }

        if (SelectedDate.HasValue)
        {
            parts.Add($"Selected: {SelectedDate.Value:MMM d}");
        }

        if (!string.IsNullOrEmpty(CurrentAccountName))
        {
            parts.Add($"Account: {CurrentAccountName}");
        }

        if (!string.IsNullOrEmpty(CurrentCategoryName))
        {
            parts.Add($"Category: {CurrentCategoryName}");
        }

        if (!string.IsNullOrEmpty(PageType))
        {
            parts.Add($"On the {PageType} page");
        }

        return parts.Count > 0 ? string.Join(". ", parts) : string.Empty;
    }
}
