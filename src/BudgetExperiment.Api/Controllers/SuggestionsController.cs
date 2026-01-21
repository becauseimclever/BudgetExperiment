// <copyright file="SuggestionsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for AI-generated rule suggestion operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/ai/[controller]")]
[Produces("application/json")]
public sealed class SuggestionsController : ControllerBase
{
    private readonly IRuleSuggestionService _suggestionService;
    private readonly IAiService _aiService;
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly ICategorizationRuleRepository _ruleRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestionsController"/> class.
    /// </summary>
    /// <param name="suggestionService">The rule suggestion service.</param>
    /// <param name="aiService">The AI service.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="ruleRepository">The categorization rule repository.</param>
    public SuggestionsController(
        IRuleSuggestionService suggestionService,
        IAiService aiService,
        IBudgetCategoryRepository categoryRepository,
        ICategorizationRuleRepository ruleRepository)
    {
        this._suggestionService = suggestionService;
        this._aiService = aiService;
        this._categoryRepository = categoryRepository;
        this._ruleRepository = ruleRepository;
    }

    /// <summary>
    /// Generates new AI suggestions.
    /// </summary>
    /// <param name="request">The generation request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated suggestions.</returns>
    [HttpPost("generate")]
    [ProducesResponseType<IReadOnlyList<RuleSuggestionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GenerateAsync(
        [FromBody] GenerateSuggestionsRequest request,
        CancellationToken cancellationToken)
    {
        var status = await this._aiService.GetStatusAsync(cancellationToken);
        if (!status.IsAvailable)
        {
            return this.StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "AI service is not available", error = status.ErrorMessage });
        }

        IReadOnlyList<RuleSuggestion> suggestions;

        if (string.IsNullOrEmpty(request.SuggestionType))
        {
            // Generate all types
            var newRules = await this._suggestionService.SuggestNewRulesAsync(request.MaxSuggestions, cancellationToken);
            var optimizations = await this._suggestionService.SuggestOptimizationsAsync(cancellationToken);
            var conflicts = await this._suggestionService.DetectConflictsAsync(cancellationToken);
            suggestions = newRules.Concat(optimizations).Concat(conflicts).ToList();
        }
        else if (request.SuggestionType.Equals("NewRule", StringComparison.OrdinalIgnoreCase))
        {
            suggestions = await this._suggestionService.SuggestNewRulesAsync(request.MaxSuggestions, cancellationToken);
        }
        else if (request.SuggestionType.Equals("Optimization", StringComparison.OrdinalIgnoreCase))
        {
            suggestions = await this._suggestionService.SuggestOptimizationsAsync(cancellationToken);
        }
        else if (request.SuggestionType.Equals("Conflict", StringComparison.OrdinalIgnoreCase))
        {
            suggestions = await this._suggestionService.DetectConflictsAsync(cancellationToken);
        }
        else
        {
            return this.BadRequest(new { message = $"Invalid suggestion type: {request.SuggestionType}. Valid values are: NewRule, Optimization, Conflict" });
        }

        var dtos = await this.MapSuggestionsToDtosAsync(suggestions, cancellationToken);
        return this.Ok(dtos);
    }

    /// <summary>
    /// Gets pending suggestions with optional type filter.
    /// </summary>
    /// <param name="type">Optional filter by suggestion type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of pending suggestions.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RuleSuggestionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingAsync(
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        SuggestionType? typeFilter = null;

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<SuggestionType>(type, ignoreCase: true, out var parsedType))
        {
            typeFilter = parsedType;
        }

        var suggestions = await this._suggestionService.GetPendingSuggestionsAsync(typeFilter, cancellationToken);
        var dtos = await this.MapSuggestionsToDtosAsync(suggestions, cancellationToken);

        return this.Ok(dtos);
    }

    /// <summary>
    /// Gets a specific suggestion by ID.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The suggestion details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<RuleSuggestionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var suggestions = await this._suggestionService.GetPendingSuggestionsAsync(ct: cancellationToken);
        var suggestion = suggestions.FirstOrDefault(s => s.Id == id);

        if (suggestion is null)
        {
            return this.NotFound();
        }

        var dto = await this.MapSuggestionToDtoAsync(suggestion, cancellationToken);
        return this.Ok(dto);
    }

    /// <summary>
    /// Accepts a suggestion and creates the corresponding rule.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created or modified rule.</returns>
    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType<CategorizationRuleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var rule = await this._suggestionService.AcceptSuggestionAsync(id, cancellationToken);
            return this.Ok(CategorizationMapper.ToDto(rule));
        }
        catch (DomainException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return this.NotFound(new { message = ex.Message });
        }
        catch (DomainException ex)
        {
            return this.BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Dismisses a suggestion.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="request">Optional dismissal reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("{id:guid}/dismiss")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DismissAsync(
        Guid id,
        [FromBody] DismissSuggestionRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            await this._suggestionService.DismissSuggestionAsync(id, request?.Reason, cancellationToken);
            return this.NoContent();
        }
        catch (DomainException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return this.NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Provides feedback on a suggestion.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="request">The feedback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("{id:guid}/feedback")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProvideFeedbackAsync(
        Guid id,
        [FromBody] FeedbackRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await this._suggestionService.ProvideFeedbackAsync(id, request.IsPositive, cancellationToken);
            return this.NoContent();
        }
        catch (DomainException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return this.NotFound(new { message = ex.Message });
        }
    }

    private async Task<IReadOnlyList<RuleSuggestionDto>> MapSuggestionsToDtosAsync(
        IReadOnlyList<RuleSuggestion> suggestions,
        CancellationToken cancellationToken)
    {
        // Pre-fetch categories and rules for efficient mapping
        var categoryIds = suggestions
            .Where(s => s.SuggestedCategoryId.HasValue)
            .Select(s => s.SuggestedCategoryId!.Value)
            .Distinct();

        var ruleIds = suggestions
            .Where(s => s.TargetRuleId.HasValue)
            .Select(s => s.TargetRuleId!.Value)
            .Distinct();

        var categories = await this._categoryRepository.ListAsync(0, int.MaxValue, cancellationToken);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name);

        var rules = await this._ruleRepository.ListAsync(0, int.MaxValue, cancellationToken);
        var ruleLookup = rules.ToDictionary(r => r.Id, r => r.Name);

        return suggestions.Select(s => CategorizationMapper.ToDto(
            s,
            s.SuggestedCategoryId.HasValue ? categoryLookup.GetValueOrDefault(s.SuggestedCategoryId.Value) : null,
            s.TargetRuleId.HasValue ? ruleLookup.GetValueOrDefault(s.TargetRuleId.Value) : null))
            .ToList();
    }

    private async Task<RuleSuggestionDto> MapSuggestionToDtoAsync(
        RuleSuggestion suggestion,
        CancellationToken cancellationToken)
    {
        string? categoryName = null;
        string? ruleName = null;

        if (suggestion.SuggestedCategoryId.HasValue)
        {
            var category = await this._categoryRepository.GetByIdAsync(suggestion.SuggestedCategoryId.Value, cancellationToken);
            categoryName = category?.Name;
        }

        if (suggestion.TargetRuleId.HasValue)
        {
            var rule = await this._ruleRepository.GetByIdAsync(suggestion.TargetRuleId.Value, cancellationToken);
            ruleName = rule?.Name;
        }

        return CategorizationMapper.ToDto(suggestion, categoryName, ruleName);
    }
}
