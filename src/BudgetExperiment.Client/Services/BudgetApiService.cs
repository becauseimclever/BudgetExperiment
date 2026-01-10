// <copyright file="BudgetApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using System.Text.Json;

using BudgetExperiment.Client.Models;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// HTTP client service for communicating with the Budget API.
/// </summary>
public sealed class BudgetApiService : IBudgetApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetApiService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    public BudgetApiService(HttpClient httpClient)
    {
        this._httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccountModel>> GetAccountsAsync()
    {
        var result = await this._httpClient.GetFromJsonAsync<List<AccountModel>>("api/v1/accounts", JsonOptions);
        return result ?? new List<AccountModel>();
    }

    /// <inheritdoc />
    public async Task<AccountModel?> GetAccountAsync(Guid id)
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<AccountModel>($"api/v1/accounts/{id}", JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<AccountModel?> CreateAccountAsync(AccountCreateModel model)
    {
        var response = await this._httpClient.PostAsJsonAsync("api/v1/accounts", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AccountModel>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAccountAsync(Guid id)
    {
        var response = await this._httpClient.DeleteAsync($"api/v1/accounts/{id}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransactionModel>> GetTransactionsAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null)
    {
        var url = $"api/v1/transactions?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
        if (accountId.HasValue)
        {
            url += $"&accountId={accountId.Value}";
        }

        var result = await this._httpClient.GetFromJsonAsync<List<TransactionModel>>(url, JsonOptions);
        return result ?? new List<TransactionModel>();
    }

    /// <inheritdoc />
    public async Task<TransactionModel?> GetTransactionAsync(Guid id)
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<TransactionModel>($"api/v1/transactions/{id}", JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<TransactionModel?> CreateTransactionAsync(TransactionCreateModel model)
    {
        // Convert client model to API format
        var apiModel = new
        {
            model.AccountId,
            Amount = new { Currency = model.Currency, Amount = model.Amount },
            model.Date,
            model.Description,
            model.Category,
        };

        var response = await this._httpClient.PostAsJsonAsync("api/v1/transactions", apiModel, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TransactionModel>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DailyTotalModel>> GetCalendarSummaryAsync(int year, int month, Guid? accountId = null)
    {
        var url = $"api/v1/calendar/summary?year={year}&month={month}";
        if (accountId.HasValue)
        {
            url += $"&accountId={accountId.Value}";
        }

        var result = await this._httpClient.GetFromJsonAsync<List<DailyTotalModel>>(url, JsonOptions);
        return result ?? new List<DailyTotalModel>();
    }
}
