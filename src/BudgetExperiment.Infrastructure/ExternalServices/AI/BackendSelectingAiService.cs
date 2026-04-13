// <copyright file="BackendSelectingAiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Ai;
using BudgetExperiment.Application.Settings;
using BudgetExperiment.Domain.Settings;
using BudgetExperiment.Shared;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BudgetExperiment.Infrastructure.ExternalServices.AI;

/// <summary>
/// Resolves the active AI backend for each request while preserving Ollama as the default.
/// </summary>
public sealed class BackendSelectingAiService : IAiService
{
    private readonly IConfiguration _configuration;
    private readonly LlamaCppAiService _llamaCppAiService;
    private readonly ILogger<BackendSelectingAiService> _logger;
    private readonly OllamaAiService _ollamaAiService;
    private readonly IAppSettingsService _settingsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendSelectingAiService"/> class.
    /// </summary>
    /// <param name="ollamaAiService">The Ollama AI service.</param>
    /// <param name="llamaCppAiService">The llama.cpp AI service.</param>
    /// <param name="settingsService">The application settings service.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">The logger.</param>
    public BackendSelectingAiService(
        OllamaAiService ollamaAiService,
        LlamaCppAiService llamaCppAiService,
        IAppSettingsService settingsService,
        IConfiguration configuration,
        ILogger<BackendSelectingAiService> logger)
    {
        _ollamaAiService = ollamaAiService;
        _llamaCppAiService = llamaCppAiService;
        _settingsService = settingsService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AiServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var aiService = await ResolveBackendAsync(cancellationToken);
        return await aiService.GetStatusAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        var aiService = await ResolveBackendAsync(cancellationToken);
        return await aiService.GetAvailableModelsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AiResponse> CompleteAsync(AiPrompt prompt, CancellationToken cancellationToken = default)
    {
        var aiService = await ResolveBackendAsync(cancellationToken);
        return await aiService.CompleteAsync(prompt, cancellationToken);
    }

    private static IAiService GetServiceForBackendType(
        AiBackendType backendType,
        OllamaAiService ollamaAiService,
        LlamaCppAiService llamaCppAiService)
    {
        return backendType switch
        {
            AiBackendType.LlamaCpp => llamaCppAiService,
            _ => ollamaAiService,
        };
    }

    private async Task<IAiService> ResolveBackendAsync(CancellationToken cancellationToken)
    {
        var configuredBackendType = GetConfiguredBackendType();

        try
        {
            var settings = await _settingsService.GetAiSettingsAsync(cancellationToken);
            return GetServiceForBackendType(settings.BackendType, _ollamaAiService, _llamaCppAiService);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Falling back to configured AI backend {BackendType} because persisted AI settings could not be read.",
                configuredBackendType);

            return GetServiceForBackendType(configuredBackendType, _ollamaAiService, _llamaCppAiService);
        }
    }

    private AiBackendType GetConfiguredBackendType()
    {
        var configuredValue = _configuration["AiSettings:BackendType"];
        if (Enum.TryParse(configuredValue, true, out AiBackendType backendType) &&
            Enum.IsDefined(backendType))
        {
            return backendType;
        }

        return AiDefaults.DefaultBackendType;
    }
}
