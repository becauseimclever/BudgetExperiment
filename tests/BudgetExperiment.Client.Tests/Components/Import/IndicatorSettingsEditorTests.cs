// <copyright file="IndicatorSettingsEditorTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Import;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Import;

/// <summary>
/// Unit tests for the <see cref="IndicatorSettingsEditor"/> component.
/// </summary>
public sealed class IndicatorSettingsEditorTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IndicatorSettingsEditorTests"/> class.
    /// </summary>
    public IndicatorSettingsEditorTests()
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
    /// Verifies the title is rendered.
    /// </summary>
    [Fact]
    public void IndicatorSettingsEditor_ShowsTitle()
    {
        var cut = Render<IndicatorSettingsEditor>();

        Assert.Contains("Debit/Credit Indicator Settings", cut.Markup);
    }

    /// <summary>
    /// Verifies both input fields are rendered with labels.
    /// </summary>
    [Fact]
    public void IndicatorSettingsEditor_ShowsBothInputFields()
    {
        var cut = Render<IndicatorSettingsEditor>();

        Assert.Contains("Debit Values (expenses)", cut.Markup);
        Assert.Contains("Credit Values (income)", cut.Markup);

        var inputs = cut.FindAll("input.form-control");
        Assert.Equal(2, inputs.Count);
    }

    /// <summary>
    /// Verifies default placeholder text for inputs.
    /// </summary>
    [Fact]
    public void IndicatorSettingsEditor_ShowsPlaceholders()
    {
        var cut = Render<IndicatorSettingsEditor>();

        var inputs = cut.FindAll("input.form-control");
        Assert.Equal("e.g., DR, Debit, D", inputs[0].GetAttribute("placeholder"));
        Assert.Equal("e.g., CR, Credit, C", inputs[1].GetAttribute("placeholder"));
    }

    /// <summary>
    /// Verifies that passing a Value populates both fields.
    /// </summary>
    [Fact]
    public void IndicatorSettingsEditor_PopulatesFromValue()
    {
        var settings = new DebitCreditIndicatorSettingsDto
        {
            DebitIndicators = "Debit,DR",
            CreditIndicators = "Credit,CR",
        };

        var cut = Render<IndicatorSettingsEditor>(parameters => parameters
            .Add(p => p.Value, settings));

        var inputs = cut.FindAll("input.form-control");
        Assert.Equal("Debit,DR", inputs[0].GetAttribute("value"));
        Assert.Equal("Credit,CR", inputs[1].GetAttribute("value"));
    }

    /// <summary>
    /// Verifies that debit change fires ValueChanged with normalized values.
    /// </summary>
    [Fact]
    public void IndicatorSettingsEditor_DebitChange_FiresValueChanged()
    {
        DebitCreditIndicatorSettingsDto? receivedSettings = null;

        var cut = Render<IndicatorSettingsEditor>(parameters => parameters
            .Add(p => p.ValueChanged, (DebitCreditIndicatorSettingsDto? val) => { receivedSettings = val; }));

        var debitInput = cut.FindAll("input.form-control")[0];
        debitInput.Change("DR , Debit , D");

        Assert.NotNull(receivedSettings);
        Assert.Equal("DR,Debit,D", receivedSettings!.DebitIndicators);
    }

    /// <summary>
    /// Verifies that credit change fires ValueChanged.
    /// </summary>
    [Fact]
    public void IndicatorSettingsEditor_CreditChange_FiresValueChanged()
    {
        DebitCreditIndicatorSettingsDto? receivedSettings = null;

        var cut = Render<IndicatorSettingsEditor>(parameters => parameters
            .Add(p => p.ValueChanged, (DebitCreditIndicatorSettingsDto? val) => { receivedSettings = val; }));

        var creditInput = cut.FindAll("input.form-control")[1];
        creditInput.Change("CR , Credit");

        Assert.NotNull(receivedSettings);
        Assert.Equal("CR,Credit", receivedSettings!.CreditIndicators);
    }

    /// <summary>
    /// Verifies validation message is shown when provided.
    /// </summary>
    [Fact]
    public void IndicatorSettingsEditor_ShowsValidationMessage()
    {
        var cut = Render<IndicatorSettingsEditor>(parameters => parameters
            .Add(p => p.ValidationMessage, "Both fields required"));

        Assert.Contains("Both fields required", cut.Markup);
        Assert.Contains("alert-warning", cut.Markup);
    }

    /// <summary>
    /// Verifies validation message is hidden when null.
    /// </summary>
    [Fact]
    public void IndicatorSettingsEditor_HidesValidationMessage_WhenNull()
    {
        var cut = Render<IndicatorSettingsEditor>(parameters => parameters
            .Add(p => p.ValidationMessage, (string?)null));

        Assert.DoesNotContain("alert-warning", cut.Markup);
    }
}
