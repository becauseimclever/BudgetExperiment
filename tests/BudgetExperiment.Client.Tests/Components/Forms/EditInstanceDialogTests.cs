// <copyright file="EditInstanceDialogTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Forms;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Forms;

/// <summary>
/// Unit tests for the <see cref="EditInstanceDialog"/> component.
/// </summary>
public sealed class EditInstanceDialogTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EditInstanceDialogTests"/> class.
    /// </summary>
    public EditInstanceDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
        Services.AddSingleton<CultureService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the scheduled date is displayed in the info box.
    /// </summary>
    [Fact]
    public void Render_ShowsScheduledDate()
    {
        // Arrange
        var date = new DateOnly(2025, 3, 15);

        // Act
        var cut = RenderDialog(scheduledDate: date);

        // Assert - "Saturday, March 15, 2025"
        Assert.Contains("March 15, 2025", cut.Markup);
    }

    /// <summary>
    /// Verifies that the original description is displayed.
    /// </summary>
    [Fact]
    public void Render_ShowsOriginalDescription()
    {
        // Act
        var cut = RenderDialog(description: "Mortgage Payment");

        // Assert
        Assert.Contains("Mortgage Payment", cut.Markup);
    }

    /// <summary>
    /// Verifies that the account name is displayed when provided.
    /// </summary>
    [Fact]
    public void Render_WithAccountName_ShowsAccountName()
    {
        // Act
        var cut = RenderDialog(accountName: "Checking Account");

        // Assert
        Assert.Contains("Checking Account", cut.Markup);
    }

    /// <summary>
    /// Verifies that submit with empty fields produces null values in DTO.
    /// </summary>
    [Fact]
    public void HandleSubmit_EmptyFields_ProducesNullsInDto()
    {
        // Arrange
        RecurringInstanceModifyDto? savedDto = null;
        var cut = Render<EditInstanceDialog>(p => p
            .Add(x => x.ScheduledDate, new DateOnly(2025, 3, 15))
            .Add(x => x.OriginalDescription, "Mortgage")
            .Add(x => x.OriginalAmount, 1500m)
            .Add(x => x.OnSave, (RecurringInstanceModifyDto dto) => savedDto = dto));

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.NotNull(savedDto);
        Assert.Null(savedDto!.Description);
        Assert.Null(savedDto.Amount);
        Assert.Null(savedDto.Date);
    }

    /// <summary>
    /// Verifies that submit with modified description sets it in DTO.
    /// </summary>
    [Fact]
    public void HandleSubmit_WithDescription_SetsDescriptionInDto()
    {
        // Arrange
        RecurringInstanceModifyDto? savedDto = null;
        var cut = Render<EditInstanceDialog>(p => p
            .Add(x => x.ScheduledDate, new DateOnly(2025, 3, 15))
            .Add(x => x.OriginalDescription, "Mortgage")
            .Add(x => x.OriginalAmount, 1500m)
            .Add(x => x.OnSave, (RecurringInstanceModifyDto dto) => savedDto = dto));

        // Act
        cut.Find("#description").Change("Modified Mortgage");
        cut.Find("form").Submit();

        // Assert
        Assert.NotNull(savedDto);
        Assert.Equal("Modified Mortgage", savedDto!.Description);
    }

    /// <summary>
    /// Verifies that cancel invokes OnCancel callback.
    /// </summary>
    [Fact]
    public void HandleCancel_InvokesOnCancelCallback()
    {
        // Arrange
        var cancelCalled = false;
        var cut = Render<EditInstanceDialog>(p => p
            .Add(x => x.ScheduledDate, new DateOnly(2025, 3, 15))
            .Add(x => x.OriginalDescription, "Mortgage")
            .Add(x => x.OriginalAmount, 1500m)
            .Add(x => x.OnCancel, () => cancelCalled = true));

        // Act
        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelBtn.Click();

        // Assert
        Assert.True(cancelCalled);
    }

    /// <summary>
    /// Verifies the submit button text.
    /// </summary>
    [Fact]
    public void Render_ShowsSaveThisOccurrenceButton()
    {
        // Act
        var cut = RenderDialog();

        // Assert
        Assert.Contains("Save This Occurrence", cut.Markup);
    }

    private IRenderedComponent<EditInstanceDialog> RenderDialog(
        DateOnly? scheduledDate = null,
        string description = "Test Transaction",
        decimal amount = 100m,
        string? accountName = null)
    {
        return Render<EditInstanceDialog>(p => p
            .Add(x => x.ScheduledDate, scheduledDate ?? new DateOnly(2025, 1, 15))
            .Add(x => x.OriginalDescription, description)
            .Add(x => x.OriginalAmount, amount)
            .Add(x => x.AccountName, accountName));
    }
}
