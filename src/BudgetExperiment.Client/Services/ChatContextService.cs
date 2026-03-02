// <copyright file="ChatContextService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Services;

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
