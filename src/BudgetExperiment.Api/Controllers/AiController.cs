// <copyright file="AiController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for AI service operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public sealed class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly IRuleSuggestionService _suggestionService;
    private readonly IAppSettingsService _settingsService;
    private readonly ILogger<AiController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiController"/> class.
    /// </summary>
    /// <param name="aiService">The AI service.</param>
    /// <param name="suggestionService">The rule suggestion service.</param>
    /// <param name="settingsService">The application settings service.</param>
    /// <param name="logger">The logger.</param>
    public AiController(
        IAiService aiService,
        IRuleSuggestionService suggestionService,
        IAppSettingsService settingsService,
        ILogger<AiController> logger)
    {
        _aiService = aiService;
        _suggestionService = suggestionService;
        _settingsService = settingsService;
        _logger = logger;
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
        var status = await _aiService.GetStatusAsync(cancellationToken);
        var settings = await _settingsService.GetAiSettingsAsync(cancellationToken);

        return this.Ok(new AiStatusDto
        {
            IsAvailable = status.IsAvailable,
            IsEnabled = settings.IsEnabled,
            CurrentModel = status.CurrentModel,
            Endpoint = settings.EndpointUrl,
            BackendType = settings.BackendType,
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
        var models = await _aiService.GetAvailableModelsAsync(cancellationToken);

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
        var settings = await _settingsService.GetAiSettingsAsync(cancellationToken);

        return this.Ok(MapSettings(settings));
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
            request.ResolveEndpointUrl(),
            request.ModelName,
            request.Temperature,
            request.MaxTokens,
            request.TimeoutSeconds,
            request.IsEnabled,
            request.BackendType);

        var updatedSettings = await _settingsService.UpdateAiSettingsAsync(settingsData, cancellationToken);

        return this.Ok(MapSettings(updatedSettings));
    }

    /// <summary>
    /// Runs comprehensive AI analysis on transactions and rules.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis results.</returns>
    [HttpPost("analyze")]
    [RequestTimeout("AiAnalysis")]
    [Produces("application/json", "application/problem+json")]
    [ProducesResponseType<AnalysisResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> AnalyzeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting AI analysis request");

        var status = await _aiService.GetStatusAsync(cancellationToken);
        if (!status.IsAvailable)
        {
            _logger.LogWarning("AI service is not available: {ErrorMessage}", status.ErrorMessage);
            var detail = string.IsNullOrWhiteSpace(status.ErrorMessage)
                ? "AI service is not available"
                : $"AI service is not available. {status.ErrorMessage}";

            return this.CreateAnalyzeProblem(StatusCodes.Status503ServiceUnavailable, detail);
        }

        try
        {
            _logger.LogInformation("AI service is available, starting analysis...");
            var analysis = await _suggestionService.AnalyzeAllAsync(progress: null, ct: cancellationToken);

            _logger.LogInformation(
                "AI analysis completed in {Duration:F2}s. Found {NewRules} new rule suggestions, {Optimizations} optimizations, {Conflicts} conflicts",
                analysis.AnalysisDuration.TotalSeconds,
                analysis.NewRuleSuggestions.Count,
                analysis.OptimizationSuggestions.Count,
                analysis.ConflictSuggestions.Count);

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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Client disconnected or cancelled - return 499 (Client Closed Request, nginx convention)
            _logger.LogInformation("AI analysis was cancelled by client");
            return this.CreateAnalyzeProblem(499, "Client cancelled the request");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "AI analysis timed out");
            return this.CreateAnalyzeProblem(
                StatusCodes.Status504GatewayTimeout,
                "AI analysis timed out. The AI service took too long to respond.",
                "Try increasing the timeout in AI Settings or ensure Ollama is running properly.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to communicate with AI service during analysis");
            return this.CreateAnalyzeProblem(
                StatusCodes.Status503ServiceUnavailable,
                $"Failed to communicate with AI service. {ex.Message}");
        }
    }

    private static AiSettingsDto MapSettings(AiSettingsData settings)
    {
        return new AiSettingsDto
        {
            EndpointUrl = settings.EndpointUrl,
            ModelName = settings.ModelName,
            Temperature = settings.Temperature,
            MaxTokens = settings.MaxTokens,
            TimeoutSeconds = settings.TimeoutSeconds,
            IsEnabled = settings.IsEnabled,
            BackendType = settings.BackendType,
        };
    }

    private IActionResult CreateAnalyzeProblem(int statusCode, string detail, string? suggestion = null)
    {
        var title = statusCode switch
        {
            StatusCodes.Status503ServiceUnavailable => "Service Unavailable",
            StatusCodes.Status504GatewayTimeout => "Gateway Timeout",
            499 => "Client Closed Request",
            _ => "Error",
        };

        var problemDetails = new ProblemDetails
        {
            Type = "about:blank",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = this.HttpContext.Request.Path,
        };

        problemDetails.Extensions["traceId"] = this.HttpContext.TraceIdentifier;

        if (!string.IsNullOrWhiteSpace(suggestion))
        {
            problemDetails.Extensions["suggestion"] = suggestion;
        }

        var result = new JsonResult(problemDetails)
        {
            StatusCode = statusCode,
            ContentType = "application/problem+json",
        };

        return result;
    }
}
