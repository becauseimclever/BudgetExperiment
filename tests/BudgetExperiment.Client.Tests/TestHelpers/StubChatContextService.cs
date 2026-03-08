// <copyright file="StubChatContextService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Tests.TestHelpers;

/// <summary>
/// Shared stub implementation of <see cref="IChatContextService"/> for page-level bUnit tests.
/// </summary>
internal sealed class StubChatContextService : IChatContextService
{
    private readonly ChatPageContext _context = new();

    /// <inheritdoc/>
    public event EventHandler? ContextChanged;

    /// <inheritdoc/>
    public ChatPageContext CurrentContext => this._context;

    /// <inheritdoc/>
    public void SetAccountContext(Guid? accountId, string? accountName)
    {
        this._context.CurrentAccountId = accountId;
        this._context.CurrentAccountName = accountName;
        this.ContextChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void SetCategoryContext(Guid? categoryId, string? categoryName)
    {
        this._context.CurrentCategoryId = categoryId;
        this._context.CurrentCategoryName = categoryName;
        this.ContextChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void SetPageType(string? pageType)
    {
        this._context.PageType = pageType;
        this.ContextChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void SetCalendarContext(int year, int month, DateOnly? selectedDate, Guid? accountId = null, string? accountName = null)
    {
        this._context.CalendarViewedYear = year;
        this._context.CalendarViewedMonth = month;
        this._context.SelectedDate = selectedDate;

        if (accountId.HasValue || !string.IsNullOrWhiteSpace(accountName))
        {
            this._context.CurrentAccountId = accountId;
            this._context.CurrentAccountName = accountName;
        }

        this.ContextChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public ChatContextDto ToDto()
    {
        return new ChatContextDto
        {
            CurrentAccountId = this._context.CurrentAccountId,
            CurrentAccountName = this._context.CurrentAccountName,
            CurrentCategoryId = this._context.CurrentCategoryId,
            CurrentCategoryName = this._context.CurrentCategoryName,
            CalendarViewedYear = this._context.CalendarViewedYear,
            CalendarViewedMonth = this._context.CalendarViewedMonth,
            SelectedDate = this._context.SelectedDate,
            PageType = this._context.PageType,
        };
    }

    /// <inheritdoc/>
    public void ClearContext()
    {
        this._context.CurrentAccountId = null;
        this._context.CurrentAccountName = null;
        this._context.CurrentCategoryId = null;
        this._context.CurrentCategoryName = null;
        this._context.CalendarViewedYear = null;
        this._context.CalendarViewedMonth = null;
        this._context.SelectedDate = null;
        this._context.PageType = null;
        this.ContextChanged?.Invoke(this, EventArgs.Empty);
    }
}
