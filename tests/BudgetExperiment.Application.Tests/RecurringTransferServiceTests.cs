// <copyright file="RecurringTransferServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Application.Services;
using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for RecurringTransferService.
/// </summary>
public class RecurringTransferServiceTests
{
    private readonly Mock<IRecurringTransferRepository> _repository;
    private readonly Mock<IAccountRepository> _accountRepo;
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly RecurringTransferService _service;
    private readonly Account _sourceAccount;
    private readonly Account _destAccount;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransferServiceTests"/> class.
    /// </summary>
    public RecurringTransferServiceTests()
    {
        this._repository = new Mock<IRecurringTransferRepository>();
        this._accountRepo = new Mock<IAccountRepository>();
        this._transactionRepo = new Mock<ITransactionRepository>();
        this._uow = new Mock<IUnitOfWork>();
        this._service = new RecurringTransferService(
            this._repository.Object,
            this._accountRepo.Object,
            this._transactionRepo.Object,
            this._uow.Object);

        this._sourceAccount = Account.Create("Checking", AccountType.Checking);
        this._destAccount = Account.Create("Savings", AccountType.Savings);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Dto_When_Found()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "Monthly Savings",
            MoneyValue.Create("USD", 500m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 2, 1));

        this._repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default)).ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destAccount.Id, default)).ReturnsAsync(this._destAccount);

        // Act
        var result = await this._service.GetByIdAsync(transfer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(transfer.Id, result.Id);
        Assert.Equal("Monthly Savings", result.Description);
        Assert.Equal(500m, result.Amount.Amount);
        Assert.Equal("Checking", result.SourceAccountName);
        Assert.Equal("Savings", result.DestinationAccountName);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        this._repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((RecurringTransfer?)null);

        // Act
        var result = await this._service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_Returns_All_Transfers()
    {
        // Arrange
        var transfer1 = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "Transfer A",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        var transfer2 = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "Transfer B",
            MoneyValue.Create("USD", 200m),
            RecurrencePattern.CreateWeekly(1, DayOfWeek.Friday),
            new DateOnly(2026, 1, 3));

        this._repository.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<RecurringTransfer> { transfer1, transfer2 });
        this._accountRepo.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<Account> { this._sourceAccount, this._destAccount });

        // Act
        var result = await this._service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Description == "Transfer A");
        Assert.Contains(result, r => r.Description == "Transfer B");
    }

    [Fact]
    public async Task CreateAsync_Creates_RecurringTransfer()
    {
        // Arrange
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default)).ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destAccount.Id, default)).ReturnsAsync(this._destAccount);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransferCreateDto
        {
            SourceAccountId = this._sourceAccount.Id,
            DestinationAccountId = this._destAccount.Id,
            Description = "Monthly Savings",
            Amount = new MoneyDto { Currency = "USD", Amount = 500m },
            Frequency = "Monthly",
            DayOfMonth = 1,
            StartDate = new DateOnly(2026, 2, 1),
        };

        // Act
        var result = await this._service.CreateAsync(dto);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Monthly Savings", result.Description);
        Assert.Equal(500m, result.Amount.Amount);
        Assert.Equal("Monthly", result.Frequency);
        Assert.Equal("Checking", result.SourceAccountName);
        Assert.Equal("Savings", result.DestinationAccountName);
        Assert.True(result.IsActive);

        this._repository.Verify(r => r.AddAsync(It.IsAny<RecurringTransfer>(), default), Times.Once);
        this._uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_SourceAccountNotFound()
    {
        // Arrange
        this._accountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Account?)null);

        var dto = new RecurringTransferCreateDto
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = this._destAccount.Id,
            Description = "Test",
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Frequency = "Monthly",
            DayOfMonth = 1,
            StartDate = new DateOnly(2026, 1, 1),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => this._service.CreateAsync(dto));
        Assert.Contains("Source account not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_DestinationAccountNotFound()
    {
        // Arrange
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default)).ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destAccount.Id, default)).ReturnsAsync((Account?)null);

        var dto = new RecurringTransferCreateDto
        {
            SourceAccountId = this._sourceAccount.Id,
            DestinationAccountId = this._destAccount.Id,
            Description = "Test",
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Frequency = "Monthly",
            DayOfMonth = 1,
            StartDate = new DateOnly(2026, 1, 1),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => this._service.CreateAsync(dto));
        Assert.Contains("Destination account not found", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_Updates_RecurringTransfer()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "Original",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        this._repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default)).ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destAccount.Id, default)).ReturnsAsync(this._destAccount);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransferUpdateDto
        {
            Description = "Updated",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Frequency = "Monthly",
            DayOfMonth = 15,
        };

        // Act
        var result = await this._service.UpdateAsync(transfer.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Description);
        Assert.Equal(200m, result.Amount.Amount);
        this._uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Null_When_NotFound()
    {
        // Arrange
        this._repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((RecurringTransfer?)null);

        var dto = new RecurringTransferUpdateDto
        {
            Description = "Updated",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Frequency = "Monthly",
            DayOfMonth = 15,
        };

        // Act
        var result = await this._service.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_Deletes_And_Returns_True()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "To Delete",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        this._repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await this._service.DeleteAsync(transfer.Id);

        // Assert
        Assert.True(result);
        this._repository.Verify(r => r.RemoveAsync(transfer, default), Times.Once);
        this._uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Returns_False_When_NotFound()
    {
        // Arrange
        this._repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((RecurringTransfer?)null);

        // Act
        var result = await this._service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
        this._repository.Verify(r => r.RemoveAsync(It.IsAny<RecurringTransfer>(), default), Times.Never);
    }

    [Fact]
    public async Task PauseAsync_Pauses_Transfer()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "To Pause",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        this._repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default)).ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destAccount.Id, default)).ReturnsAsync(this._destAccount);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await this._service.PauseAsync(transfer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        this._uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ResumeAsync_Resumes_Transfer()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "To Resume",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));
        transfer.Pause();

        this._repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default)).ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destAccount.Id, default)).ReturnsAsync(this._destAccount);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await this._service.ResumeAsync(transfer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsActive);
        this._uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task SkipNextAsync_Creates_Exception_And_Advances()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "To Skip",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));
        var originalNextOccurrence = transfer.NextOccurrence;

        this._repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default)).ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destAccount.Id, default)).ReturnsAsync(this._destAccount);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await this._service.SkipNextAsync(transfer.Id);

        // Assert
        Assert.NotNull(result);
        this._repository.Verify(r => r.AddExceptionAsync(
            It.Is<RecurringTransferException>(e => e.OriginalDate == originalNextOccurrence && e.ExceptionType == ExceptionType.Skipped),
            default), Times.Once);
        Assert.Equal(new DateOnly(2026, 2, 1), transfer.NextOccurrence);
        this._uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
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
    public async Task GetByAccountIdAsync_Returns_Transfers_For_Account()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "Account Transfer",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        this._repository.Setup(r => r.GetByAccountIdAsync(this._sourceAccount.Id, default))
            .ReturnsAsync(new List<RecurringTransfer> { transfer });
        this._accountRepo.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<Account> { this._sourceAccount, this._destAccount });

        // Act
        var result = await this._service.GetByAccountIdAsync(this._sourceAccount.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal("Account Transfer", result[0].Description);
    }

    [Fact]
    public async Task GetActiveAsync_Returns_Only_Active_Transfers()
    {
        // Arrange
        var active = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "Active",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        this._repository.Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransfer> { active });
        this._accountRepo.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<Account> { this._sourceAccount, this._destAccount });

        // Act
        var result = await this._service.GetActiveAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Active", result[0].Description);
        Assert.True(result[0].IsActive);
    }

    [Fact]
    public async Task UpdateFromDateAsync_Updates_Series_And_Removes_Future_Exceptions()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            this._sourceAccount.Id,
            this._destAccount.Id,
            "Original",
            MoneyValue.Create("USD", 100m),
            RecurrencePattern.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));
        var instanceDate = new DateOnly(2026, 3, 1);

        this._repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._sourceAccount.Id, default)).ReturnsAsync(this._sourceAccount);
        this._accountRepo.Setup(r => r.GetByIdAsync(this._destAccount.Id, default)).ReturnsAsync(this._destAccount);
        this._uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransferUpdateDto
        {
            Description = "Updated from March",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Frequency = "Monthly",
            DayOfMonth = 1,
        };

        // Act
        var result = await this._service.UpdateFromDateAsync(transfer.Id, instanceDate, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated from March", result.Description);
        Assert.Equal(200m, result.Amount.Amount);
        this._repository.Verify(r => r.RemoveExceptionsFromDateAsync(transfer.Id, instanceDate, default), Times.Once);
        this._uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
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
