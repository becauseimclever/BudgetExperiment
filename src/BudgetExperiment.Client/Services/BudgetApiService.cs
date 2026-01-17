// <copyright file="BudgetApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using BudgetExperiment.Client.Models;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

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
    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync()
    {
        try
        {
            var result = await this._httpClient.GetFromJsonAsync<List<AccountDto>>("api/v1/accounts", JsonOptions);
            return result ?? new List<AccountDto>();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new List<AccountDto>();
        }
    }

    /// <inheritdoc />
    public async Task<AccountDto?> GetAccountAsync(Guid id)
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<AccountDto>($"api/v1/accounts/{id}", JsonOptions);
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
    public async Task<AccountDto?> CreateAccountAsync(AccountCreateDto model)
    {
        try
        {
            var response = await this._httpClient.PostAsJsonAsync("api/v1/accounts", model, JsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AccountDto>(JsonOptions);
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
    public async Task<AccountDto?> UpdateAccountAsync(Guid id, AccountUpdateDto model)
    {
        try
        {
            var response = await this._httpClient.PutAsJsonAsync($"api/v1/accounts/{id}", model, JsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AccountDto>(JsonOptions);
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
    public async Task<bool> DeleteAccountAsync(Guid id)
    {
        try
        {
            var response = await this._httpClient.DeleteAsync($"api/v1/accounts/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null)
    {
        try
        {
            var url = $"api/v1/transactions?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
            if (accountId.HasValue)
            {
                url += $"&accountId={accountId.Value}";
            }

            var result = await this._httpClient.GetFromJsonAsync<List<TransactionDto>>(url, JsonOptions);
            return result ?? new List<TransactionDto>();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new List<TransactionDto>();
        }
    }

    /// <inheritdoc />
    public async Task<TransactionDto?> GetTransactionAsync(Guid id)
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<TransactionDto>($"api/v1/transactions/{id}", JsonOptions);
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
    public async Task<TransactionDto?> CreateTransactionAsync(TransactionCreateDto model)
    {
        try
        {
            var response = await this._httpClient.PostAsJsonAsync("api/v1/transactions", model, JsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TransactionDto>(JsonOptions);
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
    public async Task<CalendarGridDto> GetCalendarGridAsync(int year, int month, Guid? accountId = null)
    {
        try
        {
            var url = $"api/v1/calendar/grid?year={year}&month={month}";
            if (accountId.HasValue)
            {
                url += $"&accountId={accountId.Value}";
            }

            var result = await this._httpClient.GetFromJsonAsync<CalendarGridDto>(url, JsonOptions);
            return result ?? new CalendarGridDto { Year = year, Month = month };
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new CalendarGridDto { Year = year, Month = month };
        }
    }

    /// <inheritdoc />
    public async Task<DayDetailDto> GetDayDetailAsync(DateOnly date, Guid? accountId = null)
    {
        try
        {
            var url = $"api/v1/calendar/day/{date:yyyy-MM-dd}";
            if (accountId.HasValue)
            {
                url += $"?accountId={accountId.Value}";
            }

            var result = await this._httpClient.GetFromJsonAsync<DayDetailDto>(url, JsonOptions);
            return result ?? new DayDetailDto { Date = date };
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new DayDetailDto { Date = date };
        }
    }

    /// <inheritdoc />
    public async Task<TransactionListDto> GetAccountTransactionListAsync(
        Guid accountId,
        DateOnly startDate,
        DateOnly endDate,
        bool includeRecurring = true)
    {
        try
        {
            var url = $"api/v1/calendar/accounts/{accountId}/transactions?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&includeRecurring={includeRecurring}";
            var result = await this._httpClient.GetFromJsonAsync<TransactionListDto>(url, JsonOptions);
            return result ?? new TransactionListDto { AccountId = accountId, StartDate = startDate, EndDate = endDate };
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new TransactionListDto { AccountId = accountId, StartDate = startDate, EndDate = endDate };
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DailyTotalDto>> GetCalendarSummaryAsync(int year, int month, Guid? accountId = null)
    {
        try
        {
            var url = $"api/v1/calendar/summary?year={year}&month={month}";
            if (accountId.HasValue)
            {
                url += $"&accountId={accountId.Value}";
            }

            var result = await this._httpClient.GetFromJsonAsync<List<DailyTotalDto>>(url, JsonOptions);
            return result ?? new List<DailyTotalDto>();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new List<DailyTotalDto>();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransactionDto>> GetRecurringTransactionsAsync()
    {
        try
        {
            var result = await this._httpClient.GetFromJsonAsync<List<RecurringTransactionDto>>("api/v1/recurring-transactions", JsonOptions);
            return result ?? new List<RecurringTransactionDto>();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new List<RecurringTransactionDto>();
        }
    }

    /// <inheritdoc />
    public async Task<RecurringTransactionDto?> GetRecurringTransactionAsync(Guid id)
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<RecurringTransactionDto>($"api/v1/recurring-transactions/{id}", JsonOptions);
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
    public async Task<RecurringTransactionDto?> CreateRecurringTransactionAsync(RecurringTransactionCreateDto model)
    {
        var response = await this._httpClient.PostAsJsonAsync("api/v1/recurring-transactions", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransactionDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<RecurringTransactionDto?> UpdateRecurringTransactionAsync(Guid id, RecurringTransactionUpdateDto model)
    {
        var response = await this._httpClient.PutAsJsonAsync($"api/v1/recurring-transactions/{id}", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransactionDto>(JsonOptions);
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
    public async Task<RecurringTransactionDto?> PauseRecurringTransactionAsync(Guid id)
    {
        var response = await this._httpClient.PostAsync($"api/v1/recurring-transactions/{id}/pause", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransactionDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<RecurringTransactionDto?> ResumeRecurringTransactionAsync(Guid id)
    {
        var response = await this._httpClient.PostAsync($"api/v1/recurring-transactions/{id}/resume", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransactionDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<RecurringTransactionDto?> SkipNextRecurringAsync(Guid id)
    {
        var response = await this._httpClient.PostAsync($"api/v1/recurring-transactions/{id}/skip", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransactionDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringInstanceDto>> GetProjectedRecurringAsync(DateOnly from, DateOnly to, Guid? accountId = null)
    {
        var url = $"api/v1/recurring-transactions/projected?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        if (accountId.HasValue)
        {
            url += $"&accountId={accountId.Value}";
        }

        var result = await this._httpClient.GetFromJsonAsync<List<RecurringInstanceDto>>(url, JsonOptions);
        return result ?? new List<RecurringInstanceDto>();
    }

    /// <inheritdoc />
    public async Task<bool> SkipRecurringInstanceAsync(Guid id, DateOnly date)
    {
        var response = await this._httpClient.DeleteAsync($"api/v1/recurring-transactions/{id}/instances/{date:yyyy-MM-dd}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<RecurringInstanceDto?> ModifyRecurringInstanceAsync(Guid id, DateOnly date, RecurringInstanceModifyDto model)
    {
        var response = await this._httpClient.PutAsJsonAsync($"api/v1/recurring-transactions/{id}/instances/{date:yyyy-MM-dd}", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringInstanceDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<TransferResponse?> CreateTransferAsync(CreateTransferRequest model)
    {
        var response = await this._httpClient.PostAsJsonAsync("api/v1/transfers", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TransferResponse>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<TransferResponse?> GetTransferAsync(Guid transferId)
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<TransferResponse>($"api/v1/transfers/{transferId}", JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TransferListItemResponse>> GetTransfersAsync(
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

        var result = await this._httpClient.GetFromJsonAsync<List<TransferListItemResponse>>(url, JsonOptions);
        return result ?? new List<TransferListItemResponse>();
    }

    /// <inheritdoc />
    public async Task<TransferResponse?> UpdateTransferAsync(Guid transferId, UpdateTransferRequest model)
    {
        var response = await this._httpClient.PutAsJsonAsync($"api/v1/transfers/{transferId}", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TransferResponse>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTransferAsync(Guid transferId)
    {
        var response = await this._httpClient.DeleteAsync($"api/v1/transfers/{transferId}");
        return response.IsSuccessStatusCode;
    }

    // Recurring Transfers

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransferDto>> GetRecurringTransfersAsync(Guid? accountId = null)
    {
        var url = "api/v1/recurring-transfers";
        if (accountId.HasValue)
        {
            url += $"?accountId={accountId.Value}";
        }

        var result = await this._httpClient.GetFromJsonAsync<List<RecurringTransferDto>>(url, JsonOptions);
        return result ?? new List<RecurringTransferDto>();
    }

    /// <inheritdoc />
    public async Task<RecurringTransferDto?> GetRecurringTransferAsync(Guid id)
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<RecurringTransferDto>($"api/v1/recurring-transfers/{id}", JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<RecurringTransferDto?> CreateRecurringTransferAsync(RecurringTransferCreateDto model)
    {
        var response = await this._httpClient.PostAsJsonAsync("api/v1/recurring-transfers", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransferDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<RecurringTransferDto?> UpdateRecurringTransferAsync(Guid id, RecurringTransferUpdateDto model)
    {
        var response = await this._httpClient.PutAsJsonAsync($"api/v1/recurring-transfers/{id}", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransferDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteRecurringTransferAsync(Guid id)
    {
        var response = await this._httpClient.DeleteAsync($"api/v1/recurring-transfers/{id}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<RecurringTransferDto?> PauseRecurringTransferAsync(Guid id)
    {
        var response = await this._httpClient.PostAsync($"api/v1/recurring-transfers/{id}/pause", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransferDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<RecurringTransferDto?> ResumeRecurringTransferAsync(Guid id)
    {
        var response = await this._httpClient.PostAsync($"api/v1/recurring-transfers/{id}/resume", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransferDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<RecurringTransferDto?> SkipNextRecurringTransferAsync(Guid id)
    {
        var response = await this._httpClient.PostAsync($"api/v1/recurring-transfers/{id}/skip", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransferDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecurringTransferInstanceDto>> GetProjectedRecurringTransfersAsync(DateOnly from, DateOnly to, Guid? accountId = null)
    {
        var url = $"api/v1/recurring-transfers/projected?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        if (accountId.HasValue)
        {
            url += $"&accountId={accountId.Value}";
        }

        var result = await this._httpClient.GetFromJsonAsync<List<RecurringTransferInstanceDto>>(url, JsonOptions);
        return result ?? new List<RecurringTransferInstanceDto>();
    }

    /// <inheritdoc />
    public async Task<bool> SkipRecurringTransferInstanceAsync(Guid id, DateOnly date)
    {
        var response = await this._httpClient.DeleteAsync($"api/v1/recurring-transfers/{id}/instances/{date:yyyy-MM-dd}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<RecurringTransferInstanceDto?> ModifyRecurringTransferInstanceAsync(Guid id, DateOnly date, RecurringTransferInstanceModifyDto model)
    {
        var response = await this._httpClient.PutAsJsonAsync($"api/v1/recurring-transfers/{id}/instances/{date:yyyy-MM-dd}", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransferInstanceDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<TransactionDto?> RealizeRecurringTransactionAsync(Guid recurringTransactionId, RealizeRecurringTransactionRequest request)
    {
        var response = await this._httpClient.PostAsJsonAsync($"api/v1/recurring-transactions/{recurringTransactionId}/realize", request, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TransactionDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<TransferResponse?> RealizeRecurringTransferAsync(Guid recurringTransferId, RealizeRecurringTransferRequest request)
    {
        var response = await this._httpClient.PostAsJsonAsync($"api/v1/recurring-transfers/{recurringTransferId}/realize", request, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TransferResponse>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<PastDueSummaryDto?> GetPastDueItemsAsync(Guid? accountId = null)
    {
        var url = accountId.HasValue
            ? $"api/v1/recurring/past-due?accountId={accountId}"
            : "api/v1/recurring/past-due";

        return await this._httpClient.GetFromJsonAsync<PastDueSummaryDto>(url, JsonOptions);
    }

    /// <inheritdoc />
    public async Task<BatchRealizeResultDto?> RealizeBatchAsync(BatchRealizeRequest request)
    {
        var response = await this._httpClient.PostAsJsonAsync("api/v1/recurring/realize-batch", request, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BatchRealizeResultDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<AppSettingsDto?> GetSettingsAsync()
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<AppSettingsDto>("api/v1/settings", JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<AppSettingsDto?> UpdateSettingsAsync(AppSettingsUpdateDto dto)
    {
        var response = await this._httpClient.PutAsJsonAsync("api/v1/settings", dto, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AppSettingsDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<PaycheckAllocationSummaryDto?> GetPaycheckAllocationAsync(string frequency, decimal? amount = null, Guid? accountId = null)
    {
        var url = $"api/v1/allocations/paycheck?frequency={Uri.EscapeDataString(frequency)}";
        if (amount.HasValue)
        {
            url += $"&amount={amount.Value}";
        }

        if (accountId.HasValue)
        {
            url += $"&accountId={accountId.Value}";
        }

        try
        {
            return await this._httpClient.GetFromJsonAsync<PaycheckAllocationSummaryDto>(url, JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    // Budget Category Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetCategoryDto>> GetCategoriesAsync(bool activeOnly = false)
    {
        var url = activeOnly ? "api/v1/categories?activeOnly=true" : "api/v1/categories";
        var result = await this._httpClient.GetFromJsonAsync<List<BudgetCategoryDto>>(url, JsonOptions);
        return result ?? new List<BudgetCategoryDto>();
    }

    /// <inheritdoc />
    public async Task<BudgetCategoryDto?> GetCategoryAsync(Guid id)
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<BudgetCategoryDto>($"api/v1/categories/{id}", JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<BudgetCategoryDto?> CreateCategoryAsync(BudgetCategoryCreateDto model)
    {
        var response = await this._httpClient.PostAsJsonAsync("api/v1/categories", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BudgetCategoryDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<BudgetCategoryDto?> UpdateCategoryAsync(Guid id, BudgetCategoryUpdateDto model)
    {
        var response = await this._httpClient.PutAsJsonAsync($"api/v1/categories/{id}", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BudgetCategoryDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        var response = await this._httpClient.DeleteAsync($"api/v1/categories/{id}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateCategoryAsync(Guid id)
    {
        var response = await this._httpClient.PostAsync($"api/v1/categories/{id}/activate", null);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateCategoryAsync(Guid id)
    {
        var response = await this._httpClient.PostAsync($"api/v1/categories/{id}/deactivate", null);
        return response.IsSuccessStatusCode;
    }

    // Budget Goal Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetGoalDto>> GetBudgetGoalsAsync(int year, int month)
    {
        var result = await this._httpClient.GetFromJsonAsync<List<BudgetGoalDto>>(
            $"api/v1/budgets?year={year}&month={month}",
            JsonOptions);
        return result ?? new List<BudgetGoalDto>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetGoalDto>> GetBudgetGoalsByCategoryAsync(Guid categoryId)
    {
        var result = await this._httpClient.GetFromJsonAsync<List<BudgetGoalDto>>(
            $"api/v1/budgets/category/{categoryId}",
            JsonOptions);
        return result ?? new List<BudgetGoalDto>();
    }

    /// <inheritdoc />
    public async Task<BudgetGoalDto?> SetBudgetGoalAsync(Guid categoryId, BudgetGoalSetDto model)
    {
        var response = await this._httpClient.PutAsJsonAsync($"api/v1/budgets/{categoryId}", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BudgetGoalDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteBudgetGoalAsync(Guid categoryId, int year, int month)
    {
        var response = await this._httpClient.DeleteAsync($"api/v1/budgets/{categoryId}?year={year}&month={month}");
        return response.IsSuccessStatusCode;
    }

    // Budget Progress Operations

    /// <inheritdoc />
    public async Task<BudgetSummaryDto?> GetBudgetSummaryAsync(int year, int month)
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<BudgetSummaryDto>(
                $"api/v1/budgets/summary?year={year}&month={month}",
                JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<BudgetProgressDto?> GetCategoryProgressAsync(Guid categoryId, int year, int month)
    {
        try
        {
            return await this._httpClient.GetFromJsonAsync<BudgetProgressDto>(
                $"api/v1/budgets/progress/{categoryId}?year={year}&month={month}",
                JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
