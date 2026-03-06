// <copyright file="MerchantMappingsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Api.Models;
using BudgetExperiment.Application.Categorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// API endpoints for managing learned merchant mappings.
/// </summary>
[ApiVersion("1.0")]
[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class MerchantMappingsController : ControllerBase
{
    private readonly IMerchantMappingService _mappingService;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="MerchantMappingsController"/> class.
    /// </summary>
    /// <param name="mappingService">The merchant mapping service.</param>
    /// <param name="userContext">The user context.</param>
    public MerchantMappingsController(IMerchantMappingService mappingService, IUserContext userContext)
    {
        _mappingService = mappingService;
        _userContext = userContext;
    }

    /// <summary>
    /// Gets all learned merchant mappings for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of learned mappings.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LearnedMerchantMappingDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LearnedMerchantMappingDto>>> GetLearnedAsync(CancellationToken cancellationToken = default)
    {
        var mappings = await _mappingService.GetLearnedMappingsAsync(_userContext.UserId, cancellationToken);
        return Ok(mappings.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Learns a merchant-to-category mapping from a manual categorization.
    /// </summary>
    /// <param name="request">The learn request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("learn")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LearnAsync([FromBody] LearnMerchantMappingRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest("Description is required.");
        }

        await _mappingService.LearnFromCategorizationAsync(
            _userContext.UserId,
            request.Description,
            request.CategoryId,
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Deletes a learned merchant mapping.
    /// </summary>
    /// <param name="id">The mapping ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await _mappingService.DeleteLearnedMappingAsync(_userContext.UserId, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private static LearnedMerchantMappingDto MapToDto(LearnedMerchantMappingInfo mapping)
    {
        return new LearnedMerchantMappingDto
        {
            Id = mapping.Id,
            MerchantPattern = mapping.MerchantPattern,
            CategoryId = mapping.CategoryId,
            CategoryName = mapping.CategoryName,
            LearnCount = mapping.LearnCount,
            CreatedAtUtc = mapping.CreatedAtUtc,
            UpdatedAtUtc = mapping.UpdatedAtUtc,
        };
    }
}
