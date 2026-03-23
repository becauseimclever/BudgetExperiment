// <copyright file="RecurringTransactionInstanceServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

using Moq;

namespace BudgetExperiment.Application.Tests.Recurring;

/// <summary>
/// Unit tests for <see cref="RecurringTransactionInstanceService"/>.
/// </summary>
public class RecurringTransactionInstanceServiceTests
{
    private readonly Mock<IRecurringTransactionRepository> _repository;
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly RecurringTransactionInstanceService _service;
    private readonly Account _account;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransactionInstanceServiceTests"/> class.
    /// </summary>
    public RecurringTransactionInstanceServiceTests()
    {
        _repository = new Mock<IRecurringTransactionRepository>();
        _transactionRepo = new Mock<ITransactionRepository>();
        _uow = new Mock<IUnitOfWork>();
        _service = new RecurringTransactionInstanceService(
            _repository.Object,
            _transactionRepo.Object,
            _uow.Object);

        _account = Account.Create("Checking", AccountType.Checking);
    }

    [Fact]
    public async Task GetInstancesAsync_RecurringNotFound_ReturnsNull()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((RecurringTransaction?)null);

        // Act
        var result = await _service.GetInstancesAsync(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetInstancesAsync_WithOccurrencesInRange_ReturnsInstances()
    {
        // Arrange
        var recurring = CreateTestRecurring("Monthly Bill", 100m);
        var fromDate = new DateOnly(2026, 1, 1);
        var toDate = new DateOnly(2026, 1, 31);

        SetupDefaultInstanceMocks(recurring, fromDate, toDate);

        // Act
        var result = await _service.GetInstancesAsync(recurring.Id, fromDate, toDate);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, i => Assert.Equal(recurring.Id, i.RecurringTransactionId));
    }

