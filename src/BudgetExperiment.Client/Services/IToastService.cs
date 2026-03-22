// <copyright file="IToastService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service for displaying toast notifications.
/// </summary>
public interface IToastService
{
    /// <summary>
    /// Occurs when a toast is added or removed.
    /// </summary>
    event Action? OnChange;

    /// <summary>
    /// Gets the currently active toasts.
    /// </summary>
    IReadOnlyList<ToastItem> Toasts
    {
        get;
    }

    /// <summary>
    /// Shows a success toast notification.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title.</param>
    void ShowSuccess(string message, string? title = null);

    /// <summary>
    /// Shows an error toast notification.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title.</param>
    void ShowError(string message, string? title = null);

    /// <summary>
    /// Shows an informational toast notification.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title.</param>
    void ShowInfo(string message, string? title = null);

    /// <summary>
    /// Shows a warning toast notification.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title.</param>
    void ShowWarning(string message, string? title = null);

    /// <summary>
    /// Removes a toast by its ID.
    /// </summary>
    /// <param name="id">The toast ID to remove.</param>
    void Remove(Guid id);
}
