// <copyright file="ImportController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for CSV import operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class ImportController : ControllerBase
{
    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly ICsvParserService _csvParserService;
    private readonly IImportMappingService _mappingService;
    private readonly IImportService _importService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportController"/> class.
    /// </summary>
    /// <param name="csvParserService">The CSV parser service.</param>
    /// <param name="mappingService">The import mapping service.</param>
    /// <param name="importService">The import service.</param>
    public ImportController(
        ICsvParserService csvParserService,
        IImportMappingService mappingService,
        IImportService importService)
    {
        this._csvParserService = csvParserService;
        this._mappingService = mappingService;
        this._importService = importService;
    }

    /// <summary>
    /// Parses an uploaded CSV file and returns headers and rows.
    /// </summary>
    /// <param name="file">The CSV file to parse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed CSV data.</returns>
    [HttpPost("parse")]
    [ProducesResponseType<CsvParseResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> ParseAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return this.BadRequest(new ProblemDetails
            {
                Title = "File Required",
                Detail = "A CSV file is required for parsing.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return this.StatusCode(StatusCodes.Status413PayloadTooLarge, new ProblemDetails
            {
                Title = "File Too Large",
                Detail = $"File size exceeds the maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB.",
                Status = StatusCodes.Status413PayloadTooLarge,
            });
        }

        using var stream = file.OpenReadStream();
        var result = await this._csvParserService.ParseAsync(stream, file.FileName, rowsToSkip: 0, ct: cancellationToken);

        if (!result.Success)
        {
            return this.BadRequest(new ProblemDetails
            {
                Title = "Parse Failed",
                Detail = result.ErrorMessage,
                Status = StatusCodes.Status400BadRequest,
            });
        }

        return this.Ok(new CsvParseResultDto
        {
            Headers = result.Headers,
            Rows = result.Rows,
            DetectedDelimiter = result.DetectedDelimiter.ToString(),
            HasHeaderRow = result.HasHeaderRow,
            RowCount = result.RowCount,
            RowsSkipped = result.RowsSkipped,
        });
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
        var mappings = await this._mappingService.GetUserMappingsAsync(cancellationToken);
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
        var mapping = await this._mappingService.GetMappingAsync(id, cancellationToken);
        if (mapping is null)
        {
            return this.NotFound();
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
        var mapping = await this._mappingService.CreateMappingAsync(request, cancellationToken);
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
    public async Task<IActionResult> UpdateMappingAsync(Guid id, [FromBody] UpdateImportMappingRequest request, CancellationToken cancellationToken)
    {
        var mapping = await this._mappingService.UpdateMappingAsync(id, request, cancellationToken);
        if (mapping is null)
        {
            return this.NotFound();
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
        var deleted = await this._mappingService.DeleteMappingAsync(id, cancellationToken);
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
        var mapping = await this._mappingService.SuggestMappingAsync(request.Headers, cancellationToken);
        if (mapping is null)
        {
            return this.NoContent();
        }

        return this.Ok(mapping);
    }

    /// <summary>
    /// Previews an import with validation and categorization.
    /// </summary>
    /// <param name="request">The preview request with rows and mappings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The preview result with validation status.</returns>
    [HttpPost("preview")]
    [ProducesResponseType<ImportPreviewResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreviewAsync([FromBody] ImportPreviewRequest request, CancellationToken cancellationToken)
    {
        var result = await this._importService.PreviewAsync(request, cancellationToken);
        return this.Ok(result);
    }

    /// <summary>
    /// Executes an import, creating transactions.
    /// </summary>
    /// <param name="request">The execution request with validated transactions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The import result with counts and created IDs.</returns>
    [HttpPost("execute")]
    [ProducesResponseType<ImportResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteAsync([FromBody] ImportExecuteRequest request, CancellationToken cancellationToken)
    {
        var result = await this._importService.ExecuteAsync(request, cancellationToken);
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
        var batches = await this._importService.GetImportHistoryAsync(cancellationToken);
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
        var batches = await this._importService.GetImportHistoryAsync(cancellationToken);
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
        var count = await this._importService.DeleteImportBatchAsync(id, cancellationToken);
        if (count == 0)
        {
            return this.NotFound();
        }

        return this.Ok(new DeleteBatchResult { DeletedCount = count });
    }
}

/// <summary>
/// DTO for CSV parse result returned by API.
/// </summary>
public sealed record CsvParseResultDto
{
    /// <summary>
    /// Gets the column headers.
    /// </summary>
    public IReadOnlyList<string> Headers { get; init; } = [];

    /// <summary>
    /// Gets the data rows.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];

    /// <summary>
    /// Gets the detected delimiter as a string.
    /// </summary>
    public string DetectedDelimiter { get; init; } = ",";

    /// <summary>
    /// Gets a value indicating whether a header row was detected.
    /// </summary>
    public bool HasHeaderRow { get; init; }

    /// <summary>
    /// Gets the total row count.
    /// </summary>
    public int RowCount { get; init; }

    /// <summary>
    /// Gets the number of rows that were skipped before the header row.
    /// </summary>
    public int RowsSkipped { get; init; }
}

/// <summary>
/// Request for suggesting a mapping based on headers.
/// </summary>
public sealed record SuggestMappingRequest
{
    /// <summary>
    /// Gets the CSV headers to match against existing mappings.
    /// </summary>
    public IReadOnlyList<string> Headers { get; init; } = [];
}

/// <summary>
/// Result of deleting an import batch.
/// </summary>
public sealed record DeleteBatchResult
{
    /// <summary>
    /// Gets the number of transactions deleted.
    /// </summary>
    public int DeletedCount { get; init; }
}