    [Fact]
    public async Task GetInstancesAsync_NoOccurrencesInRange_ReturnsEmptyList()
    {
        // Arrange — starts in 2027, queried in 2026
        var recurring = RecurringTransaction.Create(
            _account.Id,
            "Future Bill",
            MoneyValue.Create("USD", 100m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            new DateOnly(2027, 1, 1));
        var fromDate = new DateOnly(2026, 1, 1);
        var toDate = new DateOnly(2026, 1, 31);

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _repository.Setup(r => r.GetExceptionsByDateRangeAsync(recurring.Id, fromDate, toDate, default))
            .ReturnsAsync(Array.Empty<RecurringTransactionException>());
        _transactionRepo.Setup(r => r.GetByDateRangeAsync(fromDate, toDate, It.IsAny<Guid?>(), default))
            .ReturnsAsync(new List<Transaction>());

        // Act
        var result = await _service.GetInstancesAsync(recurring.Id, fromDate, toDate);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetInstancesAsync_WithSkippedException_IncludesSkippedInstance()
    {
        // Arrange
        var recurring = CreateTestRecurring("Monthly Bill", 100m);
        var fromDate = new DateOnly(2026, 1, 1);
        var toDate = new DateOnly(2026, 1, 31);
        var instanceDate = new DateOnly(2026, 1, 1);

        var skippedException = RecurringTransactionException.CreateSkipped(recurring.Id, instanceDate);

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _repository.Setup(r => r.GetExceptionsByDateRangeAsync(recurring.Id, fromDate, toDate, default))
            .ReturnsAsync(new List<RecurringTransactionException> { skippedException });
        _transactionRepo.Setup(r => r.GetByDateRangeAsync(fromDate, toDate, It.IsAny<Guid?>(), default))
            .ReturnsAsync(new List<Transaction>());

        // Act
        var result = await _service.GetInstancesAsync(recurring.Id, fromDate, toDate);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result, i => i.IsSkipped);
    }

    [Fact]
    public async Task GetInstancesAsync_WithModifiedException_ReturnsModifiedInstance()
    {
        // Arrange
        var recurring = CreateTestRecurring("Monthly Bill", 100m);
        var fromDate = new DateOnly(2026, 1, 1);
        var toDate = new DateOnly(2026, 1, 31);
        var instanceDate = new DateOnly(2026, 1, 1);

        var modifiedException = RecurringTransactionException.CreateModified(
            recurring.Id,
            instanceDate,
            MoneyValue.Create("USD", 200m),
            "One-off adjustment",
            null);

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _repository.Setup(r => r.GetExceptionsByDateRangeAsync(recurring.Id, fromDate, toDate, default))
            .ReturnsAsync(new List<RecurringTransactionException> { modifiedException });
        _transactionRepo.Setup(r => r.GetByDateRangeAsync(fromDate, toDate, It.IsAny<Guid?>(), default))
            .ReturnsAsync(new List<Transaction>());

        // Act
        var result = await _service.GetInstancesAsync(recurring.Id, fromDate, toDate);

        // Assert
        Assert.NotNull(result);
        var instance = Assert.Single(result);
        Assert.True(instance.IsModified);
        Assert.Equal(200m, instance.Amount.Amount);
    }

    [Fact]
    public async Task ModifyInstanceAsync_RecurringNotFound_ReturnsNull()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((RecurringTransaction?)null);

        var dto = new RecurringInstanceModifyDto { Description = "Changed" };

        // Act
        var result = await _service.ModifyInstanceAsync(Guid.NewGuid(), new DateOnly(2026, 1, 1), dto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ModifyInstanceAsync_NoExistingException_CreatesModifiedException()
    {
        // Arrange
        var recurring = CreateTestRecurring("Monthly Bill", 100m);
        var instanceDate = new DateOnly(2026, 2, 1);

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _repository.Setup(r => r.GetExceptionAsync(recurring.Id, instanceDate, default))
            .ReturnsAsync((RecurringTransactionException?)null);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringInstanceModifyDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
            Description = "Extra this month",
        };

        // Act
        var result = await _service.ModifyInstanceAsync(recurring.Id, instanceDate, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200m, result.Amount.Amount);
        Assert.Equal("Extra this month", result.Description);
        Assert.True(result.IsModified);
        _repository.Verify(
            r => r.AddExceptionAsync(
                It.Is<RecurringTransactionException>(e =>
                    e.OriginalDate == instanceDate && e.ExceptionType == ExceptionType.Modified),
                default),
            Times.Once);
    }

    [Fact]
    public async Task ModifyInstanceAsync_ExistingException_UpdatesAndDoesNotAddNew()
    {
        // Arrange
        var recurring = CreateTestRecurring("Monthly Bill", 100m);
        var instanceDate = new DateOnly(2026, 2, 1);
        var existingException = RecurringTransactionException.CreateModified(
            recurring.Id,
            instanceDate,
            MoneyValue.Create("USD", 150m),
            "Old description",
            null);

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _repository.Setup(r => r.GetExceptionAsync(recurring.Id, instanceDate, default))
            .ReturnsAsync(existingException);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringInstanceModifyDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = 250m },
            Description = "Updated description",
        };

        // Act
        var result = await _service.ModifyInstanceAsync(recurring.Id, instanceDate, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(250m, result.Amount.Amount);
        Assert.Equal("Updated description", result.Description);
        _repository.Verify(r => r.AddExceptionAsync(It.IsAny<RecurringTransactionException>(), default), Times.Never);
    }

    [Fact]
    public async Task ModifyInstanceAsync_WithVersionToken_SetsConcurrencyTokenAndMarksModified()
    {
        // Arrange
        var recurring = CreateTestRecurring("Monthly Bill", 100m);
        var instanceDate = new DateOnly(2026, 2, 1);
        const string versionToken = "abc123";

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _repository.Setup(r => r.GetExceptionAsync(recurring.Id, instanceDate, default))
            .ReturnsAsync((RecurringTransactionException?)null);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringInstanceModifyDto { Description = "With version" };

        // Act
        await _service.ModifyInstanceAsync(recurring.Id, instanceDate, dto, versionToken);

        // Assert
        _uow.Verify(u => u.SetExpectedConcurrencyToken(recurring, versionToken), Times.Once);
        _uow.Verify(u => u.MarkAsModified(recurring), Times.Once);
    }

