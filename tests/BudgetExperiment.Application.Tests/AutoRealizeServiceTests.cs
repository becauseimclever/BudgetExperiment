// <copyright file="AutoRealizeServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for AutoRealizeService.
/// </summary>
public class AutoRealizeServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IRecurringTransactionRepository> _recurringRepo;
    private readonly Mock<IRecurringTransferRepository> _recurringTransferRepo;
    private readonly Mock<IAppSettingsRepository> _settingsRepo;
    private readonly Mock<IUnitOfWork> _unitOfWork;

    public AutoRealizeServiceTests()
    {
        _transactionRepo = new Mock<ITransactionRepository>();
        _recurringRepo = new Mock<IRecurringTransactionRepository>();
        _recurringTransferRepo = new Mock<IRecurringTransferRepository>();
        _settingsRepo = new Mock<IAppSettingsRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        // Default setup for settings - auto-realize disabled
        _settingsRepo
            .Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppSettings.CreateDefault());

        // Default setup for active recurring transactions - return empty list
        _recurringRepo
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());

        // Default setup for active recurring transfers - return empty list
        _recurringTransferRepo
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransfer>());

        // Default setup for recurring transactions by account id - return empty list
        _recurringRepo
            .Setup(r => r.GetByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());

        // Default setup for recurring transfers by account id - return empty list
        _recurringTransferRepo
            .Setup(r => r.GetByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransfer>());
    }

    private AutoRealizeService CreateService()
    {
        return new AutoRealizeService(
            _transactionRepo.Object,
            _recurringRepo.Object,
            _recurringTransferRepo.Object,
            _settingsRepo.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task AutoRealizePastDueItemsIfEnabledAsync_DoesNothing_WhenSettingDisabled()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastDueDate = today.AddDays(-5);

        var settings = AppSettings.CreateDefault();
        // AutoRealizePastDueItems is false by default
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var recurringTransaction = CreateTestRecurringTransaction(accountId, pastDueDate);
        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });

        var service = CreateService();

        // Act
        await service.AutoRealizePastDueItemsIfEnabledAsync(today, null, CancellationToken.None);

        // Assert - no transaction should be added and no repository methods called beyond settings
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AutoRealizePastDueItemsIfEnabledAsync_RealizesPastDueItems_WhenSettingEnabled()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastDueDate = today.AddDays(-5);

        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var recurringTransaction = CreateTestRecurringTransaction(accountId, pastDueDate);
        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });
        _recurringRepo.Setup(r => r.GetExceptionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransactionException?)null);
        _transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var service = CreateService();

        // Act
        await service.AutoRealizePastDueItemsIfEnabledAsync(today, null, CancellationToken.None);

        // Assert - transaction should be added
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutoRealizePastDueItemsIfEnabledAsync_SkipsAlreadyRealizedItems()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastDueDate = today.AddDays(-5);

        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var recurringTransaction = CreateTestRecurringTransaction(accountId, pastDueDate);
        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });
        _recurringRepo.Setup(r => r.GetExceptionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransactionException?)null);

        // Already realized
        var existingTransaction = Transaction.Create(accountId, MoneyValue.Create("USD", -50m), pastDueDate, "Already realized");
        _transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTransaction);

        var service = CreateService();

        // Act
        await service.AutoRealizePastDueItemsIfEnabledAsync(today, null, CancellationToken.None);

        // Assert - no new transaction should be added
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AutoRealizePastDueItemsIfEnabledAsync_SkipsSkippedItems()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastDueDate = today.AddDays(-5);

        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var recurringTransaction = CreateTestRecurringTransaction(accountId, pastDueDate);
        var skippedException = RecurringTransactionException.CreateSkipped(recurringTransaction.Id, pastDueDate);

        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });
        _recurringRepo.Setup(r => r.GetExceptionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(skippedException);
        _transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var service = CreateService();

        // Act
        await service.AutoRealizePastDueItemsIfEnabledAsync(today, null, CancellationToken.None);

        // Assert - no transaction should be added for skipped item
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AutoRealizePastDueItemsIfEnabledAsync_RespectsLookbackDays()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var withinLookback = today.AddDays(-10);

        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        settings.UpdatePastDueLookbackDays(15); // Set lookback to 15 days
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        // Create a recurring transaction that started 10 days ago (within 15-day lookback)
        var recurringWithinLookback = CreateTestRecurringTransaction(accountId, withinLookback);

        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringWithinLookback });
        _recurringRepo.Setup(r => r.GetExceptionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransactionException?)null);
        _transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var service = CreateService();

        // Act
        await service.AutoRealizePastDueItemsIfEnabledAsync(today, null, CancellationToken.None);

        // Assert - should be realized (within lookback)
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutoRealizePastDueItemsIfEnabledAsync_SkipsItemsOutsideLookback()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Set lookback to 5 days: [today-5, yesterday]
        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        settings.UpdatePastDueLookbackDays(5);
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        // Create a recurring transaction that started 10 days ago
        // Day of month = (today - 10).Day, which is outside the 5-day lookback
        var outsideLookback = today.AddDays(-10);
        var recurringOutsideLookback = CreateTestRecurringTransaction(accountId, outsideLookback);

        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringOutsideLookback });
        _recurringRepo.Setup(r => r.GetExceptionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransactionException?)null);
        _transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var service = CreateService();

        // Act
        await service.AutoRealizePastDueItemsIfEnabledAsync(today, null, CancellationToken.None);

        // Assert - should NOT be realized (occurrence on day (today-10) is outside 5-day lookback)
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AutoRealizePastDueItemsIfEnabledAsync_RealizesRecurringTransfers()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastDueDate = today.AddDays(-5);

        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var recurringTransfer = CreateTestRecurringTransfer(fromAccountId, toAccountId, pastDueDate);
        _recurringTransferRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransfer> { recurringTransfer });
        _recurringTransferRepo.Setup(r => r.GetExceptionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransferException?)null);
        _transactionRepo.Setup(r => r.GetByRecurringTransferInstanceAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());
        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        await service.AutoRealizePastDueItemsIfEnabledAsync(today, null, CancellationToken.None);

        // Assert - two transactions should be added (from and to)
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutoRealizePastDueItemsIfEnabledAsync_DoesNotRealizeToday()
    {
        // Arrange - recurring scheduled for today should NOT be auto-realized
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var recurringTransaction = CreateTestRecurringTransaction(accountId, today);
        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });

        var service = CreateService();

        // Act
        await service.AutoRealizePastDueItemsIfEnabledAsync(today, null, CancellationToken.None);

        // Assert - today's item should not be auto-realized
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AutoRealizePastDueItemsIfEnabledAsync_FiltersRecurringTransactionsByAccountId()
    {
        // Arrange
        var targetAccountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastDueDate = today.AddDays(-5);

        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var recurringTransaction = CreateTestRecurringTransaction(targetAccountId, pastDueDate);
        _recurringRepo.Setup(r => r.GetByAccountIdAsync(targetAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });
        _recurringRepo.Setup(r => r.GetExceptionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransactionException?)null);
        _transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var service = CreateService();

        // Act
        await service.AutoRealizePastDueItemsIfEnabledAsync(today, targetAccountId, CancellationToken.None);

        // Assert - should use account-specific repository method
        _recurringRepo.Verify(r => r.GetByAccountIdAsync(targetAccountId, It.IsAny<CancellationToken>()), Times.Once);
        _recurringRepo.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static RecurringTransaction CreateTestRecurringTransaction(Guid accountId, DateOnly startDate)
    {
        var amount = MoneyValue.Create("USD", -50.00m);
        var pattern = RecurrencePattern.CreateMonthly(1, startDate.Day);
        var recurring = RecurringTransaction.Create(accountId, "Test Recurring", amount, pattern, startDate);

        // Set the Id for testing
        var id = Guid.NewGuid();
        var idProperty = typeof(RecurringTransaction).GetProperty(nameof(RecurringTransaction.Id));
        idProperty?.SetValue(recurring, id);

        return recurring;
    }

    private static RecurringTransfer CreateTestRecurringTransfer(Guid fromAccountId, Guid toAccountId, DateOnly startDate)
    {
        var amount = MoneyValue.Create("USD", 100.00m);
        var pattern = RecurrencePattern.CreateMonthly(1, startDate.Day);
        var transfer = RecurringTransfer.Create(fromAccountId, toAccountId, "Test Transfer", amount, pattern, startDate);

        // Set the Id for testing
        var id = Guid.NewGuid();
        var idProperty = typeof(RecurringTransfer).GetProperty(nameof(RecurringTransfer.Id));
        idProperty?.SetValue(transfer, id);

        return transfer;
    }
}
