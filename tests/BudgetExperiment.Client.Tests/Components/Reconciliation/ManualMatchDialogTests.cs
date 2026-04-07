// <copyright file="ManualMatchDialogTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Reconciliation;
using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Reconciliation;

/// <summary>
/// Unit tests for the <see cref="ManualMatchDialog"/> component.
/// </summary>
public sealed class ManualMatchDialogTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _budgetApi;
    private readonly StubReconciliationApiService _reconciliationApi;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManualMatchDialogTests"/> class.
    /// </summary>
    public ManualMatchDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
        Services.AddSingleton<CultureService>();
        _budgetApi = new StubBudgetApiService();
        _reconciliationApi = new StubReconciliationApiService();
        Services.AddSingleton<IBudgetApiService>(_budgetApi);
        Services.AddSingleton<IReconciliationApiService>(_reconciliationApi);
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the dialog renders nothing when not visible.
    /// </summary>
    [Fact]
    public void ManualMatchDialog_RendersNothing_WhenNotVisible()
    {
        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, false));

        Assert.DoesNotContain("manual-match-dialog", cut.Markup);
    }

    /// <summary>
    /// Verifies the dialog shows transaction details when visible.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_ShowsTransactionDetails_WhenVisible()
    {
        var transaction = CreateTransaction("WALMART STORE #123");

        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, transaction));

        await Task.Delay(50);
        cut.Render();

        Assert.Contains("WALMART STORE #123", cut.Markup);
    }

    /// <summary>
    /// Verifies the dialog shows the transaction amount.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_ShowsTransactionAmount()
    {
        var transaction = CreateTransaction();

        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, transaction));

        await Task.Delay(50);
        cut.Render();

        Assert.Contains("75", cut.Markup);
    }

    /// <summary>
    /// Verifies the dialog shows loading state while loading recurring transfers.
    /// </summary>
    [Fact]
    public void ManualMatchDialog_ShowsContent_WhenVisible()
    {
        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, CreateTransaction()));

        Assert.Contains("manual-match-dialog", cut.Markup);
    }

    /// <summary>
    /// Verifies the dialog shows empty state when no recurring transfers available.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_ShowsEmptyState_WhenNoRecurringTransfers()
    {
        _budgetApi.RecurringTransfersResult = [];

        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, CreateTransaction()));

        await Task.Delay(50);
        cut.Render();

        Assert.Contains("empty-state", cut.Markup);
    }

    /// <summary>
    /// Verifies the cancel button invokes the OnCancel callback.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_CancelButton_InvokesCallback()
    {
        var cancelled = false;
        _budgetApi.RecurringTransfersResult = [];

        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, CreateTransaction())
            .Add(p => p.OnCancel, () => { cancelled = true; }));

        await Task.Delay(50);
        cut.Render();

        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelBtn.Click();

        Assert.True(cancelled);
    }

    /// <summary>
    /// Verifies the dialog shows recurring transfer options when available.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_ShowsRecurringTransferOptions()
    {
        _budgetApi.RecurringTransfersResult =
        [
            new RecurringTransferDto
            {
                Id = Guid.NewGuid(),
                Description = "Monthly Savings",
                Amount = new MoneyDto { Amount = 500m, Currency = "USD" },
                Frequency = "Monthly",
                SourceAccountId = Guid.NewGuid(),
                SourceAccountName = "Checking",
                DestinationAccountId = Guid.NewGuid(),
                DestinationAccountName = "Savings",
                IsActive = true,
            },
        ];

        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, CreateTransaction()));

        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Monthly Savings", cut.Markup);
    }

    /// <summary>
    /// Verifies the dialog shows the transaction card section.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_ShowsTransactionCard()
    {
        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, CreateTransaction()));

        await Task.Delay(50);
        cut.Render();

        Assert.Contains("transaction-card", cut.Markup);
    }

    /// <summary>
    /// Verifies that selecting a recurring transfer shows instance date section.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_SelectRecurring_ShowsInstanceDateSection()
    {
        _budgetApi.RecurringTransfersResult = [CreateActiveRecurringTransfer()];

        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, CreateTransaction()));

        await Task.Delay(50);
        cut.Render();

        cut.Find(".recurring-option").Click();

        Assert.Contains("instance-section", cut.Markup);
        Assert.Contains("Select Instance Date", cut.Markup);
    }

    /// <summary>
    /// Verifies that selecting a recurring transfer applies the selected CSS class.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_SelectRecurring_AppliesSelectedClass()
    {
        _budgetApi.RecurringTransfersResult = [CreateActiveRecurringTransfer()];

        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, CreateTransaction()));

        await Task.Delay(50);
        cut.Render();

        cut.Find(".recurring-option").Click();

        Assert.Contains("selected", cut.Find(".recurring-option").ClassList);
    }

    /// <summary>
    /// Verifies the Create Match button is disabled when no recurring is selected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_SubmitDisabled_WhenNoRecurringSelected()
    {
        _budgetApi.RecurringTransfersResult = [CreateActiveRecurringTransfer()];

        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, CreateTransaction()));

        await Task.Delay(50);
        cut.Render();

        var submitBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Create Match"));
        Assert.True(submitBtn.HasAttribute("disabled"));
    }

    /// <summary>
    /// Verifies that the transaction date is shown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_ShowsTransactionDate()
    {
        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, CreateTransaction()));

        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Mar 1, 2026", cut.Markup);
    }

    /// <summary>
    /// Verifies that the dialog shows source and destination account info.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_ShowsAccountInfo_InRecurringOption()
    {
        _budgetApi.RecurringTransfersResult = [CreateActiveRecurringTransfer()];

        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, CreateTransaction()));

        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Checking", cut.Markup);
        Assert.Contains("Savings", cut.Markup);
    }

    /// <summary>
    /// Verifies that inactive recurring transfers are filtered out.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_FiltersInactiveRecurring()
    {
        _budgetApi.RecurringTransfersResult =
        [
            new RecurringTransferDto
            {
                Id = Guid.NewGuid(),
                Description = "Inactive Transfer",
                Amount = new MoneyDto { Amount = 100m, Currency = "USD" },
                Frequency = "Monthly",
                SourceAccountId = Guid.NewGuid(),
                SourceAccountName = "Src",
                DestinationAccountId = Guid.NewGuid(),
                DestinationAccountName = "Dst",
                IsActive = false,
            },
        ];

        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, CreateTransaction()));

        await Task.Delay(50);
        cut.Render();

        Assert.Contains("empty-state", cut.Markup);
    }

    /// <summary>
    /// Verifies the dialog shows date suggestions when recurring is selected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_ShowsDateSuggestions_WhenRecurringSelected()
    {
        _budgetApi.RecurringTransfersResult = [CreateActiveRecurringTransfer()];

        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, CreateTransaction()));

        await Task.Delay(50);
        cut.Render();

        cut.Find(".recurring-option").Click();

        Assert.Contains("date-suggestions", cut.Markup);
        Assert.Contains("Transaction Date", cut.Markup);
    }

    /// <summary>
    /// Verifies HandleSubmit creates a match and invokes the callback when successful.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_HandleSubmit_CreatesMatch_WhenSuccessful()
    {
        var matchResult = new ReconciliationMatchDto
        {
            Id = Guid.NewGuid(),
            ImportedTransactionId = Guid.NewGuid(),
            ConfidenceScore = 1.0m,
        };
        _reconciliationApi.ManualMatchResult = matchResult;
        _budgetApi.RecurringTransfersResult = [CreateActiveRecurringTransfer()];

        ReconciliationMatchDto? receivedMatch = null;
        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, CreateTransaction())
            .Add(p => p.OnMatchCreated, (ReconciliationMatchDto m) => { receivedMatch = m; }));

        await Task.Delay(50);
        cut.Render();

        // Select a recurring transfer
        cut.Find(".recurring-option").Click();

        // Click Create Match
        var submitBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Create Match"));
        submitBtn.Click();

        await Task.Delay(50);

        Assert.NotNull(receivedMatch);
        Assert.Equal(matchResult.Id, receivedMatch!.Id);
    }

    /// <summary>
    /// Verifies HandleSubmit shows error when API returns null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_HandleSubmit_ShowsError_WhenApiFails()
    {
        _reconciliationApi.ManualMatchResult = null;
        _budgetApi.RecurringTransfersResult = [CreateActiveRecurringTransfer()];

        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, CreateTransaction()));

        await Task.Delay(50);
        cut.Render();

        // Select a recurring transfer
        cut.Find(".recurring-option").Click();

        // Click Create Match
        var submitBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Create Match"));
        submitBtn.Click();

        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Failed to create match", cut.Markup);
    }

    /// <summary>
    /// Verifies HandleSubmit does nothing when no transaction is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ManualMatchDialog_HandleSubmit_NoOp_WhenNoTransaction()
    {
        _budgetApi.RecurringTransfersResult = [CreateActiveRecurringTransfer()];

        var matchCreated = false;
        var cut = Render<ManualMatchDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Transaction, (TransactionDto?)null)
            .Add(p => p.OnMatchCreated, (ReconciliationMatchDto _) => { matchCreated = true; }));

        await Task.Delay(50);
        cut.Render();

        // Submit should be disabled — the button won't fire
        Assert.False(matchCreated);
    }

    private static TransactionDto CreateTransaction(string description = "Test Transaction") => new()
    {
        Id = Guid.NewGuid(),
        AccountId = Guid.NewGuid(),
        Description = description,
        Date = new DateOnly(2026, 3, 1),
        Amount = new MoneyDto { Amount = -75.00m, Currency = "USD" },
    };

    private static RecurringTransferDto CreateActiveRecurringTransfer() => new()
    {
        Id = Guid.NewGuid(),
        Description = "Monthly Savings",
        Amount = new MoneyDto { Amount = 500m, Currency = "USD" },
        Frequency = "Monthly",
        SourceAccountId = Guid.NewGuid(),
        SourceAccountName = "Checking",
        DestinationAccountId = Guid.NewGuid(),
        DestinationAccountName = "Savings",
        IsActive = true,
    };

