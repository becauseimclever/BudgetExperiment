// <copyright file="SettingsPage.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.PageObjects;

/// <summary>
/// Page object for the Settings page.
/// </summary>
public class SettingsPage : BasePage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPage"/> class.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    public SettingsPage(IPage page)
        : base(page)
    {
    }

    /// <summary>
    /// Gets the Save Settings button.
    /// </summary>
    public ILocator SaveButton => Page.GetByRole(AriaRole.Button, new() { Name = "Save" });

    /// <summary>
    /// Gets the default currency dropdown.
    /// </summary>
    public ILocator CurrencyDropdown => Page.GetByLabel("Default Currency");

    /// <summary>
    /// Gets the date format dropdown.
    /// </summary>
    public ILocator DateFormatDropdown => Page.GetByLabel("Date Format");

    /// <summary>
    /// Gets the success message after saving.
    /// </summary>
    public ILocator SuccessMessage => Page.Locator(".alert-success, .success-message");

    /// <summary>
    /// Selects a default currency.
    /// </summary>
    /// <param name="currency">The currency code to select.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SelectCurrencyAsync(string currency)
    {
        await CurrencyDropdown.SelectOptionAsync(currency);
    }

    /// <summary>
    /// Saves the settings.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task SaveSettingsAsync()
    {
        await SaveButton.ClickAsync();
    }
}
