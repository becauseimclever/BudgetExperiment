// <copyright file="CategorySuggestionsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for category suggestion operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public sealed class CategorySuggestionsController : ControllerBase
{
    private readonly ICategorySuggestionService _suggestionService;
    private readonly ICategorizationRuleService _ruleService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorySuggestionsController"/> class.
    /// </summary>
    /// <param name="suggestionService">The category suggestion service.</param>
    /// <param name="ruleService">The categorization rule service.</param>
    public CategorySuggestionsController(
        ICategorySuggestionService suggestionService,
        ICategorizationRuleService ruleService)
    {
        _suggestionService = suggestionService;
        _ruleService = ruleService;
    }

    /// <summary>
    /// Analyzes transactions and generates category suggestions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of generated category suggestions.</returns>
    [HttpPost("analyze")]
    [ProducesResponseType<IReadOnlyList<CategorySuggestionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> AnalyzeAsync(CancellationToken cancellationToken)
    {
        var suggestions = await _suggestionService.AnalyzeTransactionsAsync(cancellationToken);
        var dtos = suggestions.Select(MapToDto).ToList();
        return this.Ok(dtos);
    }

    /// <summary>
    /// Gets all pending category suggestions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of pending category suggestions.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategorySuggestionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingAsync(CancellationToken cancellationToken)
    {
        var suggestions = await _suggestionService.GetPendingSuggestionsAsync(cancellationToken);
        var dtos = suggestions.Select(MapToDto).ToList();
        return this.Ok(dtos);
    }

    /// <summary>
    /// Gets dismissed category suggestions with pagination.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged list of dismissed category suggestions.</returns>
    [HttpGet("dismissed")]
    [ProducesResponseType<IReadOnlyList<CategorySuggestionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDismissedAsync(
        [FromQuery] int skip,
        [FromQuery] int take,
        CancellationToken cancellationToken)
    {
        var suggestions = await _suggestionService.GetDismissedSuggestionsAsync(skip, take, cancellationToken);
        var dtos = suggestions.Select(MapToDto).ToList();
        return this.Ok(dtos);
    }

    /// <summary>
    /// Gets a specific category suggestion by ID.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The category suggestion if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CategorySuggestionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var suggestion = await _suggestionService.GetSuggestionAsync(id, cancellationToken);
        if (suggestion is null)
        {
            return this.NotFound();
        }

        return this.Ok(MapToDto(suggestion));
    }

    /// <summary>
    /// Accepts a category suggestion, optionally with custom name, icon, or color.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="request">Optional customization request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of accepting the suggestion.</returns>
    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType<AcceptCategorySuggestionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<AcceptCategorySuggestionResultDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<AcceptCategorySuggestionResultDto>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptAsync(
        Guid id,
        [FromBody] AcceptCategorySuggestionRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _suggestionService.AcceptSuggestionAsync(
            id,
            request?.CustomName,
            request?.CustomIcon,
            request?.CustomColor,
            cancellationToken);

        var dto = new AcceptCategorySuggestionResultDto
        {
            SuggestionId = result.SuggestionId,
            Success = result.Success,
            CategoryId = result.CreatedCategoryId,
            CategoryName = result.CategoryName,
            ErrorMessage = result.ErrorMessage,
        };

        if (!result.Success && result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
        {
            return this.NotFound(dto);
        }

        if (!result.Success)
        {
            return this.BadRequest(dto);
        }

        return this.Ok(dto);
    }

    /// <summary>
    /// Dismisses a category suggestion.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("{id:guid}/dismiss")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DismissAsync(Guid id, CancellationToken cancellationToken)
    {
        var success = await _suggestionService.DismissSuggestionAsync(id, cancellationToken);
        if (!success)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }

    /// <summary>
    /// Restores a previously dismissed category suggestion.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The restored category suggestion.</returns>
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType<CategorySuggestionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var success = await _suggestionService.RestoreSuggestionAsync(id, cancellationToken);
        if (!success)
        {
            return this.NotFound();
        }

        var suggestion = await _suggestionService.GetSuggestionAsync(id, cancellationToken);
        return this.Ok(MapToDto(suggestion!));
    }

    /// <summary>
    /// Clears all dismissed suggestion patterns.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of cleared patterns.</returns>
    [HttpDelete("dismissed-patterns")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearDismissedPatternsAsync(CancellationToken cancellationToken)
    {
        var clearedCount = await _suggestionService.ClearDismissedPatternsAsync(cancellationToken);
        return this.Ok(new { clearedCount });
    }

    /// <summary>
    /// Bulk-accepts multiple category suggestions.
    /// </summary>
    /// <param name="request">The bulk accept request containing suggestion IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The results for each suggestion.</returns>
    [HttpPost("bulk-accept")]
    [ProducesResponseType<IReadOnlyList<AcceptCategorySuggestionResultDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkAcceptAsync(
        [FromBody] BulkAcceptCategorySuggestionsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SuggestionIds == null || request.SuggestionIds.Count == 0)
        {
            return this.BadRequest(new { message = "At least one suggestion ID is required." });
        }

        var results = await _suggestionService.AcceptSuggestionsAsync(request.SuggestionIds, cancellationToken);

        var dtos = results.Select(r => new AcceptCategorySuggestionResultDto
        {
            SuggestionId = r.SuggestionId,
            Success = r.Success,
            CategoryId = r.CreatedCategoryId,
            CategoryName = r.CategoryName,
            ErrorMessage = r.ErrorMessage,
        }).ToList();

        return this.Ok(dtos);
    }

    /// <summary>
    /// Previews the rules that would be created for a category suggestion.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of suggested categorization rules.</returns>
    [HttpGet("{id:guid}/preview-rules")]
    [ProducesResponseType<IReadOnlyList<SuggestedCategoryRuleDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewRulesAsync(Guid id, CancellationToken cancellationToken)
    {
        var suggestion = await _suggestionService.GetSuggestionAsync(id, cancellationToken);
        if (suggestion is null)
        {
            return this.NotFound();
        }

        var rules = await _suggestionService.GetSuggestedRulesAsync(id, cancellationToken);
        var dtos = rules.Select(r => new SuggestedCategoryRuleDto
        {
            Pattern = r.Pattern,
            MatchType = r.MatchType.ToString(),
            MatchingTransactionCount = r.MatchingTransactionCount,
            SampleDescriptions = r.SampleDescriptions,
        }).ToList();

        return this.Ok(dtos);
    }

    /// <summary>
    /// Creates categorization rules from a category suggestion.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="request">The request specifying category and optional pattern overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of creating the rules.</returns>
    [HttpPost("{id:guid}/create-rules")]
    [ProducesResponseType<CreateRulesFromSuggestionResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<CreateRulesFromSuggestionResult>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRulesAsync(
        Guid id,
        [FromBody] CreateRulesFromSuggestionRequest request,
        CancellationToken cancellationToken)
    {
        var suggestion = await _suggestionService.GetSuggestionAsync(id, cancellationToken);
        if (suggestion is null)
        {
            return this.NotFound(new CreateRulesFromSuggestionResult
            {
                Success = false,
                ErrorMessage = "Suggestion not found.",
            });
        }

        var patterns = request.Patterns?.Count > 0
            ? request.Patterns
            : suggestion.MerchantPatterns;

        var allConflicts = new List<CategorizationRuleDto>();
        foreach (var pattern in patterns)
        {
            var conflicts = await _ruleService.CheckConflictsAsync(pattern, "Contains", null, cancellationToken);
            allConflicts.AddRange(conflicts);
        }

        allConflicts = allConflicts.GroupBy(c => c.Id).Select(g => g.First()).ToList();

        var createdRules = await _ruleService.CreateBulkFromPatternsAsync(
            request.CategoryId,
            patterns,
            cancellationToken);

        return this.Ok(new CreateRulesFromSuggestionResult
        {
            Success = true,
            CreatedRules = createdRules,
            ConflictingRules = allConflicts.Count > 0 ? allConflicts : null,
        });
    }

    private static CategorySuggestionDto MapToDto(CategorySuggestion suggestion)
    {
        return new CategorySuggestionDto
        {
            Id = suggestion.Id,
            SuggestedName = suggestion.SuggestedName,
            SuggestedIcon = suggestion.SuggestedIcon,
            SuggestedColor = suggestion.SuggestedColor,
            SuggestedType = suggestion.SuggestedType.ToString(),
            Confidence = suggestion.Confidence,
            MerchantPatterns = suggestion.MerchantPatterns,
            MatchingTransactionCount = suggestion.MatchingTransactionCount,
            Status = suggestion.Status.ToString(),
            Source = suggestion.Source.ToString(),
            Reasoning = suggestion.Reasoning,
            CreatedAtUtc = suggestion.CreatedAtUtc,
        };
    }
}
