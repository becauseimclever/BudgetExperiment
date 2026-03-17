// <copyright file="OnboardingViewModelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using Microsoft.AspNetCore.Components;
using Shouldly;

namespace BudgetExperiment.Client.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="OnboardingViewModel"/>.
/// </summary>
public sealed class OnboardingViewModelTests
{
    private readonly StubBudgetApiService _apiService = new();
    private readonly StubNavigationManager _navigationManager = new();
    private readonly StubApiErrorContext _apiErrorContext = new();
    private readonly OnboardingViewModel _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnboardingViewModelTests"/> class.
    /// </summary>
    public OnboardingViewModelTests()
    {
        this._sut = new OnboardingViewModel(this._apiService, this._navigationManager, this._apiErrorContext);
    }

    // --- Initial State ---

    /// <summary>
    /// Verifies initial state is step 1.
    /// </summary>
    [Fact]
    public void InitialState_CurrentStepIsOne()
    {
        this._sut.CurrentStep.ShouldBe(1);
    }

    /// <summary>
    /// Verifies initial currency is USD.
    /// </summary>
    [Fact]
    public void InitialState_SelectedCurrencyIsUSD()
    {
        this._sut.SelectedCurrency.ShouldBe("USD");
    }

    /// <summary>
    /// Verifies initial first day of week is Sunday.
    /// </summary>
    [Fact]
    public void InitialState_SelectedFirstDayIsSunday()
    {
        this._sut.SelectedFirstDay.ShouldBe(DayOfWeek.Sunday);
    }

    /// <summary>
    /// Verifies initial state is not saving.
    /// </summary>
    [Fact]
    public void InitialState_IsNotSaving()
    {
        this._sut.IsSaving.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies initial error message is null.
    /// </summary>
    [Fact]
    public void InitialState_ErrorMessageIsNull()
    {
        this._sut.ErrorMessage.ShouldBeNull();
    }

    // --- NextStep ---

    /// <summary>
    /// Verifies NextStep increments the step.
    /// </summary>
    [Fact]
    public void NextStep_IncrementsStep()
    {
        this._sut.NextStep();

        this._sut.CurrentStep.ShouldBe(2);
    }

    /// <summary>
    /// Verifies NextStep does not exceed step 4.
    /// </summary>
    [Fact]
    public void NextStep_DoesNotExceedMaxStep()
    {
        this._sut.NextStep(); // 2
        this._sut.NextStep(); // 3
        this._sut.NextStep(); // 4
        this._sut.NextStep(); // still 4

        this._sut.CurrentStep.ShouldBe(4);
    }

    /// <summary>
    /// Verifies NextStep invokes OnStateChanged.
    /// </summary>
    [Fact]
    public void NextStep_NotifiesStateChanged()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;

        this._sut.NextStep();

        callCount.ShouldBe(1);
    }

    /// <summary>
    /// Verifies NextStep does not notify when already at max step.
    /// </summary>
    [Fact]
    public void NextStep_DoesNotNotify_WhenAtMaxStep()
    {
        this._sut.NextStep(); // 2
        this._sut.NextStep(); // 3
        this._sut.NextStep(); // 4

        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;
        this._sut.NextStep(); // still 4

        callCount.ShouldBe(0);
    }

    // --- PreviousStep ---

    /// <summary>
    /// Verifies PreviousStep decrements the step.
    /// </summary>
    [Fact]
    public void PreviousStep_DecrementsStep()
    {
        this._sut.NextStep(); // 2
        this._sut.PreviousStep();

        this._sut.CurrentStep.ShouldBe(1);
    }

    /// <summary>
    /// Verifies PreviousStep does not go below step 1.
    /// </summary>
    [Fact]
    public void PreviousStep_DoesNotGoBelowMinStep()
    {
        this._sut.PreviousStep();

        this._sut.CurrentStep.ShouldBe(1);
    }

    /// <summary>
    /// Verifies PreviousStep invokes OnStateChanged.
    /// </summary>
    [Fact]
    public void PreviousStep_NotifiesStateChanged()
    {
        this._sut.NextStep(); // 2

        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;
        this._sut.PreviousStep();

        callCount.ShouldBe(1);
    }

