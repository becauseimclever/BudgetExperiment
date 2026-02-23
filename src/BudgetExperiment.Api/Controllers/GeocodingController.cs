// <copyright file="GeocodingController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Location;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for geocoding operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class GeocodingController : ControllerBase
{
    private readonly IGeocodingService _geocodingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeocodingController"/> class.
    /// </summary>
    /// <param name="geocodingService">The geocoding service.</param>
    public GeocodingController(IGeocodingService geocodingService)
    {
        _geocodingService = geocodingService;
    }

    /// <summary>
    /// Performs reverse geocoding to resolve GPS coordinates to an address.
    /// </summary>
    /// <param name="request">The reverse geocode request containing latitude and longitude.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The geocoded address, or 204 if no result found.</returns>
    [HttpPost("reverse")]
    [ProducesResponseType<ReverseGeocodeResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReverseGeocodeAsync(
        [FromBody] ReverseGeocodeRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.Latitude < -90 || request.Latitude > 90)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Invalid latitude",
                Detail = "Latitude must be between -90 and 90.",
                Status = StatusCodes.Status422UnprocessableEntity,
            });
        }

        if (request.Longitude < -180 || request.Longitude > 180)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Invalid longitude",
                Detail = "Longitude must be between -180 and 180.",
                Status = StatusCodes.Status422UnprocessableEntity,
            });
        }

        var result = await _geocodingService.ReverseGeocodeAsync(request.Latitude, request.Longitude, cancellationToken);

        if (result is null)
        {
            return NoContent();
        }

        return Ok(result);
    }
}
