// <copyright file="UserSettingsCurrencyProvider.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Repositories;
using BudgetExperiment.Domain.Settings;

namespace BudgetExperiment.Application.Settings;

/// <summary>
/// Resolves the active currency from the current user's <see cref="UserSettings.PreferredCurrency"/>.
/// Falls back to USD when the preference is unset or the user is not authenticated.
/// </summary>
public sealed class UserSettingsCurrencyProvider : ICurrencyProvider
{
    private const string DefaultCurrency = "USD";
    private readonly IUserContext _userContext;
    private readonly IUserSettingsRepository _settingsRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSettingsCurrencyProvider"/> class.
    /// </summary>
    /// <param name="userContext">The current user context.</param>
    /// <param name="settingsRepository">The user settings repository.</param>
    public UserSettingsCurrencyProvider(
        IUserContext userContext,
        IUserSettingsRepository settingsRepository)
    {
        _userContext = userContext;
        _settingsRepository = settingsRepository;
    }

    /// <inheritdoc/>
    public async Task<string> GetCurrencyAsync(CancellationToken cancellationToken = default)
    {
        if (!_userContext.IsAuthenticated)
        {
            return DefaultCurrency;
        }

        var userId = _userContext.UserIdAsGuid;
        if (userId is null)
        {
            return DefaultCurrency;
        }

        var settings = await _settingsRepository
            .GetByUserIdAsync(userId.Value, cancellationToken);

        return string.IsNullOrWhiteSpace(settings.PreferredCurrency)
            ? DefaultCurrency
            : settings.PreferredCurrency;
    }
}
