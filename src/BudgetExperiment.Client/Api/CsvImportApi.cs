using System.Net.Http.Headers;
using System.Net.Http.Json;
using BudgetExperiment.Client.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace BudgetExperiment.Client.Api;

/// <summary>
/// Concrete implementation of <see cref="ICsvImportApi"/> using <see cref="HttpClient"/>.
/// </summary>
public sealed class CsvImportApi : ICsvImportApi
{
    private readonly HttpClient _http;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvImportApi"/> class.
    /// </summary>
    /// <param name="http">HTTP client configured with API base address.</param>
    public CsvImportApi(HttpClient http)
    {
        _http = http;
    }

    /// <inheritdoc />
    public async Task<List<CsvImportPreviewRowDto>?> PreviewAsync(IBrowserFile file, string bankType, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", file.Name);
        content.Add(new StringContent(bankType), "bankType");

        var response = await _http.PostAsync("api/v1/csv-import/preview", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        return await response.Content.ReadFromJsonAsync<List<CsvImportPreviewRowDto>>(cancellationToken: ct);
    }

    /// <inheritdoc />
    public async Task<CsvImportResult?> CommitAsync(IEnumerable<CommitTransactionDto> items, CancellationToken ct = default)
    {
        var payload = new ImportCommitRequestDto { Items = items.ToList() };
        var response = await _http.PostAsJsonAsync("api/v1/csv-import/commit", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        return await response.Content.ReadFromJsonAsync<CsvImportResult>(cancellationToken: ct);
    }
}
