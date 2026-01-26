// <copyright file="ReconciliationApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using System.Text.Json;

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// HTTP client service for communicating with the Reconciliation API.
/// </summary>
public sealed class ReconciliationApiService : IReconciliationApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationApiService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public ReconciliationApiService(HttpClient httpClient)
    {
        this._httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<ReconciliationStatusDto?> GetStatusAsync(int year, int month, Guid? accountId = null)
    {
        try
        {
            var url = $"api/v1/reconciliation/status?year={year}&month={month}";
            if (accountId.HasValue)
            {
                url += $"&accountId={accountId.Value}";
            }

            return await this._httpClient.GetFromJsonAsync<ReconciliationStatusDto>(url, JsonOptions);
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
    public async Task<IReadOnlyList<ReconciliationMatchDto>> GetPendingMatchesAsync(Guid? accountId = null)
    {
        try
        {
            var url = "api/v1/reconciliation/pending";
            if (accountId.HasValue)
            {
                url += $"?accountId={accountId.Value}";
            }

            var result = await this._httpClient.GetFromJsonAsync<List<ReconciliationMatchDto>>(url, JsonOptions);
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
    public async Task<FindMatchesResult?> FindMatchesAsync(FindMatchesRequest request)
    {
        try
        {
            var response = await this._httpClient.PostAsJsonAsync("api/v1/reconciliation/find-matches", request, JsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<FindMatchesResult>(JsonOptions);
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
    public async Task<bool> AcceptMatchAsync(Guid matchId)
    {
        try
        {
            var response = await this._httpClient.PostAsync($"api/v1/reconciliation/{matchId}/accept", null);
            return response.IsSuccessStatusCode;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RejectMatchAsync(Guid matchId)
    {
        try
        {
            var response = await this._httpClient.PostAsync($"api/v1/reconciliation/{matchId}/reject", null);
            return response.IsSuccessStatusCode;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<int> BulkAcceptMatchesAsync(IReadOnlyList<Guid> matchIds)
    {
        try
        {
            var request = new BulkMatchActionRequest { MatchIds = matchIds };
            var response = await this._httpClient.PostAsJsonAsync("api/v1/reconciliation/bulk-accept", request, JsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BulkMatchActionResult>(JsonOptions);
                return result?.AcceptedCount ?? 0;
            }

            return 0;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<ReconciliationMatchDto?> CreateManualMatchAsync(ManualMatchRequest request)
    {
        try
        {
            var response = await this._httpClient.PostAsJsonAsync("api/v1/reconciliation/manual-match", request, JsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ReconciliationMatchDto>(JsonOptions);
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
    public async Task<MatchingTolerancesDto?> GetTolerancesAsync()
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<MatchingTolerancesDto>("api/v1/reconciliation/tolerances", JsonOptions);
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
    public async Task<bool> UpdateTolerancesAsync(MatchingTolerancesDto tolerances)
    {
        try
        {
            var response = await this._httpClient.PutAsJsonAsync("api/v1/reconciliation/tolerances", tolerances, JsonOptions);
            return response.IsSuccessStatusCode;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return false;
        }
    }
}
