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
    /// Verifies that the current scope defaults to null (All) before initialization.
    /// </summary>
    [Fact]
    public void CurrentScope_BeforeInit_ReturnsNull()
    {
        Assert.Null(_sut.CurrentScope);
    }

    /// <summary>
    /// Verifies that GetCurrentScopeDisplayName returns "All" for null scope.
    /// </summary>
    [Fact]
    public void GetCurrentScopeDisplayName_NullScope_ReturnsAll()
    {
        Assert.Equal("All", _sut.GetCurrentScopeDisplayName());
    }

    /// <summary>
    /// Verifies that GetCurrentScopeDisplayName returns correct name after setting scope.
    /// </summary>
    /// <param name="scope">The scope to set.</param>
    /// <param name="expected">The expected display name.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData(BudgetScope.Shared, "Shared")]
    [InlineData(BudgetScope.Personal, "Personal")]
    public async Task GetCurrentScopeDisplayName_AfterSet_ReturnsCorrectName(BudgetScope scope, string expected)
    {
        // Act
        await _sut.SetScopeAsync(scope);

        // Assert
        Assert.Equal(expected, _sut.GetCurrentScopeDisplayName());
    }

    /// <summary>
    /// Verifies that GetCurrentScopeIcon returns correct icon for each scope.
    /// </summary>
    [Fact]
    public void GetCurrentScopeIcon_NullScope_ReturnsLayersIcon()
    {
        Assert.Equal("layers", _sut.GetCurrentScopeIcon());
    }

    /// <summary>
    /// Verifies that GetCurrentScopeIcon returns correct icon after setting scope.
    /// </summary>
    /// <param name="scope">The scope to set.</param>
    /// <param name="expected">The expected icon name.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData(BudgetScope.Shared, "home")]
    [InlineData(BudgetScope.Personal, "user")]
    public async Task GetCurrentScopeIcon_AfterSet_ReturnsCorrectIcon(BudgetScope scope, string expected)
    {
        // Act
        await _sut.SetScopeAsync(scope);

        // Assert
        Assert.Equal(expected, _sut.GetCurrentScopeIcon());
    }

    /// <summary>
    /// Verifies that SetScopeAsync fires the ScopeChanged event.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetScopeAsync_FiresScopeChangedEvent()
    {
        // Arrange
        BudgetScope? receivedScope = BudgetScope.Shared; // sentinel
        bool eventFired = false;
        _sut.ScopeChanged += scope =>
        {
            eventFired = true;
            receivedScope = scope;
        };

        // Act
        await _sut.SetScopeAsync(BudgetScope.Personal);

        // Assert
        Assert.True(eventFired);
        Assert.Equal(BudgetScope.Personal, receivedScope);
    }

    /// <summary>
    /// Verifies that SetScopeAsync with null fires event with null (All).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetScopeAsync_Null_FiresEventWithNull()
    {
        // Arrange
        BudgetScope? receivedScope = BudgetScope.Shared; // sentinel
        _sut.ScopeChanged += scope => receivedScope = scope;

        // Act
        await _sut.SetScopeAsync(null);

        // Assert
        Assert.Null(receivedScope);
    }

    /// <summary>
    /// Verifies that SetScopeAsync persists value to localStorage.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetScopeAsync_PersistsToLocalStorage()
    {
        // Act
        await _sut.SetScopeAsync(BudgetScope.Personal);

        // Assert
        Assert.True(_jsRuntime.SetItems.ContainsKey("budget-experiment-scope"));
        Assert.Equal("Personal", _jsRuntime.SetItems["budget-experiment-scope"]);
    }

    /// <summary>
    /// Verifies that SetScopeAsync with null persists "All" to localStorage.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetScopeAsync_Null_PersistsAllToLocalStorage()
    {
        // Act
        await _sut.SetScopeAsync(null);

        // Assert
        Assert.Equal("All", _jsRuntime.SetItems["budget-experiment-scope"]);
    }

    /// <summary>
    /// Verifies that InitializeAsync restores a saved Shared scope.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_WithSavedShared_RestoresScope()
    {
        // Arrange
        _jsRuntime.GetItems["budget-experiment-scope"] = "Shared";

        // Act
        await _sut.InitializeAsync();

        // Assert
        Assert.Equal(BudgetScope.Shared, _sut.CurrentScope);
    }

    /// <summary>
    /// Verifies that InitializeAsync restores a saved Personal scope.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_WithSavedPersonal_RestoresScope()
    {
        // Arrange
        _jsRuntime.GetItems["budget-experiment-scope"] = "Personal";

        // Act
        await _sut.InitializeAsync();

        // Assert
        Assert.Equal(BudgetScope.Personal, _sut.CurrentScope);
    }

    /// <summary>
    /// Verifies that InitializeAsync with no saved value defaults to null (All).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_NoSavedValue_DefaultsToNull()
    {
        // Act
        await _sut.InitializeAsync();

        // Assert
        Assert.Null(_sut.CurrentScope);
    }

    /// <summary>
    /// Verifies that InitializeAsync only runs once.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_CalledTwice_OnlyInitializesOnce()
    {
        // Arrange
        _jsRuntime.GetItems["budget-experiment-scope"] = "Shared";
        await _sut.InitializeAsync();

        // Change the stored value
        _jsRuntime.GetItems["budget-experiment-scope"] = "Personal";

        // Act
        await _sut.InitializeAsync();

        // Assert — still Shared, not re-read
        Assert.Equal(BudgetScope.Shared, _sut.CurrentScope);
    }

    /// <summary>
    /// Verifies that AvailableScopes contains the expected three options.
    /// </summary>
    [Fact]
    public void AvailableScopes_ContainsThreeOptions()
    {
        Assert.Equal(3, ScopeService.AvailableScopes.Count);
    }

    /// <summary>
    /// Verifies that AvailableScopes includes Shared, Personal, and All.
    /// </summary>
    [Fact]
    public void AvailableScopes_ContainsExpectedScopes()
    {
        Assert.Contains(ScopeService.AvailableScopes, s => s.Scope == BudgetScope.Shared);
        Assert.Contains(ScopeService.AvailableScopes, s => s.Scope == BudgetScope.Personal);
        Assert.Contains(ScopeService.AvailableScopes, s => s.Scope == null && s.DisplayName == "All");
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
