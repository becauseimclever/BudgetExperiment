// <copyright file="ChatActionExecutorTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Settings;

using Shouldly;

using Xunit;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ChatActionExecutor"/>.
/// </summary>
public class ChatActionExecutorTests
{
    private readonly MockTransactionService _transactionService;
    private readonly MockTransferService _transferService;
    private readonly MockRecurringTransactionService _recurringTransactionService;
    private readonly MockRecurringTransferService _recurringTransferService;
    private readonly MockCurrencyProvider _currencyProvider;
    private readonly ChatActionExecutor _executor;

    public ChatActionExecutorTests()
    {
        _transactionService = new MockTransactionService();
        _transferService = new MockTransferService();
        _recurringTransactionService = new MockRecurringTransactionService();
        _recurringTransferService = new MockRecurringTransferService();
        _currencyProvider = new MockCurrencyProvider();

        _executor = new ChatActionExecutor(
            _transactionService,
            _transferService,
            _recurringTransactionService,
            _recurringTransferService,
            _currencyProvider);
    }

    [Fact]
    public async Task ExecuteActionAsync_Transaction_Creates_Transaction()
    {
        // Arrange
        var action = new CreateTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = -50m,
            Date = new DateOnly(2026, 3, 1),
            Description = "Groceries",
        };

        // Act
        var result = await _executor.ExecuteActionAsync(action);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActionType.ShouldBe(ChatActionType.CreateTransaction);
        result.CreatedEntityId.ShouldNotBeNull();
        result.Message.ShouldContain("Groceries");
        _transactionService.CreateCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteActionAsync_Transaction_Uses_Currency_From_Provider()
    {
        // Arrange
        _currencyProvider.Currency = "EUR";
        var action = new CreateTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = -25m,
            Date = new DateOnly(2026, 3, 1),
            Description = "Test",
        };

        // Act
        await _executor.ExecuteActionAsync(action);

