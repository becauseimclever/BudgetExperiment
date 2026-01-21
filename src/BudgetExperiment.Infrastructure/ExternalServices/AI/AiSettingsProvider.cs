// <copyright file="AiSettingsProvider.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.ExternalServices.AI;

/// <summary>
/// Provides AI settings from the database.
/// </summary>
public sealed class AiSettingsProvider : IAiSettingsProvider
{
    private readonly BudgetDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiSettingsProvider"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public AiSettingsProvider(BudgetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<AiSettingsData> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.AppSettings
            .FirstOrDefaultAsync(s => s.Id == AppSettings.SingletonId, cancellationToken);

        if (settings == null)
        {
            // Return defaults if no settings exist
            return new AiSettingsData(
                OllamaEndpoint: "http://localhost:11434",
                ModelName: "llama3.2",
                Temperature: 0.3m,
                MaxTokens: 2000,
                TimeoutSeconds: 120,
                IsEnabled: true);
        }

        return new AiSettingsData(
            OllamaEndpoint: settings.AiOllamaEndpoint,
            ModelName: settings.AiModelName,
            Temperature: settings.AiTemperature,
            MaxTokens: settings.AiMaxTokens,
            TimeoutSeconds: settings.AiTimeoutSeconds,
            IsEnabled: settings.AiIsEnabled);
    }

    /// <inheritdoc/>
    public async Task<AiSettingsData> UpdateSettingsAsync(AiSettingsData newSettings, CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.AppSettings
            .FirstOrDefaultAsync(s => s.Id == AppSettings.SingletonId, cancellationToken);

        if (settings == null)
        {
            throw new InvalidOperationException("AppSettings not found in database.");
        }

        settings.UpdateAiSettings(
            ollamaEndpoint: newSettings.OllamaEndpoint,
            modelName: newSettings.ModelName,
            temperature: newSettings.Temperature,
            maxTokens: newSettings.MaxTokens,
            timeoutSeconds: newSettings.TimeoutSeconds,
            isEnabled: newSettings.IsEnabled);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AiSettingsData(
            OllamaEndpoint: settings.AiOllamaEndpoint,
            ModelName: settings.AiModelName,
            Temperature: settings.AiTemperature,
            MaxTokens: settings.AiMaxTokens,
            TimeoutSeconds: settings.AiTimeoutSeconds,
            IsEnabled: settings.AiIsEnabled);
    }
}
