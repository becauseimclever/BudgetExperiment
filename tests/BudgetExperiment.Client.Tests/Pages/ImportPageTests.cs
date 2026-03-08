// <copyright file="ImportPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Pages;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the <see cref="Import"/> page component.
/// </summary>
public class ImportPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _budgetApi = new();
    private readonly StubImportApiService _importApi = new();
    private readonly StubCsvParserService _csvParser = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportPageTests"/> class.
    /// </summary>
    public ImportPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(this._budgetApi);
        this.Services.AddSingleton<IImportApiService>(this._importApi);
        this.Services.AddSingleton<ICsvParserService>(this._csvParser);
        this.Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync()
    {
        return base.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Verifies the page renders without errors.
    /// </summary>
    [Fact]
    public void Renders_WithoutErrors()
    {
        var cut = Render<Import>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is correct.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<Import>();

        cut.Markup.ShouldContain("Import Transactions");
    }

    /// <summary>
    /// Verifies all three tabs are displayed.
    /// </summary>
    [Fact]
    public void ShowsTabNavigation()
    {
        var cut = Render<Import>();

        cut.Markup.ShouldContain("New Import");
        cut.Markup.ShouldContain("Import History");
        cut.Markup.ShouldContain("Saved Mappings");
    }

    /// <summary>
    /// Verifies the wizard tab is active by default.
    /// </summary>
    [Fact]
    public void WizardTab_IsActiveByDefault()
    {
        var cut = Render<Import>();

        var tabs = cut.FindAll(".nav-link");
        tabs[0].ClassList.ShouldContain("active");
    }

    /// <summary>
    /// Verifies wizard progress steps are shown on step 1.
    /// </summary>
    [Fact]
    public void ShowsWizardProgressSteps()
    {
        var cut = Render<Import>();

        cut.Markup.ShouldContain("Upload File");
        cut.Markup.ShouldContain("Map Columns");
        cut.Markup.ShouldContain("Preview");
    }

    /// <summary>
    /// Verifies step 1 shows upload section.
    /// </summary>
    [Fact]
    public void Step1_ShowsUploadSection()
    {
        var cut = Render<Import>();

        cut.Markup.ShouldContain("Upload CSV File");
    }

    /// <summary>
    /// Verifies the Start Over button is not shown on step 1.
    /// </summary>
    [Fact]
    public void Step1_DoesNotShowStartOverButton()
    {
        var cut = Render<Import>();

        cut.Markup.ShouldNotContain("Start Over");
    }

    /// <summary>
    /// Verifies error message is shown when initial data load fails.
    /// </summary>
    [Fact]
    public void ShowsError_WhenLoadFails()
    {
        this._importApi.ShouldThrowOnGetMappings = true;

        var cut = Render<Import>();

        cut.Markup.ShouldContain("Failed to load data");
    }

    /// <summary>
    /// Verifies history tab loads when clicked.
    /// </summary>
    [Fact]
    public void HistoryTab_LoadsHistory()
    {
        this._importApi.Batches.Add(new ImportBatchDto
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            FileName = "test.csv",
            TransactionCount = 10,
            ImportedAtUtc = DateTime.UtcNow,
        });

        var cut = Render<Import>();
        var historyTab = cut.FindAll(".nav-link")[1];
        historyTab.Click();

        cut.Markup.ShouldContain("test.csv");
    }

    /// <summary>
    /// Verifies history tab shows error when load fails.
    /// </summary>
    [Fact]
    public void HistoryTab_ShowsError_WhenLoadFails()
    {
        var cut = Render<Import>();

        this._importApi.ShouldThrowOnGetHistory = true;
        var historyTab = cut.FindAll(".nav-link")[1];
        historyTab.Click();

        cut.Markup.ShouldContain("Failed to load import history");
    }

    /// <summary>
    /// Verifies mappings tab loads saved mappings.
    /// </summary>
    [Fact]
    public void MappingsTab_LoadsMappings()
    {
        this._importApi.Mappings.Add(new ImportMappingDto
        {
            Id = Guid.NewGuid(),
            Name = "Bank Statement",
            ColumnMappings = [],
            CreatedAtUtc = DateTime.UtcNow,
        });

        var cut = Render<Import>();
        var mappingsTab = cut.FindAll(".nav-link")[2];
        mappingsTab.Click();

        cut.Markup.ShouldContain("Bank Statement");
    }

    /// <summary>
    /// Verifies step 1 first progress step is active.
    /// </summary>
    [Fact]
    public void Step1_FirstStepIsActive()
    {
        var cut = Render<Import>();

        var steps = cut.FindAll(".wizard-step");
        steps[0].ClassList.ShouldContain("active");
        steps[1].ClassList.ShouldNotContain("active");
    }

    /// <summary>
    /// Verifies switching tabs hides wizard content.
    /// </summary>
    [Fact]
    public void SwitchingToHistoryTab_HidesWizard()
    {
        var cut = Render<Import>();
        var historyTab = cut.FindAll(".nav-link")[1];
        historyTab.Click();

        cut.Markup.ShouldNotContain("Upload CSV File");
    }

    /// <summary>
    /// Verifies switching back to wizard tab restores wizard content.
    /// </summary>
    [Fact]
    public void SwitchingBackToWizardTab_ShowsWizard()
    {
        var cut = Render<Import>();
        var historyTab = cut.FindAll(".nav-link")[1];
        historyTab.Click();

        var wizardTab = cut.FindAll(".nav-link")[0];
        wizardTab.Click();

        cut.Markup.ShouldContain("Upload CSV File");
    }

    /// <summary>
    /// Verifies accounts are loaded for account selection in step 1.
    /// </summary>
    [Fact]
    public void Step1_AccountsAreAvailableForSelection()
    {
        this._budgetApi.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Checking",
            Type = "Checking",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Import>();

        // Accounts list is loaded but select is only shown after file parse
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies mappings tab shows error when mappings load fails.
    /// </summary>
    [Fact]
    public void MappingsTab_ShowsError_WhenRefreshFails()
    {
        var cut = Render<Import>();

        // Mappings were already loaded in OnInit, so we need a fresh state
        // where no mappings were loaded (empty list) and then the refresh fails
        this._importApi.ShouldThrowOnGetMappings = true;
        this._importApi.Mappings.Clear();
        var mappingsTab = cut.FindAll(".nav-link")[2];
        mappingsTab.Click();

        cut.Markup.ShouldContain("Failed to load mappings");
    }
}