#pragma warning disable SA1201 // Elements should appear in the correct order
    private sealed class StubBudgetApiService : IBudgetApiService
#pragma warning restore SA1201
    {
        /// <summary>Gets or sets the recurring transfers result.</summary>
        public IReadOnlyList<RecurringTransferDto>? RecurringTransfersResult
        {
            get; set;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<RecurringTransferDto>> GetRecurringTransfersAsync(Guid? accountId = null) => Task.FromResult<IReadOnlyList<RecurringTransferDto>>(this.RecurringTransfersResult ?? []);

        /// <inheritdoc/>
        public Task<IReadOnlyList<AccountDto>> GetAccountsAsync() => Task.FromResult<IReadOnlyList<AccountDto>>([]);

        /// <inheritdoc/>
        public Task<AccountDto?> GetAccountAsync(Guid id) => Task.FromResult<AccountDto?>(null);

        /// <inheritdoc/>
        public Task<AccountDto?> CreateAccountAsync(AccountCreateDto model) => Task.FromResult<AccountDto?>(null);

        /// <inheritdoc/>
        public Task<ApiResult<AccountDto>> UpdateAccountAsync(Guid id, AccountUpdateDto model, string? version = null) => Task.FromResult(ApiResult<AccountDto>.Failure());

        /// <inheritdoc/>
        public Task<bool> DeleteAccountAsync(Guid id) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null) => Task.FromResult<IReadOnlyList<TransactionDto>>([]);

        /// <inheritdoc/>
        public Task<TransactionDto?> GetTransactionAsync(Guid id) => Task.FromResult<TransactionDto?>(null);

        /// <inheritdoc/>
        public Task<TransactionDto?> CreateTransactionAsync(TransactionCreateDto model) => Task.FromResult<TransactionDto?>(null);

        /// <inheritdoc/>
        public Task<ApiResult<TransactionDto>> UpdateTransactionAsync(Guid id, TransactionUpdateDto model, string? version = null) => Task.FromResult(ApiResult<TransactionDto>.Failure());

        /// <inheritdoc/>
        public Task<bool> DeleteTransactionAsync(Guid id) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<ApiResult<TransactionDto>> UpdateTransactionLocationAsync(Guid id, TransactionLocationUpdateDto dto, string? version = null) => Task.FromResult(ApiResult<TransactionDto>.Failure());

        /// <inheritdoc/>
        public Task<bool> ClearTransactionLocationAsync(Guid id) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<ReverseGeocodeResponseDto?> ReverseGeocodeAsync(decimal latitude, decimal longitude) => Task.FromResult<ReverseGeocodeResponseDto?>(null);

        /// <inheritdoc/>
        public Task<CalendarGridDto> GetCalendarGridAsync(int year, int month, Guid? accountId = null) => Task.FromResult(new CalendarGridDto());

        /// <inheritdoc/>
        public Task<DayDetailDto> GetDayDetailAsync(DateOnly date, Guid? accountId = null) => Task.FromResult(new DayDetailDto());

        /// <inheritdoc/>
        public Task<TransactionListDto> GetAccountTransactionListAsync(Guid accountId, DateOnly startDate, DateOnly endDate, bool includeRecurring = true) => Task.FromResult(new TransactionListDto());

        /// <inheritdoc/>
        public Task<IReadOnlyList<DailyTotalDto>> GetCalendarSummaryAsync(int year, int month, Guid? accountId = null) => Task.FromResult<IReadOnlyList<DailyTotalDto>>([]);

        /// <inheritdoc/>
        public Task<IReadOnlyList<RecurringTransactionDto>> GetRecurringTransactionsAsync() => Task.FromResult<IReadOnlyList<RecurringTransactionDto>>([]);

        /// <inheritdoc/>
        public Task<RecurringTransactionDto?> GetRecurringTransactionAsync(Guid id) => Task.FromResult<RecurringTransactionDto?>(null);

        /// <inheritdoc/>
        public Task<RecurringTransactionDto?> CreateRecurringTransactionAsync(RecurringTransactionCreateDto model) => Task.FromResult<RecurringTransactionDto?>(null);

        /// <inheritdoc/>
        public Task<ApiResult<RecurringTransactionDto>> UpdateRecurringTransactionAsync(Guid id, RecurringTransactionUpdateDto model, string? version = null) => Task.FromResult(ApiResult<RecurringTransactionDto>.Failure());

        /// <inheritdoc/>
        public Task<bool> DeleteRecurringTransactionAsync(Guid id) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<RecurringTransactionDto?> PauseRecurringTransactionAsync(Guid id) => Task.FromResult<RecurringTransactionDto?>(null);

        /// <inheritdoc/>
        public Task<RecurringTransactionDto?> ResumeRecurringTransactionAsync(Guid id) => Task.FromResult<RecurringTransactionDto?>(null);

        /// <inheritdoc/>
        public Task<RecurringTransactionDto?> SkipNextRecurringAsync(Guid id) => Task.FromResult<RecurringTransactionDto?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<RecurringInstanceDto>> GetProjectedRecurringAsync(DateOnly from, DateOnly to, Guid? accountId = null) => Task.FromResult<IReadOnlyList<RecurringInstanceDto>>([]);

        /// <inheritdoc/>
        public Task<bool> SkipRecurringInstanceAsync(Guid id, DateOnly date) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<ApiResult<RecurringInstanceDto>> ModifyRecurringInstanceAsync(Guid id, DateOnly date, RecurringInstanceModifyDto model, string? version = null) => Task.FromResult(ApiResult<RecurringInstanceDto>.Failure());

        /// <inheritdoc/>
        public Task<TransferResponse?> CreateTransferAsync(CreateTransferRequest model) => Task.FromResult<TransferResponse?>(null);

        /// <inheritdoc/>
        public Task<TransferResponse?> GetTransferAsync(Guid transferId) => Task.FromResult<TransferResponse?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<TransferListItemResponse>> GetTransfersAsync(Guid? accountId = null, DateOnly? from = null, DateOnly? to = null, int page = 1, int pageSize = 20) => Task.FromResult<IReadOnlyList<TransferListItemResponse>>([]);

        /// <inheritdoc/>
        public Task<TransferResponse?> UpdateTransferAsync(Guid transferId, UpdateTransferRequest model) => Task.FromResult<TransferResponse?>(null);

        /// <inheritdoc/>
        public Task<bool> DeleteTransferAsync(Guid transferId) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<RecurringTransferDto?> GetRecurringTransferAsync(Guid id) => Task.FromResult<RecurringTransferDto?>(null);

        /// <inheritdoc/>
        public Task<RecurringTransferDto?> CreateRecurringTransferAsync(RecurringTransferCreateDto model) => Task.FromResult<RecurringTransferDto?>(null);

        /// <inheritdoc/>
        public Task<ApiResult<RecurringTransferDto>> UpdateRecurringTransferAsync(Guid id, RecurringTransferUpdateDto model, string? version = null) => Task.FromResult(ApiResult<RecurringTransferDto>.Failure());

        /// <inheritdoc/>
        public Task<bool> DeleteRecurringTransferAsync(Guid id) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<RecurringTransferDto?> PauseRecurringTransferAsync(Guid id) => Task.FromResult<RecurringTransferDto?>(null);

        /// <inheritdoc/>
        public Task<RecurringTransferDto?> ResumeRecurringTransferAsync(Guid id) => Task.FromResult<RecurringTransferDto?>(null);

        /// <inheritdoc/>
        public Task<RecurringTransferDto?> SkipNextRecurringTransferAsync(Guid id) => Task.FromResult<RecurringTransferDto?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<RecurringTransferInstanceDto>> GetProjectedRecurringTransfersAsync(DateOnly from, DateOnly to, Guid? accountId = null) => Task.FromResult<IReadOnlyList<RecurringTransferInstanceDto>>([]);

        /// <inheritdoc/>
        public Task<bool> SkipRecurringTransferInstanceAsync(Guid id, DateOnly date) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<ApiResult<RecurringTransferInstanceDto>> ModifyRecurringTransferInstanceAsync(Guid id, DateOnly date, RecurringTransferInstanceModifyDto model, string? version = null) => Task.FromResult(ApiResult<RecurringTransferInstanceDto>.Failure());

        /// <inheritdoc/>
        public Task<TransactionDto?> RealizeRecurringTransactionAsync(Guid recurringTransactionId, RealizeRecurringTransactionRequest request) => Task.FromResult<TransactionDto?>(null);

        /// <inheritdoc/>
        public Task<TransferResponse?> RealizeRecurringTransferAsync(Guid recurringTransferId, RealizeRecurringTransferRequest request) => Task.FromResult<TransferResponse?>(null);

        /// <inheritdoc/>
        public Task<PastDueSummaryDto?> GetPastDueItemsAsync(Guid? accountId = null) => Task.FromResult<PastDueSummaryDto?>(null);

        /// <inheritdoc/>
        public Task<BatchRealizeResultDto?> RealizeBatchAsync(BatchRealizeRequest request) => Task.FromResult<BatchRealizeResultDto?>(null);

        /// <inheritdoc/>
        public Task<AppSettingsDto?> GetSettingsAsync() => Task.FromResult<AppSettingsDto?>(null);

        /// <inheritdoc/>
        public Task<AppSettingsDto?> UpdateSettingsAsync(AppSettingsUpdateDto dto) => Task.FromResult<AppSettingsDto?>(null);

        /// <inheritdoc/>
        public Task<LocationDataClearedDto?> DeleteAllLocationDataAsync() => Task.FromResult<LocationDataClearedDto?>(null);

        /// <inheritdoc/>
        public Task<PaycheckAllocationSummaryDto?> GetPaycheckAllocationAsync(string frequency, decimal? amount = null, Guid? accountId = null) => Task.FromResult<PaycheckAllocationSummaryDto?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<BudgetCategoryDto>> GetCategoriesAsync(bool activeOnly = false) => Task.FromResult<IReadOnlyList<BudgetCategoryDto>>([]);

        /// <inheritdoc/>
        public Task<BudgetCategoryDto?> GetCategoryAsync(Guid id) => Task.FromResult<BudgetCategoryDto?>(null);

        /// <inheritdoc/>
        public Task<BudgetCategoryDto?> CreateCategoryAsync(BudgetCategoryCreateDto model) => Task.FromResult<BudgetCategoryDto?>(null);

        /// <inheritdoc/>
        public Task<ApiResult<BudgetCategoryDto>> UpdateCategoryAsync(Guid id, BudgetCategoryUpdateDto model, string? version = null) => Task.FromResult(ApiResult<BudgetCategoryDto>.Failure());

        /// <inheritdoc/>
        public Task<bool> DeleteCategoryAsync(Guid id) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<bool> ActivateCategoryAsync(Guid id) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<bool> DeactivateCategoryAsync(Guid id) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<IReadOnlyList<BudgetGoalDto>> GetBudgetGoalsAsync(int year, int month) => Task.FromResult<IReadOnlyList<BudgetGoalDto>>([]);

        /// <inheritdoc/>
        public Task<IReadOnlyList<BudgetGoalDto>> GetBudgetGoalsByCategoryAsync(Guid categoryId) => Task.FromResult<IReadOnlyList<BudgetGoalDto>>([]);

        /// <inheritdoc/>
        public Task<ApiResult<BudgetGoalDto>> SetBudgetGoalAsync(Guid categoryId, BudgetGoalSetDto model, string? version = null) => Task.FromResult(ApiResult<BudgetGoalDto>.Failure());

        /// <inheritdoc/>
        public Task<bool> DeleteBudgetGoalAsync(Guid categoryId, int year, int month) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<CopyBudgetGoalsResult?> CopyBudgetGoalsAsync(CopyBudgetGoalsRequest request) => Task.FromResult<CopyBudgetGoalsResult?>(null);

        /// <inheritdoc/>
        public Task<BudgetSummaryDto?> GetBudgetSummaryAsync(int year, int month) => Task.FromResult<BudgetSummaryDto?>(null);

        /// <inheritdoc/>
        public Task<BudgetProgressDto?> GetCategoryProgressAsync(Guid categoryId, int year, int month) => Task.FromResult<BudgetProgressDto?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<CategorizationRuleDto>> GetCategorizationRulesAsync(bool activeOnly = false) => Task.FromResult<IReadOnlyList<CategorizationRuleDto>>([]);

        /// <inheritdoc/>
        public Task<CategorizationRulePageResponse> GetCategorizationRulesPagedAsync(CategorizationRuleListRequest request) => Task.FromResult(new CategorizationRulePageResponse());

        /// <inheritdoc/>
        public Task<CategorizationRuleDto?> GetCategorizationRuleAsync(Guid id) => Task.FromResult<CategorizationRuleDto?>(null);

        /// <inheritdoc/>
        public Task<CategorizationRuleDto?> CreateCategorizationRuleAsync(CategorizationRuleCreateDto model) => Task.FromResult<CategorizationRuleDto?>(null);

        /// <inheritdoc/>
        public Task<ApiResult<CategorizationRuleDto>> UpdateCategorizationRuleAsync(Guid id, CategorizationRuleUpdateDto model, string? version = null) => Task.FromResult(ApiResult<CategorizationRuleDto>.Failure());

        /// <inheritdoc/>
        public Task<bool> DeleteCategorizationRuleAsync(Guid id) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<bool> ActivateCategorizationRuleAsync(Guid id) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<bool> DeactivateCategorizationRuleAsync(Guid id) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<TestPatternResponse?> TestPatternAsync(TestPatternRequest request) => Task.FromResult<TestPatternResponse?>(null);

        /// <inheritdoc/>
        public Task<ApplyRulesResponse?> ApplyCategorizationRulesAsync(ApplyRulesRequest request) => Task.FromResult<ApplyRulesResponse?>(null);

        /// <inheritdoc/>
        public Task<bool> ReorderCategorizationRulesAsync(IReadOnlyList<Guid> ruleIds) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<BulkRuleActionResponse?> BulkDeleteCategorizationRulesAsync(IReadOnlyList<Guid> ids) => Task.FromResult<BulkRuleActionResponse?>(null);

        /// <inheritdoc/>
        public Task<BulkRuleActionResponse?> BulkActivateCategorizationRulesAsync(IReadOnlyList<Guid> ids) => Task.FromResult<BulkRuleActionResponse?>(null);

        /// <inheritdoc/>
        public Task<BulkRuleActionResponse?> BulkDeactivateCategorizationRulesAsync(IReadOnlyList<Guid> ids) => Task.FromResult<BulkRuleActionResponse?>(null);

        /// <inheritdoc/>
        public Task<UncategorizedTransactionPageDto> GetUncategorizedTransactionsAsync(UncategorizedTransactionFilterDto filter) => Task.FromResult(new UncategorizedTransactionPageDto());

        /// <inheritdoc/>
        public Task<BulkCategorizeResponse> BulkCategorizeTransactionsAsync(BulkCategorizeRequest request) => Task.FromResult(new BulkCategorizeResponse());

        /// <inheritdoc/>
        public Task<MonthlyCategoryReportDto?> GetMonthlyCategoryReportAsync(int year, int month) => Task.FromResult<MonthlyCategoryReportDto?>(null);

        /// <inheritdoc/>
        public Task<DateRangeCategoryReportDto?> GetCategoryReportByRangeAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null) => Task.FromResult<DateRangeCategoryReportDto?>(null);

        /// <inheritdoc/>
        public Task<SpendingTrendsReportDto?> GetSpendingTrendsAsync(int months = 6, int? endYear = null, int? endMonth = null, Guid? categoryId = null) => Task.FromResult<SpendingTrendsReportDto?>(null);

        /// <inheritdoc/>
        public Task<DaySummaryDto?> GetDaySummaryAsync(DateOnly date, Guid? accountId = null) => Task.FromResult<DaySummaryDto?>(null);

        /// <inheritdoc/>
        public Task<LocationSpendingReportDto?> GetSpendingByLocationAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null) => Task.FromResult<LocationSpendingReportDto?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<CustomReportLayoutDto>> GetCustomReportLayoutsAsync() => Task.FromResult<IReadOnlyList<CustomReportLayoutDto>>([]);

        /// <inheritdoc/>
        public Task<CustomReportLayoutDto?> GetCustomReportLayoutAsync(Guid id) => Task.FromResult<CustomReportLayoutDto?>(null);

        /// <inheritdoc/>
        public Task<CustomReportLayoutDto?> CreateCustomReportLayoutAsync(CustomReportLayoutCreateDto dto) => Task.FromResult<CustomReportLayoutDto?>(null);

        /// <inheritdoc/>
        public Task<ApiResult<CustomReportLayoutDto>> UpdateCustomReportLayoutAsync(Guid id, CustomReportLayoutUpdateDto dto, string? version = null) => Task.FromResult(ApiResult<CustomReportLayoutDto>.Failure());

        /// <inheritdoc/>
        public Task<bool> DeleteCustomReportLayoutAsync(Guid id) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<ImportPatternsDto?> GetImportPatternsAsync(Guid recurringTransactionId) => Task.FromResult<ImportPatternsDto?>(null);

        /// <inheritdoc/>
        public Task<ImportPatternsDto?> UpdateImportPatternsAsync(Guid recurringTransactionId, ImportPatternsDto patterns) => Task.FromResult<ImportPatternsDto?>(null);

        /// <inheritdoc/>
        public Task<UserSettingsDto?> GetUserSettingsAsync() => Task.FromResult<UserSettingsDto?>(null);

        /// <inheritdoc/>
        public Task<UserSettingsDto?> UpdateUserSettingsAsync(UserSettingsUpdateDto dto) => Task.FromResult<UserSettingsDto?>(null);

        /// <inheritdoc/>
        public Task<UnifiedTransactionPageDto> GetUnifiedTransactionsAsync(UnifiedTransactionFilterDto filter) => Task.FromResult(new UnifiedTransactionPageDto());

        /// <inheritdoc/>
        public Task<TransactionDto?> UpdateTransactionCategoryAsync(Guid transactionId, Guid? categoryId) => Task.FromResult<TransactionDto?>(null);

        /// <inheritdoc/>
        public Task<BatchSuggestCategoriesResponse> GetBatchCategorySuggestionsAsync(IReadOnlyList<Guid> transactionIds) => Task.FromResult(new BatchSuggestCategoriesResponse());

        /// <inheritdoc/>
        public Task<UserSettingsDto?> CompleteOnboardingAsync() => Task.FromResult<UserSettingsDto?>(null);

