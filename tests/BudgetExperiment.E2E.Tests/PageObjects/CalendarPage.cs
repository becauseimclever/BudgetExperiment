// <copyright file="CalendarPage.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.PageObjects;

/// <summary>
/// Page object for the Calendar page.
/// </summary>
public class CalendarPage : BasePage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarPage"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public CalendarPage(IPage page)
        : base(page)
    {
    }

    /// <summary>
    /// Gets the previous month button.
    /// </summary>
    public ILocator PreviousMonthButton => Page.Locator("button[title='Previous month']");

    /// <summary>
    /// Gets the next month button.
    /// </summary>
    public ILocator NextMonthButton => Page.Locator("button[title='Next month']");

    /// <summary>
    /// Gets the current month display.
    /// </summary>
    public ILocator CurrentMonthDisplay => Page.Locator(".current-month, .month-display");

    /// <summary>
    /// Gets all calendar day cells.
    /// </summary>
    public ILocator CalendarDays => Page.Locator(".calendar-day, .day-cell");

    /// <summary>
    /// Gets the selected day panel/details.
    /// </summary>
    public ILocator DayDetails => Page.Locator(".day-details, .selected-day");

    /// <summary>
    /// Navigates to the previous month.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task GoToPreviousMonthAsync()
    {
        await PreviousMonthButton.ClickAsync();
        await WaitForLoadingCompleteAsync();
    }

    /// <summary>
    /// Navigates to the next month.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task GoToNextMonthAsync()
    {
        await NextMonthButton.ClickAsync();
        await WaitForLoadingCompleteAsync();
    }

    /// <summary>
    /// Clicks on a specific day in the calendar.
    /// </summary>
    /// <param name="day">The day number to click.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SelectDayAsync(int day)
    {
        await CalendarDays.Filter(new() { HasText = day.ToString() }).First.ClickAsync();
    }

    /// <summary>
    /// Gets the current month text.
    /// </summary>
    /// <returns>The current month display text.</returns>
    public async Task<string> GetCurrentMonthTextAsync()
    {
        return await CurrentMonthDisplay.TextContentAsync() ?? string.Empty;
    }
}
