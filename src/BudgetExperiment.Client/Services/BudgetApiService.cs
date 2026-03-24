// <copyright file="BudgetApiService.cs" company="BecauseImClever">
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
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<ApiResult<AccountDto>> UpdateAccountAsync(Guid id, AccountUpdateDto model, string? version = null)
    {
        return await this.SendUpdateAsync<AccountDto>(HttpMethod.Put, $"api/v1/accounts/{id}", model, version);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<AccountDto>>("api/v1/accounts", JsonOptions);
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
            return await _httpClient.GetFromJsonAsync<AccountDto>($"api/v1/accounts/{id}", JsonOptions);
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
            var response = await _httpClient.PostAsJsonAsync("api/v1/accounts", model, JsonOptions);
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
            var response = await _httpClient.DeleteAsync($"api/v1/accounts/{id}");
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

            var result = await _httpClient.GetFromJsonAsync<List<TransactionDto>>(url, JsonOptions);
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
            return await _httpClient.GetFromJsonAsync<TransactionDto>($"api/v1/transactions/{id}", JsonOptions);
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
            var response = await _httpClient.PostAsJsonAsync("api/v1/transactions", model, JsonOptions);
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
    public async Task<ApiResult<TransactionDto>> UpdateTransactionAsync(Guid id, TransactionUpdateDto model, string? version = null)
    {
        return await this.SendUpdateAsync<TransactionDto>(HttpMethod.Put, $"api/v1/transactions/{id}", model, version);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTransactionAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/transactions/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ApiResult<TransactionDto>> UpdateTransactionLocationAsync(Guid id, TransactionLocationUpdateDto dto, string? version = null)
    {
        return await this.SendUpdateAsync<TransactionDto>(HttpMethod.Patch, $"api/v1/transactions/{id}/location", dto, version);
    }

    /// <inheritdoc />
    public async Task<bool> ClearTransactionLocationAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/transactions/{id}/location");
            return response.IsSuccessStatusCode;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ReverseGeocodeResponseDto?> ReverseGeocodeAsync(decimal latitude, decimal longitude)
    {
        try
        {
            var request = new ReverseGeocodeRequestDto { Latitude = latitude, Longitude = longitude };
            var response = await _httpClient.PostAsJsonAsync("api/v1/geocoding/reverse", request, JsonOptions);

            if (response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                return await response.Content.ReadFromJsonAsync<ReverseGeocodeResponseDto>(JsonOptions);
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

            var result = await _httpClient.GetFromJsonAsync<CalendarGridDto>(url, JsonOptions);
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

            var result = await _httpClient.GetFromJsonAsync<DayDetailDto>(url, JsonOptions);
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
            var result = await _httpClient.GetFromJsonAsync<TransactionListDto>(url, JsonOptions);
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

            var result = await _httpClient.GetFromJsonAsync<List<DailyTotalDto>>(url, JsonOptions);
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
            var result = await _httpClient.GetFromJsonAsync<List<RecurringTransactionDto>>("api/v1/recurring-transactions", JsonOptions);
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
            return await _httpClient.GetFromJsonAsync<RecurringTransactionDto>($"api/v1/recurring-transactions/{id}", JsonOptions);
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
        var response = await _httpClient.PostAsJsonAsync("api/v1/recurring-transactions", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransactionDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<ApiResult<RecurringTransactionDto>> UpdateRecurringTransactionAsync(Guid id, RecurringTransactionUpdateDto model, string? version = null)
    {
        return await this.SendUpdateAsync<RecurringTransactionDto>(HttpMethod.Put, $"api/v1/recurring-transactions/{id}", model, version);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteRecurringTransactionAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/recurring-transactions/{id}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<RecurringTransactionDto?> PauseRecurringTransactionAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"api/v1/recurring-transactions/{id}/pause", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransactionDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<RecurringTransactionDto?> ResumeRecurringTransactionAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"api/v1/recurring-transactions/{id}/resume", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransactionDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<RecurringTransactionDto?> SkipNextRecurringAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"api/v1/recurring-transactions/{id}/skip", null);
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

        var result = await _httpClient.GetFromJsonAsync<List<RecurringInstanceDto>>(url, JsonOptions);
        return result ?? new List<RecurringInstanceDto>();
    }

    /// <inheritdoc />
    public async Task<bool> SkipRecurringInstanceAsync(Guid id, DateOnly date)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/recurring-transactions/{id}/instances/{date:yyyy-MM-dd}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<ApiResult<RecurringInstanceDto>> ModifyRecurringInstanceAsync(Guid id, DateOnly date, RecurringInstanceModifyDto model, string? version = null)
    {
        return await this.SendUpdateAsync<RecurringInstanceDto>(HttpMethod.Put, $"api/v1/recurring-transactions/{id}/instances/{date:yyyy-MM-dd}", model, version);
    }

    /// <inheritdoc />
    public async Task<TransferResponse?> CreateTransferAsync(CreateTransferRequest model)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/transfers", model, JsonOptions);
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
            return await _httpClient.GetFromJsonAsync<TransferResponse>($"api/v1/transfers/{transferId}", JsonOptions);
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

        var result = await _httpClient.GetFromJsonAsync<TransferListPageResponse>(url, JsonOptions);
        return result?.Items ?? new List<TransferListItemResponse>();
    }

    /// <inheritdoc />
    public async Task<TransferResponse?> UpdateTransferAsync(Guid transferId, UpdateTransferRequest model)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/v1/transfers/{transferId}", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TransferResponse>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTransferAsync(Guid transferId)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/transfers/{transferId}");
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

        var result = await _httpClient.GetFromJsonAsync<List<RecurringTransferDto>>(url, JsonOptions);
        return result ?? new List<RecurringTransferDto>();
    }

    /// <inheritdoc />
    public async Task<RecurringTransferDto?> GetRecurringTransferAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<RecurringTransferDto>($"api/v1/recurring-transfers/{id}", JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<RecurringTransferDto?> CreateRecurringTransferAsync(RecurringTransferCreateDto model)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/recurring-transfers", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransferDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<ApiResult<RecurringTransferDto>> UpdateRecurringTransferAsync(Guid id, RecurringTransferUpdateDto model, string? version = null)
    {
        return await this.SendUpdateAsync<RecurringTransferDto>(HttpMethod.Put, $"api/v1/recurring-transfers/{id}", model, version);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteRecurringTransferAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/recurring-transfers/{id}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<RecurringTransferDto?> PauseRecurringTransferAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"api/v1/recurring-transfers/{id}/pause", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransferDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<RecurringTransferDto?> ResumeRecurringTransferAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"api/v1/recurring-transfers/{id}/resume", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecurringTransferDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<RecurringTransferDto?> SkipNextRecurringTransferAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"api/v1/recurring-transfers/{id}/skip", null);
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

        var result = await _httpClient.GetFromJsonAsync<List<RecurringTransferInstanceDto>>(url, JsonOptions);
        return result ?? new List<RecurringTransferInstanceDto>();
    }

    /// <inheritdoc />
    public async Task<bool> SkipRecurringTransferInstanceAsync(Guid id, DateOnly date)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/recurring-transfers/{id}/instances/{date:yyyy-MM-dd}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<ApiResult<RecurringTransferInstanceDto>> ModifyRecurringTransferInstanceAsync(Guid id, DateOnly date, RecurringTransferInstanceModifyDto model, string? version = null)
    {
        return await this.SendUpdateAsync<RecurringTransferInstanceDto>(HttpMethod.Put, $"api/v1/recurring-transfers/{id}/instances/{date:yyyy-MM-dd}", model, version);
    }

    /// <inheritdoc />
    public async Task<TransactionDto?> RealizeRecurringTransactionAsync(Guid recurringTransactionId, RealizeRecurringTransactionRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/v1/recurring-transactions/{recurringTransactionId}/realize", request, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TransactionDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<TransferResponse?> RealizeRecurringTransferAsync(Guid recurringTransferId, RealizeRecurringTransferRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/v1/recurring-transfers/{recurringTransferId}/realize", request, JsonOptions);
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

        return await _httpClient.GetFromJsonAsync<PastDueSummaryDto>(url, JsonOptions);
    }

    /// <inheritdoc />
    public async Task<BatchRealizeResultDto?> RealizeBatchAsync(BatchRealizeRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/recurring/realize-batch", request, JsonOptions);
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
            return await _httpClient.GetFromJsonAsync<AppSettingsDto>("api/v1/settings", JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<AppSettingsDto?> UpdateSettingsAsync(AppSettingsUpdateDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync("api/v1/settings", dto, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AppSettingsDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<LocationDataClearedDto?> DeleteAllLocationDataAsync()
    {
        try
        {
            var response = await _httpClient.DeleteAsync("api/v1/settings/location-data");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LocationDataClearedDto>(JsonOptions);
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
    public async Task<UserSettingsDto?> GetUserSettingsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserSettingsDto>("api/v1/user/settings", JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<UserSettingsDto?> UpdateUserSettingsAsync(UserSettingsUpdateDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync("api/v1/user/settings", dto, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UserSettingsDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<UserSettingsDto?> CompleteOnboardingAsync()
    {
        var response = await _httpClient.PostAsync("api/v1/user/settings/complete-onboarding", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UserSettingsDto>(JsonOptions);
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
            return await _httpClient.GetFromJsonAsync<PaycheckAllocationSummaryDto>(url, JsonOptions);
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
        var result = await _httpClient.GetFromJsonAsync<List<BudgetCategoryDto>>(url, JsonOptions);
        return result ?? new List<BudgetCategoryDto>();
    }

    /// <inheritdoc />
    public async Task<BudgetCategoryDto?> GetCategoryAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<BudgetCategoryDto>($"api/v1/categories/{id}", JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<BudgetCategoryDto?> CreateCategoryAsync(BudgetCategoryCreateDto model)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/categories", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BudgetCategoryDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<ApiResult<BudgetCategoryDto>> UpdateCategoryAsync(Guid id, BudgetCategoryUpdateDto model, string? version = null)
    {
        return await this.SendUpdateAsync<BudgetCategoryDto>(HttpMethod.Put, $"api/v1/categories/{id}", model, version);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/categories/{id}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateCategoryAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"api/v1/categories/{id}/activate", null);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateCategoryAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"api/v1/categories/{id}/deactivate", null);
        return response.IsSuccessStatusCode;
    }

    // Budget Goal Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetGoalDto>> GetBudgetGoalsAsync(int year, int month)
    {
        var result = await _httpClient.GetFromJsonAsync<List<BudgetGoalDto>>(
            $"api/v1/budgets?year={year}&month={month}",
            JsonOptions);
        return result ?? new List<BudgetGoalDto>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BudgetGoalDto>> GetBudgetGoalsByCategoryAsync(Guid categoryId)
    {
        var result = await _httpClient.GetFromJsonAsync<List<BudgetGoalDto>>(
            $"api/v1/budgets/category/{categoryId}",
            JsonOptions);
        return result ?? new List<BudgetGoalDto>();
    }

    /// <inheritdoc />
    public async Task<ApiResult<BudgetGoalDto>> SetBudgetGoalAsync(Guid categoryId, BudgetGoalSetDto model, string? version = null)
    {
        return await this.SendUpdateAsync<BudgetGoalDto>(HttpMethod.Put, $"api/v1/budgets/{categoryId}", model, version);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteBudgetGoalAsync(Guid categoryId, int year, int month)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/budgets/{categoryId}?year={year}&month={month}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<CopyBudgetGoalsResult?> CopyBudgetGoalsAsync(CopyBudgetGoalsRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/budgets/copy", request, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CopyBudgetGoalsResult>(JsonOptions);
        }

        return null;
    }

    // Budget Progress Operations

    /// <inheritdoc />
    public async Task<BudgetSummaryDto?> GetBudgetSummaryAsync(int year, int month)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<BudgetSummaryDto>(
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
            return await _httpClient.GetFromJsonAsync<BudgetProgressDto>(
                $"api/v1/budgets/progress/{categoryId}?year={year}&month={month}",
                JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    // Categorization Rule Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategorizationRuleDto>> GetCategorizationRulesAsync(bool activeOnly = false)
    {
        var url = activeOnly ? "api/v1/categorizationrules?activeOnly=true" : "api/v1/categorizationrules";
        var result = await _httpClient.GetFromJsonAsync<List<CategorizationRuleDto>>(url, JsonOptions);
        return result ?? new List<CategorizationRuleDto>();
    }

    /// <inheritdoc />
    public async Task<CategorizationRulePageResponse> GetCategorizationRulesPagedAsync(CategorizationRuleListRequest request)
    {
        var queryParams = new List<string>
        {
            $"page={request.Page}",
            $"pageSize={request.PageSize}",
        };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(request.Search)}");
        }

        if (request.CategoryId.HasValue)
        {
            queryParams.Add($"categoryId={request.CategoryId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            queryParams.Add($"status={Uri.EscapeDataString(request.Status)}");
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");
        }

        if (!string.IsNullOrWhiteSpace(request.SortDirection))
        {
            queryParams.Add($"sortDirection={Uri.EscapeDataString(request.SortDirection)}");
        }

        var url = $"api/v1/categorizationrules?{string.Join("&", queryParams)}";
        var result = await _httpClient.GetFromJsonAsync<CategorizationRulePageResponse>(url, JsonOptions);
        return result ?? new CategorizationRulePageResponse();
    }

    /// <inheritdoc />
    public async Task<CategorizationRuleDto?> GetCategorizationRuleAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CategorizationRuleDto>(
                $"api/v1/categorizationrules/{id}",
                JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<CategorizationRuleDto?> CreateCategorizationRuleAsync(CategorizationRuleCreateDto model)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/categorizationrules", model, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CategorizationRuleDto>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<ApiResult<CategorizationRuleDto>> UpdateCategorizationRuleAsync(Guid id, CategorizationRuleUpdateDto model, string? version = null)
    {
        return await this.SendUpdateAsync<CategorizationRuleDto>(HttpMethod.Put, $"api/v1/categorizationrules/{id}", model, version);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCategorizationRuleAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/v1/categorizationrules/{id}");
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateCategorizationRuleAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"api/v1/categorizationrules/{id}/activate", null);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateCategorizationRuleAsync(Guid id)
    {
        var response = await _httpClient.PostAsync($"api/v1/categorizationrules/{id}/deactivate", null);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<TestPatternResponse?> TestPatternAsync(TestPatternRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/categorizationrules/test", request, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TestPatternResponse>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<ApplyRulesResponse?> ApplyCategorizationRulesAsync(ApplyRulesRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/categorizationrules/apply", request, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ApplyRulesResponse>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> ReorderCategorizationRulesAsync(IReadOnlyList<Guid> ruleIds)
    {
        var request = new ReorderRulesRequest { RuleIds = ruleIds };
        var response = await _httpClient.PutAsJsonAsync("api/v1/categorizationrules/reorder", request, JsonOptions);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<BulkRuleActionResponse?> BulkDeleteCategorizationRulesAsync(IReadOnlyList<Guid> ids)
    {
        var request = new BulkRuleActionRequest { Ids = ids };
        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, "api/v1/categorizationrules/bulk")
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };
        var response = await _httpClient.SendAsync(httpRequest);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BulkRuleActionResponse>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<BulkRuleActionResponse?> BulkActivateCategorizationRulesAsync(IReadOnlyList<Guid> ids)
    {
        var request = new BulkRuleActionRequest { Ids = ids };
        var response = await _httpClient.PostAsJsonAsync("api/v1/categorizationrules/bulk/activate", request, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BulkRuleActionResponse>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<BulkRuleActionResponse?> BulkDeactivateCategorizationRulesAsync(IReadOnlyList<Guid> ids)
    {
        var request = new BulkRuleActionRequest { Ids = ids };
        var response = await _httpClient.PostAsJsonAsync("api/v1/categorizationrules/bulk/deactivate", request, JsonOptions);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BulkRuleActionResponse>(JsonOptions);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<UnifiedTransactionPageDto> GetUnifiedTransactionsAsync(UnifiedTransactionFilterDto filter)
    {
        try
        {
            var queryParams = new List<string>();

            if (filter.AccountId.HasValue)
            {
                queryParams.Add($"accountId={filter.AccountId.Value}");
            }

            if (filter.CategoryId.HasValue)
            {
                queryParams.Add($"categoryId={filter.CategoryId.Value}");
            }

            if (filter.Uncategorized.HasValue)
            {
                queryParams.Add($"uncategorized={filter.Uncategorized.Value}");
            }

            if (filter.StartDate.HasValue)
            {
                queryParams.Add($"startDate={filter.StartDate.Value:yyyy-MM-dd}");
            }

            if (filter.EndDate.HasValue)
            {
                queryParams.Add($"endDate={filter.EndDate.Value:yyyy-MM-dd}");
            }

            if (!string.IsNullOrWhiteSpace(filter.Description))
            {
                queryParams.Add($"description={Uri.EscapeDataString(filter.Description)}");
            }

            if (filter.MinAmount.HasValue)
            {
                queryParams.Add($"minAmount={filter.MinAmount.Value}");
            }

            if (filter.MaxAmount.HasValue)
            {
                queryParams.Add($"maxAmount={filter.MaxAmount.Value}");
            }

            queryParams.Add($"sortBy={filter.SortBy}");
            queryParams.Add($"sortDescending={filter.SortDescending}");
            queryParams.Add($"page={filter.Page}");
            queryParams.Add($"pageSize={filter.PageSize}");

            var queryString = string.Join("&", queryParams);
            var result = await _httpClient.GetFromJsonAsync<UnifiedTransactionPageDto>(
                $"api/v1/transactions/paged?{queryString}",
                JsonOptions);

            return result ?? new UnifiedTransactionPageDto();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new UnifiedTransactionPageDto();
        }
        catch (HttpRequestException)
        {
            return new UnifiedTransactionPageDto();
        }
    }

    /// <inheritdoc />
    public async Task<TransactionDto?> UpdateTransactionCategoryAsync(Guid transactionId, Guid? categoryId)
    {
        try
        {
            var dto = new TransactionCategoryUpdateDto { CategoryId = categoryId };
            var response = await _httpClient.PatchAsJsonAsync(
                $"api/v1/transactions/{transactionId}/category",
                dto,
                JsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TransactionDto>(JsonOptions);
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<BatchSuggestCategoriesResponse> GetBatchCategorySuggestionsAsync(IReadOnlyList<Guid> transactionIds)
    {
        try
        {
            var request = new BatchSuggestCategoriesRequest { TransactionIds = transactionIds };
            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/transactions/suggest-categories",
                request,
                JsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                return new BatchSuggestCategoriesResponse();
            }

            return await response.Content.ReadFromJsonAsync<BatchSuggestCategoriesResponse>(JsonOptions)
                ?? new BatchSuggestCategoriesResponse();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new BatchSuggestCategoriesResponse();
        }
        catch (HttpRequestException)
        {
            return new BatchSuggestCategoriesResponse();
        }
    }

    /// <inheritdoc />
    public async Task<UncategorizedTransactionPageDto> GetUncategorizedTransactionsAsync(UncategorizedTransactionFilterDto filter)
    {
        try
        {
            var queryParams = new List<string>();

            if (filter.StartDate.HasValue)
            {
                queryParams.Add($"startDate={filter.StartDate.Value:yyyy-MM-dd}");
            }

            if (filter.EndDate.HasValue)
            {
                queryParams.Add($"endDate={filter.EndDate.Value:yyyy-MM-dd}");
            }

            if (filter.MinAmount.HasValue)
            {
                queryParams.Add($"minAmount={filter.MinAmount.Value}");
            }

            if (filter.MaxAmount.HasValue)
            {
                queryParams.Add($"maxAmount={filter.MaxAmount.Value}");
            }

            if (!string.IsNullOrWhiteSpace(filter.DescriptionContains))
            {
                queryParams.Add($"descriptionContains={Uri.EscapeDataString(filter.DescriptionContains)}");
            }

            if (filter.AccountId.HasValue)
            {
                queryParams.Add($"accountId={filter.AccountId.Value}");
            }

            queryParams.Add($"sortBy={filter.SortBy}");
            queryParams.Add($"sortDescending={filter.SortDescending}");
            queryParams.Add($"page={filter.Page}");
            queryParams.Add($"pageSize={filter.PageSize}");

            var queryString = string.Join("&", queryParams);
            var result = await _httpClient.GetFromJsonAsync<UncategorizedTransactionPageDto>(
                $"api/v1/transactions/uncategorized?{queryString}",
                JsonOptions);

            return result ?? new UncategorizedTransactionPageDto();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new UncategorizedTransactionPageDto();
        }
        catch (HttpRequestException)
        {
            return new UncategorizedTransactionPageDto();
        }
    }

    /// <inheritdoc />
    public async Task<BulkCategorizeResponse> BulkCategorizeTransactionsAsync(BulkCategorizeRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/transactions/bulk-categorize", request, JsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BulkCategorizeResponse>(JsonOptions);
                return result ?? new BulkCategorizeResponse { TotalRequested = request.TransactionIds.Count, FailedCount = request.TransactionIds.Count, Errors = ["Unexpected empty response"] };
            }

            return new BulkCategorizeResponse
            {
                TotalRequested = request.TransactionIds.Count,
                FailedCount = request.TransactionIds.Count,
                Errors = [$"Request failed with status {response.StatusCode}"],
            };
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new BulkCategorizeResponse
            {
                TotalRequested = request.TransactionIds.Count,
                FailedCount = request.TransactionIds.Count,
                Errors = ["Authentication required"],
            };
        }
        catch (HttpRequestException ex)
        {
            return new BulkCategorizeResponse
            {
                TotalRequested = request.TransactionIds.Count,
                FailedCount = request.TransactionIds.Count,
                Errors = [$"Network error: {ex.Message}"],
            };
        }
    }

    /// <inheritdoc />
    public async Task<MonthlyCategoryReportDto?> GetMonthlyCategoryReportAsync(int year, int month)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<MonthlyCategoryReportDto>(
                $"api/v1/reports/categories/monthly?year={year}&month={month}",
                JsonOptions);
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
    public async Task<DateRangeCategoryReportDto?> GetCategoryReportByRangeAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null)
    {
        try
        {
            var url = $"api/v1/reports/categories/range?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
            if (accountId.HasValue)
            {
                url += $"&accountId={accountId.Value}";
            }

            return await _httpClient.GetFromJsonAsync<DateRangeCategoryReportDto>(url, JsonOptions);
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
    public async Task<SpendingTrendsReportDto?> GetSpendingTrendsAsync(int months = 6, int? endYear = null, int? endMonth = null, Guid? categoryId = null)
    {
        try
        {
            var url = $"api/v1/reports/trends?months={months}";
            if (endYear.HasValue)
            {
                url += $"&endYear={endYear.Value}";
            }

            if (endMonth.HasValue)
            {
                url += $"&endMonth={endMonth.Value}";
            }

            if (categoryId.HasValue)
            {
                url += $"&categoryId={categoryId.Value}";
            }

            return await _httpClient.GetFromJsonAsync<SpendingTrendsReportDto>(url, JsonOptions);
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
    public async Task<DaySummaryDto?> GetDaySummaryAsync(DateOnly date, Guid? accountId = null)
    {
        try
        {
            var url = $"api/v1/reports/day-summary/{date:yyyy-MM-dd}";
            if (accountId.HasValue)
            {
                url += $"?accountId={accountId.Value}";
            }

            return await _httpClient.GetFromJsonAsync<DaySummaryDto>(url, JsonOptions);
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
    public async Task<LocationSpendingReportDto?> GetSpendingByLocationAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null)
    {
        try
        {
            var url = $"api/v1/reports/spending-by-location?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
            if (accountId.HasValue)
            {
                url += $"&accountId={accountId.Value}";
            }

            return await _httpClient.GetFromJsonAsync<LocationSpendingReportDto>(url, JsonOptions);
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
    public async Task<IReadOnlyList<CustomReportLayoutDto>> GetCustomReportLayoutsAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<CustomReportLayoutDto>>(
                "api/v1/custom-reports",
                JsonOptions);
            return result ?? new List<CustomReportLayoutDto>();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return new List<CustomReportLayoutDto>();
        }
        catch (HttpRequestException)
        {
            return new List<CustomReportLayoutDto>();
        }
    }

    /// <inheritdoc />
    public async Task<CustomReportLayoutDto?> GetCustomReportLayoutAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CustomReportLayoutDto>(
                $"api/v1/custom-reports/{id}",
                JsonOptions);
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
    public async Task<CustomReportLayoutDto?> CreateCustomReportLayoutAsync(CustomReportLayoutCreateDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/custom-reports",
                dto,
                JsonOptions);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CustomReportLayoutDto>(JsonOptions);
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
    public async Task<ApiResult<CustomReportLayoutDto>> UpdateCustomReportLayoutAsync(Guid id, CustomReportLayoutUpdateDto dto, string? version = null)
    {
        return await this.SendUpdateAsync<CustomReportLayoutDto>(HttpMethod.Put, $"api/v1/custom-reports/{id}", dto, version);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCustomReportLayoutAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/custom-reports/{id}");
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
    public async Task<ImportPatternsDto?> GetImportPatternsAsync(Guid recurringTransactionId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ImportPatternsDto>(
                $"api/v1/recurring-transactions/{recurringTransactionId}/import-patterns",
                JsonOptions);
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
    public async Task<ImportPatternsDto?> UpdateImportPatternsAsync(Guid recurringTransactionId, ImportPatternsDto patterns)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"api/v1/recurring-transactions/{recurringTransactionId}/import-patterns",
                patterns,
                JsonOptions);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ImportPatternsDto>(JsonOptions);
            }

            return null;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return null;
        }
    }

    // ─── Statement Reconciliation (Feature 125b) ──────────────────────────────

    /// <inheritdoc />
    public async Task<TransactionDto?> MarkTransactionClearedAsync(MarkClearedRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/statement-reconciliation/clear", request, JsonOptions);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<TransactionDto>(JsonOptions)
                : null;
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
    public async Task<TransactionDto?> MarkTransactionUnclearedAsync(MarkUnclearedRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/statement-reconciliation/unclear", request, JsonOptions);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<TransactionDto>(JsonOptions)
                : null;
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
    public async Task<IReadOnlyList<TransactionDto>?> BulkMarkTransactionsClearedAsync(BulkMarkClearedRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/statement-reconciliation/bulk-clear", request, JsonOptions);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<TransactionDto>>(JsonOptions)
                : null;
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
    public async Task<IReadOnlyList<TransactionDto>?> BulkMarkTransactionsUnclearedAsync(BulkMarkUnclearedRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/statement-reconciliation/bulk-unclear", request, JsonOptions);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<List<TransactionDto>>(JsonOptions)
                : null;
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
    public async Task<StatementBalanceDto?> GetActiveStatementBalanceAsync(Guid accountId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<StatementBalanceDto>(
                $"api/v1/statement-reconciliation/statement-balance?accountId={accountId}", JsonOptions);
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
    public async Task<ClearedBalanceDto?> GetClearedBalanceAsync(Guid accountId, DateOnly? upToDate = null)
    {
        try
        {
            var url = $"api/v1/statement-reconciliation/cleared-balance?accountId={accountId}";
            if (upToDate.HasValue)
            {
                url += $"&upToDate={upToDate.Value:yyyy-MM-dd}";
            }

            return await _httpClient.GetFromJsonAsync<ClearedBalanceDto>(url, JsonOptions);
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
    public async Task<StatementBalanceDto?> SetStatementBalanceAsync(SetStatementBalanceRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/statement-reconciliation/statement-balance", request, JsonOptions);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<StatementBalanceDto>(JsonOptions)
                : null;
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
    public async Task<ApiResult<ReconciliationRecordDto>> CompleteReconciliationAsync(CompleteReconciliationRequest request)
    {
        return await this.SendUpdateAsync<ReconciliationRecordDto>(HttpMethod.Post, "api/v1/statement-reconciliation/complete", request, null);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReconciliationRecordDto>?> GetReconciliationHistoryAsync(Guid accountId, int page = 1, int pageSize = 20)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ReconciliationRecordDto>>(
                $"api/v1/statement-reconciliation/history?accountId={accountId}&page={page}&pageSize={pageSize}", JsonOptions);
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
    public async Task<IReadOnlyList<TransactionDto>?> GetReconciliationTransactionsAsync(Guid reconciliationRecordId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<TransactionDto>>(
                $"api/v1/statement-reconciliation/records/{reconciliationRecordId}/transactions", JsonOptions);
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
    public async Task<DataHealthReportDto?> GetDataHealthReportAsync(Guid? accountId = null)
    {
        try
        {
            var url = accountId.HasValue
                ? $"api/v1/datahealth/report?accountId={accountId}"
                : "api/v1/datahealth/report";
            return await _httpClient.GetFromJsonAsync<DataHealthReportDto>(url, JsonOptions);
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
    public async Task<IReadOnlyList<DuplicateClusterDto>?> GetDuplicatesAsync(Guid? accountId = null)
    {
        try
        {
            var url = accountId.HasValue
                ? $"api/v1/datahealth/duplicates?accountId={accountId}"
                : "api/v1/datahealth/duplicates";
            return await _httpClient.GetFromJsonAsync<List<DuplicateClusterDto>>(url, JsonOptions);
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
    public async Task<IReadOnlyList<AmountOutlierDto>?> GetOutliersAsync(Guid? accountId = null)
    {
        try
        {
            var url = accountId.HasValue
                ? $"api/v1/datahealth/outliers?accountId={accountId}"
                : "api/v1/datahealth/outliers";
            return await _httpClient.GetFromJsonAsync<List<AmountOutlierDto>>(url, JsonOptions);
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
    public async Task<IReadOnlyList<DateGapDto>?> GetDateGapsAsync(Guid? accountId = null, int minGapDays = 7)
    {
        try
        {
            var url = accountId.HasValue
                ? $"api/v1/datahealth/date-gaps?accountId={accountId}&minGapDays={minGapDays}"
                : $"api/v1/datahealth/date-gaps?minGapDays={minGapDays}";
            return await _httpClient.GetFromJsonAsync<List<DateGapDto>>(url, JsonOptions);
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
    public async Task<UncategorizedSummaryDto?> GetUncategorizedSummaryAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UncategorizedSummaryDto>("api/v1/datahealth/uncategorized", JsonOptions);
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
    public async Task MergeDuplicatesAsync(MergeDuplicatesRequest request)
    {
        try
        {
            await _httpClient.PostAsJsonAsync("api/v1/datahealth/merge-duplicates", request, JsonOptions);
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
        }
        catch (HttpRequestException)
        {
        }
    }

    /// <inheritdoc />
    public async Task DismissOutlierAsync(Guid transactionId)
    {
        try
        {
            await _httpClient.PostAsJsonAsync(
                $"api/v1/datahealth/dismiss-outlier/{transactionId}",
                new { },
                JsonOptions);
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
        }
        catch (HttpRequestException)
        {
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
