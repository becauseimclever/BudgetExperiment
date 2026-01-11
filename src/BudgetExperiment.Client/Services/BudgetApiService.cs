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

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransactionModel>> GetRecurringTransactionsAsync()
    {
        var result = await this._httpClient.GetFromJsonAsync<List<RecurringTransactionModel>>("api/v1/recurring-transactions", JsonOptions);
        return result ?? new List<RecurringTransactionModel>();
    }

    /// <inheritdoc />
    public async Task<RecurringTransactionModel?> GetRecurringTransactionAsync(Guid id)
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<RecurringTransactionModel>($"api/v1/recurring-transactions/{id}", JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<RecurringTransactionModel?> CreateRecurringTransactionAsync(RecurringTransactionCreateModel model)
    {
        var apiModel = new
        {
            model.AccountId,
            model.Description,
            Amount = new { Currency = model.Currency, Amount = model.Amount },
            model.Frequency,
            model.DayOfMonth,
            model.DayOfWeek,
            model.StartDate,
            model.EndDate,
            model.Category,
        };

        var response = await this._httpClient.PostAsJsonAsync("api/v1/recurring-transactions", apiModel, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransactionModel>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<RecurringTransactionModel?> UpdateRecurringTransactionAsync(Guid id, RecurringTransactionUpdateModel model)
    {
        object? amountObj = null;
        if (model.Amount.HasValue)
        {
            amountObj = new { Currency = model.Currency ?? "USD", Amount = model.Amount.Value };
        }

        var apiModel = new
        {
            model.Description,
            Amount = amountObj,
            model.EndDate,
            model.Category,
        };

        var response = await this._httpClient.PutAsJsonAsync($"api/v1/recurring-transactions/{id}", apiModel, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransactionModel>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteRecurringTransactionAsync(Guid id)
    {
        var response = await this._httpClient.DeleteAsync($"api/v1/recurring-transactions/{id}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<RecurringTransactionModel?> PauseRecurringTransactionAsync(Guid id)
    {
        var response = await this._httpClient.PostAsync($"api/v1/recurring-transactions/{id}/pause", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransactionModel>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<RecurringTransactionModel?> ResumeRecurringTransactionAsync(Guid id)
    {
        var response = await this._httpClient.PostAsync($"api/v1/recurring-transactions/{id}/resume", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransactionModel>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<RecurringTransactionModel?> SkipNextRecurringAsync(Guid id)
    {
        var response = await this._httpClient.PostAsync($"api/v1/recurring-transactions/{id}/skip", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransactionModel>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringInstanceModel>> GetProjectedRecurringAsync(DateOnly from, DateOnly to, Guid? accountId = null)
    {
        var url = $"api/v1/recurring-transactions/projected?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        if (accountId.HasValue)
        {
            url += $"&accountId={accountId.Value}";
        }

        var result = await this._httpClient.GetFromJsonAsync<List<RecurringInstanceModel>>(url, JsonOptions);
        return result ?? new List<RecurringInstanceModel>();
    }

    /// <inheritdoc />
    public async Task<bool> SkipRecurringInstanceAsync(Guid id, DateOnly date)
    {
        var response = await this._httpClient.DeleteAsync($"api/v1/recurring-transactions/{id}/instances/{date:yyyy-MM-dd}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<RecurringInstanceModel?> ModifyRecurringInstanceAsync(Guid id, DateOnly date, RecurringInstanceModifyModel model)
    {
        object? amountObj = null;
        if (model.Amount.HasValue)
        {
            amountObj = new { Currency = model.Currency ?? "USD", Amount = model.Amount.Value };
        }

        var apiModel = new
        {
            model.NewDate,
            Amount = amountObj,
            model.Description,
        };

        var response = await this._httpClient.PutAsJsonAsync($"api/v1/recurring-transactions/{id}/instances/{date:yyyy-MM-dd}", apiModel, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringInstanceModel>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<TransferModel?> CreateTransferAsync(TransferCreateModel model)
    {
        var response = await this._httpClient.PostAsJsonAsync("api/v1/transfers", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TransferModel>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<TransferModel?> GetTransferAsync(Guid transferId)
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<TransferModel>($"api/v1/transfers/{transferId}", JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransferListItemModel>> GetTransfersAsync(
        Guid? accountId = null,
        DateOnly? from = null,
        DateOnly? to = null,
        int page = 1,
        int pageSize = 20)
    {
        var url = $"api/v1/transfers?page={page}&pageSize={pageSize}";
        if (accountId.HasValue)
        {
            url += $"&accountId={accountId.Value}";
        }

        if (from.HasValue)
        {
            url += $"&from={from.Value:yyyy-MM-dd}";
        }

        if (to.HasValue)
        {
            url += $"&to={to.Value:yyyy-MM-dd}";
        }

        var result = await this._httpClient.GetFromJsonAsync<List<TransferListItemModel>>(url, JsonOptions);
        return result ?? new List<TransferListItemModel>();
    }

    /// <inheritdoc />
    public async Task<TransferModel?> UpdateTransferAsync(Guid transferId, TransferUpdateModel model)
    {
        var response = await this._httpClient.PutAsJsonAsync($"api/v1/transfers/{transferId}", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TransferModel>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTransferAsync(Guid transferId)
    {
        var response = await this._httpClient.DeleteAsync($"api/v1/transfers/{transferId}");
        return response.IsSuccessStatusCode;
    }
}
