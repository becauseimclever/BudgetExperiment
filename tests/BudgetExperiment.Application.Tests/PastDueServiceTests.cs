// <copyright file="PastDueServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Settings;
using Moq;
using Xunit;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for <see cref="PastDueService"/>.
/// </summary>
public sealed class PastDueServiceTests
{
    private readonly Mock<IRecurringTransactionRepository> _recurringTransactionRepo;
    private readonly Mock<IRecurringTransferRepository> _recurringTransferRepo;
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IAccountRepository> _accountRepo;
    private readonly Mock<IRecurringTransactionRealizationService> _transactionRealizationService;
    private readonly Mock<IRecurringTransferRealizationService> _transferRealizationService;
    private readonly Mock<ICurrencyProvider> _currencyProvider;
    private readonly PastDueService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="PastDueServiceTests"/> class.
    /// </summary>
    public PastDueServiceTests()
    {
        this._recurringTransactionRepo = new Mock<IRecurringTransactionRepository>();
        this._recurringTransferRepo = new Mock<IRecurringTransferRepository>();
        this._transactionRepo = new Mock<ITransactionRepository>();
        this._accountRepo = new Mock<IAccountRepository>();
        this._transactionRealizationService = new Mock<IRecurringTransactionRealizationService>();
        this._transferRealizationService = new Mock<IRecurringTransferRealizationService>();
        this._currencyProvider = new Mock<ICurrencyProvider>();
        this._currencyProvider.Setup(x => x.GetCurrencyAsync(It.IsAny<CancellationToken>())).ReturnsAsync("USD");

        this._service = new PastDueService(
            this._recurringTransactionRepo.Object,
            this._recurringTransferRepo.Object,
            this._transactionRepo.Object,
            this._accountRepo.Object,
            this._transactionRealizationService.Object,
            this._transferRealizationService.Object,
            this._currencyProvider.Object,
            () => new DateOnly(2026, 1, 11)); // Fixed "today" for testing
    }

    /// <summary>
    /// GetPastDueItemsAsync returns empty when no recurring items exist.
    /// </summary>
    [Fact]
    public async Task GetPastDueItemsAsync_NoRecurringItems_ReturnsEmpty()
    {
        // Arrange
        this._recurringTransactionRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        this._recurringTransferRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await this._service.GetPastDueItemsAsync();

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Null(result.OldestDate);
    }

    /// <summary>
    /// GetPastDueItemsAsync returns past-due recurring transaction.
    /// </summary>
    [Fact]
    public async Task GetPastDueItemsAsync_RecurringTransactionPastDue_ReturnsItem()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var recurring = RecurringTransaction.Create(
            accountId,
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePattern.CreateMonthly(1, 5),
            new DateOnly(2026, 1, 5), // Started on Jan 5
            null);

        var account = Account.Create("Checking", AccountType.Checking);

