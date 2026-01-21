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
        this._repository = new Mock<IRecurringTransferRepository>();
        this._accountRepo = new Mock<IAccountRepository>();
        this._transactionRepo = new Mock<ITransactionRepository>();
        this._uow = new Mock<IUnitOfWork>();
        this._service = new RecurringTransferRealizationService(
            this._repository.Object,
            this._accountRepo.Object,
            this._transactionRepo.Object,
            this._uow.Object);

        this._sourceAccount = Account.Create("Checking", AccountType.Checking);
        this._destAccount = Account.Create("Savings", AccountType.Savings);
    }

    [Fact]
    public async Task RealizeInstanceAsync_Creates_Transfer_Transactions()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "Monthly Savings",
            MoneyValue.Create("USD", 500m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var instanceDate = new DateOnly(2026, 1, 15);
        var request = new RealizeRecurringTransferRequest
        {
            InstanceDate = instanceDate,
        };

        this._repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        this._repository.Setup(r => r.GetExceptionAsync(transfer.Id, instanceDate, default)).ReturnsAsync((RecurringTransferException?)null);
        this._transactionRepo.Setup(r => r.GetByRecurringTransferInstanceAsync(transfer.Id, instanceDate, default)).ReturnsAsync([]);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default)).ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destAccount.Id, default)).ReturnsAsync(this._destAccount);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await this._service.RealizeInstanceAsync(transfer.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(this._sourceAccount.Id, result.SourceAccountId);
        Assert.Equal(this._destAccount.Id, result.DestinationAccountId);
        Assert.Equal(500m, result.Amount);
        Assert.Equal(instanceDate, result.Date);
        this._transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), default), Times.Exactly(2));
        this._uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RealizeInstanceAsync_Throws_When_Transfer_Not_Found()
    {
        // Arrange
        this._repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((RecurringTransfer?)null);
        var request = new RealizeRecurringTransferRequest
        {
            InstanceDate = new DateOnly(2026, 1, 15),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => this._service.RealizeInstanceAsync(Guid.NewGuid(), request));
        Assert.Equal("Recurring transfer not found.", ex.Message);
    }

    [Fact]
    public async Task RealizeInstanceAsync_Throws_When_Already_Realized()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "Monthly Savings",
            MoneyValue.Create("USD", 500m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var instanceDate = new DateOnly(2026, 1, 15);
        var existingTransactions = new List<Transaction>
        {
            Transaction.CreateFromRecurringTransfer(
                this._sourceAccount.Id,
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

        this._repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        this._transactionRepo.Setup(r => r.GetByRecurringTransferInstanceAsync(transfer.Id, instanceDate, default)).ReturnsAsync(existingTransactions);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => this._service.RealizeInstanceAsync(transfer.Id, request));
        Assert.Equal("This instance has already been realized.", ex.Message);
    }

    [Fact]
    public async Task RealizeInstanceAsync_Uses_Override_Values()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "Monthly Savings",
            MoneyValue.Create("USD", 500m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var instanceDate = new DateOnly(2026, 1, 15);
        var actualDate = new DateOnly(2026, 1, 17);
        var request = new RealizeRecurringTransferRequest
        {
            InstanceDate = instanceDate,
            Date = actualDate,
            Amount = new MoneyDto { Currency = "USD", Amount = 600m },
        };

        this._repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        this._repository.Setup(r => r.GetExceptionAsync(transfer.Id, instanceDate, default)).ReturnsAsync((RecurringTransferException?)null);
        this._transactionRepo.Setup(r => r.GetByRecurringTransferInstanceAsync(transfer.Id, instanceDate, default)).ReturnsAsync([]);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default)).ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destAccount.Id, default)).ReturnsAsync(this._destAccount);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await this._service.RealizeInstanceAsync(transfer.Id, request);

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
            this._sourceAccount.Id,
            this._destAccount.Id,
            "Monthly Savings",
            MoneyValue.Create("USD", 500m),
            RecurrencePattern.CreateMonthly(1, 15),
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

        this._repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        this._repository.Setup(r => r.GetExceptionAsync(transfer.Id, instanceDate, default)).ReturnsAsync(exception);
        this._transactionRepo.Setup(r => r.GetByRecurringTransferInstanceAsync(transfer.Id, instanceDate, default)).ReturnsAsync([]);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default)).ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destAccount.Id, default)).ReturnsAsync(this._destAccount);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await this._service.RealizeInstanceAsync(transfer.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(modifiedDate, result.Date);
        Assert.Equal(750m, result.Amount);
    }
}
