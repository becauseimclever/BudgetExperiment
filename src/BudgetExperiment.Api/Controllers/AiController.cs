// <copyright file="AiController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
using BudgetExperiment.Contracts.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for AI service operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly IRuleSuggestionService _suggestionService;
    private readonly IAppSettingsService _settingsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiController"/> class.
    /// </summary>
    /// <param name="aiService">The AI service.</param>
    /// <param name="suggestionService">The rule suggestion service.</param>
    /// <param name="settingsService">The application settings service.</param>
    public AiController(
        IAiService aiService,
        IRuleSuggestionService suggestionService,
        IAppSettingsService settingsService)
    {
        this._aiService = aiService;
        this._suggestionService = suggestionService;
        this._settingsService = settingsService;
    }

    /// <summary>
    /// Gets the current AI service status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The AI service status.</returns>
    [HttpGet("status")]
    [ProducesResponseType<AiStatusDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        var status = await this._aiService.GetStatusAsync(cancellationToken);
        var settings = await this._settingsService.GetAiSettingsAsync(cancellationToken);

        return this.Ok(new AiStatusDto
        {
            IsAvailable = status.IsAvailable,
            IsEnabled = settings.IsEnabled,
            CurrentModel = status.CurrentModel,
            Endpoint = settings.OllamaEndpoint,
            ErrorMessage = status.ErrorMessage,
        });
    }

    /// <summary>
    /// Gets available AI models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of available models.</returns>
    [HttpGet("models")]
    [ProducesResponseType<IReadOnlyList<AiModelDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModelsAsync(CancellationToken cancellationToken)
    {
        var models = await this._aiService.GetAvailableModelsAsync(cancellationToken);

        var dtos = models.Select(m => new AiModelDto
        {
            Name = m.Name,
            ModifiedAt = m.ModifiedAt,
            SizeBytes = m.SizeBytes,
        }).ToList();

        return this.Ok(dtos);
    }

    /// <summary>
    /// Gets the current AI settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current AI settings.</returns>
    [HttpGet("settings")]
    [ProducesResponseType<AiSettingsDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await this._settingsService.GetAiSettingsAsync(cancellationToken);

        return this.Ok(new AiSettingsDto
        {
            OllamaEndpoint = settings.OllamaEndpoint,
            ModelName = settings.ModelName,
            Temperature = settings.Temperature,
            MaxTokens = settings.MaxTokens,
            TimeoutSeconds = settings.TimeoutSeconds,
            IsEnabled = settings.IsEnabled,
        });
    }

    /// <summary>
    /// Updates AI settings.
    /// </summary>
    /// <param name="request">The new settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated settings.</returns>
    [HttpPut("settings")]
    [ProducesResponseType<AiSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettingsAsync(
        [FromBody] AiSettingsDto request,
        CancellationToken cancellationToken)
    {
        var settingsData = new AiSettingsData(
            request.OllamaEndpoint,
            request.ModelName,
            request.Temperature,
            request.MaxTokens,
            request.TimeoutSeconds,
            request.IsEnabled);

        await this._settingsService.UpdateAiSettingsAsync(settingsData, cancellationToken);

        return this.Ok(request);
    }

    /// <summary>
    /// Runs comprehensive AI analysis on transactions and rules.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis results.</returns>
    [HttpPost("analyze")]
    [ProducesResponseType<AnalysisResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AnalyzeAsync(CancellationToken cancellationToken)
    {
        var status = await this._aiService.GetStatusAsync(cancellationToken);
        if (!status.IsAvailable)
        {
            return this.StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "AI service is not available", error = status.ErrorMessage });
        }

        var analysis = await this._suggestionService.AnalyzeAllAsync(progress: null, ct: cancellationToken);

        return this.Ok(new AnalysisResponseDto
        {
            NewRuleSuggestions = analysis.NewRuleSuggestions.Count,
            OptimizationSuggestions = analysis.OptimizationSuggestions.Count,
            ConflictSuggestions = analysis.ConflictSuggestions.Count,
            UncategorizedTransactionsAnalyzed = analysis.UncategorizedTransactionsAnalyzed,
            RulesAnalyzed = analysis.RulesAnalyzed,
            AnalysisDurationSeconds = analysis.AnalysisDuration.TotalSeconds,
        });
    }
}
