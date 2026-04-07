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
        _sut = new OnboardingViewModel(_apiService, _navigationManager, _apiErrorContext);
    }

    // --- Initial State ---

    /// <summary>
    /// Verifies initial state is step 1.
    /// </summary>
    [Fact]
    public void InitialState_CurrentStepIsOne()
    {
        _sut.CurrentStep.ShouldBe(1);
    }

    /// <summary>
    /// Verifies initial currency is USD.
    /// </summary>
    [Fact]
    public void InitialState_SelectedCurrencyIsUSD()
    {
        _sut.SelectedCurrency.ShouldBe("USD");
    }

    /// <summary>
    /// Verifies initial first day of week is Sunday.
    /// </summary>
    [Fact]
    public void InitialState_SelectedFirstDayIsSunday()
    {
        _sut.SelectedFirstDay.ShouldBe(DayOfWeek.Sunday);
    }

    /// <summary>
    /// Verifies initial state is not saving.
    /// </summary>
    [Fact]
    public void InitialState_IsNotSaving()
    {
        _sut.IsSaving.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies initial error message is null.
    /// </summary>
    [Fact]
    public void InitialState_ErrorMessageIsNull()
    {
        _sut.ErrorMessage.ShouldBeNull();
    }

    // --- NextStep ---

    /// <summary>
    /// Verifies NextStep increments the step.
    /// </summary>
    [Fact]
    public void NextStep_IncrementsStep()
    {
        _sut.NextStep();

        _sut.CurrentStep.ShouldBe(2);
    }

    /// <summary>
    /// Verifies NextStep does not exceed step 5.
    /// </summary>
    [Fact]
    public void NextStep_DoesNotExceedMaxStep()
    {
        _sut.NextStep(); // 2
        _sut.NextStep(); // 3
        _sut.NextStep(); // 4
        _sut.NextStep(); // 5
        _sut.NextStep(); // still 5

        _sut.CurrentStep.ShouldBe(5);
    }

    /// <summary>
    /// Verifies NextStep invokes OnStateChanged.
    /// </summary>
    [Fact]
    public void NextStep_NotifiesStateChanged()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;

        _sut.NextStep();

        callCount.ShouldBe(1);
    }

    /// <summary>
    /// Verifies NextStep does not notify when already at max step.
    /// </summary>
    [Fact]
    public void NextStep_DoesNotNotify_WhenAtMaxStep()
    {
        _sut.NextStep(); // 2
        _sut.NextStep(); // 3
        _sut.NextStep(); // 4
        _sut.NextStep(); // 5

        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;
        _sut.NextStep(); // still 5

        callCount.ShouldBe(0);
    }

    // --- PreviousStep ---

    /// <summary>
    /// Verifies PreviousStep decrements the step.
    /// </summary>
    [Fact]
    public void PreviousStep_DecrementsStep()
    {
        _sut.NextStep(); // 2
        _sut.PreviousStep();

        _sut.CurrentStep.ShouldBe(1);
    }

    /// <summary>
    /// Verifies PreviousStep does not go below step 1.
    /// </summary>
    [Fact]
    public void PreviousStep_DoesNotGoBelowMinStep()
    {
        _sut.PreviousStep();

        _sut.CurrentStep.ShouldBe(1);
    }

    /// <summary>
    /// Verifies PreviousStep invokes OnStateChanged.
    /// </summary>
    [Fact]
    public void PreviousStep_NotifiesStateChanged()
    {
        _sut.NextStep(); // 2

        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;
        _sut.PreviousStep();

        callCount.ShouldBe(1);
    }

    /// <summary>
    /// Verifies PreviousStep does not notify when already at step 1.
    /// </summary>
    [Fact]
    public void PreviousStep_DoesNotNotify_WhenAtMinStep()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;
        _sut.PreviousStep();

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

        _sut.SelectCurrency(currency);

        _sut.SelectedCurrency.ShouldBe("EUR");
    }

    /// <summary>
    /// Verifies SelectCurrency clears the search term.
    /// </summary>
    [Fact]
    public void SelectCurrency_ClearsSearch()
    {
        _sut.CurrencySearch = "eu";
        var currency = new CurrencyOption("EUR", "Euro", "\u20ac");

        _sut.SelectCurrency(currency);

        _sut.CurrencySearch.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies SelectCurrency closes the dropdown.
    /// </summary>
    [Fact]
    public void SelectCurrency_ClosesDropdown()
    {
        _sut.ShowCurrencyDropdown = true;
        var currency = new CurrencyOption("EUR", "Euro", "\u20ac");

        _sut.SelectCurrency(currency);

        _sut.ShowCurrencyDropdown.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies SelectCurrency notifies state changed.
    /// </summary>
    [Fact]
    public void SelectCurrency_NotifiesStateChanged()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;
        var currency = new CurrencyOption("EUR", "Euro", "\u20ac");

        _sut.SelectCurrency(currency);

        callCount.ShouldBe(1);
    }

    // --- GetCurrencyDisplay ---

    /// <summary>
    /// Verifies GetCurrencyDisplay returns formatted string for known currency.
    /// </summary>
    [Fact]
    public void GetCurrencyDisplay_ReturnsFormattedString_ForKnownCurrency()
    {
        _sut.SelectedCurrency = "USD";

        var result = _sut.GetCurrencyDisplay();

        result.ShouldBe("USD \u2014 $ US Dollar");
    }

    /// <summary>
    /// Verifies GetCurrencyDisplay returns code for unknown currency.
    /// </summary>
    [Fact]
    public void GetCurrencyDisplay_ReturnsCode_ForUnknownCurrency()
    {
        _sut.SelectedCurrency = "XYZ";

        var result = _sut.GetCurrencyDisplay();

        result.ShouldBe("XYZ");
    }

    // --- FilteredCurrencies ---

    /// <summary>
    /// Verifies FilteredCurrencies returns all currencies when search is empty.
    /// </summary>
    [Fact]
    public void FilteredCurrencies_ReturnsAll_WhenSearchEmpty()
    {
        _sut.CurrencySearch = string.Empty;

        _sut.FilteredCurrencies.ShouldBe(CurrencyList.Currencies);
    }

    /// <summary>
    /// Verifies FilteredCurrencies filters by currency code.
    /// </summary>
    [Fact]
    public void FilteredCurrencies_FiltersByCode()
    {
        _sut.CurrencySearch = "eur";

        var results = _sut.FilteredCurrencies.ToList();

        results.Count.ShouldBe(1);
        results[0].Code.ShouldBe("EUR");
    }

    /// <summary>
    /// Verifies FilteredCurrencies filters by currency name.
    /// </summary>
    [Fact]
    public void FilteredCurrencies_FiltersByName()
    {
        _sut.CurrencySearch = "dollar";

        var results = _sut.FilteredCurrencies.ToList();

        results.ShouldAllBe(c => c.Name.Contains("Dollar", StringComparison.OrdinalIgnoreCase));
        results.Count.ShouldBeGreaterThan(1);
    }

    /// <summary>
    /// Verifies FilteredCurrencies is case-insensitive.
    /// </summary>
    [Fact]
    public void FilteredCurrencies_IsCaseInsensitive()
    {
        _sut.CurrencySearch = "EUR";
        var upperResults = _sut.FilteredCurrencies.ToList();

        _sut.CurrencySearch = "eur";
        var lowerResults = _sut.FilteredCurrencies.ToList();

        upperResults.Count.ShouldBe(lowerResults.Count);
    }

    /// <summary>
    /// Verifies FilteredCurrencies returns all when search is whitespace-only.
    /// </summary>
    [Fact]
    public void FilteredCurrencies_ReturnsAll_WhenSearchIsWhitespace()
    {
        _sut.CurrencySearch = "   ";

        _sut.FilteredCurrencies.ShouldBe(CurrencyList.Currencies);
    }

    // --- SkipOnboardingAsync ---

    /// <summary>
    /// Verifies SkipOnboardingAsync saves defaults and navigates home.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipOnboardingAsync_SavesDefaultsAndNavigates()
    {
        await _sut.SkipOnboardingAsync();

        _navigationManager.LastNavigatedUri.ShouldBe("/");
        _sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies SkipOnboardingAsync sets IsSaving to true during execution.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipOnboardingAsync_NotifiesStateChanged()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;

        await _sut.SkipOnboardingAsync();

        callCount.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies SkipOnboardingAsync handles API failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipOnboardingAsync_SetsErrorMessage_WhenApiFails()
    {
        _apiService.UpdateUserSettingsException = new HttpRequestException("Server error");

        await _sut.SkipOnboardingAsync();

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage!.ShouldContain("Failed to save");
        _sut.IsSaving.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies SkipOnboardingAsync clears error message before saving.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SkipOnboardingAsync_ClearsErrorBeforeSaving()
    {
        _apiService.UpdateUserSettingsException = new HttpRequestException("fail");
        await _sut.SkipOnboardingAsync();
        _sut.ErrorMessage.ShouldNotBeNull();

        _apiService.UpdateUserSettingsException = null;
        await _sut.SkipOnboardingAsync();

        _sut.ErrorMessage.ShouldBeNull();
    }

    // --- CompleteOnboardingAsync ---

    /// <summary>
    /// Verifies CompleteOnboardingAsync saves preferences and navigates home.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteOnboardingAsync_SavesPreferencesAndNavigates()
    {
        _sut.SelectedCurrency = "EUR";
        _sut.SelectedFirstDay = DayOfWeek.Monday;

        await _sut.CompleteOnboardingAsync();

        _navigationManager.LastNavigatedUri.ShouldBe("/");
        _sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies CompleteOnboardingAsync notifies state changed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteOnboardingAsync_NotifiesStateChanged()
    {
        int callCount = 0;
        _sut.OnStateChanged = () => callCount++;

        await _sut.CompleteOnboardingAsync();

        callCount.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies CompleteOnboardingAsync handles API failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteOnboardingAsync_SetsErrorMessage_WhenApiFails()
    {
        _apiService.UpdateUserSettingsException = new HttpRequestException("Server error");

        await _sut.CompleteOnboardingAsync();

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage!.ShouldContain("Failed to save");
        _sut.IsSaving.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies CompleteOnboardingAsync handles CompleteOnboarding API failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteOnboardingAsync_SetsErrorMessage_WhenCompleteOnboardingFails()
    {
        _apiService.CompleteOnboardingException = new HttpRequestException("Onboarding error");

        await _sut.CompleteOnboardingAsync();

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage!.ShouldContain("Failed to save");
        _sut.IsSaving.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies CompleteOnboardingAsync clears error before saving.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteOnboardingAsync_ClearsErrorBeforeSaving()
    {
        _apiService.UpdateUserSettingsException = new HttpRequestException("fail");
        await _sut.CompleteOnboardingAsync();
        _sut.ErrorMessage.ShouldNotBeNull();

        _apiService.UpdateUserSettingsException = null;
        await _sut.CompleteOnboardingAsync();

        _sut.ErrorMessage.ShouldBeNull();
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
        public string? LastNavigatedUri
        {
            get; private set;
        }

        /// <inheritdoc/>
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            this.LastNavigatedUri = uri;
        }
    }
}
