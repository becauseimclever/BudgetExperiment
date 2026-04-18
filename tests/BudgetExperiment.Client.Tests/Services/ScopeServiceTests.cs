// <copyright file="ScopeServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="ScopeService"/> class.
/// </summary>
public sealed class ScopeServiceTests : IAsyncDisposable
{
    private readonly StubJSRuntime _jsRuntime = new();
    private readonly ScopeService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopeServiceTests"/> class.
    /// </summary>
    public ScopeServiceTests()
    {
        _sut = new ScopeService(_jsRuntime);
    }

    /// <summary>
    /// Verifies the current scope is safe for household behavior even before initialization.
    /// </summary>
    [Fact]
    public void CurrentScope_BeforeInit_ReturnsShared()
    {
        Assert.Equal(BudgetScope.Shared, _sut.CurrentScope);
    }

    /// <summary>
    /// Verifies missing persisted scope still defaults to Shared.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_NoSavedValue_DefaultsToShared()
    {
        await _sut.InitializeAsync();

        Assert.Equal(BudgetScope.Shared, _sut.CurrentScope);
    }

    /// <summary>
    /// Verifies legacy Personal selections are coerced back to Shared.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_WithSavedPersonal_DefaultsToShared()
    {
        _jsRuntime.GetItems["budget-experiment-scope"] = "Personal";

        await _sut.InitializeAsync();

        Assert.Equal(BudgetScope.Shared, _sut.CurrentScope);
    }

    /// <summary>
    /// Verifies attempts to switch to Personal still persist Shared.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetScopeAsync_WithPersonal_PersistsShared()
    {
        await _sut.SetScopeAsync(BudgetScope.Personal);

        Assert.Equal("Shared", _jsRuntime.SetItems["budget-experiment-scope"]);
    }

    /// <summary>
    /// Verifies scope change notifications stay pinned to Shared.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetScopeAsync_WithPersonal_RaisesSharedScopeChangedEvent()
    {
        BudgetScope? receivedScope = null;
        _sut.ScopeChanged += scope => receivedScope = scope;

        await _sut.SetScopeAsync(BudgetScope.Personal);

        Assert.Equal(BudgetScope.Shared, receivedScope);
    }

    /// <summary>
    /// Verifies Shared is the only remaining scope option.
    /// </summary>
    [Fact]
    public void AvailableScopes_ContainsOnlyShared()
    {
        Assert.Single(ScopeService.AvailableScopes);
        Assert.Equal(BudgetScope.Shared, ScopeService.AvailableScopes[0].Scope);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        return _sut.DisposeAsync();
    }

    /// <summary>
    /// Stub JavaScript runtime for testing localStorage interactions.
    /// </summary>
    private sealed class StubJSRuntime : IJSRuntime
    {
        /// <summary>
        /// Gets items that were set via localStorage.setItem.
        /// </summary>
        public Dictionary<string, string> SetItems { get; } = [];

        /// <summary>
        /// Gets items available for localStorage.getItem.
        /// </summary>
        public Dictionary<string, string?> GetItems { get; } = [];

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "localStorage.getItem" && args?.Length > 0)
            {
                var key = args[0]?.ToString()!;
                if (GetItems.TryGetValue(key, out var value))
                {
                    return new ValueTask<TValue>((TValue)(object)value!);
                }

                return new ValueTask<TValue>(default(TValue)!);
            }

            if (identifier == "localStorage.setItem" && args?.Length >= 2)
            {
                var key = args[0]?.ToString()!;
                var value = args[1]?.ToString()!;
                SetItems[key] = value;
            }

            return new ValueTask<TValue>(default(TValue)!);
        }
    }
}
