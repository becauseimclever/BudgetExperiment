// <copyright file="ImportController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Api.Models;
using BudgetExperiment.Application.Import;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for CSV import operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public sealed class ImportController : ControllerBase
{
    private readonly IImportMappingService _mappingService;
    private readonly IImportService _importService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportController"/> class.
    /// </summary>
    /// <param name="mappingService">The import mapping service.</param>
    /// <param name="importService">The import service.</param>
    public ImportController(
        IImportMappingService mappingService,
        IImportService importService)
    {
        _mappingService = mappingService;
        _importService = importService;
    }

    /// <summary>
    /// Gets all saved import mappings for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of saved mappings.</returns>
    [HttpGet("mappings")]
    [ProducesResponseType<IReadOnlyList<ImportMappingDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMappingsAsync(CancellationToken cancellationToken)
    {
        var mappings = await _mappingService.GetUserMappingsAsync(cancellationToken);
        return this.Ok(mappings);
    }

    /// <summary>
    /// Gets a specific import mapping by ID.
    /// </summary>
    /// <param name="id">The mapping identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapping if found.</returns>
    [HttpGet("mappings/{id:guid}")]
    [ProducesResponseType<ImportMappingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMappingByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var mapping = await _mappingService.GetMappingAsync(id, cancellationToken);
        if (mapping is null)
        {
            return this.NotFound();
        }

        if (mapping.Version is not null)
        {
            this.Response.Headers.ETag = $"\"{mapping.Version}\"";
        }

        return this.Ok(mapping);
    }

    /// <summary>
    /// Creates a new import mapping.
    /// </summary>
    /// <param name="request">The mapping creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created mapping.</returns>
    [HttpPost("mappings")]
    [ProducesResponseType<ImportMappingDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMappingAsync([FromBody] CreateImportMappingRequest request, CancellationToken cancellationToken)
    {
        var mapping = await _mappingService.CreateMappingAsync(request, cancellationToken);
        return this.CreatedAtAction("GetMappingById", new { id = mapping.Id }, mapping);
    }

    /// <summary>
    /// Updates an existing import mapping.
    /// </summary>
    /// <param name="id">The mapping identifier.</param>
    /// <param name="request">The mapping update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated mapping.</returns>
    [HttpPut("mappings/{id:guid}")]
    [ProducesResponseType<ImportMappingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMappingAsync(Guid id, [FromBody] UpdateImportMappingRequest request, CancellationToken cancellationToken)
    {
        string? expectedVersion = null;
        if (this.Request.Headers.TryGetValue("If-Match", out var ifMatch))
        {
            expectedVersion = ifMatch.ToString().Trim('"');
        }

        var mapping = await _mappingService.UpdateMappingAsync(id, request, expectedVersion, cancellationToken);
        if (mapping is null)
        {
            return this.NotFound();
        }

        if (mapping.Version is not null)
        {
            this.Response.Headers.ETag = $"\"{mapping.Version}\"";
        }

        return this.Ok(mapping);
    }

    /// <summary>
    /// Deletes an import mapping.
    /// </summary>
    /// <param name="id">The mapping identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("mappings/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMappingAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _mappingService.DeleteMappingAsync(id, cancellationToken);
        if (!deleted)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }

    /// <summary>
    /// Suggests an existing mapping based on CSV headers.
    /// </summary>
    /// <param name="request">The headers to match against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A suggested mapping if found.</returns>
    [HttpPost("mappings/suggest")]
    [ProducesResponseType<ImportMappingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SuggestMappingAsync([FromBody] SuggestMappingRequest request, CancellationToken cancellationToken)
    {
        var mapping = await _mappingService.SuggestMappingAsync(request.Headers, cancellationToken);
        if (mapping is null)
        {
            return this.NoContent();
        }

        return this.Ok(mapping);
    }

    /// <summary>
    /// Previews an import with validation and categorization.
    /// Accepts pre-parsed CSV rows (parsed client-side in Blazor WASM).
    /// Rejects requests exceeding 10,000 rows (400) or 10 MB body size (413).
    /// </summary>
    /// <param name="request">The preview request with rows and mappings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The preview result with validation status.</returns>
    [HttpPost("preview")]
    [RequestSizeLimit(ImportValidationConstants.PreviewRequestSizeLimitBytes)]
    [ProducesResponseType<ImportPreviewResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreviewAsync([FromBody] ImportPreviewRequest request, CancellationToken cancellationToken)
    {
        if (request.Rows.Count > ImportValidationConstants.MaxPreviewRows)
        {
            return this.StatusCode(
                StatusCodes.Status400BadRequest,
                new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Bad Request",
                    Detail = $"Preview exceeds maximum of {ImportValidationConstants.MaxPreviewRows} rows.",
                    Extensions = { ["traceId"] = this.HttpContext.TraceIdentifier },
                });
        }

        var result = await _importService.PreviewAsync(request, cancellationToken);
        return this.Ok(result);
    }

    /// <summary>
    /// Executes an import, creating transactions.
    /// Validates transaction count (max 5,000), field lengths, date range, and amount range.
    /// Rejects invalid requests with 400 (structural) or 422 (field-level) errors.
    /// </summary>
    /// <param name="request">The execution request with validated transactions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The import result with counts and created IDs.</returns>
    [HttpPost("execute")]
    [RequestSizeLimit(ImportValidationConstants.ExecuteRequestSizeLimitBytes)]
    [ProducesResponseType<ImportResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ExecuteAsync([FromBody] ImportExecuteRequest request, CancellationToken cancellationToken)
    {
        var validationResult = ImportExecuteRequestValidator.Validate(request);
        if (!validationResult.IsValid)
        {
            var statusCode = validationResult.IsBadRequest
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status422UnprocessableEntity;

            return this.StatusCode(
                statusCode,
                new ProblemDetails
                {
                    Status = statusCode,
                    Title = validationResult.IsBadRequest ? "Bad Request" : "Unprocessable Entity",
                    Detail = string.Join("; ", validationResult.Errors),
                    Extensions = { ["traceId"] = this.HttpContext.TraceIdentifier },
                });
        }

        var result = await _importService.ExecuteAsync(request, cancellationToken);
        return this.CreatedAtAction("GetBatchById", new { id = result.BatchId }, result);
    }

    /// <summary>
    /// Gets import history for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of import batches.</returns>
    [HttpGet("history")]
    [ProducesResponseType<IReadOnlyList<ImportBatchDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistoryAsync(CancellationToken cancellationToken)
    {
        var batches = await _importService.GetImportHistoryAsync(cancellationToken);
        return this.Ok(batches);
    }

    /// <summary>
    /// Gets a specific import batch by ID.
    /// </summary>
    /// <param name="id">The batch identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The batch if found.</returns>
    [HttpGet("batches/{id:guid}")]
    [ProducesResponseType<ImportBatchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var batches = await _importService.GetImportHistoryAsync(cancellationToken);
        var batch = batches.FirstOrDefault(b => b.Id == id);
        if (batch is null)
        {
            return this.NotFound();
        }

        return this.Ok(batch);
    }

    /// <summary>
    /// Deletes all transactions from an import batch (undo import).
    /// </summary>
    /// <param name="id">The batch identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of deleted transactions.</returns>
    [HttpDelete("batches/{id:guid}")]
    [ProducesResponseType<DeleteBatchResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBatchAsync(Guid id, CancellationToken cancellationToken)
    {
        var count = await _importService.DeleteImportBatchAsync(id, cancellationToken);
        if (count == 0)
        {
            return this.NotFound();
        }

        return this.Ok(new DeleteBatchResult { DeletedCount = count });
    }
}