        // Assert
        _transactionService.LastCreateDto.ShouldNotBeNull();
        _transactionService.LastCreateDto!.Amount.Currency.ShouldBe("EUR");
    }

    [Fact]
    public async Task ExecuteActionAsync_Transfer_Creates_Transfer()
    {
        // Arrange
        var action = new CreateTransferAction
        {
            FromAccountId = Guid.NewGuid(),
            FromAccountName = "Checking",
            ToAccountId = Guid.NewGuid(),
            ToAccountName = "Savings",
            Amount = 100m,
            Date = new DateOnly(2026, 3, 1),
            Description = "Monthly savings",
        };

        // Act
        var result = await _executor.ExecuteActionAsync(action);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActionType.ShouldBe(ChatActionType.CreateTransfer);
        result.CreatedEntityId.ShouldNotBeNull();
        result.Message.ShouldContain("Checking");
        result.Message.ShouldContain("Savings");
        _transferService.CreateCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteActionAsync_RecurringTransaction_Creates_RecurringTransaction()
    {
        // Arrange
        var action = new CreateRecurringTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = -75m,
            Description = "Netflix",
            Recurrence = RecurrencePatternValue.CreateMonthly(1, 15),
            StartDate = new DateOnly(2026, 3, 15),
        };

        // Act
        var result = await _executor.ExecuteActionAsync(action);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActionType.ShouldBe(ChatActionType.CreateRecurringTransaction);
        result.CreatedEntityId.ShouldNotBeNull();
        result.Message.ShouldContain("Netflix");
        _recurringTransactionService.CreateCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteActionAsync_RecurringTransfer_Creates_RecurringTransfer()
    {
        // Arrange
        var action = new CreateRecurringTransferAction
        {
            FromAccountId = Guid.NewGuid(),
            FromAccountName = "Checking",
            ToAccountId = Guid.NewGuid(),
            ToAccountName = "Savings",
            Amount = 200m,
            Description = "Bi-weekly savings",
            Recurrence = RecurrencePatternValue.CreateBiWeekly(DayOfWeek.Friday),
            StartDate = new DateOnly(2026, 3, 1),
        };

        // Act
        var result = await _executor.ExecuteActionAsync(action);

        // Assert
        result.Success.ShouldBeTrue();
        result.ActionType.ShouldBe(ChatActionType.CreateRecurringTransfer);
        result.CreatedEntityId.ShouldNotBeNull();
        result.Message.ShouldContain("Bi-weekly savings");
        _recurringTransferService.CreateCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteActionAsync_Clarification_Returns_Failure()
    {
        // Arrange
        var action = new ClarificationNeededAction
        {
            Question = "Which account?",
            FieldName = "AccountId",
        };

        // Act
        var result = await _executor.ExecuteActionAsync(action);

        // Assert
        result.Success.ShouldBeFalse();
        result.ActionType.ShouldBe(ChatActionType.ClarificationNeeded);
        result.ErrorMessage!.ShouldContain("clarification");
    }

    [Fact]
    public async Task ExecuteActionAsync_RecurringTransfer_NullDescription_UsesDefault()
    {
        // Arrange
        var action = new CreateRecurringTransferAction
        {
            FromAccountId = Guid.NewGuid(),
            FromAccountName = "Checking",
            ToAccountId = Guid.NewGuid(),
            ToAccountName = "Savings",
            Amount = 100m,
            Description = null,
            Recurrence = RecurrencePatternValue.CreateMonthly(1, 1),
            StartDate = new DateOnly(2026, 3, 1),
        };

        // Act
        var result = await _executor.ExecuteActionAsync(action);

        // Assert
        result.Success.ShouldBeTrue();
        _recurringTransferService.LastCreateDto.ShouldNotBeNull();
        _recurringTransferService.LastCreateDto!.Description.ShouldBe("Recurring transfer");
    }

    [Fact]
    public async Task ExecuteActionAsync_Transaction_Maps_All_Properties()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var action = new CreateTransactionAction
        {
            AccountId = accountId,
            AccountName = "Checking",
            Amount = -99.99m,
            Date = new DateOnly(2026, 6, 15),
            Description = "Amazon order",
            CategoryId = categoryId,
        };

        // Act
        await _executor.ExecuteActionAsync(action);

        // Assert
        var dto = _transactionService.LastCreateDto!;
        dto.AccountId.ShouldBe(accountId);
        dto.Amount.Amount.ShouldBe(-99.99m);
        dto.Amount.Currency.ShouldBe("USD");
        dto.Date.ShouldBe(new DateOnly(2026, 6, 15));
        dto.Description.ShouldBe("Amazon order");
        dto.CategoryId.ShouldBe(categoryId);
    }

    private sealed class MockTransactionService : ITransactionService
    {
        public bool CreateCalled
        {
            get; private set;
        }

        public TransactionCreateDto? LastCreateDto
        {
            get; private set;
        }

        public Task<TransactionDto> CreateAsync(TransactionCreateDto dto, CancellationToken cancellationToken = default)
        {
            CreateCalled = true;
            LastCreateDto = dto;
            return Task.FromResult(new TransactionDto
            {
                Id = Guid.NewGuid(),
                AccountId = dto.AccountId,
                Amount = dto.Amount,
                Date = dto.Date,
                Description = dto.Description,
            });
        }

        public Task<TransactionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransactionDto?>(null);

        public Task<IReadOnlyList<TransactionDto>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null, KakeiboCategory? kakeiboCategory = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TransactionDto>>(new List<TransactionDto>());

        public Task<TransactionDto?> UpdateAsync(Guid id, TransactionUpdateDto dto, string? expectedVersion = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransactionDto?>(null);

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<TransactionDto?> UpdateLocationAsync(Guid id, TransactionLocationUpdateDto dto, string? expectedVersion = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransactionDto?>(null);

        public Task<TransactionDto?> UpdateCategoryAsync(Guid id, TransactionCategoryUpdateDto dto, string? expectedVersion = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransactionDto?>(null);

        public Task<bool> ClearLocationAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<int> ClearAllLocationDataAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class MockTransferService : ITransferService
    {
        public bool CreateCalled
        {
            get; private set;
        }

        public Task<TransferResponse> CreateAsync(CreateTransferRequest request, CancellationToken cancellationToken = default)
        {
            CreateCalled = true;
            return Task.FromResult(new TransferResponse
            {
                TransferId = Guid.NewGuid(),
                SourceAccountId = request.SourceAccountId,
                DestinationAccountId = request.DestinationAccountId,
                Amount = request.Amount,
                Currency = request.Currency,
                Date = request.Date,
            });
        }

        public Task<TransferResponse?> GetByIdAsync(Guid transferId, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransferResponse?>(null);

        public Task<TransferListPageResponse> ListAsync(Guid? accountId = null, DateOnly? fromDate = null, DateOnly? toDate = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
            Task.FromResult(new TransferListPageResponse());

        public Task<TransferResponse?> UpdateAsync(Guid transferId, UpdateTransferRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransferResponse?>(null);

        public Task<bool> DeleteAsync(Guid transferId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class MockRecurringTransactionService : IRecurringTransactionService
    {
        public bool CreateCalled
        {
            get; private set;
        }

        public Task<RecurringTransactionDto> CreateAsync(RecurringTransactionCreateDto dto, CancellationToken cancellationToken = default)
        {
            CreateCalled = true;
            return Task.FromResult(new RecurringTransactionDto
            {
                Id = Guid.NewGuid(),
                AccountId = dto.AccountId,
                AccountName = "Test Account",
                Description = dto.Description,
                Amount = dto.Amount,
                Frequency = dto.Frequency,
                StartDate = dto.StartDate,
            });
        }

        public Task<RecurringTransactionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransactionDto?>(null);

        public Task<IReadOnlyList<RecurringTransactionDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RecurringTransactionDto>>(new List<RecurringTransactionDto>());

        public Task<IReadOnlyList<RecurringTransactionDto>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RecurringTransactionDto>>(new List<RecurringTransactionDto>());

        public Task<RecurringTransactionDto?> UpdateAsync(Guid id, RecurringTransactionUpdateDto dto, string? expectedVersion = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransactionDto?>(null);

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<RecurringTransactionDto?> PauseAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransactionDto?>(null);

        public Task<RecurringTransactionDto?> ResumeAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransactionDto?>(null);

        public Task<RecurringTransactionDto?> SkipNextAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransactionDto?>(null);

        public Task<RecurringTransactionDto?> UpdateFromDateAsync(Guid id, DateOnly instanceDate, RecurringTransactionUpdateDto dto, string? expectedVersion = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransactionDto?>(null);

        public Task<ImportPatternsDto?> GetImportPatternsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ImportPatternsDto?>(null);

        public Task<ImportPatternsDto?> UpdateImportPatternsAsync(Guid id, ImportPatternsDto dto, CancellationToken cancellationToken = default) =>
            Task.FromResult<ImportPatternsDto?>(null);
    }

    private sealed class MockRecurringTransferService : IRecurringTransferService
    {
        public bool CreateCalled
        {
            get; private set;
        }

        public RecurringTransferCreateDto? LastCreateDto
        {
            get; private set;
        }

        public Task<RecurringTransferDto> CreateAsync(RecurringTransferCreateDto dto, CancellationToken cancellationToken = default)
        {
            CreateCalled = true;
            LastCreateDto = dto;
            return Task.FromResult(new RecurringTransferDto
            {
                Id = Guid.NewGuid(),
                SourceAccountId = dto.SourceAccountId,
                SourceAccountName = "Source Account",
                DestinationAccountId = dto.DestinationAccountId,
                DestinationAccountName = "Dest Account",
                Description = dto.Description,
                Amount = dto.Amount,
                Frequency = dto.Frequency,
                StartDate = dto.StartDate,
            });
        }

        public Task<RecurringTransferDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransferDto?>(null);

        public Task<IReadOnlyList<RecurringTransferDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RecurringTransferDto>>(new List<RecurringTransferDto>());

        public Task<IReadOnlyList<RecurringTransferDto>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RecurringTransferDto>>(new List<RecurringTransferDto>());

        public Task<IReadOnlyList<RecurringTransferDto>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RecurringTransferDto>>(new List<RecurringTransferDto>());

        public Task<RecurringTransferDto?> UpdateAsync(Guid id, RecurringTransferUpdateDto dto, string? expectedVersion = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransferDto?>(null);

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<RecurringTransferDto?> PauseAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransferDto?>(null);

        public Task<RecurringTransferDto?> ResumeAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransferDto?>(null);

        public Task<RecurringTransferDto?> SkipNextAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransferDto?>(null);

        public Task<RecurringTransferDto?> UpdateFromDateAsync(Guid id, DateOnly instanceDate, RecurringTransferUpdateDto dto, string? expectedVersion = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransferDto?>(null);
    }

    private sealed class MockCurrencyProvider : ICurrencyProvider
    {
        public string Currency { get; set; } = "USD";

        public Task<string> GetCurrencyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Currency);
    }
}
