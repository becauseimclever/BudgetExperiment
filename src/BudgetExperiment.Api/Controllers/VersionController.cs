// <copyright file="VersionController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Reflection;

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// Provides version and build information for the API.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class VersionController : ControllerBase
{
    private static readonly DateTime BuildDateUtc = ExtractBuildDate();
    private static readonly string AppVersion = ExtractVersion();
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionController"/> class.
    /// </summary>
    /// <param name="environment">The hosting environment.</param>
    public VersionController(IHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// Gets version and build information for the API.
    /// </summary>
    /// <returns>Version information including semantic version, build date, commit hash, and environment.</returns>
    /// <response code="200">Returns version information.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VersionInfoDto), StatusCodes.Status200OK)]
    public ActionResult<VersionInfoDto> GetVersion()
    {
        var versionInfo = new VersionInfoDto(
            Version: AppVersion,
            BuildDateUtc: BuildDateUtc,
            CommitHash: Environment.GetEnvironmentVariable("GIT_COMMIT"),
            Environment: _environment.EnvironmentName);

        return Ok(versionInfo);
    }

    private static string ExtractVersion()
    {
        var assembly = typeof(Program).Assembly;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        // MinVer adds +commitHash to InformationalVersion; strip it for cleaner display
        if (informationalVersion is not null)
        {
            var plusIndex = informationalVersion.IndexOf('+');
            if (plusIndex > 0)
            {
                return informationalVersion[..plusIndex];
            }

            return informationalVersion;
        }

        // Fallback to FileVersion if InformationalVersion not available
        return assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "unknown";
    }

    private static DateTime ExtractBuildDate()
    {
        // Use linker timestamp from assembly (embedded during build)
        var assembly = typeof(Program).Assembly;

        // Try to get build date from environment variable (set by CI)
        var buildDateEnv = Environment.GetEnvironmentVariable("BUILD_DATE");
        if (DateTime.TryParse(buildDateEnv, out var parsedDate))
        {
            return parsedDate.ToUniversalTime();
        }

        // Fallback: use assembly last write time
        try
        {
            var location = assembly.Location;
            if (!string.IsNullOrEmpty(location))
            {
                return System.IO.File.GetLastWriteTimeUtc(location);
            }
        }
        catch
        {
            // Ignore file access errors
        }

        // Ultimate fallback
        return DateTime.UtcNow;
    }
}
