// <copyright file="RecurringTransactionRealizationServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for RecurringTransactionRealizationService.
/// </summary>
public class RecurringTransactionRealizationServiceTests
{
    private readonly Mock<IRecurringTransactionRepository> _repository;
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly RecurringTransactionRealizationService _service;
    private readonly Account _account;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransactionRealizationServiceTests"/> class.
    /// </summary>
    public RecurringTransactionRealizationServiceTests()
    {
        this._repository = new Mock<IRecurringTransactionRepository>();
        this._transactionRepo = new Mock<ITransactionRepository>();
        this._uow = new Mock<IUnitOfWork>();
        this._service = new RecurringTransactionRealizationService(
            this._repository.Object,
            this._transactionRepo.Object,
            this._uow.Object);

        this._account = Account.Create("Checking", AccountType.Checking);
    }

    [Fact]
    public async Task RealizeInstanceAsync_Creates_Transaction_From_Recurring()
    {
        // Arrange
        var recurring = RecurringTransaction.Create(
            this._account.Id,
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var instanceDate = new DateOnly(2026, 1, 15);
        var request = new RealizeRecurringTransactionRequest
        {
            InstanceDate = instanceDate,
        };

        this._repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        this._repository.Setup(r => r.GetExceptionAsync(recurring.Id, instanceDate, default)).ReturnsAsync((RecurringTransactionException?)null);
        this._transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(recurring.Id, instanceDate, default)).ReturnsAsync((Transaction?)null);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await this._service.RealizeInstanceAsync(recurring.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(this._account.Id, result.AccountId);
        Assert.Equal(-15.99m, result.Amount.Amount);
        Assert.Equal(instanceDate, result.Date);
        Assert.Equal("Netflix", result.Description);
        Assert.Equal(recurring.Id, result.RecurringTransactionId);
        Assert.Equal(instanceDate, result.RecurringInstanceDate);
        this._transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), default), Times.Once);
        this._uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RealizeInstanceAsync_Throws_When_Recurring_Not_Found()
    {
        // Arrange
        this._repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((RecurringTransaction?)null);
        var request = new RealizeRecurringTransactionRequest
        {
            InstanceDate = new DateOnly(2026, 1, 15),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => this._service.RealizeInstanceAsync(Guid.NewGuid(), request));
        Assert.Equal("Recurring transaction not found.", ex.Message);
    }

    [Fact]
    public async Task RealizeInstanceAsync_Throws_When_Already_Realized()
    {
        // Arrange
        var recurring = RecurringTransaction.Create(
            this._account.Id,
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var instanceDate = new DateOnly(2026, 1, 15);
        var existingTransaction = Transaction.CreateFromRecurring(
            this._account.Id,
            MoneyValue.Create("USD", -15.99m),
            instanceDate,
            "Netflix",
            recurring.Id,
            instanceDate);

        var request = new RealizeRecurringTransactionRequest
        {
            InstanceDate = instanceDate,
        };

        this._repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        this._transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(recurring.Id, instanceDate, default)).ReturnsAsync(existingTransaction);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => this._service.RealizeInstanceAsync(recurring.Id, request));
        Assert.Equal("This instance has already been realized.", ex.Message);
    }

    [Fact]
    public async Task RealizeInstanceAsync_Uses_Override_Values()
    {
        // Arrange
        var recurring = RecurringTransaction.Create(
            this._account.Id,
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var instanceDate = new DateOnly(2026, 1, 15);
        var actualDate = new DateOnly(2026, 1, 17);
        var request = new RealizeRecurringTransactionRequest
        {
            InstanceDate = instanceDate,
            Date = actualDate,
            Amount = new MoneyDto { Currency = "USD", Amount = -19.99m },
            Description = "Netflix Premium",
        };

        this._repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        this._repository.Setup(r => r.GetExceptionAsync(recurring.Id, instanceDate, default)).ReturnsAsync((RecurringTransactionException?)null);
        this._transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(recurring.Id, instanceDate, default)).ReturnsAsync((Transaction?)null);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await this._service.RealizeInstanceAsync(recurring.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(actualDate, result.Date);
        Assert.Equal(-19.99m, result.Amount.Amount);
        Assert.Equal("Netflix Premium", result.Description);
        Assert.Equal(instanceDate, result.RecurringInstanceDate);
    }

    [Fact]
    public async Task RealizeInstanceAsync_Applies_Exception_Modifications()
    {
        // Arrange
        var recurring = RecurringTransaction.Create(
            this._account.Id,
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var instanceDate = new DateOnly(2026, 1, 15);
        var modifiedDate = new DateOnly(2026, 1, 18);
        var exception = RecurringTransactionException.CreateModified(
            recurring.Id,
            instanceDate,
            MoneyValue.Create("USD", -20.00m),
            "Netflix Premium",
            modifiedDate);

        var request = new RealizeRecurringTransactionRequest
        {
            InstanceDate = instanceDate,
        };

        this._repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        this._repository.Setup(r => r.GetExceptionAsync(recurring.Id, instanceDate, default)).ReturnsAsync(exception);
        this._transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(recurring.Id, instanceDate, default)).ReturnsAsync((Transaction?)null);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await this._service.RealizeInstanceAsync(recurring.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(modifiedDate, result.Date);
        Assert.Equal(-20.00m, result.Amount.Amount);
        Assert.Equal("Netflix Premium", result.Description);
    }

    [Fact]
    public async Task RealizeInstanceAsync_Request_Overrides_Exception()
    {
        // Arrange
        var recurring = RecurringTransaction.Create(
            this._account.Id,
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            RecurrencePattern.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 15));

        var instanceDate = new DateOnly(2026, 1, 15);
        var exceptionDate = new DateOnly(2026, 1, 18);
        var exception = RecurringTransactionException.CreateModified(
            recurring.Id,
            instanceDate,
            MoneyValue.Create("USD", -20.00m),
            "Netflix Premium",
            exceptionDate);

        var requestDate = new DateOnly(2026, 1, 20);
        var request = new RealizeRecurringTransactionRequest
        {
            InstanceDate = instanceDate,
            Date = requestDate,
            Amount = new MoneyDto { Currency = "USD", Amount = -25.00m },
        };

        this._repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        this._repository.Setup(r => r.GetExceptionAsync(recurring.Id, instanceDate, default)).ReturnsAsync(exception);
        this._transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(recurring.Id, instanceDate, default)).ReturnsAsync((Transaction?)null);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await this._service.RealizeInstanceAsync(recurring.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(requestDate, result.Date);
        Assert.Equal(-25.00m, result.Amount.Amount);

        // Description from exception since request didn't override it
        Assert.Equal("Netflix Premium", result.Description);
    }
}
