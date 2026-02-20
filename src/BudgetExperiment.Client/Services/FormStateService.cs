// <copyright file="FormStateService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Manages form data preservation across session re-authentication using localStorage.
/// Forms register data providers, and when a session expires all registered state is
/// persisted so it can be restored after the user logs back in.
/// </summary>
public sealed class FormStateService : IFormStateService
{
    private const string _storageKeyPrefix = "budget-form-state:";

    private readonly IJSRuntime _jsRuntime;
    private readonly Dictionary<string, Func<object?>> _registeredForms = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="FormStateService"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime for localStorage access.</param>
    public FormStateService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc/>
    public void RegisterForm(string formKey, Func<object?> dataProvider)
    {
        _registeredForms[formKey] = dataProvider;
    }

    /// <inheritdoc/>
    public void UnregisterForm(string formKey)
    {
        _registeredForms.Remove(formKey);
    }

    /// <inheritdoc/>
    public async Task SaveAllAsync()
    {
        foreach (var (formKey, dataProvider) in _registeredForms)
        {
            try
            {
                var data = dataProvider();
                if (data is null)
                {
                    continue;
                }

                var json = JsonSerializer.Serialize(data);
                await _jsRuntime.InvokeVoidAsync(
                    "localStorage.setItem",
                    _storageKeyPrefix + formKey,
                    json);
            }
            catch (Exception)
            {
                // Swallow exceptions from individual form providers;
                // other forms should still be saved.
            }
        }
    }

    /// <inheritdoc/>
    public async Task<T?> RestoreAsync<T>(string formKey)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem",
                _storageKeyPrefix + formKey);

            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception)
        {
            return default;
        }
    }

    /// <inheritdoc/>
    public async Task ClearAsync(string formKey)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "localStorage.removeItem",
                _storageKeyPrefix + formKey);
        }
        catch (Exception)
        {
            // localStorage may not be available
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasSavedStateAsync(string formKey)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem",
                _storageKeyPrefix + formKey);

            return !string.IsNullOrEmpty(json);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
