// <copyright file="RecurringChargeSuggestionApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using System.Text.Json;

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// HTTP client service for recurring charge suggestion API operations.
/// </summary>
public sealed class RecurringChargeSuggestionApiService : IRecurringChargeSuggestionApiService
{
    private const string BaseUrl = "api/v1/recurring-charge-suggestions";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringChargeSuggestionApiService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public RecurringChargeSuggestionApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<int> DetectAsync(Guid? accountId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new DetectRecurringChargesRequest { AccountId = accountId };
            var response = await _httpClient.PostAsJsonAsync(
                $"{BaseUrl}/detect",
                request,
                JsonOptions,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<int>(JsonOptions, cancellationToken);
            }

            return 0;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return 0;
        }
        catch (HttpRequestException)
        {
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringChargeSuggestionDto>> GetSuggestionsAsync(
        Guid? accountId = null,
        string? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{BaseUrl}?skip={skip}&take={take}";
            if (accountId.HasValue)
            {
                url += $"&accountId={accountId.Value}";
            }

            if (!string.IsNullOrEmpty(status))
            {
                url += $"&status={status}";
            }

            var result = await _httpClient.GetFromJsonAsync<List<RecurringChargeSuggestionDto>>(
                url,
                JsonOptions,
                cancellationToken);
            return result ?? [];
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<RecurringChargeSuggestionDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<RecurringChargeSuggestionDto>(
                $"{BaseUrl}/{id}",
                JsonOptions,
                cancellationToken);
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
    public async Task<AcceptRecurringChargeSuggestionResultDto?> AcceptAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/{id}/accept",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AcceptRecurringChargeSuggestionResultDto>(
                    JsonOptions,
                    cancellationToken);
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
    public async Task<bool> DismissAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/{id}/dismiss",
                null,
                cancellationToken);
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
