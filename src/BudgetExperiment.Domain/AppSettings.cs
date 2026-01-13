// <copyright file="AppSettings.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Application-wide settings (singleton entity).
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// The singleton identifier for the single AppSettings record.
    /// </summary>
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Initializes a new instance of the <see cref="AppSettings"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private AppSettings()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

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
    /// Gets the UTC timestamp when the settings were created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the settings were last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new default AppSettings instance.
    /// </summary>
    /// <returns>A new AppSettings instance with default values.</returns>
    public static AppSettings CreateDefault()
    {
        var now = DateTime.UtcNow;
        return new AppSettings
        {
            Id = SingletonId,
            AutoRealizePastDueItems = false,
            PastDueLookbackDays = 30,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Updates the auto-realize past-due items setting.
    /// </summary>
    /// <param name="enabled">Whether to enable auto-realize.</param>
    public void UpdateAutoRealize(bool enabled)
    {
        AutoRealizePastDueItems = enabled;
        UpdatedAtUtc = DateTime.UtcNow;
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

        PastDueLookbackDays = days;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
