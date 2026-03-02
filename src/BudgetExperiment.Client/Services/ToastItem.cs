// <copyright file="ToastItem.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Represents an active toast notification.
/// </summary>
/// <param name="Id">Unique identifier for the toast.</param>
/// <param name="Level">The severity level of the toast.</param>
/// <param name="Message">The message to display.</param>
/// <param name="Title">Optional title for the toast.</param>
/// <param name="CreatedAtUtc">When the toast was created.</param>
public sealed record ToastItem(
    Guid Id,
    ToastLevel Level,
    string Message,
    string? Title = null,
    DateTime? CreatedAtUtc = null);
