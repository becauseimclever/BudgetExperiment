// <copyright file="RecurringTransferServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

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
        _repository = new Mock<IRecurringTransferRepository>();
        _accountRepo = new Mock<IAccountRepository>();
        _transactionRepo = new Mock<ITransactionRepository>();
        _uow = new Mock<IUnitOfWork>();
        _service = new RecurringTransferService(
            _repository.Object,
            _accountRepo.Object,
            _transactionRepo.Object,
            _uow.Object);

        _sourceAccount = Account.Create("Checking", AccountType.Checking);
        _destAccount = Account.Create("Savings", AccountType.Savings);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Dto_When_Found()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            _sourceAccount.Id,
            _destAccount.Id,
            "Monthly Savings",
            MoneyValue.Create("USD", 500m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            new DateOnly(2026, 2, 1));

        _repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        _accountRepo.Setup(r => r.GetByIdAsync(_sourceAccount.Id, default)).ReturnsAsync(_sourceAccount);
        _accountRepo.Setup(r => r.GetByIdAsync(_destAccount.Id, default)).ReturnsAsync(_destAccount);

        // Act
        var result = await _service.GetByIdAsync(transfer.Id);

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
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((RecurringTransfer?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_Returns_All_Transfers()
    {
        // Arrange
        var transfer1 = RecurringTransfer.Create(
            _sourceAccount.Id,
            _destAccount.Id,
            "Transfer A",
            MoneyValue.Create("USD", 100m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        var transfer2 = RecurringTransfer.Create(
            _sourceAccount.Id,
            _destAccount.Id,
            "Transfer B",
            MoneyValue.Create("USD", 200m),
            RecurrencePatternValue.CreateWeekly(1, DayOfWeek.Friday),
            new DateOnly(2026, 1, 3));

        _repository.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<RecurringTransfer> { transfer1, transfer2 });
        _accountRepo.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<Account> { _sourceAccount, _destAccount });

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Description == "Transfer A");
        Assert.Contains(result, r => r.Description == "Transfer B");
    }

    [Fact]
    public async Task CreateAsync_Creates_RecurringTransfer()
    {
        // Arrange
        _accountRepo.Setup(r => r.GetByIdAsync(_sourceAccount.Id, default)).ReturnsAsync(_sourceAccount);
        _accountRepo.Setup(r => r.GetByIdAsync(_destAccount.Id, default)).ReturnsAsync(_destAccount);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransferCreateDto
        {
            SourceAccountId = _sourceAccount.Id,
            DestinationAccountId = _destAccount.Id,
            Description = "Monthly Savings",
            Amount = new MoneyDto { Currency = "USD", Amount = 500m },
            Frequency = "Monthly",
            DayOfMonth = 1,
            StartDate = new DateOnly(2026, 2, 1),
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Monthly Savings", result.Description);
        Assert.Equal(500m, result.Amount.Amount);
        Assert.Equal("Monthly", result.Frequency);
        Assert.Equal("Checking", result.SourceAccountName);
        Assert.Equal("Savings", result.DestinationAccountName);
        Assert.True(result.IsActive);

        _repository.Verify(r => r.AddAsync(It.IsAny<RecurringTransfer>(), default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_SourceAccountNotFound()
    {
        // Arrange
        _accountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Account?)null);

        var dto = new RecurringTransferCreateDto
        {
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = _destAccount.Id,
            Description = "Test",
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Frequency = "Monthly",
            DayOfMonth = 1,
            StartDate = new DateOnly(2026, 1, 1),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(dto));
        Assert.Contains("Source account not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_DestinationAccountNotFound()
    {
        // Arrange
        _accountRepo.Setup(r => r.GetByIdAsync(_sourceAccount.Id, default)).ReturnsAsync(_sourceAccount);
        _accountRepo.Setup(r => r.GetByIdAsync(_destAccount.Id, default)).ReturnsAsync((Account?)null);

        var dto = new RecurringTransferCreateDto
        {
            SourceAccountId = _sourceAccount.Id,
            DestinationAccountId = _destAccount.Id,
            Description = "Test",
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Frequency = "Monthly",
            DayOfMonth = 1,
            StartDate = new DateOnly(2026, 1, 1),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(dto));
        Assert.Contains("Destination account not found", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_Updates_RecurringTransfer()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            _sourceAccount.Id,
            _destAccount.Id,
            "Original",
            MoneyValue.Create("USD", 100m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        _repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        _accountRepo.Setup(r => r.GetByIdAsync(_sourceAccount.Id, default)).ReturnsAsync(_sourceAccount);
        _accountRepo.Setup(r => r.GetByIdAsync(_destAccount.Id, default)).ReturnsAsync(_destAccount);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransferUpdateDto
        {
            Description = "Updated",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Frequency = "Monthly",
            DayOfMonth = 15,
        };

        // Act
        var result = await _service.UpdateAsync(transfer.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Description);
        Assert.Equal(200m, result.Amount.Amount);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Null_When_NotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((RecurringTransfer?)null);

        var dto = new RecurringTransferUpdateDto
        {
            Description = "Updated",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Frequency = "Monthly",
            DayOfMonth = 15,
        };

        // Act
        var result = await _service.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_Deletes_And_Returns_True()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            _sourceAccount.Id,
            _destAccount.Id,
            "To Delete",
            MoneyValue.Create("USD", 100m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        _repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(transfer.Id);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.RemoveAsync(transfer, default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Returns_False_When_NotFound()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((RecurringTransfer?)null);

        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
        _repository.Verify(r => r.RemoveAsync(It.IsAny<RecurringTransfer>(), default), Times.Never);
    }

    [Fact]
    public async Task PauseAsync_Pauses_Transfer()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            _sourceAccount.Id,
            _destAccount.Id,
            "To Pause",
            MoneyValue.Create("USD", 100m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        _repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        _accountRepo.Setup(r => r.GetByIdAsync(_sourceAccount.Id, default)).ReturnsAsync(_sourceAccount);
        _accountRepo.Setup(r => r.GetByIdAsync(_destAccount.Id, default)).ReturnsAsync(_destAccount);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _service.PauseAsync(transfer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ResumeAsync_Resumes_Transfer()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            _sourceAccount.Id,
            _destAccount.Id,
            "To Resume",
            MoneyValue.Create("USD", 100m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));
        transfer.Pause();

        _repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        _accountRepo.Setup(r => r.GetByIdAsync(_sourceAccount.Id, default)).ReturnsAsync(_sourceAccount);
        _accountRepo.Setup(r => r.GetByIdAsync(_destAccount.Id, default)).ReturnsAsync(_destAccount);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _service.ResumeAsync(transfer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsActive);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task SkipNextAsync_Creates_Exception_And_Advances()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            _sourceAccount.Id,
            _destAccount.Id,
            "To Skip",
            MoneyValue.Create("USD", 100m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));
        var originalNextOccurrence = transfer.NextOccurrence;

        _repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        _accountRepo.Setup(r => r.GetByIdAsync(_sourceAccount.Id, default)).ReturnsAsync(_sourceAccount);
        _accountRepo.Setup(r => r.GetByIdAsync(_destAccount.Id, default)).ReturnsAsync(_destAccount);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _service.SkipNextAsync(transfer.Id);

        // Assert
        Assert.NotNull(result);
        _repository.Verify(
            r => r.AddExceptionAsync(
            It.Is<RecurringTransferException>(e => e.OriginalDate == originalNextOccurrence && e.ExceptionType == ExceptionType.Skipped),
            default),
            Times.Once);
        Assert.Equal(new DateOnly(2026, 2, 1), transfer.NextOccurrence);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetByAccountIdAsync_Returns_Transfers_For_Account()
    {
        // Arrange
        var transfer = RecurringTransfer.Create(
            _sourceAccount.Id,
            _destAccount.Id,
            "Account Transfer",
            MoneyValue.Create("USD", 100m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        _repository.Setup(r => r.GetByAccountIdAsync(_sourceAccount.Id, default))
            .ReturnsAsync(new List<RecurringTransfer> { transfer });
        _accountRepo.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<Account> { _sourceAccount, _destAccount });

        // Act
        var result = await _service.GetByAccountIdAsync(_sourceAccount.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal("Account Transfer", result[0].Description);
    }

    [Fact]
    public async Task GetActiveAsync_Returns_Only_Active_Transfers()
    {
        // Arrange
        var active = RecurringTransfer.Create(
            _sourceAccount.Id,
            _destAccount.Id,
            "Active",
            MoneyValue.Create("USD", 100m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        _repository.Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransfer> { active });
        _accountRepo.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<Account> { _sourceAccount, _destAccount });

        // Act
        var result = await _service.GetActiveAsync();

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
            _sourceAccount.Id,
            _destAccount.Id,
            "Original",
            MoneyValue.Create("USD", 100m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));
        var instanceDate = new DateOnly(2026, 3, 1);

        _repository.Setup(r => r.GetByIdAsync(transfer.Id, default)).ReturnsAsync(transfer);
        _accountRepo.Setup(r => r.GetByIdAsync(_sourceAccount.Id, default)).ReturnsAsync(_sourceAccount);
        _accountRepo.Setup(r => r.GetByIdAsync(_destAccount.Id, default)).ReturnsAsync(_destAccount);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransferUpdateDto
        {
            Description = "Updated from March",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Frequency = "Monthly",
            DayOfMonth = 1,
        };

        // Act
        var result = await _service.UpdateFromDateAsync(transfer.Id, instanceDate, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated from March", result.Description);
        Assert.Equal(200m, result.Amount.Amount);
        _repository.Verify(r => r.RemoveExceptionsFromDateAsync(transfer.Id, instanceDate, default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
