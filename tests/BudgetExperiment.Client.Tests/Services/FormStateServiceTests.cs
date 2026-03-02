// <copyright file="FormStateServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;

using BudgetExperiment.Client.Services;

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="FormStateService"/> class.
/// </summary>
public sealed class FormStateServiceTests
{
    private readonly StubJSRuntime _jsRuntime = new();
    private readonly FormStateService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormStateServiceTests"/> class.
    /// </summary>
    public FormStateServiceTests()
    {
        _sut = new FormStateService(_jsRuntime);
    }

    /// <summary>
    /// Verifies that SaveAllAsync saves registered form data to localStorage.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveAllAsync_SavesRegisteredFormData()
    {
        // Arrange
        var formData = new TestFormData { Name = "Test", Amount = 42.5m };
        _sut.RegisterForm("TestForm", () => formData);

        // Act
        await _sut.SaveAllAsync();

        // Assert
        Assert.True(_jsRuntime.SetItems.ContainsKey("budget-form-state:TestForm"));
        var saved = _jsRuntime.SetItems["budget-form-state:TestForm"];
        Assert.Contains("Test", saved);
        Assert.Contains("42.5", saved);
    }

    /// <summary>
    /// Verifies that SaveAllAsync skips forms that return null data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveAllAsync_SkipsNullData()
    {
        // Arrange
        _sut.RegisterForm("EmptyForm", () => null);

        // Act
        await _sut.SaveAllAsync();

        // Assert
        Assert.False(_jsRuntime.SetItems.ContainsKey("budget-form-state:EmptyForm"));
    }

    /// <summary>
    /// Verifies that SaveAllAsync saves multiple registered forms.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveAllAsync_SavesMultipleForms()
    {
        // Arrange
        _sut.RegisterForm("Form1", () => new TestFormData { Name = "First" });
        _sut.RegisterForm("Form2", () => new TestFormData { Name = "Second" });

        // Act
        await _sut.SaveAllAsync();

        // Assert
        Assert.Equal(2, _jsRuntime.SetItems.Count);
        Assert.True(_jsRuntime.SetItems.ContainsKey("budget-form-state:Form1"));
        Assert.True(_jsRuntime.SetItems.ContainsKey("budget-form-state:Form2"));
    }

    /// <summary>
    /// Verifies that UnregisterForm prevents future saves for that form.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UnregisterForm_PreventsFormFromBeingSaved()
    {
        // Arrange
        _sut.RegisterForm("TestForm", () => new TestFormData { Name = "Test" });
        _sut.UnregisterForm("TestForm");

        // Act
        await _sut.SaveAllAsync();

        // Assert
        Assert.Empty(_jsRuntime.SetItems);
    }

    /// <summary>
    /// Verifies that RestoreAsync returns deserialized data from localStorage.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RestoreAsync_ReturnsDeserializedData()
    {
        // Arrange
        var formData = new TestFormData { Name = "Restored", Amount = 99.99m };
        var json = JsonSerializer.Serialize(formData);
        _jsRuntime.GetItems["budget-form-state:TestForm"] = json;

        // Act
        var result = await _sut.RestoreAsync<TestFormData>("TestForm");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Restored", result!.Name);
        Assert.Equal(99.99m, result.Amount);
    }

    /// <summary>
    /// Verifies that RestoreAsync returns default when no data is saved.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RestoreAsync_ReturnsDefault_WhenNoDataSaved()
    {
        // Act
        var result = await _sut.RestoreAsync<TestFormData>("NonExistent");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that ClearAsync removes saved data from localStorage.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClearAsync_RemovesSavedData()
    {
        // Arrange
        _jsRuntime.GetItems["budget-form-state:TestForm"] = "{}";

        // Act
        await _sut.ClearAsync("TestForm");

        // Assert
        Assert.Contains("budget-form-state:TestForm", _jsRuntime.RemovedItems);
    }

    /// <summary>
    /// Verifies that HasSavedStateAsync returns true when data exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HasSavedStateAsync_ReturnsTrue_WhenDataExists()
    {
        // Arrange
        _jsRuntime.GetItems["budget-form-state:TestForm"] = "{\"Name\":\"Test\"}";

        // Act
        var result = await _sut.HasSavedStateAsync("TestForm");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies that HasSavedStateAsync returns false when no data exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HasSavedStateAsync_ReturnsFalse_WhenNoData()
    {
        // Act
        var result = await _sut.HasSavedStateAsync("TestForm");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that registering duplicate keys replaces the previous provider.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RegisterForm_DuplicateKey_ReplacesProvider()
    {
        // Arrange
        _sut.RegisterForm("TestForm", () => new TestFormData { Name = "Old" });
        _sut.RegisterForm("TestForm", () => new TestFormData { Name = "New" });

        // Act
        await _sut.SaveAllAsync();

        // Assert
        Assert.Single(_jsRuntime.SetItems);
        Assert.Contains("New", _jsRuntime.SetItems["budget-form-state:TestForm"]);
    }

    /// <summary>
    /// Verifies that SaveAllAsync handles exceptions from data providers gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveAllAsync_HandlesProviderException_Gracefully()
    {
        // Arrange
        _sut.RegisterForm("BadForm", () => throw new InvalidOperationException("boom"));
        _sut.RegisterForm("GoodForm", () => new TestFormData { Name = "OK" });

        // Act
        await _sut.SaveAllAsync();

        // Assert — GoodForm should still be saved despite BadForm throwing
        Assert.True(_jsRuntime.SetItems.ContainsKey("budget-form-state:GoodForm"));
    }

    /// <summary>
    /// Test data class for form state tests.
    /// </summary>
    private sealed class TestFormData
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the amount.
        /// </summary>
        public decimal Amount { get; set; }
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

        /// <summary>
        /// Gets keys that were removed via localStorage.removeItem.
        /// </summary>
        public List<string> RemovedItems { get; } = [];

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

            if (identifier == "localStorage.removeItem" && args?.Length > 0)
            {
                var key = args[0]?.ToString()!;
                RemovedItems.Add(key);
            }

            return new ValueTask<TValue>(default(TValue)!);
        }
    }
}
