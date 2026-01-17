// <copyright file="UserSettings.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Per-user application settings.
/// </summary>
public sealed class UserSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSettings"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private UserSettings()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the user ID (from Authentik 'sub' claim).
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the user's default budget scope preference.
    /// </summary>
    public BudgetScope DefaultScope { get; private set; } = BudgetScope.Shared;

    /// <summary>
    /// Gets a value indicating whether past-due recurring items are automatically realized
    /// without requiring manual confirmation.
    /// </summary>
    public bool AutoRealizePastDueItems { get; private set; }

    /// <summary>
    /// Gets how many days back to look for past-due items.
    /// </summary>
    public int PastDueLookbackDays { get; private set; } = 30;

    /// <summary>
    /// Gets the user's preferred currency code (e.g., "USD", "EUR").
    /// </summary>
    public string? PreferredCurrency { get; private set; }

    /// <summary>
    /// Gets the user's time zone ID (IANA format, e.g., "America/New_York").
    /// </summary>
    public string? TimeZoneId { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the settings were created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the settings were last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new default UserSettings instance for a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>A new UserSettings instance with default values.</returns>
    /// <exception cref="DomainException">Thrown when userId is empty.</exception>
    public static UserSettings CreateDefault(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User ID is required.");
        }

        var now = DateTime.UtcNow;
        return new UserSettings
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DefaultScope = BudgetScope.Shared,
            AutoRealizePastDueItems = false,
            PastDueLookbackDays = 30,
            PreferredCurrency = null,
            TimeZoneId = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Updates the default scope preference.
    /// </summary>
    /// <param name="scope">The new default scope.</param>
    public void UpdateDefaultScope(BudgetScope scope)
    {
        this.DefaultScope = scope;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the auto-realize past-due items setting.
    /// </summary>
    /// <param name="enabled">Whether to enable auto-realize.</param>
    public void UpdateAutoRealize(bool enabled)
    {
        this.AutoRealizePastDueItems = enabled;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the past-due lookback days setting.
    /// </summary>
    /// <param name="days">Number of days to look back (1-365).</param>
    /// <exception cref="DomainException">Thrown when days is outside valid range.</exception>
    public void UpdatePastDueLookbackDays(int days)
    {
        if (days < 1 || days > 365)
        {
            throw new DomainException("Lookback days must be between 1 and 365.");
        }

        this.PastDueLookbackDays = days;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the preferred currency.
    /// </summary>
    /// <param name="currencyCode">The currency code (e.g., "USD", "EUR"), or null to clear.</param>
    public void UpdatePreferredCurrency(string? currencyCode)
    {
        this.PreferredCurrency = currencyCode?.Trim().ToUpperInvariant();
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the time zone ID.
    /// </summary>
    /// <param name="timeZoneId">The IANA time zone ID (e.g., "America/New_York"), or null to clear.</param>
    public void UpdateTimeZoneId(string? timeZoneId)
    {
        this.TimeZoneId = timeZoneId?.Trim();
        this.UpdatedAtUtc = DateTime.UtcNow;
    }
}
