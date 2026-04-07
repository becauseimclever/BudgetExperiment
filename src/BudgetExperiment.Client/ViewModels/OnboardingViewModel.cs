// <copyright file="OnboardingViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.ViewModels;

/// <summary>
/// ViewModel for the Onboarding page. Contains all handler logic, state fields,
/// and computed properties extracted from the Onboarding.razor @code block.
/// </summary>
public sealed class OnboardingViewModel
{
    private readonly IBudgetApiService _apiService;
    private readonly NavigationManager _navigationManager;
    private readonly IApiErrorContext _apiErrorContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnboardingViewModel"/> class.
    /// </summary>
    /// <param name="apiService">The budget API service.</param>
    /// <param name="navigationManager">The navigation manager.</param>
    /// <param name="apiErrorContext">The API error context for trace ID tracking.</param>
    public OnboardingViewModel(IBudgetApiService apiService, NavigationManager navigationManager, IApiErrorContext apiErrorContext)
    {
        _apiService = apiService;
        _navigationManager = navigationManager;
        _apiErrorContext = apiErrorContext;
    }

    /// <summary>
    /// Gets or sets the callback to notify the Razor page that state has changed and it should re-render.
    /// </summary>
    public Action? OnStateChanged
    {
        get; set;
    }

    /// <summary>
    /// Gets the current wizard step (1–5).
    /// </summary>
    public int CurrentStep { get; private set; } = 1;

    /// <summary>
    /// Gets or sets the selected currency code.
    /// </summary>
    public string SelectedCurrency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the selected first day of the week.
    /// </summary>
    public DayOfWeek SelectedFirstDay { get; set; } = DayOfWeek.Sunday;

    /// <summary>
    /// Gets or sets the currency search term.
    /// </summary>
    public string CurrencySearch { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the currency dropdown is visible.
    /// </summary>
    public bool ShowCurrencyDropdown
    {
        get; set;
    }

    /// <summary>
    /// Gets a value indicating whether a save operation is in progress.
    /// </summary>
    public bool IsSaving
    {
        get; private set;
    }

    /// <summary>
    /// Gets the current error message, if any.
    /// </summary>
    public string? ErrorMessage
    {
        get; private set;
    }

    /// <summary>
    /// Gets the trace ID associated with the current error, if any.
    /// </summary>
    public string? ErrorTraceId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the filtered list of currencies based on the search term.
    /// </summary>
    public IEnumerable<CurrencyOption> FilteredCurrencies =>
        string.IsNullOrWhiteSpace(this.CurrencySearch)
            ? CurrencyList.Currencies
            : CurrencyList.Currencies.Where(c =>
                c.Code.Contains(this.CurrencySearch, StringComparison.OrdinalIgnoreCase) ||
                c.Name.Contains(this.CurrencySearch, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Advances to the next wizard step.
    /// </summary>
    public void NextStep()
    {
        if (this.CurrentStep < 5)
        {
            this.CurrentStep++;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Jumps directly to the specified wizard step.
    /// </summary>
    /// <param name="step">The step number (1–5).</param>
    public void JumpToStep(int step)
    {
        if (step >= 1 && step <= 5)
        {
            this.CurrentStep = step;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Returns to the previous wizard step.
    /// </summary>
    public void PreviousStep()
    {
        if (this.CurrentStep > 1)
        {
            this.CurrentStep--;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Selects a currency option and closes the dropdown.
    /// </summary>
    /// <param name="currency">The currency to select.</param>
    public void SelectCurrency(CurrencyOption currency)
    {
        this.SelectedCurrency = currency.Code;
        this.CurrencySearch = string.Empty;
        this.ShowCurrencyDropdown = false;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Returns a formatted display string for the currently selected currency.
    /// </summary>
    /// <returns>A string showing the currency code, symbol, and name.</returns>
    public string GetCurrencyDisplay()
    {
        var currency = CurrencyList.Currencies.FirstOrDefault(c => c.Code == this.SelectedCurrency);
        return currency != null ? $"{currency.Code} — {currency.Symbol} {currency.Name}" : this.SelectedCurrency;
    }

    /// <summary>
    /// Skips onboarding by saving defaults and navigating to the home page.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task SkipOnboardingAsync()
    {
        this.IsSaving = true;
        this.ErrorMessage = null;
        this.ErrorTraceId = null;
        this.NotifyStateChanged();

        try
        {
            await _apiService.UpdateUserSettingsAsync(new UserSettingsUpdateDto
            {
                PreferredCurrency = "USD",
                FirstDayOfWeek = DayOfWeek.Sunday,
            });
            await _apiService.CompleteOnboardingAsync();
            _navigationManager.NavigateTo("/", forceLoad: true);
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to save: {ex.Message}";
            this.ErrorTraceId = _apiErrorContext.LastTraceId;
            this.IsSaving = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Completes onboarding by saving selected preferences and navigating to the home page.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task CompleteOnboardingAsync()
    {
        this.IsSaving = true;
        this.ErrorMessage = null;
        this.ErrorTraceId = null;
        this.NotifyStateChanged();

        try
        {
            await _apiService.UpdateUserSettingsAsync(new UserSettingsUpdateDto
            {
                PreferredCurrency = this.SelectedCurrency,
                FirstDayOfWeek = this.SelectedFirstDay,
            });
            await _apiService.CompleteOnboardingAsync();
            _navigationManager.NavigateTo("/", forceLoad: true);
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to save: {ex.Message}";
            this.ErrorTraceId = _apiErrorContext.LastTraceId;
            this.IsSaving = false;
            this.NotifyStateChanged();
        }
    }

    private void NotifyStateChanged()
    {
        this.OnStateChanged?.Invoke();
    }
}
