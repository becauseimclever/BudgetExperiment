// <copyright file="EditRecurringTransferFormTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Forms;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Forms;

/// <summary>
/// Unit tests for the <see cref="EditRecurringTransferForm"/> component.
/// </summary>
public sealed class EditRecurringTransferFormTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EditRecurringTransferFormTests"/> class.
    /// </summary>
    public EditRecurringTransferFormTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the form renders all expected fields.
    /// </summary>
    [Fact]
    public void Render_ShowsAllFormFields()
    {
        // Act
        var cut = RenderForm();

        // Assert
        Assert.NotNull(cut.Find("#description"));
        Assert.NotNull(cut.Find("#amount"));
        Assert.NotNull(cut.Find("#frequency"));
        Assert.NotNull(cut.Find("#endDate"));
    }

    /// <summary>
    /// Verifies that Weekly frequency shows day of week field.
    /// </summary>
    [Fact]
    public void Render_WeeklyFrequency_ShowsDayOfWeekField()
    {
        // Arrange
        var model = CreateDefaultModel();
        model.Frequency = "Weekly";
        model.DayOfWeek = "Monday";

        // Act
        var cut = RenderForm(model);

        // Assert
        Assert.NotNull(cut.Find("#dayOfWeek"));
        Assert.Empty(cut.FindAll("#dayOfMonth"));
    }

    /// <summary>
    /// Verifies that Monthly frequency shows day of month field.
    /// </summary>
    [Fact]
    public void Render_MonthlyFrequency_ShowsDayOfMonthField()
    {
        // Arrange
        var model = CreateDefaultModel();
        model.Frequency = "Monthly";

        // Act
        var cut = RenderForm(model);

        // Assert
        Assert.NotNull(cut.Find("#dayOfMonth"));
        Assert.Empty(cut.FindAll("#dayOfWeek"));
    }

    /// <summary>
    /// Verifies that Daily frequency hides both day of week and day of month fields.
    /// </summary>
    [Fact]
    public void Render_DailyFrequency_HidesDayFields()
    {
        // Arrange
        var model = CreateDefaultModel();
        model.Frequency = "Daily";

        // Act
        var cut = RenderForm(model);

        // Assert
        Assert.Empty(cut.FindAll("#dayOfWeek"));
        Assert.Empty(cut.FindAll("#dayOfMonth"));
    }

    /// <summary>
    /// Verifies OnFrequencyChanged sets DayOfWeek default for Weekly.
    /// </summary>
    [Fact]
    public void FrequencyChange_ToWeekly_SetsDayOfWeekDefault()
    {
        // Arrange
        var model = CreateDefaultModel();
        model.Frequency = "Daily";
        var cut = RenderForm(model);

        // Act
        cut.Find("#frequency").Change("Weekly");

        // Assert
        Assert.Equal("Monday", model.DayOfWeek);
        Assert.Null(model.DayOfMonth);
    }

    /// <summary>
    /// Verifies OnFrequencyChanged clears DayOfWeek for Monthly.
    /// </summary>
    [Fact]
    public void FrequencyChange_ToMonthly_ClearsDayOfWeek()
    {
        // Arrange
        var model = CreateDefaultModel();
        model.Frequency = "Weekly";
        model.DayOfWeek = "Tuesday";
        var cut = RenderForm(model);

        // Act
        cut.Find("#frequency").Change("Monthly");

        // Assert
        Assert.Null(model.DayOfWeek);
    }

    /// <summary>
    /// Verifies OnFrequencyChanged clears both day fields for Daily.
    /// </summary>
    [Fact]
    public void FrequencyChange_ToDaily_ClearsBothDayFields()
    {
        // Arrange
        var model = CreateDefaultModel();
        model.Frequency = "Weekly";
        model.DayOfWeek = "Monday";
        model.DayOfMonth = 15;
        var cut = RenderForm(model);

        // Act
        cut.Find("#frequency").Change("Daily");

        // Assert
        Assert.Null(model.DayOfWeek);
        Assert.Null(model.DayOfMonth);
    }

    /// <summary>
    /// Verifies that submitting invokes OnSubmit with the model.
    /// </summary>
    [Fact]
    public void Submit_InvokesOnSubmit()
    {
        // Arrange
        RecurringTransferUpdateDto? submitted = null;
        var model = CreateDefaultModel();
        model.Description = "Monthly Savings";

        var cut = Render<EditRecurringTransferForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.OnSubmit, (RecurringTransferUpdateDto dto) => submitted = dto));

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.NotNull(submitted);
        Assert.Equal("Monthly Savings", submitted!.Description);
    }

    /// <summary>
    /// Verifies that clicking cancel invokes OnCancel.
    /// </summary>
    [Fact]
    public void ClickCancel_InvokesOnCancel()
    {
        // Arrange
        var cancelCalled = false;
        var cut = Render<EditRecurringTransferForm>(p => p
            .Add(x => x.Model, CreateDefaultModel())
            .Add(x => x.OnCancel, () => cancelCalled = true));

        // Act
        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelBtn.Click();

        // Assert
        Assert.True(cancelCalled);
    }

    private static RecurringTransferUpdateDto CreateDefaultModel()
    {
        return new RecurringTransferUpdateDto
        {
            Description = "Test Transfer",
            Amount = new MoneyDto { Amount = 200m, Currency = "USD" },
            Frequency = "Monthly",
        };
    }

    private IRenderedComponent<EditRecurringTransferForm> RenderForm(RecurringTransferUpdateDto? model = null)
    {
        return Render<EditRecurringTransferForm>(p => p
            .Add(x => x.Model, model ?? CreateDefaultModel()));
    }
}
