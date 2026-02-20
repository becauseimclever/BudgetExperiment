// <copyright file="IFormStateService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service for preserving and restoring form data across session re-authentication.
/// Forms register their data providers, and when a session expires the service
/// persists all registered form state to localStorage so it survives a redirect.
/// </summary>
public interface IFormStateService
{
    /// <summary>
    /// Registers a form's data provider so its state can be saved on session expiry.
    /// </summary>
    /// <param name="formKey">A unique key identifying the form (e.g., "TransactionForm").</param>
    /// <param name="dataProvider">A function that returns the current form data to serialize.</param>
    void RegisterForm(string formKey, Func<object?> dataProvider);

    /// <summary>
    /// Unregisters a form's data provider (call on component dispose).
    /// </summary>
    /// <param name="formKey">The form key to unregister.</param>
    void UnregisterForm(string formKey);

    /// <summary>
    /// Saves all registered form data to localStorage.
    /// Called by the token refresh handler when session refresh fails.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    Task SaveAllAsync();

    /// <summary>
    /// Restores previously saved form data for a specific form key.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the saved data into.</typeparam>
    /// <param name="formKey">The form key to restore data for.</param>
    /// <returns>The restored data, or default if none was saved.</returns>
    Task<T?> RestoreAsync<T>(string formKey);

    /// <summary>
    /// Clears saved form data for a specific form key.
    /// </summary>
    /// <param name="formKey">The form key to clear.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ClearAsync(string formKey);

    /// <summary>
    /// Checks whether there is saved state for a specific form key.
    /// </summary>
    /// <param name="formKey">The form key to check.</param>
    /// <returns>True if saved state exists; otherwise false.</returns>
    Task<bool> HasSavedStateAsync(string formKey);
}
