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
    public ChatPageContext CurrentContext => _context;

    /// <inheritdoc/>
    public void SetAccountContext(Guid? accountId, string? accountName)
    {
        _context.CurrentAccountId = accountId;
        _context.CurrentAccountName = accountName;
        this.ContextChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void SetCategoryContext(Guid? categoryId, string? categoryName)
    {
        _context.CurrentCategoryId = categoryId;
        _context.CurrentCategoryName = categoryName;
        this.ContextChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void SetPageType(string? pageType)
    {
        _context.PageType = pageType;
        this.ContextChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void SetCalendarContext(int year, int month, DateOnly? selectedDate, Guid? accountId = null, string? accountName = null)
    {
        _context.CalendarViewedYear = year;
        _context.CalendarViewedMonth = month;
        _context.SelectedDate = selectedDate;

        if (accountId.HasValue || !string.IsNullOrWhiteSpace(accountName))
        {
            _context.CurrentAccountId = accountId;
            _context.CurrentAccountName = accountName;
        }

        this.ContextChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public ChatContextDto ToDto()
    {
        return new ChatContextDto
        {
            CurrentAccountId = _context.CurrentAccountId,
            CurrentAccountName = _context.CurrentAccountName,
            CurrentCategoryId = _context.CurrentCategoryId,
            CurrentCategoryName = _context.CurrentCategoryName,
            CalendarViewedYear = _context.CalendarViewedYear,
            CalendarViewedMonth = _context.CalendarViewedMonth,
            SelectedDate = _context.SelectedDate,
            PageType = _context.PageType,
        };
    }

    /// <inheritdoc/>
    public void ClearContext()
    {
        _context.CurrentAccountId = null;
        _context.CurrentAccountName = null;
        _context.CurrentCategoryId = null;
        _context.CurrentCategoryName = null;
        _context.CalendarViewedYear = null;
        _context.CalendarViewedMonth = null;
        _context.SelectedDate = null;
        _context.PageType = null;
        this.ContextChanged?.Invoke(this, EventArgs.Empty);
    }
}
