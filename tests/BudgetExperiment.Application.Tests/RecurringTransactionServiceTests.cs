// <copyright file="RecurringTransactionServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using BudgetExperiment.Domain;

using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for <see cref="RecurringTransactionService"/>.
/// </summary>
public class RecurringTransactionServiceTests
{
    private readonly Mock<IRecurringTransactionRepository> _repository;
    private readonly Mock<IAccountRepository> _accountRepo;
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly RecurringTransactionService _service;
    private readonly Account _account;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransactionServiceTests"/> class.
    /// </summary>
    public RecurringTransactionServiceTests()
    {
        _repository = new Mock<IRecurringTransactionRepository>();
        _accountRepo = new Mock<IAccountRepository>();
        _transactionRepo = new Mock<ITransactionRepository>();
        _uow = new Mock<IUnitOfWork>();
        _service = new RecurringTransactionService(
            _repository.Object,
            _accountRepo.Object,
            _transactionRepo.Object,
            _uow.Object);

        _account = Account.Create("Checking", AccountType.Checking);
    }

    // --- GetByIdAsync ---
    [Fact]
    public async Task GetByIdAsync_Returns_Dto_When_Found()
    {
        // Arrange
        var recurring = CreateTestRecurring("Monthly Rent", 1200m);

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);