        this._recurringTransactionRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([recurring]);
        this._recurringTransferRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        this._accountRepo.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        this._recurringTransactionRepo.Setup(r => r.GetExceptionAsync(recurring.Id, new DateOnly(2026, 1, 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransactionException?)null);
        this._transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(recurring.Id, new DateOnly(2026, 1, 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        // Act
        var result = await this._service.GetPastDueItemsAsync();

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(new DateOnly(2026, 1, 5), result.OldestDate);

        var item = result.Items[0];
        Assert.Equal(recurring.Id, item.Id);
        Assert.Equal("recurring-transaction", item.Type);
        Assert.Equal(new DateOnly(2026, 1, 5), item.InstanceDate);
        Assert.Equal(6, item.DaysPastDue); // Jan 11 - Jan 5 = 6 days
        Assert.Equal("Netflix", item.Description);
        Assert.Equal(-15.99m, item.Amount.Amount);
    }

    /// <summary>
    /// GetPastDueItemsAsync excludes skipped instances.
    /// </summary>
    [Fact]
    public async Task GetPastDueItemsAsync_SkippedInstance_ExcludesItem()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var recurring = RecurringTransaction.Create(
            accountId,
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePattern.CreateMonthly(1, 5),
            new DateOnly(2026, 1, 5),
            null);

        var exception = RecurringTransactionException.CreateSkipped(recurring.Id, new DateOnly(2026, 1, 5));

        this._recurringTransactionRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([recurring]);
        this._recurringTransferRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        this._recurringTransactionRepo.Setup(r => r.GetExceptionAsync(recurring.Id, new DateOnly(2026, 1, 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exception);

        // Act
        var result = await this._service.GetPastDueItemsAsync();

        // Assert
        Assert.Empty(result.Items);
    }

    /// <summary>
    /// GetPastDueItemsAsync excludes already realized instances.
    /// </summary>
    [Fact]
    public async Task GetPastDueItemsAsync_RealizedInstance_ExcludesItem()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var recurring = RecurringTransaction.Create(
            accountId,
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePattern.CreateMonthly(1, 5),
            new DateOnly(2026, 1, 5),
            null);

        var realizedTransaction = Transaction.CreateFromRecurring(
            accountId,
            MoneyValue.Create("USD", -15.99m),
            new DateOnly(2026, 1, 5),
            "Netflix",
            recurring.Id,
            new DateOnly(2026, 1, 5),
            null);

        this._recurringTransactionRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([recurring]);
        this._recurringTransferRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        this._recurringTransactionRepo.Setup(r => r.GetExceptionAsync(recurring.Id, new DateOnly(2026, 1, 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransactionException?)null);
        this._transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(recurring.Id, new DateOnly(2026, 1, 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(realizedTransaction);

        // Act
        var result = await this._service.GetPastDueItemsAsync();

        // Assert
        Assert.Empty(result.Items);
    }

    /// <summary>
    /// GetPastDueItemsAsync excludes future instances.
    /// </summary>
    [Fact]
    public async Task GetPastDueItemsAsync_FutureInstance_ExcludesItem()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var recurring = RecurringTransaction.Create(
            accountId,
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15), // Future date
            null);

        this._recurringTransactionRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([recurring]);
        this._recurringTransferRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await this._service.GetPastDueItemsAsync();

        // Assert
        Assert.Empty(result.Items);
    }

    /// <summary>
    /// GetPastDueItemsAsync returns past-due recurring transfer.
    /// </summary>
    [Fact]
    public async Task GetPastDueItemsAsync_RecurringTransferPastDue_ReturnsItem()
    {
        // Arrange
        var sourceAccountId = Guid.NewGuid();
        var destAccountId = Guid.NewGuid();
        var recurring = RecurringTransfer.Create(
            sourceAccountId,
            destAccountId,
            "Savings Transfer",
            MoneyValue.Create("USD", 500m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1),
            null);

        var sourceAccount = Account.Create("Checking", AccountType.Checking);
        var destAccount = Account.Create("Savings", AccountType.Savings);

        this._recurringTransactionRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        this._recurringTransferRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([recurring]);
        this._accountRepo.Setup(r => r.GetByIdAsync(sourceAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(destAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destAccount);
        this._recurringTransferRepo.Setup(r => r.GetExceptionAsync(recurring.Id, new DateOnly(2026, 1, 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransferException?)null);
        this._transactionRepo.Setup(r => r.GetByRecurringTransferInstanceAsync(recurring.Id, new DateOnly(2026, 1, 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await this._service.GetPastDueItemsAsync();

        // Assert
        Assert.Single(result.Items);
        var item = result.Items[0];
        Assert.Equal(recurring.Id, item.Id);
        Assert.Equal("recurring-transfer", item.Type);
        Assert.Equal(new DateOnly(2026, 1, 1), item.InstanceDate);
        Assert.Equal(10, item.DaysPastDue); // Jan 11 - Jan 1 = 10 days
        Assert.Equal("Savings Transfer", item.Description);
        Assert.Equal("Checking", item.SourceAccountName);
        Assert.Equal("Savings", item.DestinationAccountName);
    }

    /// <summary>
    /// GetPastDueItemsAsync respects 30-day lookback window.
    /// </summary>
    [Fact]
    public async Task GetPastDueItemsAsync_OlderThan30Days_ExcludesItem()
    {
        // Arrange - Create a recurring with start date more than 30 days before "today" (Jan 11)
        // and end date also more than 30 days before, so no occurrences are in the lookback window
        var accountId = Guid.NewGuid();
        var recurring = RecurringTransaction.Create(
            accountId,
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2025, 11, 1), // Started Nov 1
            new DateOnly(2025, 11, 30)); // Ended Nov 30 - no occurrences in Dec-Jan

        this._recurringTransactionRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([recurring]);
        this._recurringTransferRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await this._service.GetPastDueItemsAsync();

        // Assert
        Assert.Empty(result.Items);
    }

    /// <summary>
    /// GetPastDueItemsAsync filters by accountId when provided.
    /// </summary>
    [Fact]
    public async Task GetPastDueItemsAsync_WithAccountFilter_FiltersResults()
    {
        // Arrange
        var targetAccountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();

        var targetRecurring = RecurringTransaction.Create(
            targetAccountId,
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePattern.CreateMonthly(1, 5),
            new DateOnly(2026, 1, 5),
            null);

        var targetAccount = Account.Create("Checking", AccountType.Checking);

        this._recurringTransactionRepo.Setup(r => r.GetByAccountIdAsync(targetAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([targetRecurring]);
        this._recurringTransferRepo.Setup(r => r.GetByAccountIdAsync(targetAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        this._accountRepo.Setup(r => r.GetByIdAsync(targetAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetAccount);
        this._recurringTransactionRepo.Setup(r => r.GetExceptionAsync(targetRecurring.Id, new DateOnly(2026, 1, 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransactionException?)null);
        this._transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(targetRecurring.Id, new DateOnly(2026, 1, 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        // Act
        var result = await this._service.GetPastDueItemsAsync(targetAccountId);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(targetRecurring.Id, result.Items[0].Id);
    }

    /// <summary>
    /// GetPastDueItemsAsync calculates total amount correctly.
    /// </summary>
    [Fact]
    public async Task GetPastDueItemsAsync_MultipleItems_CalculatesTotalAmount()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        var recurring1 = RecurringTransaction.Create(
            accountId,
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePattern.CreateMonthly(1, 5),
            new DateOnly(2026, 1, 5),
            null);

        var recurring2 = RecurringTransaction.Create(
            accountId,
            "Gym",
            MoneyValue.Create("USD", -29.99m),
            RecurrencePattern.CreateMonthly(1, 8),
            new DateOnly(2026, 1, 8),
            null);

        var account = Account.Create("Checking", AccountType.Checking);

        this._recurringTransactionRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([recurring1, recurring2]);
        this._recurringTransferRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        this._accountRepo.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        this._recurringTransactionRepo.Setup(r => r.GetExceptionAsync(recurring1.Id, new DateOnly(2026, 1, 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransactionException?)null);
        this._recurringTransactionRepo.Setup(r => r.GetExceptionAsync(recurring2.Id, new DateOnly(2026, 1, 8), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransactionException?)null);
        this._transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(recurring1.Id, new DateOnly(2026, 1, 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        this._transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(recurring2.Id, new DateOnly(2026, 1, 8), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        // Act
        var result = await this._service.GetPastDueItemsAsync();

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.NotNull(result.TotalAmount);
        Assert.Equal(-45.98m, result.TotalAmount.Amount);
        Assert.Equal(new DateOnly(2026, 1, 5), result.OldestDate);
    }

    /// <summary>
    /// RealizeBatchAsync realizes mixed transaction and transfer items successfully.
    /// </summary>
    [Fact]
    public async Task RealizeBatchAsync_MixedItems_ReturnsAllSuccess()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transferId = Guid.NewGuid();
        var request = new BatchRealizeRequest
        {
            Items =
            [
                new BatchRealizeItemRequest { Id = transactionId, Type = "recurring-transaction", InstanceDate = new DateOnly(2026, 1, 5) },
                new BatchRealizeItemRequest { Id = transferId, Type = "recurring-transfer", InstanceDate = new DateOnly(2026, 1, 6) },
            ],
        };

        this._transactionRealizationService
            .Setup(s => s.RealizeInstanceAsync(
                transactionId,
                It.Is<RealizeRecurringTransactionRequest>(r => r.InstanceDate == new DateOnly(2026, 1, 5)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionDto());

        this._transferRealizationService
            .Setup(s => s.RealizeInstanceAsync(
                transferId,
                It.Is<RealizeRecurringTransferRequest>(r => r.InstanceDate == new DateOnly(2026, 1, 6)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransferResponse());

        // Act
        var result = await this._service.RealizeBatchAsync(request);

        // Assert
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Empty(result.Failures);
    }

    /// <summary>
    /// RealizeBatchAsync reports failure for unknown item type.
    /// </summary>
    [Fact]
    public async Task RealizeBatchAsync_UnknownType_ReturnsFailure()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var request = new BatchRealizeRequest
        {
            Items =
            [
                new BatchRealizeItemRequest { Id = itemId, Type = "unknown-type", InstanceDate = new DateOnly(2026, 1, 5) },
            ],
        };

        // Act
        var result = await this._service.RealizeBatchAsync(request);

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Single(result.Failures);
        Assert.Equal(itemId, result.Failures[0].Id);
        Assert.Contains("Unknown item type", result.Failures[0].Error);
    }

    /// <summary>
    /// RealizeBatchAsync handles partial failure when one item throws.
    /// </summary>
    [Fact]
    public async Task RealizeBatchAsync_PartialFailure_ReportsSuccessAndFailure()
    {
        // Arrange
        var successId = Guid.NewGuid();
        var failId = Guid.NewGuid();
        var request = new BatchRealizeRequest
        {
            Items =
            [
                new BatchRealizeItemRequest { Id = successId, Type = "recurring-transaction", InstanceDate = new DateOnly(2026, 1, 5) },
                new BatchRealizeItemRequest { Id = failId, Type = "recurring-transaction", InstanceDate = new DateOnly(2026, 1, 6) },
            ],
        };

        this._transactionRealizationService
            .Setup(s => s.RealizeInstanceAsync(
                successId,
                It.IsAny<RealizeRecurringTransactionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionDto());

        this._transactionRealizationService
            .Setup(s => s.RealizeInstanceAsync(
                failId,
                It.IsAny<RealizeRecurringTransactionRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Already realized"));

        // Act
        var result = await this._service.RealizeBatchAsync(request);

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Single(result.Failures);
        Assert.Equal(failId, result.Failures[0].Id);
        Assert.Equal("Already realized", result.Failures[0].Error);
    }
}
