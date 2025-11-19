// <copyright file="CsvImportController.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using BudgetExperiment.Application.CsvImport;
using BudgetExperiment.Application.CsvImport.Models;
using BudgetExperiment.Domain;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

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

    /// <summary>
    /// Preview import: parse CSV and detect duplicates, no data is persisted.
    /// </summary>
    [HttpPost("preview")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(IReadOnlyList<CsvImportPreviewRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview(
        [FromForm] IFormFile file,
        [FromForm] string bankType,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
        {
            return this.BadRequest(new { error = "File is required and cannot be empty." });
        }
        if (!Enum.TryParse<BankType>(bankType, ignoreCase: true, out var parsedBankType))
        {
            return this.BadRequest(new { error = $"Invalid bank type. Allowed values: {string.Join(", ", Enum.GetNames<BankType>())}" });
        }

        await using var stream = file.OpenReadStream();
        var rows = await this._importService.PreviewAsync(stream, parsedBankType, ct).ConfigureAwait(false);
        return this.Ok(rows);
    }

    /// <summary>
    /// Commit a set of transactions (optionally edited). Rows marked ForceImport=true are created even if duplicates.
    /// </summary>
    [HttpPost("commit")]
    [ProducesResponseType(typeof(CsvImportResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Commit([FromBody, Required] ImportCommitRequest request, CancellationToken ct = default)
    {
        if (request is null || request.Items is null)
        {
            return this.BadRequest(new { error = "Commit request items are required." });
        }

        var items = request.Items.Select(i => new CommitTransaction(
            i.RowNumber,
            i.Date,
            i.Description,
            i.Amount,
            i.TransactionType,
            i.Category,
            i.ForceImport));

        var result = await this._importService.CommitAsync(items, ct).ConfigureAwait(false);
        return this.Ok(result);
    }

    /// <summary>
    /// Request DTO for import commit.
    /// </summary>
    public sealed class ImportCommitRequest
    {
        /// <summary>
        /// Gets or sets the collection of items to commit.
        /// </summary>
        public required List<ImportCommitItem> Items { get; set; }
    }

    /// <summary>
    /// Commit item DTO.
    /// </summary>
    public sealed class ImportCommitItem
    {
        /// <summary>
        /// Gets or sets the CSV row number (2-based including header row).
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// Gets or sets the transaction date.
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// Gets or sets the transaction description.
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Gets or sets the transaction amount.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets the transaction type (Income or Expense).
        /// </summary>
        public TransactionType TransactionType { get; set; }

        /// <summary>
        /// Gets or sets the optional transaction category.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to import even if a duplicate is detected.
        /// </summary>
        public bool ForceImport { get; set; }
    }
}
