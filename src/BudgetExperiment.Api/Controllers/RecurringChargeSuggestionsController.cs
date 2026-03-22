// <copyright file="RecurringChargeSuggestionsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Common;
using BudgetExperiment.Application.Recurring;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Recurring;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for recurring charge suggestion operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/recurring-charge-suggestions")]
[Produces("application/json")]
public sealed class RecurringChargeSuggestionsController : ControllerBase
{
    private readonly IRecurringChargeDetectionService _detectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringChargeSuggestionsController"/> class.
    /// </summary>
    /// <param name="detectionService">The recurring charge detection service.</param>
    public RecurringChargeSuggestionsController(
        IRecurringChargeDetectionService detectionService)
    {
        _detectionService = detectionService;
    }

    /// <summary>
    /// Triggers recurring charge detection for an account or all accounts.
    /// </summary>
    /// <param name="request">Optional request with account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of new or updated suggestions.</returns>
    [HttpPost("detect")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<IActionResult> DetectAsync(
        [FromBody] DetectRecurringChargesRequest? request,
        CancellationToken cancellationToken)
    {
        var count = await _detectionService.DetectAsync(
            request?.AccountId,
            cancellationToken);
        return this.Ok(count);
    }

    /// <summary>
    /// Lists recurring charge suggestions with optional filtering and pagination.
    /// </summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="status">Optional status filter (Pending, Accepted, Dismissed).</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recurring charge suggestions.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RecurringChargeSuggestionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuggestionsAsync(
        [FromQuery] Guid? accountId,
        [FromQuery] string? status,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        SuggestionStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<SuggestionStatus>(status, ignoreCase: true, out var parsed))
        {
            statusFilter = parsed;
        }

        var (items, totalCount) = await _detectionService.GetSuggestionsAsync(
            accountId,
            statusFilter,
            skip,
            take,
            cancellationToken);

        this.Response.Headers["X-Pagination-TotalCount"] = totalCount.ToString();
        var dtos = items.Select(MapToDto).ToList();
        return this.Ok(dtos);
    }

    /// <summary>
    /// Gets a single recurring charge suggestion by ID.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recurring charge suggestion.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<RecurringChargeSuggestionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var suggestion = await _detectionService.GetSuggestionByIdAsync(
            id,
            cancellationToken);

        if (suggestion is null)
        {
            return this.NotFound();
        }

        return this.Ok(MapToDto(suggestion));
    }

    /// <summary>
    /// Accepts a recurring charge suggestion, creating a RecurringTransaction.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The acceptance result with created transaction info.</returns>
    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType<AcceptRecurringChargeSuggestionResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _detectionService.AcceptAsync(id, cancellationToken);
            return this.Ok(new AcceptRecurringChargeSuggestionResultDto
            {
                RecurringTransactionId = result.RecurringTransactionId,
                LinkedTransactionCount = result.LinkedTransactionCount,
            });
        }
        catch (DomainException ex) when (ex.Message.Contains("not found"))
        {
            return this.NotFound();
        }
        catch (DomainException ex)
        {
            return this.BadRequest(new ProblemDetails
            {
                Title = "Invalid operation",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
            });
        }
    }

    /// <summary>
    /// Dismisses a recurring charge suggestion.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("{id:guid}/dismiss")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DismissAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _detectionService.DismissAsync(id, cancellationToken);
            return this.NoContent();
        }
        catch (DomainException ex) when (ex.Message.Contains("not found"))
        {
            return this.NotFound();
        }
    }

    private static RecurringChargeSuggestionDto MapToDto(RecurringChargeSuggestion suggestion)
    {
        return new RecurringChargeSuggestionDto
        {
            Id = suggestion.Id,
            AccountId = suggestion.AccountId,
            NormalizedDescription = suggestion.NormalizedDescription,
            SampleDescription = suggestion.SampleDescription,
            AverageAmount = CommonMapper.ToDto(suggestion.AverageAmount),
            DetectedFrequency = suggestion.DetectedFrequency.ToString(),
            DetectedInterval = suggestion.DetectedInterval,
            Confidence = suggestion.Confidence,
            MatchingTransactionCount = suggestion.MatchingTransactionCount,
            FirstOccurrence = suggestion.FirstOccurrence,
            LastOccurrence = suggestion.LastOccurrence,
            CategoryId = suggestion.CategoryId,
            Status = suggestion.Status.ToString(),
            AcceptedRecurringTransactionId = suggestion.AcceptedRecurringTransactionId,
            CreatedAtUtc = suggestion.CreatedAtUtc,
            UpdatedAtUtc = suggestion.UpdatedAtUtc,
        };
    }
}
