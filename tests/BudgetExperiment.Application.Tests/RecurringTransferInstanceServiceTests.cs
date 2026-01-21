// <copyright file="RecurringTransferInstanceServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for RecurringTransferInstanceService.
/// </summary>
public class RecurringTransferInstanceServiceTests
{
    private readonly Mock<IRecurringTransferRepository> _repository;
    private readonly Mock<IAccountRepository> _accountRepo;
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly RecurringTransferInstanceService _service;
    private readonly Account _sourceAccount;
    private readonly Account _destAccount;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransferInstanceServiceTests"/> class.
    /// </summary>
    public RecurringTransferInstanceServiceTests()
    {
        this._repository = new Mock<IRecurringTransferRepository>();
        this._accountRepo = new Mock<IAccountRepository>();
        this._transactionRepo = new Mock<ITransactionRepository>();
        this._uow = new Mock<IUnitOfWork>();
        this._service = new RecurringTransferInstanceService(
            this._repository.Object,
            this._accountRepo.Object,
            this._transactionRepo.Object,
            this._uow.Object);

        this._sourceAccount = Account.Create("Checking", AccountType.Checking);
        this._destAccount = Account.Create("Savings", AccountType.Savings);
    }

    [Fact]
    public async Task SkipInstanceAsync_Creates_Skipped_Exception()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "Skip Instance",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));
        var instanceDate = new DateOnly(2026, 3, 1);

        this._repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        this._repository.Setup(r => r.GetExceptionAsync(transfer.Id, instanceDate, default)).ReturnsAsync((RecurringTransferException?)null);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await this._service.SkipInstanceAsync(transfer.Id, instanceDate);

        // Assert
        Assert.True(result);
        this._repository.Verify(r => r.AddExceptionAsync(
            It.Is<RecurringTransferException>(e => e.OriginalDate == instanceDate && e.ExceptionType == ExceptionType.Skipped),
            default), Times.Once);
    }

    [Fact]
    public async Task SkipInstanceAsync_Returns_False_When_Transfer_Not_Found()
    {
        // Arrange
        this._repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((RecurringTransfer?)null);

        // Act
        var result = await this._service.SkipInstanceAsync(Guid.NewGuid(), new DateOnly(2026, 1, 1));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ModifyInstanceAsync_Creates_Exception()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "Modify Instance",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));
        var instanceDate = new DateOnly(2026, 2, 1);

        this._repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        this._repository.Setup(r => r.GetExceptionAsync(transfer.Id, instanceDate, default)).ReturnsAsync((RecurringTransferException?)null);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default)).ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destAccount.Id, default)).ReturnsAsync(this._destAccount);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransferInstanceModifyDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Description = "Extra this month",
        };

        // Act
        var result = await this._service.ModifyInstanceAsync(transfer.Id, instanceDate, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200m, result.Amount.Amount);
        Assert.Equal("Extra this month", result.Description);
        Assert.True(result.IsModified);
        this._repository.Verify(r => r.AddExceptionAsync(
            It.Is<RecurringTransferException>(e => e.OriginalDate == instanceDate && e.ExceptionType == ExceptionType.Modified),
            default), Times.Once);
    }

    [Fact]
    public async Task ModifyInstanceAsync_Returns_Null_When_Transfer_Not_Found()
    {
        // Arrange
        this._repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((RecurringTransfer?)null);
        var dto = new RecurringTransferInstanceModifyDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
        };

        // Act
        var result = await this._service.ModifyInstanceAsync(Guid.NewGuid(), new DateOnly(2026, 1, 1), dto);

        // Assert
        Assert.Null(result);
    }
}
