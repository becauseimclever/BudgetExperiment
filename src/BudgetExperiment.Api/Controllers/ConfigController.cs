// <copyright file="ConfigController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for client configuration.
/// Provides configuration settings for the Blazor WebAssembly client.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class ConfigController : ControllerBase
{
    private readonly IOptions<ClientConfigOptions> _clientConfigOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigController"/> class.
    /// </summary>
    /// <param name="clientConfigOptions">The client configuration options.</param>
    public ConfigController(IOptions<ClientConfigOptions> clientConfigOptions)
    {
        _clientConfigOptions = clientConfigOptions;
    }

    /// <summary>
    /// Gets the client configuration settings.
    /// </summary>
    /// <remarks>
    /// This endpoint is public (no authentication required) because the client
    /// needs configuration before it can authenticate. Only non-secret settings
    /// are exposed.
    /// </remarks>
    /// <returns>The client configuration.</returns>
    [HttpGet]
    [ProducesResponseType<ClientConfigDto>(StatusCodes.Status200OK)]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public ActionResult<ClientConfigDto> GetConfig()
    {
        var options = _clientConfigOptions.Value;
        return Ok(options.ToDto());
    }
}