    /// <summary>
    /// Verifies PreviousStep does not notify when already at step 1.
    /// </summary>
    [Fact]
    public void PreviousStep_DoesNotNotify_WhenAtMinStep()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;
        this._sut.PreviousStep();

        callCount.ShouldBe(0);
    }

    // --- SelectCurrency ---

    /// <summary>
    /// Verifies SelectCurrency sets the selected currency code.
    /// </summary>
    [Fact]
    public void SelectCurrency_SetsSelectedCurrency()
    {
        var currency = new CurrencyOption("EUR", "Euro", "\u20ac");

        this._sut.SelectCurrency(currency);

        this._sut.SelectedCurrency.ShouldBe("EUR");
    }

    /// <summary>
    /// Verifies SelectCurrency clears the search term.
    /// </summary>
    [Fact]
    public void SelectCurrency_ClearsSearch()
    {
        this._sut.CurrencySearch = "eu";
        var currency = new CurrencyOption("EUR", "Euro", "\u20ac");

        this._sut.SelectCurrency(currency);

        this._sut.CurrencySearch.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies SelectCurrency closes the dropdown.
    /// </summary>
    [Fact]
    public void SelectCurrency_ClosesDropdown()
    {
        this._sut.ShowCurrencyDropdown = true;
        var currency = new CurrencyOption("EUR", "Euro", "\u20ac");

        this._sut.SelectCurrency(currency);

        this._sut.ShowCurrencyDropdown.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies SelectCurrency notifies state changed.
    /// </summary>
    [Fact]
    public void SelectCurrency_NotifiesStateChanged()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;
        var currency = new CurrencyOption("EUR", "Euro", "\u20ac");

        this._sut.SelectCurrency(currency);

        callCount.ShouldBe(1);
    }

    // --- GetCurrencyDisplay ---

    /// <summary>
    /// Verifies GetCurrencyDisplay returns formatted string for known currency.
    /// </summary>
    [Fact]
    public void GetCurrencyDisplay_ReturnsFormattedString_ForKnownCurrency()
    {
        this._sut.SelectedCurrency = "USD";

        var result = this._sut.GetCurrencyDisplay();

        result.ShouldBe("USD \u2014 $ US Dollar");
    }

    /// <summary>
    /// Verifies GetCurrencyDisplay returns code for unknown currency.
    /// </summary>
    [Fact]
    public void GetCurrencyDisplay_ReturnsCode_ForUnknownCurrency()
    {
        this._sut.SelectedCurrency = "XYZ";

        var result = this._sut.GetCurrencyDisplay();

        result.ShouldBe("XYZ");
    }

    // --- FilteredCurrencies ---

    /// <summary>
    /// Verifies FilteredCurrencies returns all currencies when search is empty.
    /// </summary>
    [Fact]
    public void FilteredCurrencies_ReturnsAll_WhenSearchEmpty()
    {
        this._sut.CurrencySearch = string.Empty;

        this._sut.FilteredCurrencies.ShouldBe(CurrencyList.Currencies);
    }

    /// <summary>
    /// Verifies FilteredCurrencies filters by currency code.
    /// </summary>
    [Fact]
    public void FilteredCurrencies_FiltersByCode()
    {
        this._sut.CurrencySearch = "eur";

        var results = this._sut.FilteredCurrencies.ToList();

        results.Count.ShouldBe(1);
        results[0].Code.ShouldBe("EUR");
    }

    /// <summary>
    /// Verifies FilteredCurrencies filters by currency name.
    /// </summary>
    [Fact]
    public void FilteredCurrencies_FiltersByName()
    {
        this._sut.CurrencySearch = "dollar";

        var results = this._sut.FilteredCurrencies.ToList();

        results.ShouldAllBe(c => c.Name.Contains("Dollar", StringComparison.OrdinalIgnoreCase));
        results.Count.ShouldBeGreaterThan(1);
    }

    /// <summary>
    /// Verifies FilteredCurrencies is case-insensitive.
    /// </summary>
    [Fact]
    public void FilteredCurrencies_IsCaseInsensitive()
    {
        this._sut.CurrencySearch = "EUR";
        var upperResults = this._sut.FilteredCurrencies.ToList();

        this._sut.CurrencySearch = "eur";
        var lowerResults = this._sut.FilteredCurrencies.ToList();

        upperResults.Count.ShouldBe(lowerResults.Count);
    }

    /// <summary>
    /// Verifies FilteredCurrencies returns all when search is whitespace-only.
    /// </summary>
    [Fact]
    public void FilteredCurrencies_ReturnsAll_WhenSearchIsWhitespace()
    {
        this._sut.CurrencySearch = "   ";

        this._sut.FilteredCurrencies.ShouldBe(CurrencyList.Currencies);
    }

    // --- SkipOnboardingAsync ---

    /// <summary>
    /// Verifies SkipOnboardingAsync saves defaults and navigates home.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipOnboardingAsync_SavesDefaultsAndNavigates()
    {
        await this._sut.SkipOnboardingAsync();

        this._navigationManager.LastNavigatedUri.ShouldBe("/");
        this._sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies SkipOnboardingAsync sets IsSaving to true during execution.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipOnboardingAsync_NotifiesStateChanged()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;

        await this._sut.SkipOnboardingAsync();

        callCount.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies SkipOnboardingAsync handles API failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipOnboardingAsync_SetsErrorMessage_WhenApiFails()
    {
        this._apiService.UpdateUserSettingsException = new HttpRequestException("Server error");

        await this._sut.SkipOnboardingAsync();

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.ErrorMessage!.ShouldContain("Failed to save");
        this._sut.IsSaving.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies SkipOnboardingAsync clears error message before saving.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipOnboardingAsync_ClearsErrorBeforeSaving()
    {
        this._apiService.UpdateUserSettingsException = new HttpRequestException("fail");
        await this._sut.SkipOnboardingAsync();
        this._sut.ErrorMessage.ShouldNotBeNull();

        this._apiService.UpdateUserSettingsException = null;
        await this._sut.SkipOnboardingAsync();

        this._sut.ErrorMessage.ShouldBeNull();
    }

    // --- CompleteOnboardingAsync ---

    /// <summary>
    /// Verifies CompleteOnboardingAsync saves preferences and navigates home.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteOnboardingAsync_SavesPreferencesAndNavigates()
    {
        this._sut.SelectedCurrency = "EUR";
        this._sut.SelectedFirstDay = DayOfWeek.Monday;

        await this._sut.CompleteOnboardingAsync();

        this._navigationManager.LastNavigatedUri.ShouldBe("/");
        this._sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies CompleteOnboardingAsync notifies state changed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteOnboardingAsync_NotifiesStateChanged()
    {
        int callCount = 0;
        this._sut.OnStateChanged = () => callCount++;

        await this._sut.CompleteOnboardingAsync();

        callCount.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies CompleteOnboardingAsync handles API failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteOnboardingAsync_SetsErrorMessage_WhenApiFails()
    {
        this._apiService.UpdateUserSettingsException = new HttpRequestException("Server error");

        await this._sut.CompleteOnboardingAsync();

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.ErrorMessage!.ShouldContain("Failed to save");
        this._sut.IsSaving.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies CompleteOnboardingAsync handles CompleteOnboarding API failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteOnboardingAsync_SetsErrorMessage_WhenCompleteOnboardingFails()
    {
        this._apiService.CompleteOnboardingException = new HttpRequestException("Onboarding error");

        await this._sut.CompleteOnboardingAsync();

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.ErrorMessage!.ShouldContain("Failed to save");
        this._sut.IsSaving.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies CompleteOnboardingAsync clears error before saving.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteOnboardingAsync_ClearsErrorBeforeSaving()
    {
        this._apiService.UpdateUserSettingsException = new HttpRequestException("fail");
        await this._sut.CompleteOnboardingAsync();
        this._sut.ErrorMessage.ShouldNotBeNull();

        this._apiService.UpdateUserSettingsException = null;
        await this._sut.CompleteOnboardingAsync();

        this._sut.ErrorMessage.ShouldBeNull();
    }

    private sealed class StubNavigationManager : NavigationManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StubNavigationManager"/> class.
        /// </summary>
        public StubNavigationManager()
        {
            this.Initialize("https://localhost/", "https://localhost/");
        }

        /// <summary>
        /// Gets the last URI that was navigated to.
        /// </summary>
        public string? LastNavigatedUri { get; private set; }

        /// <inheritdoc/>
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            this.LastNavigatedUri = uri;
        }
    }
}
