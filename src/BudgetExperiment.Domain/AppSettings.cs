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

    // ============ AI Settings ============

    /// <summary>
    /// Gets the Ollama API endpoint URL.
    /// </summary>
    public string AiOllamaEndpoint { get; private set; } = "http://localhost:11434";

    /// <summary>
    /// Gets the AI model name to use.
    /// </summary>
    public string AiModelName { get; private set; } = "llama3.2";

    /// <summary>
    /// Gets the temperature for AI generation (0.0 to 1.0).
    /// Lower values are more deterministic.
    /// </summary>
    public decimal AiTemperature { get; private set; } = 0.3m;

    /// <summary>
    /// Gets the maximum tokens for AI responses.
    /// </summary>
    public int AiMaxTokens { get; private set; } = 2000;

    /// <summary>
    /// Gets the AI request timeout in seconds.
    /// </summary>
    public int AiTimeoutSeconds { get; private set; } = 120;

    /// <summary>
    /// Gets a value indicating whether AI features are enabled.
    /// </summary>
    public bool AiIsEnabled { get; private set; } = true;

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

    /// <summary>
    /// Updates the AI settings.
    /// </summary>
    /// <param name="ollamaEndpoint">The Ollama endpoint URL.</param>
    /// <param name="modelName">The model name.</param>
    /// <param name="temperature">The temperature (0.0 to 1.0).</param>
    /// <param name="maxTokens">The maximum tokens.</param>
    /// <param name="timeoutSeconds">The timeout in seconds.</param>
    /// <param name="isEnabled">Whether AI is enabled.</param>
    /// <exception cref="DomainException">Thrown when parameters are invalid.</exception>
    public void UpdateAiSettings(
        string ollamaEndpoint,
        string modelName,
        decimal temperature,
        int maxTokens,
        int timeoutSeconds,
        bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(ollamaEndpoint))
        {
            throw new DomainException("Ollama endpoint is required.");
        }

        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new DomainException("Model name is required.");
        }

        if (temperature < 0 || temperature > 1)
        {
            throw new DomainException("Temperature must be between 0 and 1.");
        }

        if (maxTokens < 100 || maxTokens > 8000)
        {
            throw new DomainException("Max tokens must be between 100 and 8000.");
        }

        if (timeoutSeconds < 30 || timeoutSeconds > 600)
        {
            throw new DomainException("Timeout must be between 30 and 600 seconds.");
        }

        AiOllamaEndpoint = ollamaEndpoint;
        AiModelName = modelName;
        AiTemperature = temperature;
        AiMaxTokens = maxTokens;
        AiTimeoutSeconds = timeoutSeconds;
        AiIsEnabled = isEnabled;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
