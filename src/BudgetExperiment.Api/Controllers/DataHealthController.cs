// <copyright file="DataHealthController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.DataHealth;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>REST API for data health analysis and fix actions (Feature 125a).</summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/datahealth")]
[Produces("application/json")]
public sealed class DataHealthController : ControllerBase
{
    private readonly IDataHealthService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataHealthController"/> class.
    /// </summary>
    /// <param name="service">The data health service.</param>
    public DataHealthController(IDataHealthService service)
    {
        _service = service;
    }

    /// <summary>Gets the full data health report.</summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Data health report.</returns>
    [HttpGet("report")]
    [ProducesResponseType<DataHealthReportDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportAsync([FromQuery] Guid? accountId, CancellationToken ct)
    {
        var report = await _service.AnalyzeAsync(accountId, ct);
        return this.Ok(report);
    }

    /// <summary>Gets duplicate transaction clusters.</summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of duplicate clusters.</returns>
    [HttpGet("duplicates")]
    [ProducesResponseType<IReadOnlyList<DuplicateClusterDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDuplicatesAsync([FromQuery] Guid? accountId, CancellationToken ct)
    {
        var duplicates = await _service.FindDuplicatesAsync(accountId, ct);
        return this.Ok(duplicates);
    }

    /// <summary>Gets amount outlier transactions.</summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of outliers.</returns>
    [HttpGet("outliers")]
    [ProducesResponseType<IReadOnlyList<AmountOutlierDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutliersAsync([FromQuery] Guid? accountId, CancellationToken ct)
    {
        var outliers = await _service.FindOutliersAsync(accountId, ct);
        return this.Ok(outliers);
    }

    /// <summary>Gets date gaps in transaction history.</summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="minGapDays">Minimum gap size to report (default 7).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of date gaps.</returns>
    [HttpGet("date-gaps")]
    [ProducesResponseType<IReadOnlyList<DateGapDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDateGapsAsync([FromQuery] Guid? accountId, [FromQuery] int minGapDays = 7, CancellationToken ct = default)
    {
        var gaps = await _service.FindDateGapsAsync(accountId, minGapDays, ct);
        return this.Ok(gaps);
    }

    /// <summary>Gets summary of uncategorized transactions.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Uncategorized summary.</returns>
    [HttpGet("uncategorized")]
    [ProducesResponseType<UncategorizedSummaryDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUncategorizedAsync(CancellationToken ct)
    {
        var summary = await _service.GetUncategorizedSummaryAsync(ct);
        return this.Ok(summary);
    }

    /// <summary>Merges duplicate transactions into a primary transaction.</summary>
    /// <param name="request">The merge request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content on success; 404 if primary not found.</returns>
    [HttpPost("merge-duplicates")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MergeDuplicatesAsync([FromBody] MergeDuplicatesRequest request, CancellationToken ct)
    {
        await _service.MergeDuplicatesAsync(request.PrimaryTransactionId, request.DuplicateIds, ct);
        return this.NoContent();
    }

    /// <summary>Dismisses a transaction outlier.</summary>
    /// <param name="transactionId">The transaction identifier to dismiss.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("dismiss-outlier/{transactionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DismissOutlierAsync(Guid transactionId, CancellationToken ct)
    {
        await _service.DismissOutlierAsync(transactionId, ct);
        return this.NoContent();
    }
}