#pragma warning disable SA1648 // inheritdoc should be used with inheriting class
        /// <inheritdoc/>
        public Task<TransactionDto?> MarkTransactionClearedAsync(MarkClearedRequest request) => Task.FromResult<TransactionDto?>(null);

        /// <inheritdoc/>
        public Task<TransactionDto?> MarkTransactionUnclearedAsync(MarkUnclearedRequest request) => Task.FromResult<TransactionDto?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<TransactionDto>?> BulkMarkTransactionsClearedAsync(BulkMarkClearedRequest request) => Task.FromResult<IReadOnlyList<TransactionDto>?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<TransactionDto>?> BulkMarkTransactionsUnclearedAsync(BulkMarkUnclearedRequest request) => Task.FromResult<IReadOnlyList<TransactionDto>?>(null);

        /// <inheritdoc/>
        public Task<StatementBalanceDto?> GetActiveStatementBalanceAsync(Guid accountId) => Task.FromResult<StatementBalanceDto?>(null);

        /// <inheritdoc/>
        public Task<ClearedBalanceDto?> GetClearedBalanceAsync(Guid accountId, DateOnly? upToDate = null) => Task.FromResult<ClearedBalanceDto?>(null);

        /// <inheritdoc/>
        public Task<StatementBalanceDto?> SetStatementBalanceAsync(SetStatementBalanceRequest request) => Task.FromResult<StatementBalanceDto?>(null);

        /// <inheritdoc/>
        public Task<ApiResult<ReconciliationRecordDto>> CompleteReconciliationAsync(CompleteReconciliationRequest request) => Task.FromResult(ApiResult<ReconciliationRecordDto>.Failure());

        /// <inheritdoc/>
        public Task<IReadOnlyList<ReconciliationRecordDto>?> GetReconciliationHistoryAsync(Guid accountId, int page = 1, int pageSize = 20) => Task.FromResult<IReadOnlyList<ReconciliationRecordDto>?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<TransactionDto>?> GetReconciliationTransactionsAsync(Guid reconciliationRecordId) => Task.FromResult<IReadOnlyList<TransactionDto>?>(null);

        /// <inheritdoc/>
        public Task<DataHealthReportDto?> GetDataHealthReportAsync(Guid? accountId = null) => Task.FromResult<DataHealthReportDto?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<DuplicateClusterDto>?> GetDuplicatesAsync(Guid? accountId = null) => Task.FromResult<IReadOnlyList<DuplicateClusterDto>?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<AmountOutlierDto>?> GetOutliersAsync(Guid? accountId = null) => Task.FromResult<IReadOnlyList<AmountOutlierDto>?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<DateGapDto>?> GetDateGapsAsync(Guid? accountId = null, int minGapDays = 7) => Task.FromResult<IReadOnlyList<DateGapDto>?>(null);

        /// <inheritdoc/>
        public Task<UncategorizedSummaryDto?> GetUncategorizedSummaryAsync() => Task.FromResult<UncategorizedSummaryDto?>(null);

        /// <inheritdoc/>
        public Task MergeDuplicatesAsync(MergeDuplicatesRequest request) => Task.CompletedTask;

        /// <inheritdoc/>
        public Task DismissOutlierAsync(Guid transactionId) => Task.CompletedTask;

        /// <inheritdoc/>
        public Task<MonthlyReflectionDto?> GetReflectionByMonthAsync(int year, int month) => Task.FromResult<MonthlyReflectionDto?>(null);

        /// <inheritdoc/>
        public Task<MonthlyReflectionDto?> CreateOrUpdateReflectionAsync(int year, int month, CreateOrUpdateMonthlyReflectionDto dto) => Task.FromResult<MonthlyReflectionDto?>(null);

        /// <inheritdoc/>
        public Task<MonthFinancialSummaryDto?> GetMonthFinancialSummaryAsync(int year, int month) => Task.FromResult<MonthFinancialSummaryDto?>(null);

        /// <inheritdoc/>
        public Task<bool> DeleteReflectionAsync(Guid reflectionId) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<bool> MarkKakeiboSetupCompleteAsync() => Task.FromResult(true);

        /// <inheritdoc/>
        public Task<KaizenGoalDto?> GetKaizenGoalByWeekAsync(DateOnly weekStart) => Task.FromResult<KaizenGoalDto?>(null);

        /// <inheritdoc/>
        public Task<KaizenGoalDto?> CreateKaizenGoalAsync(DateOnly weekStart, CreateKaizenGoalDto dto) => Task.FromResult<KaizenGoalDto?>(null);

        /// <inheritdoc/>
        public Task<KaizenGoalDto?> UpdateKaizenGoalAsync(Guid goalId, UpdateKaizenGoalDto dto) => Task.FromResult<KaizenGoalDto?>(null);

        /// <inheritdoc/>
        public Task<bool> DeleteKaizenGoalAsync(Guid goalId) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<IReadOnlyList<KaizenGoalDto>?> GetKaizenGoalsRangeAsync(DateOnly from, DateOnly to) => Task.FromResult<IReadOnlyList<KaizenGoalDto>?>(null);

        /// <inheritdoc />
        public Task<HeatmapDataResponse?> GetCalendarHeatmapAsync(int year, int month, CancellationToken ct = default)
            => Task.FromResult<HeatmapDataResponse?>(null);
    }
