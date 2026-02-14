// <copyright file="ExportDownloadService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net.Http.Headers;

using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Downloads export files and triggers client-side download behavior.
/// </summary>
public sealed class ExportDownloadService : IExportDownloadService, IAsyncDisposable
{
    private readonly HttpClient httpClient;
    private readonly IJSRuntime jsRuntime;
    private IJSObjectReference? module;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportDownloadService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="jsRuntime">The JavaScript runtime.</param>
    public ExportDownloadService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        this.httpClient = httpClient;
        this.jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public async Task<ExportDownloadResult> DownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await this.httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var reason = response.ReasonPhrase ?? "Unknown error";
                return ExportDownloadResult.Fail($"Export failed ({(int)response.StatusCode} {reason}).");
            }

            var contentType = response.Content.Headers.ContentType?.ToString() ?? "text/csv";
            var fileName = GetFileName(response) ?? "export.csv";
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            await EnsureModuleAsync(cancellationToken);
            if (this.module == null)
            {
                return ExportDownloadResult.Fail("Download helper unavailable.");
            }

            using var streamRef = new DotNetStreamReference(new MemoryStream(bytes));
            await this.module.InvokeVoidAsync(
                "downloadFileFromStream",
                cancellationToken,
                fileName,
                contentType,
                streamRef);

            return ExportDownloadResult.Ok();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return ExportDownloadResult.Fail("Authentication required to export.");
        }
        catch (OperationCanceledException)
        {
            return ExportDownloadResult.Fail("Export was canceled.");
        }
        catch (JSException)
        {
            return ExportDownloadResult.Fail("Unable to start the download.");
        }
        catch (HttpRequestException ex)
        {
            return ExportDownloadResult.Fail($"Export failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ExportDownloadResult.Fail($"Export failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (this.module != null)
        {
            await this.module.DisposeAsync();
        }
    }

    private async Task EnsureModuleAsync(CancellationToken cancellationToken)
    {
        if (this.module != null)
        {
            return;
        }

        this.module = await this.jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            cancellationToken,
            "./js/file-download.js");
    }

    private static string? GetFileName(HttpResponseMessage response)
    {
        var contentDisposition = response.Content.Headers.ContentDisposition;
        if (contentDisposition != null)
        {
            var name = contentDisposition.FileNameStar ?? contentDisposition.FileName;
            return name?.Trim('"');
        }

        if (response.Content.Headers.TryGetValues("Content-Disposition", out var values))
        {
            if (ContentDispositionHeaderValue.TryParse(values.FirstOrDefault(), out var header))
            {
                var name = header.FileNameStar ?? header.FileName;
                return name?.Trim('"');
            }
        }

        return null;
    }
}
