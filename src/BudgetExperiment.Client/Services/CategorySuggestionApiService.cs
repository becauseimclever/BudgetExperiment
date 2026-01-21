// <copyright file="CategorySuggestionApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using System.Text.Json;
using BudgetExperiment.Contracts.Dtos;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// HTTP client service for category suggestion API operations.
/// </summary>
public sealed class CategorySuggestionApiService : ICategorySuggestionApiService
{
    private const string BaseUrl = "api/v1/category-suggestions";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorySuggestionApiService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public CategorySuggestionApiService(HttpClient httpClient)
    {
        this._httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorySuggestionDto>> AnalyzeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await this._httpClient.PostAsync($"{BaseUrl}/analyze", null, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<CategorySuggestionDto>>(JsonOptions, cancellationToken);
                return result ?? new List<CategorySuggestionDto>();
            }

            return new List<CategorySuggestionDto>();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new List<CategorySuggestionDto>();
        }
        catch (HttpRequestException)
        {
            return new List<CategorySuggestionDto>();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorySuggestionDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await this._httpClient.GetFromJsonAsync<List<CategorySuggestionDto>>(BaseUrl, JsonOptions, cancellationToken);
            return result ?? new List<CategorySuggestionDto>();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new List<CategorySuggestionDto>();
        }
        catch (HttpRequestException)
        {
            return new List<CategorySuggestionDto>();
        }
    }

    /// <inheritdoc />
    public async Task<CategorySuggestionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<CategorySuggestionDto>($"{BaseUrl}/{id}", JsonOptions, cancellationToken);
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
    public async Task<AcceptCategorySuggestionResultDto> AcceptAsync(
        Guid id,
        AcceptCategorySuggestionRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await this._httpClient.PostAsJsonAsync(
                $"{BaseUrl}/{id}/accept",
                request ?? new AcceptCategorySuggestionRequest(),
                JsonOptions,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AcceptCategorySuggestionResultDto>(JsonOptions, cancellationToken);
                return result ?? CreateFailedAcceptResult(id);
            }

            return CreateFailedAcceptResult(id);
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return CreateFailedAcceptResult(id);
        }
        catch (HttpRequestException)
        {
            return CreateFailedAcceptResult(id);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DismissAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await this._httpClient.PostAsync($"{BaseUrl}/{id}/dismiss", null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AcceptCategorySuggestionResultDto>> BulkAcceptAsync(
        IEnumerable<Guid> suggestionIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new BulkAcceptCategorySuggestionsRequest { SuggestionIds = suggestionIds.ToList() };
            var response = await this._httpClient.PostAsJsonAsync($"{BaseUrl}/bulk-accept", request, JsonOptions, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<AcceptCategorySuggestionResultDto>>(JsonOptions, cancellationToken);
                return result ?? new List<AcceptCategorySuggestionResultDto>();
            }

            return new List<AcceptCategorySuggestionResultDto>();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new List<AcceptCategorySuggestionResultDto>();
        }
        catch (HttpRequestException)
        {
            return new List<AcceptCategorySuggestionResultDto>();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SuggestedCategoryRuleDto>> PreviewRulesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await this._httpClient.GetFromJsonAsync<List<SuggestedCategoryRuleDto>>(
                $"{BaseUrl}/{id}/preview-rules",
                JsonOptions,
                cancellationToken);
            return result ?? new List<SuggestedCategoryRuleDto>();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new List<SuggestedCategoryRuleDto>();
        }
        catch (HttpRequestException)
        {
            return new List<SuggestedCategoryRuleDto>();
        }
    }

    /// <inheritdoc />
    public async Task<CreateRulesFromSuggestionResult> CreateRulesAsync(
        Guid id,
        CreateRulesFromSuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await this._httpClient.PostAsJsonAsync(
                $"{BaseUrl}/{id}/create-rules",
                request,
                JsonOptions,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CreateRulesFromSuggestionResult>(JsonOptions, cancellationToken);
                return result ?? CreateFailedRulesResult();
            }

            return CreateFailedRulesResult();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return CreateFailedRulesResult();
        }
        catch (HttpRequestException)
        {
            return CreateFailedRulesResult();
        }
    }

    private static AcceptCategorySuggestionResultDto CreateFailedAcceptResult(Guid suggestionId)
    {
        return new AcceptCategorySuggestionResultDto
        {
            SuggestionId = suggestionId,
            Success = false,
        };
    }

    private static CreateRulesFromSuggestionResult CreateFailedRulesResult()
    {
        return new CreateRulesFromSuggestionResult
        {
            Success = false,
        };
    }
}
