// <copyright file="AiApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using System.Text.Json;
using BudgetExperiment.Contracts.Dtos;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// HTTP client service for communicating with the AI API endpoints.
/// </summary>
public sealed class AiApiService : IAiApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiApiService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public AiApiService(HttpClient httpClient)
    {
        this._httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<AiStatusDto?> GetStatusAsync()
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<AiStatusDto>("api/v1/ai/status", JsonOptions);
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
    public async Task<IReadOnlyList<AiModelDto>> GetModelsAsync()
    {
        try
        {
            var result = await this._httpClient.GetFromJsonAsync<List<AiModelDto>>("api/v1/ai/models", JsonOptions);
            return result ?? new List<AiModelDto>();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new List<AiModelDto>();
        }
        catch (HttpRequestException)
        {
            return new List<AiModelDto>();
        }
    }

    /// <inheritdoc />
    public async Task<AiSettingsDto?> GetSettingsAsync()
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<AiSettingsDto>("api/v1/ai/settings", JsonOptions);
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
    public async Task<AiSettingsDto?> UpdateSettingsAsync(AiSettingsDto settings)
    {
        try
        {
            var response = await this._httpClient.PutAsJsonAsync("api/v1/ai/settings", settings, JsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AiSettingsDto>(JsonOptions);
            }

            return null;
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
    public async Task<AnalysisResponseDto?> AnalyzeAsync()
    {
        try
        {
            var response = await this._httpClient.PostAsync("api/v1/ai/analyze", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AnalysisResponseDto>(JsonOptions);
            }

            // Try to get error details from the response
            var errorContent = await response.Content.ReadAsStringAsync();
            var statusCode = (int)response.StatusCode;

            // Provide specific messages for known error codes
            var errorMessage = statusCode switch
            {
                504 => "AI analysis timed out. The AI service took too long to respond. Try increasing the timeout in AI Settings.",
                503 => "AI service is unavailable. Please check that Ollama is running and configured correctly.",
                _ => $"AI analysis failed with status {statusCode}. {errorContent}",
            };

            throw new InvalidOperationException(errorMessage);
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to connect to the server: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new InvalidOperationException("The request timed out. The AI service may be overloaded.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuleSuggestionDto>> GenerateSuggestionsAsync(GenerateSuggestionsRequest request)
    {
        try
        {
            var response = await this._httpClient.PostAsJsonAsync("api/v1/ai/suggestions/generate", request, JsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<RuleSuggestionDto>>(JsonOptions);
                return result ?? new List<RuleSuggestionDto>();
            }

            return new List<RuleSuggestionDto>();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new List<RuleSuggestionDto>();
        }
        catch (HttpRequestException)
        {
            return new List<RuleSuggestionDto>();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuleSuggestionDto>> GetPendingSuggestionsAsync(string? type = null)
    {
        try
        {
            var url = string.IsNullOrEmpty(type)
                ? "api/v1/ai/suggestions"
                : $"api/v1/ai/suggestions?type={type}";

            var result = await this._httpClient.GetFromJsonAsync<List<RuleSuggestionDto>>(url, JsonOptions);
            return result ?? new List<RuleSuggestionDto>();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new List<RuleSuggestionDto>();
        }
        catch (HttpRequestException)
        {
            return new List<RuleSuggestionDto>();
        }
    }

    /// <inheritdoc />
    public async Task<RuleSuggestionDto?> GetSuggestionAsync(Guid id)
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<RuleSuggestionDto>($"api/v1/ai/suggestions/{id}", JsonOptions);
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
    public async Task<CategorizationRuleDto?> AcceptSuggestionAsync(Guid id)
    {
        try
        {
            var response = await this._httpClient.PostAsync($"api/v1/ai/suggestions/{id}/accept", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CategorizationRuleDto>(JsonOptions);
            }

            return null;
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
    public async Task<bool> DismissSuggestionAsync(Guid id, string? reason = null)
    {
        try
        {
            var request = new DismissSuggestionRequest { Reason = reason };
            var response = await this._httpClient.PostAsJsonAsync($"api/v1/ai/suggestions/{id}/dismiss", request, JsonOptions);
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
    public async Task<bool> ProvideFeedbackAsync(Guid id, bool isPositive)
    {
        try
        {
            var request = new FeedbackRequest { IsPositive = isPositive };
            var response = await this._httpClient.PostAsJsonAsync($"api/v1/ai/suggestions/{id}/feedback", request, JsonOptions);
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
}
