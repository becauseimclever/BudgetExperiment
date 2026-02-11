// <copyright file="ChatContextServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="ChatContextService"/> class.
/// </summary>
public sealed class ChatContextServiceTests
{
    /// <summary>
    /// Verifies that SetCalendarContext updates calendar and account fields.
    /// </summary>
    [Fact]
    public void SetCalendarContext_UpdatesCalendarFields()
    {
        // Arrange
        var service = new ChatContextService();
        var selectedDate = new DateOnly(2026, 1, 15);
        var accountId = Guid.NewGuid();

        // Act
        service.SetCalendarContext(2026, 1, selectedDate, accountId, "Checking");

        // Assert
        Assert.Equal(2026, service.CurrentContext.CalendarViewedYear);
        Assert.Equal(1, service.CurrentContext.CalendarViewedMonth);
        Assert.Equal(selectedDate, service.CurrentContext.SelectedDate);
        Assert.Equal(accountId, service.CurrentContext.CurrentAccountId);
        Assert.Equal("Checking", service.CurrentContext.CurrentAccountName);
    }

    /// <summary>
    /// Verifies that GetContextSummary includes calendar details.
    /// </summary>
    [Fact]
    public void GetContextSummary_IncludesCalendarContext()
    {
        // Arrange
        var service = new ChatContextService();
        service.SetCalendarContext(2026, 1, new DateOnly(2026, 1, 15), Guid.NewGuid(), "Checking");
        service.SetCategoryContext(Guid.NewGuid(), "Groceries");
        service.SetPageType("calendar");

        // Act
        var summary = service.CurrentContext.GetContextSummary();

        // Assert
        Assert.Contains("Viewing January 2026", summary, StringComparison.Ordinal);
        Assert.Contains("Selected: Jan 15", summary, StringComparison.Ordinal);
        Assert.Contains("Account: Checking", summary, StringComparison.Ordinal);
        Assert.Contains("Category: Groceries", summary, StringComparison.Ordinal);
        Assert.Contains("On the calendar page", summary, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that ToDto maps all context fields.
    /// </summary>
    [Fact]
    public void ToDto_MapsContextValues()
    {
        // Arrange
        var service = new ChatContextService();
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var selectedDate = new DateOnly(2026, 2, 10);

        service.SetAccountContext(accountId, "Savings");
        service.SetCategoryContext(categoryId, "Utilities");
        service.SetCalendarContext(2026, 2, selectedDate);
        service.SetPageType("calendar");

        // Act
        var dto = service.ToDto();

        // Assert
        Assert.Equal(accountId, dto.CurrentAccountId);
        Assert.Equal("Savings", dto.CurrentAccountName);
        Assert.Equal(categoryId, dto.CurrentCategoryId);
        Assert.Equal("Utilities", dto.CurrentCategoryName);
        Assert.Equal(2026, dto.CalendarViewedYear);
        Assert.Equal(2, dto.CalendarViewedMonth);
        Assert.Equal(selectedDate, dto.SelectedDate);
        Assert.Equal("calendar", dto.PageType);
    }

    /// <summary>
    /// Verifies that ClearContext clears calendar fields.
    /// </summary>
    [Fact]
    public void ClearContext_ClearsCalendarContext()
    {
        // Arrange
        var service = new ChatContextService();
        service.SetCalendarContext(2026, 1, new DateOnly(2026, 1, 15), Guid.NewGuid(), "Checking");
        service.SetCategoryContext(Guid.NewGuid(), "Groceries");
        service.SetPageType("calendar");

        // Act
        service.ClearContext();

        // Assert
        Assert.Null(service.CurrentContext.CalendarViewedYear);
        Assert.Null(service.CurrentContext.CalendarViewedMonth);
        Assert.Null(service.CurrentContext.SelectedDate);
        Assert.Null(service.CurrentContext.CurrentAccountId);
        Assert.Null(service.CurrentContext.CurrentAccountName);
        Assert.Null(service.CurrentContext.CurrentCategoryId);
        Assert.Null(service.CurrentContext.CurrentCategoryName);
        Assert.Null(service.CurrentContext.PageType);
    }
}
