// <copyright file="DebugLogController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using BudgetExperiment.Api.Observability;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for debug log exports. Returns sanitized debug bundles
/// containing recent log entries with all PII redacted.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/debug/logs")]
public sealed class DebugLogController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IDebugLogBuffer? _buffer;
    private readonly ILogSanitizer _sanitizer;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugLogController"/> class.
    /// </summary>
    /// <param name="sanitizer">The PII log sanitizer.</param>
    /// <param name="environment">The web host environment.</param>
    /// <param name="buffer">The debug log buffer (null when feature is disabled).</param>
    public DebugLogController(
        ILogSanitizer sanitizer,
        IWebHostEnvironment environment,
        IDebugLogBuffer? buffer = null)
    {
        _sanitizer = sanitizer;
        _environment = environment;
        _buffer = buffer;
    }

    /// <summary>
    /// Gets sanit&amp;ized debug log bundle for the specified trace ID.
    /// </summary>
    /// <param name="traceId">The trace ID to retrieve logs for.</param>
    /// <returns>Sanitized debug log bundle as downloadable JSON.</returns>
    [HttpGet("{traceId}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetDebugLog(string traceId)
    {
        if (_buffer == null || !_buffer.IsEnabled)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
            {
                Title = "Debug Log Export Not Available",
                Detail = "The debug log export feature is not enabled. Ensure structured logging (Feature 109) is active.",
                Status = StatusCodes.Status501NotImplemented,
            });
        }

        var entries = _buffer.GetByTraceId(traceId);
        if (entries.Count == 0)
        {
            return NotFound(new ProblemDetails
            {
                Title = "No Debug Logs Found",
                Detail = $"No log entries found for trace ID. The entries may have expired from the buffer.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        var environmentContext = BuildEnvironmentContext();
        var bundle = _sanitizer.Sanitize(entries, traceId, environmentContext);

        var json = JsonSerializer.SerializeToUtf8Bytes(bundle, JsonOptions);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var filename = $"budget-debug-{traceId}-{timestamp}.json";

        return File(json, "application/json", filename);
    }

    private EnvironmentContext BuildEnvironmentContext()
    {
        var appVersion = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "unknown";

        return new EnvironmentContext
        {
            AppVersion = appVersion,
            DotnetVersion = RuntimeInformation.FrameworkDescription,
            OsDescription = RuntimeInformation.OSDescription,
            EnvironmentName = _environment.EnvironmentName,
            MachineName = Environment.MachineName,
        };
    }
}
