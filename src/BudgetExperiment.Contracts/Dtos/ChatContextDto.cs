// <copyright file="ChatContextDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Context from the client UI to inform AI responses.
/// </summary>
public sealed class ChatContextDto
{
    /// <summary>
    /// Gets or sets the currently selected account ID.
    /// </summary>
    public Guid? CurrentAccountId { get; set; }

    /// <summary>
    /// Gets or sets the currently selected account name.
    /// </summary>
    public string? CurrentAccountName { get; set; }

    /// <summary>
    /// Gets or sets the currently selected category ID.
    /// </summary>
    public Guid? CurrentCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the currently selected category name.
    /// </summary>
    public string? CurrentCategoryName { get; set; }

    /// <summary>
    /// Gets or sets the year being viewed on the calendar.
    /// </summary>
    public int? CalendarViewedYear { get; set; }

    /// <summary>
    /// Gets or sets the month being viewed on the calendar (1-12).
    /// </summary>
    public int? CalendarViewedMonth { get; set; }

    /// <summary>
    /// Gets or sets the selected date on the calendar (if any).
    /// </summary>
    public DateOnly? SelectedDate { get; set; }

    /// <summary>
    /// Gets or sets the current page type (e.g., "calendar", "transactions").
    /// </summary>
    public string? PageType { get; set; }
}
