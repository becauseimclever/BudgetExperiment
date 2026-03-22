// <copyright file="ImportApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using BudgetExperiment.Client.Models;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// HTTP client service for communicating with the Import API.
/// </summary>
public sealed class ImportApiService : IImportApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportApiService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public ImportApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImportMappingDto>> GetMappingsAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<ImportMappingDto>>("api/v1/import/mappings", JsonOptions);
            return result ?? [];
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<ImportMappingDto?> GetMappingAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ImportMappingDto>($"api/v1/import/mappings/{id}", JsonOptions);
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ImportMappingDto?> CreateMappingAsync(CreateImportMappingRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/import/mappings", request, JsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ImportMappingDto>(JsonOptions);
            }

            return null;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ApiResult<ImportMappingDto>> UpdateMappingAsync(Guid id, UpdateImportMappingRequest request, string? version = null)
    {
        return await this.SendUpdateAsync<ImportMappingDto>(HttpMethod.Put, $"api/v1/import/mappings/{id}", request, version);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteMappingAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/import/mappings/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ImportMappingDto?> SuggestMappingAsync(IReadOnlyList<string> headers)
    {
        try
        {
            var request = new { Headers = headers };
            var response = await _httpClient.PostAsJsonAsync("api/v1/import/mappings/suggest", request, JsonOptions);
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ImportMappingDto>(JsonOptions);
            }

            return null;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ImportPreviewResult?> PreviewAsync(ImportPreviewRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/import/preview", request, JsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ImportPreviewResult>(JsonOptions);
            }

            return null;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ImportResult?> ExecuteAsync(ImportExecuteRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/import/execute", request, JsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ImportResult>(JsonOptions);
            }

            return null;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ImportBatchDto>> GetHistoryAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<ImportBatchDto>>("api/v1/import/history", JsonOptions);
            return result ?? [];
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<ImportBatchDto?> GetBatchAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ImportBatchDto>($"api/v1/import/batches/{id}", JsonOptions);
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<int?> DeleteBatchAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/import/batches/{id}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DeleteBatchResultModel>(JsonOptions);
                return result?.DeletedCount;
            }

            return null;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
    }

    private async Task<ApiResult<T>> SendUpdateAsync<T>(HttpMethod method, string url, object body, string? version)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url)
            {
                Content = JsonContent.Create(body, body.GetType(), options: JsonOptions),
            };

            if (!string.IsNullOrEmpty(version))
            {
                request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{version}\""));
            }

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return ApiResult<T>.Conflict();
            }

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                return data is not null ? ApiResult<T>.Success(data) : ApiResult<T>.Failure();
            }

            return ApiResult<T>.Failure();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return ApiResult<T>.Failure();
        }
    }
}