    [Fact]
    public async Task ModifyInstanceAsync_WithoutVersionToken_DoesNotSetConcurrencyToken()
    {
        // Arrange
        var recurring = CreateTestRecurring("Monthly Bill", 100m);
        var instanceDate = new DateOnly(2026, 2, 1);

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _repository.Setup(r => r.GetExceptionAsync(recurring.Id, instanceDate, default))
            .ReturnsAsync((RecurringTransactionException?)null);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new RecurringInstanceModifyDto { Description = "No version" };

        // Act
        await _service.ModifyInstanceAsync(recurring.Id, instanceDate, dto);

        // Assert
        _uow.Verify(u => u.SetExpectedConcurrencyToken(It.IsAny<RecurringTransaction>(), It.IsAny<string>()), Times.Never);
        _uow.Verify(u => u.MarkAsModified(It.IsAny<RecurringTransaction>()), Times.Never);
    }

    [Fact]
    public async Task SkipInstanceAsync_RecurringNotFound_ReturnsFalse()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((RecurringTransaction?)null);

        // Act
        var result = await _service.SkipInstanceAsync(Guid.NewGuid(), new DateOnly(2026, 1, 1));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SkipInstanceAsync_NoExistingException_CreatesSkippedException()
    {
        // Arrange
        var recurring = CreateTestRecurring("Monthly Bill", 100m);
        var instanceDate = new DateOnly(2026, 2, 1);

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _repository.Setup(r => r.GetExceptionAsync(recurring.Id, instanceDate, default))
            .ReturnsAsync((RecurringTransactionException?)null);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _service.SkipInstanceAsync(recurring.Id, instanceDate);

        // Assert
        Assert.True(result);
        _repository.Verify(
            r => r.AddExceptionAsync(
                It.Is<RecurringTransactionException>(e =>
                    e.OriginalDate == instanceDate && e.ExceptionType == ExceptionType.Skipped),
                default),
            Times.Once);
    }

