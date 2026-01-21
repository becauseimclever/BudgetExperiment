// <copyright file="CategorySuggestionsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Categorization;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain.Categorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for AI-powered category suggestion operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class CategorySuggestionsController : ControllerBase
{
    private readonly ICategorySuggestionService _suggestionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorySuggestionsController"/> class.
    /// </summary>
    /// <param name="suggestionService">The category suggestion service.</param>
    public CategorySuggestionsController(ICategorySuggestionService suggestionService)
    {
        _suggestionService = suggestionService;
    }

    /// <summary>
    /// Analyzes uncategorized transactions and generates category suggestions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated category suggestions.</returns>
    [HttpPost("analyze")]
    [ProducesResponseType<IReadOnlyList<CategorySuggestionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> AnalyzeAsync(CancellationToken cancellationToken)
    {
        var suggestions = await _suggestionService.AnalyzeTransactionsAsync(cancellationToken);
        var dtos = suggestions.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Gets all pending category suggestions for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of pending category suggestions.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategorySuggestionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingAsync(CancellationToken cancellationToken)
    {
        var suggestions = await _suggestionService.GetPendingSuggestionsAsync(cancellationToken);
        var dtos = suggestions.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    /// <summary>
    /// Gets a specific category suggestion by ID.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The suggestion details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CategorySuggestionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var suggestion = await _suggestionService.GetSuggestionAsync(id, cancellationToken);
        if (suggestion is null)
        {
            return NotFound();
        }

        return Ok(MapToDto(suggestion));
    }

    /// <summary>
    /// Accepts a category suggestion and creates the category.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="request">Optional customization for the accepted category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the accept operation.</returns>
    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType<AcceptCategorySuggestionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
            return NotFound(dto);
        }

        if (!result.Success)
        {
            return BadRequest(dto);
        }

        return Ok(dto);
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
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Accepts multiple category suggestions in bulk.
    /// </summary>
    /// <param name="request">The bulk accept request.</param>
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
            return BadRequest(new { message = "At least one suggestion ID is required." });
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

        return Ok(dtos);
    }

    /// <summary>
    /// Previews the categorization rules that would be created for a suggestion.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of suggested rules.</returns>
    [HttpGet("{id:guid}/preview-rules")]
    [ProducesResponseType<IReadOnlyList<SuggestedCategoryRuleDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewRulesAsync(Guid id, CancellationToken cancellationToken)
    {
        var suggestion = await _suggestionService.GetSuggestionAsync(id, cancellationToken);
        if (suggestion is null)
        {
            return NotFound();
        }

        var rules = await _suggestionService.GetSuggestedRulesAsync(id, cancellationToken);
        var dtos = rules.Select(r => new SuggestedCategoryRuleDto
        {
            Pattern = r.Pattern,
            MatchType = r.MatchType.ToString(),
            MatchingTransactionCount = r.MatchingTransactionCount,
            SampleDescriptions = r.SampleDescriptions,
        }).ToList();

        return Ok(dtos);
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
            CreatedAtUtc = suggestion.CreatedAtUtc,
        };
    }
}
