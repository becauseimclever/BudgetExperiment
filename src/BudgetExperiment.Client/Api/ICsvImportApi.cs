using BudgetExperiment.Client.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace BudgetExperiment.Client.Api;

/// <summary>
/// Typed client for CSV import endpoints providing preview and commit operations.
/// </summary>
public interface ICsvImportApi
{
    /// <summary>
    /// Uploads a CSV for preview and duplicate detection.
    /// </summary>
    /// <param name="file">The CSV file selected by the user.</param>
    /// <param name="bankType">The bank type identifier string.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Preview rows with duplicate flags, or null on failure.</returns>
    Task<List<CsvImportPreviewRowDto>?> PreviewAsync(IBrowserFile file, string bankType, CancellationToken ct = default);

    /// <summary>
    /// Commits edited/selected transactions to the server.
    /// </summary>
    /// <param name="items">Transactions to commit.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Import result, or null on failure.</returns>
    Task<CsvImportResult?> CommitAsync(IEnumerable<CommitTransactionDto> items, CancellationToken ct = default);
}
