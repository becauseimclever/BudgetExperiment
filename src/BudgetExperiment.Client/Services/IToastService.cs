// <copyright file="IToastService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Defines the toast notification levels.
/// </summary>
public enum ToastLevel
{
    /// <summary>Informational toast.</summary>
    Info,

    /// <summary>Success toast.</summary>
    Success,

    /// <summary>Warning toast.</summary>
    Warning,

    /// <summary>Error toast.</summary>
    Error,
}

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
    IReadOnlyList<ToastItem> Toasts { get; }

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
