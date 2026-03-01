// <copyright file="ChatContext.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Context from the current UI state to inform AI responses.
/// </summary>
/// <param name="CurrentAccountId">The currently selected account ID.</param>
/// <param name="CurrentAccountName">The currently selected account name.</param>
/// <param name="CurrentCategoryId">The currently selected category ID.</param>
/// <param name="CurrentCategoryName">The currently selected category name.</param>
/// <param name="CurrentDate">The current date being viewed.</param>
/// <param name="CurrentPage">The current page/route in the app.</param>
public sealed record ChatContext(
    Guid? CurrentAccountId = null,
    string? CurrentAccountName = null,
    Guid? CurrentCategoryId = null,
    string? CurrentCategoryName = null,
    DateOnly? CurrentDate = null,
    string? CurrentPage = null);