#pragma warning restore SA1648

    private sealed class StubReconciliationApiService : IReconciliationApiService
    {
        /// <summary>Gets or sets the result for <see cref="CreateManualMatchAsync"/>.</summary>
        public ReconciliationMatchDto? ManualMatchResult
        {
            get; set;
        }

        /// <inheritdoc/>
        public Task<ReconciliationMatchDto?> CreateManualMatchAsync(ManualMatchRequest request) => Task.FromResult(this.ManualMatchResult);

        /// <inheritdoc/>
        public Task<ReconciliationStatusDto?> GetStatusAsync(int year, int month, Guid? accountId = null) => Task.FromResult<ReconciliationStatusDto?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<ReconciliationMatchDto>> GetPendingMatchesAsync(Guid? accountId = null) => Task.FromResult<IReadOnlyList<ReconciliationMatchDto>>([]);

        /// <inheritdoc/>
        public Task<FindMatchesResult?> FindMatchesAsync(FindMatchesRequest request) => Task.FromResult<FindMatchesResult?>(null);

        /// <inheritdoc/>
        public Task<bool> AcceptMatchAsync(Guid matchId) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<bool> RejectMatchAsync(Guid matchId) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<int> BulkAcceptMatchesAsync(IReadOnlyList<Guid> matchIds) => Task.FromResult(0);

        /// <inheritdoc/>
        public Task<MatchingTolerancesDto?> GetTolerancesAsync() => Task.FromResult<MatchingTolerancesDto?>(null);

        /// <inheritdoc/>
        public Task<bool> UpdateTolerancesAsync(MatchingTolerancesDto tolerances) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<bool> UnlinkMatchAsync(Guid matchId) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<IReadOnlyList<LinkableInstanceDto>> GetLinkableInstancesAsync(Guid transactionId) => Task.FromResult<IReadOnlyList<LinkableInstanceDto>>([]);

#pragma warning disable SA1648 // inheritdoc should be used with inheriting class
        /// <inheritdoc/>
        public Task<TransactionDto?> MarkTransactionClearedAsync(MarkClearedRequest request) => Task.FromResult<TransactionDto?>(null);

        /// <inheritdoc/>
        public Task<TransactionDto?> MarkTransactionUnclearedAsync(MarkUnclearedRequest request) => Task.FromResult<TransactionDto?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<TransactionDto>?> BulkMarkTransactionsClearedAsync(BulkMarkClearedRequest request) => Task.FromResult<IReadOnlyList<TransactionDto>?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<TransactionDto>?> BulkMarkTransactionsUnclearedAsync(BulkMarkUnclearedRequest request) => Task.FromResult<IReadOnlyList<TransactionDto>?>(null);

        /// <inheritdoc/>
        public Task<StatementBalanceDto?> GetActiveStatementBalanceAsync(Guid accountId) => Task.FromResult<StatementBalanceDto?>(null);

        /// <inheritdoc/>
        public Task<ClearedBalanceDto?> GetClearedBalanceAsync(Guid accountId, DateOnly? upToDate = null) => Task.FromResult<ClearedBalanceDto?>(null);

        /// <inheritdoc/>
        public Task<StatementBalanceDto?> SetStatementBalanceAsync(SetStatementBalanceRequest request) => Task.FromResult<StatementBalanceDto?>(null);

        /// <inheritdoc/>
        public Task<ApiResult<ReconciliationRecordDto>> CompleteReconciliationAsync(CompleteReconciliationRequest request) => Task.FromResult(ApiResult<ReconciliationRecordDto>.Failure());

        /// <inheritdoc/>
        public Task<IReadOnlyList<ReconciliationRecordDto>?> GetReconciliationHistoryAsync(Guid accountId, int page = 1, int pageSize = 20) => Task.FromResult<IReadOnlyList<ReconciliationRecordDto>?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<TransactionDto>?> GetReconciliationTransactionsAsync(Guid reconciliationRecordId) => Task.FromResult<IReadOnlyList<TransactionDto>?>(null);

        /// <inheritdoc/>
        public Task<DataHealthReportDto?> GetDataHealthReportAsync(Guid? accountId = null) => Task.FromResult<DataHealthReportDto?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<DuplicateClusterDto>?> GetDuplicatesAsync(Guid? accountId = null) => Task.FromResult<IReadOnlyList<DuplicateClusterDto>?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<AmountOutlierDto>?> GetOutliersAsync(Guid? accountId = null) => Task.FromResult<IReadOnlyList<AmountOutlierDto>?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<DateGapDto>?> GetDateGapsAsync(Guid? accountId = null, int minGapDays = 7) => Task.FromResult<IReadOnlyList<DateGapDto>?>(null);

        /// <inheritdoc/>
        public Task<UncategorizedSummaryDto?> GetUncategorizedSummaryAsync() => Task.FromResult<UncategorizedSummaryDto?>(null);

        /// <inheritdoc/>
        public Task MergeDuplicatesAsync(MergeDuplicatesRequest request) => Task.CompletedTask;

        /// <inheritdoc/>
        public Task DismissOutlierAsync(Guid transactionId) => Task.CompletedTask;

        /// <inheritdoc/>
        public Task<MonthlyReflectionDto?> GetReflectionByMonthAsync(int year, int month) => Task.FromResult<MonthlyReflectionDto?>(null);

        /// <inheritdoc/>
        public Task<MonthlyReflectionDto?> CreateOrUpdateReflectionAsync(int year, int month, CreateOrUpdateMonthlyReflectionDto dto) => Task.FromResult<MonthlyReflectionDto?>(null);

        /// <inheritdoc/>
        public Task<MonthFinancialSummaryDto?> GetMonthFinancialSummaryAsync(int year, int month) => Task.FromResult<MonthFinancialSummaryDto?>(null);

        /// <inheritdoc/>
        public Task<bool> DeleteReflectionAsync(Guid reflectionId) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<bool> MarkKakeiboSetupCompleteAsync() => Task.FromResult(true);

        /// <inheritdoc/>
        public Task<KaizenGoalDto?> GetKaizenGoalByWeekAsync(DateOnly weekStart) => Task.FromResult<KaizenGoalDto?>(null);

        /// <inheritdoc/>
        public Task<KaizenGoalDto?> CreateKaizenGoalAsync(DateOnly weekStart, CreateKaizenGoalDto dto) => Task.FromResult<KaizenGoalDto?>(null);

        /// <inheritdoc/>
        public Task<KaizenGoalDto?> UpdateKaizenGoalAsync(Guid goalId, UpdateKaizenGoalDto dto) => Task.FromResult<KaizenGoalDto?>(null);

        /// <inheritdoc/>
        public Task<bool> DeleteKaizenGoalAsync(Guid goalId) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<IReadOnlyList<KaizenGoalDto>?> GetKaizenGoalsRangeAsync(DateOnly from, DateOnly to) => Task.FromResult<IReadOnlyList<KaizenGoalDto>?>(null);

        /// <inheritdoc />
        public Task<HeatmapDataResponse?> GetCalendarHeatmapAsync(int year, int month, CancellationToken ct = default)
            => Task.FromResult<HeatmapDataResponse?>(null);
    }
#pragma warning restore SA1648
}
