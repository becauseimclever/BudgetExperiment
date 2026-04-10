// <copyright file="RecurringTransferRealizationServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for RecurringTransferRealizationService.
/// </summary>
public class RecurringTransferRealizationServiceTests
{
    private readonly Mock<IRecurringTransferRepository> _repository;
    private readonly Mock<IAccountRepository> _accountRepo;
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly RecurringTransferRealizationService _service;
    private readonly Account _sourceAccount;
    private readonly Account _destAccount;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransferRealizationServiceTests"/> class.
    /// </summary>
    public RecurringTransferRealizationServiceTests()
    {
        _repository = new Mock<IRecurringTransferRepository>();
        _accountRepo = new Mock<IAccountRepository>();
        _transactionRepo = new Mock<ITransactionRepository>();
        _uow = new Mock<IUnitOfWork>();
        _service = new RecurringTransferRealizationService(
            _repository.Object,
            _accountRepo.Object,
            _transactionRepo.Object,
            _uow.Object);

        _sourceAccount = Account.Create("Checking", AccountType.Checking);
        _destAccount = Account.Create("Savings", AccountType.Savings);
    }

    [Fact]
    public async Task RealizeInstanceAsync_Creates_Transfer_Transactions()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            _sourceAccount.Id,
            _destAccount.Id,
            "Monthly Savings",
            MoneyValue.Create("USD", 500m),
            RecurrencePatternValue.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var instanceDate = new DateOnly(2026, 1, 15);
        var request = new RealizeRecurringTransferRequest
        {
            InstanceDate = instanceDate,
        };

        _repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        _repository.Setup(r => r.GetExceptionAsync(transfer.Id, instanceDate, default)).ReturnsAsync((RecurringTransferException?)null);
        _transactionRepo.Setup(r => r.GetByRecurringTransferInstanceAsync(transfer.Id, instanceDate, default)).ReturnsAsync([]);
        _accountRepo.Setup(r => r.GetByIdAsync(_sourceAccount.Id, default)).ReturnsAsync(_sourceAccount);
        _accountRepo.Setup(r => r.GetByIdAsync(_destAccount.Id, default)).ReturnsAsync(_destAccount);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _service.RealizeInstanceAsync(transfer.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_sourceAccount.Id, result.SourceAccountId);
        Assert.Equal(_destAccount.Id, result.DestinationAccountId);
        Assert.Equal(500m, result.Amount);
        Assert.Equal(instanceDate, result.Date);
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), default), Times.Exactly(2));
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RealizeInstanceAsync_Throws_When_Transfer_Not_Found()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((RecurringTransfer?)null);
        var request = new RealizeRecurringTransferRequest
        {
            InstanceDate = new DateOnly(2026, 1, 15),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.RealizeInstanceAsync(Guid.NewGuid(), request));
        Assert.Equal("Recurring transfer not found.", ex.Message);
    }

    [Fact]
    public async Task RealizeInstanceAsync_Throws_When_Already_Realized()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            _sourceAccount.Id,
            _destAccount.Id,
            "Monthly Savings",
            MoneyValue.Create("USD", 500m),
            RecurrencePatternValue.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var instanceDate = new DateOnly(2026, 1, 15);
        var existingTransactions = new List<Transaction>
        {
            TransactionFactory.CreateFromRecurringTransfer(
                _sourceAccount.Id,
                MoneyValue.Create("USD", -500m),
                instanceDate,
                "Monthly Savings",
                Guid.NewGuid(),
                TransferDirection.Source,
                transfer.Id,
                instanceDate),
        };

        var request = new RealizeRecurringTransferRequest
        {
            InstanceDate = instanceDate,
        };

        _repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        _transactionRepo.Setup(r => r.GetByRecurringTransferInstanceAsync(transfer.Id, instanceDate, default)).ReturnsAsync(existingTransactions);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.RealizeInstanceAsync(transfer.Id, request));
        Assert.Equal("This instance has already been realized.", ex.Message);
    }

    [Fact]
    public async Task RealizeInstanceAsync_Uses_Override_Values()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            _sourceAccount.Id,
            _destAccount.Id,
            "Monthly Savings",
            MoneyValue.Create("USD", 500m),
            RecurrencePatternValue.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var instanceDate = new DateOnly(2026, 1, 15);
        var actualDate = new DateOnly(2026, 1, 17);
        var request = new RealizeRecurringTransferRequest
        {
            InstanceDate = instanceDate,
            Date = actualDate,
            Amount = new MoneyDto { Currency = "USD", Amount = 600m },
        };

        _repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        _repository.Setup(r => r.GetExceptionAsync(transfer.Id, instanceDate, default)).ReturnsAsync((RecurringTransferException?)null);
        _transactionRepo.Setup(r => r.GetByRecurringTransferInstanceAsync(transfer.Id, instanceDate, default)).ReturnsAsync([]);
        _accountRepo.Setup(r => r.GetByIdAsync(_sourceAccount.Id, default)).ReturnsAsync(_sourceAccount);
        _accountRepo.Setup(r => r.GetByIdAsync(_destAccount.Id, default)).ReturnsAsync(_destAccount);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _service.RealizeInstanceAsync(transfer.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(actualDate, result.Date);
        Assert.Equal(600m, result.Amount);
    }

    [Fact]
    public async Task RealizeInstanceAsync_Applies_Exception_Modifications()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            _sourceAccount.Id,
            _destAccount.Id,
            "Monthly Savings",
            MoneyValue.Create("USD", 500m),
            RecurrencePatternValue.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var instanceDate = new DateOnly(2026, 1, 15);
        var modifiedDate = new DateOnly(2026, 1, 18);
        var exception = RecurringTransferException.CreateModified(
            transfer.Id,
            instanceDate,
            MoneyValue.Create("USD", 750m),
            null,
            modifiedDate);

        var request = new RealizeRecurringTransferRequest
        {
            InstanceDate = instanceDate,
        };

        _repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        _repository.Setup(r => r.GetExceptionAsync(transfer.Id, instanceDate, default)).ReturnsAsync(exception);
        _transactionRepo.Setup(r => r.GetByRecurringTransferInstanceAsync(transfer.Id, instanceDate, default)).ReturnsAsync([]);
        _accountRepo.Setup(r => r.GetByIdAsync(_sourceAccount.Id, default)).ReturnsAsync(_sourceAccount);
        _accountRepo.Setup(r => r.GetByIdAsync(_destAccount.Id, default)).ReturnsAsync(_destAccount);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _service.RealizeInstanceAsync(transfer.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(modifiedDate, result.Date);
        Assert.Equal(750m, result.Amount);
    }
}
