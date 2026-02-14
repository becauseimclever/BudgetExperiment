// <copyright file="ChatContextService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Represents context information from the current page for the chat assistant.
/// </summary>
public class ChatPageContext
{
    /// <summary>
    /// Gets or sets the current account ID, if viewing an account-related page.
    /// </summary>
    public Guid? CurrentAccountId { get; set; }

    /// <summary>
    /// Gets or sets the current account name, if viewing an account-related page.
    /// </summary>
    public string? CurrentAccountName { get; set; }

    /// <summary>
    /// Gets or sets the current category ID, if viewing a category-related page.
    /// </summary>
    public Guid? CurrentCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the current category name, if viewing a category-related page.
    /// </summary>
    public string? CurrentCategoryName { get; set; }

    /// <summary>
    /// Gets or sets the page type (e.g., "transactions", "transfers", "recurring").
    /// </summary>
    public string? PageType { get; set; }

    /// <summary>
    /// Gets or sets the year being viewed on the calendar.
    /// </summary>
    public int? CalendarViewedYear { get; set; }

    /// <summary>
    /// Gets or sets the month being viewed on the calendar (1-12).
    /// </summary>
    public int? CalendarViewedMonth { get; set; }

    /// <summary>
    /// Gets or sets the selected date on the calendar.
    /// </summary>
    public DateOnly? SelectedDate { get; set; }

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

/// <summary>
/// Service for tracking page context to provide to the chat assistant.
/// </summary>
public interface IChatContextService
{
    /// <summary>
    /// Gets the current page context.
    /// </summary>
    ChatPageContext CurrentContext { get; }

    /// <summary>
    /// Occurs when the context changes.
    /// </summary>
    event EventHandler? ContextChanged;

    /// <summary>
    /// Sets the current account context.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="accountName">The account name.</param>
    void SetAccountContext(Guid? accountId, string? accountName);

    /// <summary>
    /// Sets the current category context.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="categoryName">The category name.</param>
    void SetCategoryContext(Guid? categoryId, string? categoryName);

    /// <summary>
    /// Sets the current page type.
    /// </summary>
    /// <param name="pageType">The page type identifier.</param>
    void SetPageType(string? pageType);

    /// <summary>
    /// Sets the current calendar context.
    /// </summary>
    /// <param name="year">The calendar year being viewed.</param>
    /// <param name="month">The calendar month being viewed (1-12).</param>
    /// <param name="selectedDate">The selected date, if any.</param>
    /// <param name="accountId">The selected account ID, if any.</param>
    /// <param name="accountName">The selected account name, if any.</param>
    void SetCalendarContext(int year, int month, DateOnly? selectedDate, Guid? accountId = null, string? accountName = null);

    /// <summary>
    /// Converts the current context to a DTO for API transmission.
    /// </summary>
    /// <returns>The current context as a DTO.</returns>
    ChatContextDto ToDto();

    /// <summary>
    /// Clears all context.
    /// </summary>
    void ClearContext();
}

/// <summary>
/// Implementation of <see cref="IChatContextService"/>.
/// </summary>
public class ChatContextService : IChatContextService
{
    /// <inheritdoc />
    public ChatPageContext CurrentContext { get; } = new();

    /// <inheritdoc />
    public event EventHandler? ContextChanged;

    /// <inheritdoc />
    public void SetAccountContext(Guid? accountId, string? accountName)
    {
        CurrentContext.CurrentAccountId = accountId;
        CurrentContext.CurrentAccountName = accountName;
        OnContextChanged();
    }

    /// <inheritdoc />
    public void SetCategoryContext(Guid? categoryId, string? categoryName)
    {
        CurrentContext.CurrentCategoryId = categoryId;
        CurrentContext.CurrentCategoryName = categoryName;
        OnContextChanged();
    }

    /// <inheritdoc />
    public void SetPageType(string? pageType)
    {
        CurrentContext.PageType = pageType;
        OnContextChanged();
    }

    /// <inheritdoc />
    public void SetCalendarContext(int year, int month, DateOnly? selectedDate, Guid? accountId = null, string? accountName = null)
    {
        CurrentContext.CalendarViewedYear = year;
        CurrentContext.CalendarViewedMonth = month;
        CurrentContext.SelectedDate = selectedDate;

        if (accountId.HasValue || !string.IsNullOrWhiteSpace(accountName))
        {
            CurrentContext.CurrentAccountId = accountId;
            CurrentContext.CurrentAccountName = accountName;
        }

        OnContextChanged();
    }

    /// <inheritdoc />
    public ChatContextDto ToDto()
    {
        return new ChatContextDto
        {
            CurrentAccountId = CurrentContext.CurrentAccountId,
            CurrentAccountName = CurrentContext.CurrentAccountName,
            CurrentCategoryId = CurrentContext.CurrentCategoryId,
            CurrentCategoryName = CurrentContext.CurrentCategoryName,
            CalendarViewedYear = CurrentContext.CalendarViewedYear,
            CalendarViewedMonth = CurrentContext.CalendarViewedMonth,
            SelectedDate = CurrentContext.SelectedDate,
            PageType = CurrentContext.PageType,
        };
    }

    /// <inheritdoc />
    public void ClearContext()
    {
        CurrentContext.CurrentAccountId = null;
        CurrentContext.CurrentAccountName = null;
        CurrentContext.CurrentCategoryId = null;
        CurrentContext.CurrentCategoryName = null;
        CurrentContext.PageType = null;
        CurrentContext.CalendarViewedYear = null;
        CurrentContext.CalendarViewedMonth = null;
        CurrentContext.SelectedDate = null;
        OnContextChanged();
    }

    private void OnContextChanged()
    {
        ContextChanged?.Invoke(this, EventArgs.Empty);
    }
}
