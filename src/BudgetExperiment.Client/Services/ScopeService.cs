// -----------------------------------------------------------------------
// <copyright file="ScopeService.cs" company="BecauseImClever">
//     Copyright (c) BecauseImClever. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using BudgetExperiment.Domain;

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service for managing the current budget scope selection (Shared, Personal, or All).
/// </summary>
public sealed class ScopeService : IAsyncDisposable
{
    private const string StorageKey = "budget-experiment-scope";

    private readonly IJSRuntime jsRuntime;
    private BudgetScope? currentScope;
    private bool isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopeService"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime.</param>
    public ScopeService(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Event raised when the scope changes.
    /// </summary>
    public event Action<BudgetScope?>? ScopeChanged;

    /// <summary>
    /// Gets the available scope options.
    /// </summary>
    public static IReadOnlyList<ScopeOption> AvailableScopes { get; } = new List<ScopeOption>
    {
        new(BudgetScope.Shared, "Shared", "home", "Household budget visible to all family members"),
        new(BudgetScope.Personal, "Personal", "user", "Your private budget"),
        new(null, "All", "layers", "View both shared and personal items"),
    };

    /// <summary>
    /// Gets the current scope. Null means "All" (both Shared and Personal).
    /// </summary>
    public BudgetScope? CurrentScope => this.currentScope;

    /// <summary>
    /// Initializes the scope service by loading the saved scope from localStorage.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task InitializeAsync()
    {
        if (this.isInitialized)
        {
            return;
        }

        try
        {
            var savedScope = await this.jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);

            this.currentScope = savedScope switch
            {
                "Shared" => BudgetScope.Shared,
                "Personal" => BudgetScope.Personal,
                _ => null, // "All" or unset defaults to null (all scopes)
            };

            this.isInitialized = true;
        }
        catch (JSException)
        {
            // JS interop not available (e.g., prerendering)
            this.currentScope = null;
        }
    }

    /// <summary>
    /// Sets the current scope.
    /// </summary>
    /// <param name="scope">The scope to set. Null means "All".</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SetScopeAsync(BudgetScope? scope)
    {
        this.currentScope = scope;

        try
        {
            var storageValue = scope?.ToString() ?? "All";
            await this.jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, storageValue);
        }
        catch (JSException)
        {
            // JS interop not available
        }

        this.ScopeChanged?.Invoke(scope);
    }

    /// <summary>
    /// Gets the display name for the current scope.
    /// </summary>
    /// <returns>The display name.</returns>
    public string GetCurrentScopeDisplayName()
    {
        return this.currentScope switch
        {
            BudgetScope.Shared => "Shared",
            BudgetScope.Personal => "Personal",
            _ => "All",
        };
    }

    /// <summary>
    /// Gets the icon name for the current scope.
    /// </summary>
    /// <returns>The icon name.</returns>
    public string GetCurrentScopeIcon()
    {
        return this.currentScope switch
        {
            BudgetScope.Shared => "home",
            BudgetScope.Personal => "user",
            _ => "layers",
        };
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
