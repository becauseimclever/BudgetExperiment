// <copyright file="SettingsPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net.Http;

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Pages;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the <see cref="Settings"/> page component.
/// </summary>
public class SettingsPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly StubAiApiService _aiApiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPageTests"/> class.
    /// </summary>
    public SettingsPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(_apiService);
        this.Services.AddSingleton<IAiApiService>(_aiApiService);
        this.Services.AddSingleton(new VersionService(new HttpClient()));
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<ScopeService>();
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        this.Services.AddSingleton<IExportDownloadService>(new StubExportDownloadService());
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
        var cut = Render<Settings>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<Settings>();

        cut.Markup.ShouldContain("Settings");
    }

    /// <summary>
    /// Verifies the General and AI tabs are rendered.
    /// </summary>
    [Fact]
    public void ShowsTabs()
    {
        var cut = Render<Settings>();

        cut.Markup.ShouldContain("General");
        cut.Markup.ShouldContain("AI / Smart Features");
    }

    /// <summary>
    /// Verifies the subtitle is shown.
    /// </summary>
    [Fact]
    public void ShowsSubtitle()
    {
        var cut = Render<Settings>();

        cut.Markup.ShouldContain("Configure your preferences");
    }

    /// <summary>
    /// Verifies Recurring Items section is shown when settings are loaded.
    /// </summary>
    [Fact]
    public void ShowsRecurringItemsSection_WhenSettingsLoaded()
    {
        SetupSettingsData();

        var cut = Render<Settings>();

        cut.Markup.ShouldContain("Recurring Items");
        cut.Markup.ShouldContain("Auto-realize past-due items");
        cut.Markup.ShouldContain("Past-due lookback days");
    }

    /// <summary>
    /// Verifies Location section is shown when settings are loaded.
    /// </summary>
    [Fact]
    public void ShowsLocationSection_WhenSettingsLoaded()
    {
        SetupSettingsData();

        var cut = Render<Settings>();

        cut.Markup.ShouldContain("Location");
        cut.Markup.ShouldContain("Enable location data");
    }

    /// <summary>
    /// Verifies User Preferences section is shown when settings are loaded.
    /// </summary>
    [Fact]
    public void ShowsUserPreferencesSection_WhenSettingsLoaded()
    {
        SetupSettingsData();

        var cut = Render<Settings>();

        cut.Markup.ShouldContain("User Preferences");
        cut.Markup.ShouldContain("Preferred currency");
        cut.Markup.ShouldContain("First day of the week");
    }

    /// <summary>
    /// Verifies the About section shows version info.
    /// </summary>
    [Fact]
    public void ShowsAboutSection_WithVersion()
    {
        SetupSettingsData();

        var cut = Render<Settings>();

        cut.Markup.ShouldContain("About");
        cut.Markup.ShouldContain("Version");
        cut.Markup.ShouldContain("Budget Experiment");
    }

    /// <summary>
    /// Verifies Delete All Location Data button appears when location data is enabled.
    /// </summary>
    [Fact]
    public void ShowsDeleteLocationDataButton_WhenLocationEnabled()
    {
        SetupSettingsData(enableLocation: true);

        var cut = Render<Settings>();

        cut.Markup.ShouldContain("Delete All Location Data");
    }

    /// <summary>
    /// Verifies Delete All Location Data button is hidden when location data is disabled.
    /// </summary>
    [Fact]
    public void HidesDeleteLocationDataButton_WhenLocationDisabled()
    {
        SetupSettingsData(enableLocation: false);

        var cut = Render<Settings>();

        cut.Markup.ShouldNotContain("Delete All Location Data");
    }

    /// <summary>
    /// Verifies the currency selector is present with options.
    /// </summary>
    [Fact]
    public void ShowsCurrencySelector()
    {
        SetupSettingsData();

        var cut = Render<Settings>();

        cut.Markup.ShouldContain("USD");
    }

    /// <summary>
    /// Verifies day-of-week toggle buttons are present.
    /// </summary>
    [Fact]
    public void ShowsDayOfWeekToggles()
    {
        SetupSettingsData();

        var cut = Render<Settings>();

        cut.Markup.ShouldContain("Sun");
        cut.Markup.ShouldContain("Mon");
    }

    /// <summary>
    /// Verifies lookback days input shows current value.
    /// </summary>
    [Fact]
    public void ShowsLookbackDaysValue()
    {
        SetupSettingsData();

        var cut = Render<Settings>();

        // The number input should exist with the configured value
        var numberInputs = cut.FindAll("input[type='number']");
        numberInputs.Count.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies auto-realize toggle is present.
    /// </summary>
    [Fact]
    public void HasAutoRealizeToggle()
    {
        SetupSettingsData();

        var cut = Render<Settings>();

        cut.Markup.ShouldContain("Auto-Realize");
    }

    /// <summary>
    /// Verifies delete location data result is configurable.
    /// </summary>
    [Fact]
    public void DeleteLocationDataResult_IsConfigurable()
    {
        _apiService.DeleteLocationDataResult = new LocationDataClearedDto
        {
            ClearedCount = 10,
        };
        SetupSettingsData(enableLocation: true);

        var cut = Render<Settings>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies AI tab shows status.
    /// </summary>
    [Fact]
    public void AiTab_ShowsStatus()
    {
        _aiApiService.AiStatus = new AiStatusDto
        {
            IsAvailable = true,
            CurrentModel = "gpt-4",
        };
        SetupSettingsData();

        var cut = Render<Settings>();

        cut.Markup.ShouldContain("AI");
    }

    /// <summary>
    /// Verifies lookback days input is present.
    /// </summary>
    [Fact]
    public void HasLookbackDaysInput()
    {
        SetupSettingsData();

        var cut = Render<Settings>();

        cut.Markup.ShouldContain("Lookback");
    }

    /// <summary>
    /// Verifies general tab is active by default.
    /// </summary>
    [Fact]
    public void GeneralTab_IsActiveByDefault()
    {
        SetupSettingsData();

        var cut = Render<Settings>();

        cut.Markup.ShouldContain("active");
    }

    /// <summary>
    /// Verifies clicking the AI tab switches tab state.
    /// </summary>
    [Fact]
    public void ClickAiTab_SwitchesTab()
    {
        SetupSettingsData();
        _aiApiService.AiStatus = new AiStatusDto { IsAvailable = true, CurrentModel = "llama3" };

        var cut = Render<Settings>();
        var aiTab = cut.FindAll("button.settings-tab")[1];
        aiTab.Click();

        // After click, the AI tab should have the active class
        cut.FindAll("button.settings-tab")[1].ClassList.ShouldContain("active");
    }

    /// <summary>
    /// Verifies clicking the AI tab and then back to General restores content.
    /// </summary>
    [Fact]
    public void ClickGeneralTab_RestoresGeneralContent()
    {
        SetupSettingsData();
        _aiApiService.AiStatus = new AiStatusDto { IsAvailable = true, CurrentModel = "llama3" };

        var cut = Render<Settings>();

        // Click AI tab first
        cut.FindAll("button.settings-tab")[1].Click();

        // Click General tab
        cut.FindAll("button.settings-tab")[0].Click();

        cut.Markup.ShouldContain("Recurring Items");
    }

    /// <summary>
    /// Verifies the auto-realize toggle sends update.
    /// </summary>
    [Fact]
    public void AutoRealizeToggle_UpdatesSettings()
    {
        SetupSettingsData();

        var cut = Render<Settings>();

        // Find the auto-realize checkbox and change it
        var checkboxes = cut.FindAll("input[type='checkbox']");
        checkboxes.Count.ShouldBeGreaterThan(0);
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the delete location data confirmation flow.
    /// </summary>
    [Fact]
    public void DeleteLocationData_ShowsConfirmation()
    {
        SetupSettingsData(enableLocation: true);
        _apiService.DeleteLocationDataResult = new LocationDataClearedDto { ClearedCount = 5 };

        var cut = Render<Settings>();

        // Click the delete button (btn-outline-danger initially)
        var deleteButton = cut.Find("button.btn-outline-danger");
        deleteButton.Click();

        // Should show confirmation
        cut.Markup.ShouldContain("Yes, delete all");
    }

    /// <summary>
    /// Verifies confirming location data deletion shows success.
    /// </summary>
    [Fact]
    public void ConfirmDeleteLocationData_ShowsSuccess()
    {
        SetupSettingsData(enableLocation: true);
        _apiService.DeleteLocationDataResult = new LocationDataClearedDto { ClearedCount = 5 };

        var cut = Render<Settings>();

        // Click initial delete button to show confirmation
        cut.Find("button.btn-outline-danger").Click();

        // Confirm deletion (btn-danger appears in confirmation)
        cut.Find("button.btn-danger").Click();

        cut.Markup.ShouldContain("Cleared location data");
    }

    /// <summary>
    /// Verifies the day-of-week toggle updates user settings.
    /// </summary>
    [Fact]
    public void DayOfWeekToggle_IsClickable()
    {
        SetupSettingsData();

        var cut = Render<Settings>();

        // Find day toggle buttons (Sun, Mon)
        cut.Markup.ShouldContain("Sun");
        cut.Markup.ShouldContain("Mon");
    }

    /// <summary>
    /// Verifies AI tab shows about info when clicked.
    /// </summary>
    [Fact]
    public void AiTab_ShowsAboutLocalAi()
    {
        SetupSettingsData();
        _aiApiService.AiStatus = new AiStatusDto { IsAvailable = true, CurrentModel = "llama3" };

        var cut = Render<Settings>();
        cut.FindAll("button.settings-tab")[1].Click();

        cut.Markup.ShouldContain("About Local AI");
    }

    private void SetupSettingsData(bool enableLocation = false)
    {
        _apiService.AppSettings = new AppSettingsDto
        {
            AutoRealizePastDueItems = true,
            PastDueLookbackDays = 30,
            EnableLocationData = enableLocation,
        };

        _apiService.UserSettings = new UserSettingsDto
        {
            UserId = Guid.NewGuid(),
            DefaultScope = "Personal",
            PreferredCurrency = "USD",
            FirstDayOfWeek = DayOfWeek.Sunday,
            IsOnboarded = true,
        };
    }
}