    [Fact]
    public async Task SkipInstanceAsync_ExistingModifiedException_RemovesOldAndCreatesSkipped()
    {
        // Arrange
        var recurring = CreateTestRecurring("Monthly Bill", 100m);
        var instanceDate = new DateOnly(2026, 2, 1);
        var existingException = RecurringTransactionException.CreateModified(
            recurring.Id,
            instanceDate,
            MoneyValue.Create("USD", 150m),
            "Modified",
            null);

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _repository.Setup(r => r.GetExceptionAsync(recurring.Id, instanceDate, default))
            .ReturnsAsync(existingException);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _service.SkipInstanceAsync(recurring.Id, instanceDate);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.RemoveExceptionAsync(existingException, default), Times.Once);
        _repository.Verify(
            r => r.AddExceptionAsync(
                It.Is<RecurringTransactionException>(e => e.ExceptionType == ExceptionType.Skipped),
                default),
            Times.Once);
    }

    [Fact]
    public async Task SkipInstanceAsync_SavesChanges()
    {
        // Arrange
        var recurring = CreateTestRecurring("Monthly Bill", 100m);
        var instanceDate = new DateOnly(2026, 2, 1);

        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _repository.Setup(r => r.GetExceptionAsync(recurring.Id, instanceDate, default))
            .ReturnsAsync((RecurringTransactionException?)null);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        await _service.SkipInstanceAsync(recurring.Id, instanceDate);

        // Assert
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetProjectedInstancesAsync_WithAccountId_UsesAccountFilter()
    {
        // Arrange
        var accountId = _account.Id;
        var recurring = CreateTestRecurring("Monthly Bill", 100m);
        var fromDate = new DateOnly(2026, 1, 1);
        var toDate = new DateOnly(2026, 1, 31);

        _repository.Setup(r => r.GetByAccountIdAsync(accountId, default))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });
        SetupDefaultInstanceMocks(recurring, fromDate, toDate);

        // Act
        var result = await _service.GetProjectedInstancesAsync(fromDate, toDate, accountId);

        // Assert
        Assert.NotNull(result);
        _repository.Verify(r => r.GetByAccountIdAsync(accountId, default), Times.Once);
        _repository.Verify(r => r.GetActiveAsync(default), Times.Never);
    }

    [Fact]
    public async Task GetProjectedInstancesAsync_WithoutAccountId_GetsAllActive()
    {
        // Arrange
        var recurring = CreateTestRecurring("Monthly Bill", 100m);
        var fromDate = new DateOnly(2026, 1, 1);
        var toDate = new DateOnly(2026, 1, 31);

        _repository.Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });
        SetupDefaultInstanceMocks(recurring, fromDate, toDate);

        // Act
        var result = await _service.GetProjectedInstancesAsync(fromDate, toDate);

        // Assert
        Assert.NotNull(result);
        _repository.Verify(r => r.GetActiveAsync(default), Times.Once);
        _repository.Verify(r => r.GetByAccountIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task GetProjectedInstancesAsync_EmptyActiveList_ReturnsEmptyList()
    {
        // Arrange
        var fromDate = new DateOnly(2026, 1, 1);
        var toDate = new DateOnly(2026, 1, 31);

        _repository.Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(Array.Empty<RecurringTransaction>());

        // Act
        var result = await _service.GetProjectedInstancesAsync(fromDate, toDate);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProjectedInstancesAsync_SkipsSkippedInstances()
    {
        // Arrange
        var recurring = CreateTestRecurring("Monthly Bill", 100m);
        var fromDate = new DateOnly(2026, 1, 1);
        var toDate = new DateOnly(2026, 1, 31);
        var instanceDate = new DateOnly(2026, 1, 1);

        var skippedException = RecurringTransactionException.CreateSkipped(recurring.Id, instanceDate);

        _repository.Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });
        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _repository.Setup(r => r.GetExceptionsByDateRangeAsync(recurring.Id, fromDate, toDate, default))
            .ReturnsAsync(new List<RecurringTransactionException> { skippedException });
        _transactionRepo.Setup(r => r.GetByDateRangeAsync(fromDate, toDate, It.IsAny<Guid?>(), default))
            .ReturnsAsync(new List<Transaction>());

        // Act
        var result = await _service.GetProjectedInstancesAsync(fromDate, toDate);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProjectedInstancesAsync_ResultsOrderedByEffectiveDate()
    {
        // Arrange — two monthly recurrings in the same range
        var recurring1 = CreateTestRecurring("Bill A", 100m);
        var recurring2 = RecurringTransaction.Create(
            _account.Id,
            "Bill B",
            MoneyValue.Create("USD", 50m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));
        var fromDate = new DateOnly(2026, 1, 1);
        var toDate = new DateOnly(2026, 3, 31);

        _repository.Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction> { recurring1, recurring2 });
        SetupDefaultInstanceMocks(recurring1, fromDate, toDate);
        SetupDefaultInstanceMocks(recurring2, fromDate, toDate);

        // Act
        var result = await _service.GetProjectedInstancesAsync(fromDate, toDate);

        // Assert
        Assert.NotNull(result);
        var dates = result.Select(i => i.EffectiveDate).ToList();
        Assert.Equal(dates.OrderBy(d => d).ToList(), dates);
    }

    private RecurringTransaction CreateTestRecurring(string description, decimal amount = 100m)
    {
        return RecurringTransaction.Create(
            _account.Id,
            description,
            MoneyValue.Create("USD", amount),
            RecurrencePatternValue.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));
    }

    private void SetupDefaultInstanceMocks(RecurringTransaction recurring, DateOnly fromDate, DateOnly toDate)
    {
        _repository.Setup(r => r.GetByIdAsync(recurring.Id, default)).ReturnsAsync(recurring);
        _repository.Setup(r => r.GetExceptionsByDateRangeAsync(recurring.Id, fromDate, toDate, default))
            .ReturnsAsync(Array.Empty<RecurringTransactionException>());
        _transactionRepo.Setup(r => r.GetByDateRangeAsync(fromDate, toDate, It.IsAny<Guid?>(), default))
            .ReturnsAsync(new List<Transaction>());
    }
}