        // Act
        var result = await _service.GetByIdAsync(recurring.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(recurring.Id, result.Id);
        Assert.Equal("Monthly Rent", result.Description);
        Assert.Equal(1200m, result.Amount.Amount);
        Assert.Equal("USD", result.Amount.Currency);
        Assert.Equal("Checking", result.AccountName);
        Assert.Equal("Monthly", result.Frequency);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((RecurringTransaction?)null);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_Uses_Empty_AccountName_When_Account_Not_Found()
    {
        // Arrange
        var recurring = CreateTestRecurring("Rent");

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync((Account?)null);

        // Act
        var result = await _service.GetByIdAsync(recurring.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.AccountName);
    }

    // --- GetAllAsync ---
    [Fact]
    public async Task GetAllAsync_Returns_All_Recurring_Transactions()
    {
        // Arrange
        var recurring1 = CreateTestRecurring("Rent", 1200m);
        var recurring2 = CreateTestRecurring("Netflix", 15.99m);

        _repository.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<RecurringTransaction> { recurring1, recurring2 });
        _accountRepo.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<Account> { _account });

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Description == "Rent");
        Assert.Contains(result, r => r.Description == "Netflix");
    }

    [Fact]
    public async Task GetAllAsync_Returns_Empty_When_None_Exist()
    {
        // Arrange
        _repository.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());
        _accountRepo.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<Account>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    // --- GetByAccountIdAsync ---
    [Fact]
    public async Task GetByAccountIdAsync_Returns_Transactions_For_Account()
    {
        // Arrange
        var recurring = CreateTestRecurring("Rent");

        _repository.Setup(r => r.GetByAccountIdAsync(_account.Id, default))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);

        // Act
        var result = await _service.GetByAccountIdAsync(_account.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal("Rent", result[0].Description);
        Assert.Equal("Checking", result[0].AccountName);
    }

    [Fact]
    public async Task GetByAccountIdAsync_Returns_Empty_When_Account_Has_None()
    {
        // Arrange
        _repository.Setup(r => r.GetByAccountIdAsync(_account.Id, default))
            .ReturnsAsync(new List<RecurringTransaction>());
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);

        // Act
        var result = await _service.GetByAccountIdAsync(_account.Id);

        // Assert
        Assert.Empty(result);
    }

    // --- CreateAsync ---
    [Fact]
    public async Task CreateAsync_Creates_RecurringTransaction()
    {
        // Arrange
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransactionCreateDto
        {
            AccountId = _account.Id,
            Description = "Monthly Rent",
            Amount = new MoneyDto { Currency = "USD", Amount = 1200m },
            Frequency = "Monthly",
            Interval = 1,
            DayOfMonth = 1,
            StartDate = new DateOnly(2026, 3, 1),
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Monthly Rent", result.Description);
        Assert.Equal(1200m, result.Amount.Amount);
        Assert.Equal("Monthly", result.Frequency);
        Assert.Equal("Checking", result.AccountName);
        Assert.True(result.IsActive);

        _repository.Verify(r => r.AddAsync(It.IsAny<RecurringTransaction>(), default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_Account_Not_Found()
    {
        // Arrange
        _accountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Account?)null);

        var dto = new RecurringTransactionCreateDto
        {
            AccountId = Guid.NewGuid(),
            Description = "Rent",
            Amount = new MoneyDto { Currency = "USD", Amount = 1200m },
            Frequency = "Monthly",
            DayOfMonth = 1,
            StartDate = new DateOnly(2026, 3, 1),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(dto));
        Assert.Contains("Account not found", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_With_Weekly_Frequency_Sets_DayOfWeek()
    {
        // Arrange
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransactionCreateDto
        {
            AccountId = _account.Id,
            Description = "Weekly Groceries",
            Amount = new MoneyDto { Currency = "USD", Amount = 150m },
            Frequency = "Weekly",
            Interval = 1,
            DayOfWeek = "Saturday",
            StartDate = new DateOnly(2026, 3, 7),
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal("Weekly", result.Frequency);
        Assert.Equal("Saturday", result.DayOfWeek);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_Frequency_Invalid()
    {
        // Arrange
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);

        var dto = new RecurringTransactionCreateDto
        {
            AccountId = _account.Id,
            Description = "Bad",
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Frequency = "InvalidFrequency",
            StartDate = new DateOnly(2026, 3, 1),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(dto));
        Assert.Contains("Invalid frequency", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_Weekly_Throws_When_DayOfWeek_Missing()
    {
        // Arrange
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);

        var dto = new RecurringTransactionCreateDto
        {
            AccountId = _account.Id,
            Description = "Weekly",
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Frequency = "Weekly",
            Interval = 1,
            StartDate = new DateOnly(2026, 3, 1),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(dto));
        Assert.Contains("Day of week is required", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_With_EndDate()
    {
        // Arrange
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransactionCreateDto
        {
            AccountId = _account.Id,
            Description = "Lease",
            Amount = new MoneyDto { Currency = "USD", Amount = 1500m },
            Frequency = "Monthly",
            DayOfMonth = 1,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal(new DateOnly(2026, 12, 31), result.EndDate);
    }

    // --- UpdateAsync ---
    [Fact]
    public async Task UpdateAsync_Updates_RecurringTransaction()
    {
        // Arrange
        var recurring = CreateTestRecurring("Original", 100m);

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransactionUpdateDto
        {
            Description = "Updated",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Frequency = "Monthly",
            DayOfMonth = 15,
        };

        // Act
        var result = await _service.UpdateAsync(recurring.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Description);
        Assert.Equal(200m, result.Amount.Amount);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((RecurringTransaction?)null);

        var dto = new RecurringTransactionUpdateDto
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

    // --- DeleteAsync ---
    [Fact]
    public async Task DeleteAsync_Deletes_And_Returns_True()
    {
        // Arrange
        var recurring = CreateTestRecurring("To Delete");

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(recurring.Id);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.RemoveAsync(recurring, default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Returns_False_When_Not_Found()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((RecurringTransaction?)null);

        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
        _repository.Verify(r => r.RemoveAsync(It.IsAny<RecurringTransaction>(), default), Times.Never);
    }

    // --- PauseAsync ---
    [Fact]
    public async Task PauseAsync_Pauses_Transaction()
    {
        // Arrange
        var recurring = CreateTestRecurring("To Pause");

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _service.PauseAsync(recurring.Id);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task PauseAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((RecurringTransaction?)null);

        // Act
        var result = await _service.PauseAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    // --- ResumeAsync ---
    [Fact]
    public async Task ResumeAsync_Resumes_Transaction()
    {
        // Arrange
        var recurring = CreateTestRecurring("To Resume");
        recurring.Pause();

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _service.ResumeAsync(recurring.Id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsActive);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ResumeAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((RecurringTransaction?)null);

        // Act
        var result = await _service.ResumeAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    // --- SkipNextAsync ---
    [Fact]
    public async Task SkipNextAsync_Creates_Exception_And_Advances()
    {
        // Arrange
        var recurring = CreateTestRecurring("To Skip");
        var originalNextOccurrence = recurring.NextOccurrence;

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _service.SkipNextAsync(recurring.Id);

        // Assert
        Assert.NotNull(result);
        _repository.Verify(
            r => r.AddExceptionAsync(
                It.Is<RecurringTransactionException>(e =>
                    e.OriginalDate == originalNextOccurrence &&
                    e.ExceptionType == ExceptionType.Skipped),
                default),
            Times.Once);
        Assert.NotEqual(originalNextOccurrence, recurring.NextOccurrence);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task SkipNextAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((RecurringTransaction?)null);

        // Act
        var result = await _service.SkipNextAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    // --- UpdateFromDateAsync ---
    [Fact]
    public async Task UpdateFromDateAsync_Updates_Series_And_Removes_Future_Exceptions()
    {
        // Arrange
        var recurring = CreateTestRecurring("Original", 100m);
        var instanceDate = new DateOnly(2026, 5, 1);

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransactionUpdateDto
        {
            Description = "Updated from May",
            Amount = new MoneyDto { Currency = "USD", Amount = 250m },
            Frequency = "Monthly",
            DayOfMonth = 1,
        };

        // Act
        var result = await _service.UpdateFromDateAsync(recurring.Id, instanceDate, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated from May", result.Description);
        Assert.Equal(250m, result.Amount.Amount);
        _repository.Verify(r => r.RemoveExceptionsFromDateAsync(recurring.Id, instanceDate, default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateFromDateAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((RecurringTransaction?)null);

        var dto = new RecurringTransactionUpdateDto
        {
            Description = "Updated",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Frequency = "Monthly",
            DayOfMonth = 1,
        };

        // Act
        var result = await _service.UpdateFromDateAsync(Guid.NewGuid(), new DateOnly(2026, 5, 1), dto);

        // Assert
        Assert.Null(result);
    }

    // --- GetImportPatternsAsync ---
    [Fact]
    public async Task GetImportPatternsAsync_Returns_Patterns()
    {
        // Arrange
        var recurring = CreateTestRecurring("With Patterns");
        recurring.AddImportPattern(ImportPatternValue.Create("RENT*"));
        recurring.AddImportPattern(ImportPatternValue.Create("LANDLORD*"));

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);

        // Act
        var result = await _service.GetImportPatternsAsync(recurring.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Patterns.Count);
        Assert.Contains("RENT*", result.Patterns);
        Assert.Contains("LANDLORD*", result.Patterns);
    }

    [Fact]
    public async Task GetImportPatternsAsync_Returns_Empty_When_No_Patterns()
    {
        // Arrange
        var recurring = CreateTestRecurring("No Patterns");

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);

        // Act
        var result = await _service.GetImportPatternsAsync(recurring.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Patterns);
    }

    [Fact]
    public async Task GetImportPatternsAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((RecurringTransaction?)null);

        // Act
        var result = await _service.GetImportPatternsAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    // --- UpdateImportPatternsAsync ---
    [Fact]
    public async Task UpdateImportPatternsAsync_Replaces_Patterns()
    {
        // Arrange
        var recurring = CreateTestRecurring("Update Patterns");
        recurring.AddImportPattern(ImportPatternValue.Create("OLD*"));

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new ImportPatternsDto
        {
            Patterns = new List<string> { "NEW1*", "NEW2*" },
        };

        // Act
        var result = await _service.UpdateImportPatternsAsync(recurring.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Patterns.Count);
        Assert.Contains("NEW1*", result.Patterns);
        Assert.Contains("NEW2*", result.Patterns);
        Assert.DoesNotContain("OLD*", result.Patterns);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateImportPatternsAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((RecurringTransaction?)null);

        var dto = new ImportPatternsDto
        {
            Patterns = new List<string> { "PATTERN*" },
        };

        // Act
        var result = await _service.UpdateImportPatternsAsync(Guid.NewGuid(), dto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateImportPatternsAsync_Clears_All_When_Empty_List()
    {
        // Arrange
        var recurring = CreateTestRecurring("Clear Patterns");
        recurring.AddImportPattern(ImportPatternValue.Create("EXISTING*"));

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new ImportPatternsDto
        {
            Patterns = new List<string>(),
        };

        // Act
        var result = await _service.UpdateImportPatternsAsync(recurring.Id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Patterns);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    // --- Frequency coverage via CreateAsync ---
    [Fact]
    public async Task CreateAsync_With_Daily_Frequency()
    {
        // Arrange
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransactionCreateDto
        {
            AccountId = _account.Id,
            Description = "Daily Coffee",
            Amount = new MoneyDto { Currency = "USD", Amount = 5m },
            Frequency = "Daily",
            Interval = 1,
            StartDate = new DateOnly(2026, 3, 1),
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal("Daily", result.Frequency);
    }

    [Fact]
    public async Task CreateAsync_With_BiWeekly_Frequency()
    {
        // Arrange
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransactionCreateDto
        {
            AccountId = _account.Id,
            Description = "Biweekly Paycheck",
            Amount = new MoneyDto { Currency = "USD", Amount = 2000m },
            Frequency = "BiWeekly",
            DayOfWeek = "Friday",
            StartDate = new DateOnly(2026, 3, 6),
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal("BiWeekly", result.Frequency);
        Assert.Equal("Friday", result.DayOfWeek);
    }

    [Fact]
    public async Task CreateAsync_With_Quarterly_Frequency()
    {
        // Arrange
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransactionCreateDto
        {
            AccountId = _account.Id,
            Description = "Quarterly Tax",
            Amount = new MoneyDto { Currency = "USD", Amount = 3000m },
            Frequency = "Quarterly",
            DayOfMonth = 15,
            StartDate = new DateOnly(2026, 1, 15),
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal("Quarterly", result.Frequency);
    }

    [Fact]
    public async Task CreateAsync_With_Yearly_Frequency()
    {
        // Arrange
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringTransactionCreateDto
        {
            AccountId = _account.Id,
            Description = "Annual Insurance",
            Amount = new MoneyDto { Currency = "USD", Amount = 1200m },
            Frequency = "Yearly",
            DayOfMonth = 1,
            MonthOfYear = 6,
            StartDate = new DateOnly(2026, 6, 1),
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal("Yearly", result.Frequency);
        Assert.Equal(6, result.MonthOfYear);
    }

    [Fact]
    public async Task CreateAsync_Weekly_Throws_When_DayOfWeek_Invalid()
    {
        // Arrange
        _accountRepo.Setup(r => r.GetByIdAsync(_account.Id, default)).ReturnsAsync(_account);

        var dto = new RecurringTransactionCreateDto
        {
            AccountId = _account.Id,
            Description = "Weekly",
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Frequency = "Weekly",
            Interval = 1,
            DayOfWeek = "NotADay",
            StartDate = new DateOnly(2026, 3, 1),
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => _service.CreateAsync(dto));
        Assert.Contains("Invalid day of week", ex.Message);
    }

    // --- Helper ---
    private RecurringTransaction CreateTestRecurring(string description, decimal amount = 100m)
    {
        return RecurringTransaction.Create(
            _account.Id,
            description,
            MoneyValue.Create("USD", amount),
            RecurrencePatternValue.CreateMonthly(1, 1),
            new DateOnly(2026, 3, 1));
    }
}
