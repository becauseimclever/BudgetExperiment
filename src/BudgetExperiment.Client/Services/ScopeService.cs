// -----------------------------------------------------------------------
// <copyright file="ScopeService.cs" company="BecauseImClever">
//     Copyright (c) BecauseImClever. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service for managing the current budget scope selection.
/// </summary>
public sealed class ScopeService : IAsyncDisposable
{
    private const string StorageKey = "budget-experiment-scope";

    private readonly IJSRuntime jsRuntime;
    private BudgetScope? currentScope = BudgetScope.Shared;
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
    public static IReadOnlyList<ScopeOption> AvailableScopes
    {
        get;
    }

    = new List<ScopeOption>
    {
        new(BudgetScope.Shared, "Shared", "home", "Household budget visible to all family members"),
    };

    /// <summary>
    /// Gets the current scope.
    /// </summary>
    public BudgetScope? CurrentScope => currentScope;

    /// <summary>
    /// Initializes the scope service with the household default.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task InitializeAsync()
    {
        if (isInitialized)
        {
            return;
        }

        try
        {
            var savedScope = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            currentScope = BudgetScope.Shared;

            if (!string.Equals(savedScope, BudgetScope.Shared.ToString(), StringComparison.Ordinal))
            {
                await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, BudgetScope.Shared.ToString());
            }
        }
        catch (JSException)
        {
            // JS interop not available (e.g., prerendering)
            currentScope = BudgetScope.Shared;
        }

        isInitialized = true;
    }

    /// <summary>
    /// Sets the current scope.
    /// </summary>
    /// <param name="scope">The requested scope.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SetScopeAsync(BudgetScope? scope)
    {
        _ = scope;
        currentScope = BudgetScope.Shared;

        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, BudgetScope.Shared.ToString());
        }
        catch (JSException)
        {
            // JS interop not available
        }

        this.ScopeChanged?.Invoke(BudgetScope.Shared);
    }

    /// <summary>
    /// Gets the display name for the current scope.
    /// </summary>
    /// <returns>The display name.</returns>
    public string GetCurrentScopeDisplayName()
    {
        return currentScope switch
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
        return currentScope switch
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
