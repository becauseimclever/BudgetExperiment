// <copyright file="ToastServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="ToastService"/> class.
/// </summary>
public sealed class ToastServiceTests : IDisposable
{
    private readonly ToastService _sut = new();

    /// <summary>
    /// Verifies that ShowSuccess adds a success toast.
    /// </summary>
    [Fact]
    public void ShowSuccess_AddsSuccessToast()
    {
        // Act
        _sut.ShowSuccess("Transaction saved.");

        // Assert
        Assert.Single(_sut.Toasts);
        Assert.Equal(ToastLevel.Success, _sut.Toasts[0].Level);
        Assert.Equal("Transaction saved.", _sut.Toasts[0].Message);
    }

    /// <summary>
    /// Verifies that ShowError adds an error toast.
    /// </summary>
    [Fact]
    public void ShowError_AddsErrorToast()
    {
        // Act
        _sut.ShowError("Something went wrong.", "Error");

        // Assert
        Assert.Single(_sut.Toasts);
        Assert.Equal(ToastLevel.Error, _sut.Toasts[0].Level);
        Assert.Equal("Something went wrong.", _sut.Toasts[0].Message);
        Assert.Equal("Error", _sut.Toasts[0].Title);
    }

    /// <summary>
    /// Verifies that ShowInfo adds an info toast.
    /// </summary>
    [Fact]
    public void ShowInfo_AddsInfoToast()
    {
        // Act
        _sut.ShowInfo("FYI message.");

        // Assert
        Assert.Single(_sut.Toasts);
        Assert.Equal(ToastLevel.Info, _sut.Toasts[0].Level);
    }

    /// <summary>
    /// Verifies that ShowWarning adds a warning toast.
    /// </summary>
    [Fact]
    public void ShowWarning_AddsWarningToast()
    {
        // Act
        _sut.ShowWarning("Watch out!");

        // Assert
        Assert.Single(_sut.Toasts);
        Assert.Equal(ToastLevel.Warning, _sut.Toasts[0].Level);
    }

    /// <summary>
    /// Verifies that multiple toasts can be added and are tracked.
    /// </summary>
    [Fact]
    public void Show_MultipleTimes_AccumulatesToasts()
    {
        // Act
        _sut.ShowSuccess("First");
        _sut.ShowError("Second");
        _sut.ShowInfo("Third");

        // Assert
        Assert.Equal(3, _sut.Toasts.Count);
    }

    /// <summary>
    /// Verifies that Remove removes a specific toast.
    /// </summary>
    [Fact]
    public void Remove_RemovesToastById()
    {
        // Arrange
        _sut.ShowSuccess("Keep me");
        _sut.ShowError("Remove me");
        var toRemove = _sut.Toasts[1].Id;

        // Act
        _sut.Remove(toRemove);

        // Assert
        Assert.Single(_sut.Toasts);
        Assert.Equal("Keep me", _sut.Toasts[0].Message);
    }

    /// <summary>
    /// Verifies that Remove with unknown ID does nothing.
    /// </summary>
    [Fact]
    public void Remove_WithUnknownId_DoesNothing()
    {
        // Arrange
        _sut.ShowSuccess("Stay");

        // Act
        _sut.Remove(Guid.NewGuid());

        // Assert
        Assert.Single(_sut.Toasts);
    }

    /// <summary>
    /// Verifies that OnChange fires when a toast is added.
    /// </summary>
    [Fact]
    public void OnChange_FiresWhenToastAdded()
    {
        // Arrange
        var fired = false;
        _sut.OnChange += () => fired = true;

        // Act
        _sut.ShowSuccess("Notification");

        // Assert
        Assert.True(fired);
    }

    /// <summary>
    /// Verifies that OnChange fires when a toast is removed.
    /// </summary>
    [Fact]
    public void OnChange_FiresWhenToastRemoved()
    {
        // Arrange
        _sut.ShowSuccess("Will be removed");
        var toastId = _sut.Toasts[0].Id;

        var fired = false;
        _sut.OnChange += () => fired = true;

        // Act
        _sut.Remove(toastId);

        // Assert
        Assert.True(fired);
    }

    /// <summary>
    /// Verifies that toasts have unique IDs.
    /// </summary>
    [Fact]
    public void Show_AssignsUniqueIds()
    {
        // Act
        _sut.ShowSuccess("First");
        _sut.ShowSuccess("Second");

        // Assert
        Assert.NotEqual(_sut.Toasts[0].Id, _sut.Toasts[1].Id);
    }

    /// <summary>
    /// Verifies that toast has a UTC timestamp.
    /// </summary>
    [Fact]
    public void Show_SetsUtcTimestamp()
    {
        // Act
        _sut.ShowSuccess("Timestamped");

        // Assert
        Assert.NotNull(_sut.Toasts[0].CreatedAtUtc);
        Assert.True(_sut.Toasts[0].CreatedAtUtc <= DateTime.UtcNow);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _sut.Dispose();
    }
}
