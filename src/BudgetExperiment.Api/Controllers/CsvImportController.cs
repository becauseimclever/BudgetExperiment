// <copyright file="CsvImportController.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using BudgetExperiment.Application.CsvImport;
using BudgetExperiment.Application.CsvImport.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// CSV import API for bank transactions.
/// </summary>
[ApiController]
[Route("api/v1/csv-import")]
public sealed class CsvImportController : ControllerBase
{
    private readonly ICsvImportService _importService;
    private const long _maxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvImportController"/> class.
    /// </summary>
    /// <param name="importService">CSV import service.</param>
    public CsvImportController(ICsvImportService importService)
    {
        this._importService = importService ?? throw new ArgumentNullException(nameof(importService));
    }

    /// <summary>
    /// Import bank transactions from CSV file.
    /// </summary>
    /// <remarks>
    /// Supported banks:
    /// - Bank of America
    /// - Capital One (Phase 2)
    /// - United Heritage Credit Union (Phase 3)
    /// 
    /// CSV format varies by bank. Ensure the bankType parameter matches your CSV source.
    /// 
    /// Maximum file size: 5MB
    /// Allowed file extensions: .csv
    /// </remarks>
    /// <param name="file">CSV file to import.</param>
    /// <param name="bankType">Bank that generated the CSV (BankOfAmerica, CapitalOne, UnitedHeritageCreditUnion).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Import result with success/failure counts and error details.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CsvImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ImportCsv(
        [FromForm] IFormFile file,
        [FromForm] string bankType,
        CancellationToken ct = default)
    {
        // Validate file presence
        if (file == null || file.Length == 0)
        {
            return this.BadRequest(new { error = "File is required and cannot be empty." });
        }

        // Validate file size
        if (file.Length > _maxFileSizeBytes)
        {
            return this.StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = $"File size exceeds maximum allowed size of {_maxFileSizeBytes / 1024 / 1024}MB." });
        }

        // Validate file extension
        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return this.BadRequest(new { error = "Only .csv files are allowed." });
        }

        // Validate and parse bank type
        if (!Enum.TryParse<BankType>(bankType, ignoreCase: true, out var parsedBankType))
        {
            return this.BadRequest(new { error = $"Invalid bank type. Allowed values: {string.Join(", ", Enum.GetNames<BankType>())}" });
        }

        try
        {
            // Import transactions
            await using var stream = file.OpenReadStream();
            var result = await this._importService.ImportAsync(stream, parsedBankType, ct).ConfigureAwait(false);

            return this.Ok(result);
        }
        catch (ArgumentException ex)
        {
            // Unsupported bank type (no parser registered)
            return this.BadRequest(new { error = ex.Message });
        }
        catch (Domain.DomainException ex)
        {
            // CSV parsing or validation error
            return this.StatusCode(StatusCodes.Status422UnprocessableEntity, new { error = ex.Message, traceId = this.HttpContext.TraceIdentifier });
        }
    }
}
