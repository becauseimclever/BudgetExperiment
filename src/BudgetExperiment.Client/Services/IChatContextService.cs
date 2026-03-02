// <copyright file="IChatContextService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Services;

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
