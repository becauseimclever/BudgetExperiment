// <copyright file="PaycheckAllocationServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for <see cref="PaycheckAllocationService"/>.
/// </summary>
public class PaycheckAllocationServiceTests
{
    private readonly Mock<IRecurringTransactionRepository> _recurringRepo;

    public PaycheckAllocationServiceTests()
    {
        this._recurringRepo = new Mock<IRecurringTransactionRepository>();

        // Default setup - return empty collections
        this._recurringRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());
        this._recurringRepo
            .Setup(r => r.GetByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());
    }

    private PaycheckAllocationService CreateService()
    {
        return new PaycheckAllocationService(this._recurringRepo.Object);
    }

    #region GetAllocationSummaryAsync Tests

    [Fact]
    public async Task GetAllocationSummaryAsync_NoRecurringTransactions_ReturnsEmptyWithWarning()
    {
        // Arrange
        var service = this.CreateService();

        // Act
        var result = await service.GetAllocationSummaryAsync(RecurrenceFrequency.BiWeekly);

        // Assert
        Assert.Empty(result.Allocations);
        Assert.True(result.HasWarnings);
        Assert.Contains(result.Warnings, w => w.Type == "NoBillsConfigured");
        Assert.Equal(0m, result.TotalPerPaycheck.Amount);
        Assert.Equal("BiWeekly", result.PaycheckFrequency);
    }

    [Fact]
    public async Task GetAllocationSummaryAsync_WithBills_CalculatesAllocations()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var recurring = CreateRecurringTransaction(
            accountId,
            "Rent",
            -1200m,
            RecurrenceFrequency.Monthly);

        this._recurringRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });

        var service = this.CreateService();

        // Act
        var result = await service.GetAllocationSummaryAsync(
            RecurrenceFrequency.BiWeekly,
            paycheckAmount: 2000m);

        // Assert
        Assert.Single(result.Allocations);
        var allocation = result.Allocations[0];
        Assert.Equal("Rent", allocation.Description);
        Assert.Equal(1200m, allocation.BillAmount.Amount);
        Assert.Equal("Monthly", allocation.BillFrequency);
        Assert.Equal(553.85m, allocation.AmountPerPaycheck.Amount);
        Assert.Equal(14400m, allocation.AnnualAmount.Amount);
        Assert.Equal(recurring.Id, allocation.RecurringTransactionId);
    }

    [Fact]
    public async Task GetAllocationSummaryAsync_WithPaycheckAmount_CalculatesTotalsCorrectly()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var rent = CreateRecurringTransaction(accountId, "Rent", -1200m, RecurrenceFrequency.Monthly);
        var insurance = CreateRecurringTransaction(accountId, "Insurance", -600m, RecurrenceFrequency.Quarterly);

        this._recurringRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { rent, insurance });

        var service = this.CreateService();

        // Act
        var result = await service.GetAllocationSummaryAsync(
            RecurrenceFrequency.BiWeekly,
            paycheckAmount: 2000m);

        // Assert
        Assert.Equal(2, result.Allocations.Count);
        Assert.Equal(16800m, result.TotalAnnualBills.Amount); // $14,400 + $2,400
        Assert.Equal(646.16m, result.TotalPerPaycheck.Amount); // $553.85 + $92.31
        Assert.Equal(2000m, result.PaycheckAmount?.Amount);
        Assert.Equal(52000m, result.TotalAnnualIncome?.Amount); // $2,000 × 26
        Assert.Equal(1353.84m, result.RemainingPerPaycheck.Amount);
        Assert.Equal(0m, result.Shortfall.Amount);
        Assert.False(result.CannotReconcile);
    }

    [Fact]
    public async Task GetAllocationSummaryAsync_InsufficientIncome_GeneratesWarning()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var recurring = CreateRecurringTransaction(
            accountId,
            "Rent",
            -1200m,
            RecurrenceFrequency.Monthly);

        this._recurringRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });

        var service = this.CreateService();

        // Act - Paycheck amount less than required allocation ($553.85)
        var result = await service.GetAllocationSummaryAsync(
            RecurrenceFrequency.BiWeekly,
            paycheckAmount: 400m);

        // Assert
        Assert.True(result.HasWarnings);
        Assert.Contains(result.Warnings, w => w.Type == "InsufficientIncome");
        Assert.Equal(153.85m, result.Shortfall.Amount);
        Assert.Equal(0m, result.RemainingPerPaycheck.Amount);
    }

    [Fact]
    public async Task GetAllocationSummaryAsync_CannotReconcile_GeneratesWarning()
    {
        // Arrange - Annual bills exceed annual income
        var accountId = Guid.NewGuid();
        var recurring = CreateRecurringTransaction(
            accountId,
            "Expensive Rent",
            -5000m,
            RecurrenceFrequency.Monthly); // $60,000/year

        this._recurringRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });

        var service = this.CreateService();

        // Act - Paycheck $2,000 biweekly = $52,000/year
        var result = await service.GetAllocationSummaryAsync(
            RecurrenceFrequency.BiWeekly,
            paycheckAmount: 2000m);

        // Assert
        Assert.True(result.CannotReconcile);
        Assert.Contains(result.Warnings, w => w.Type == "CannotReconcile");
    }

    [Fact]
    public async Task GetAllocationSummaryAsync_NoPaycheckAmount_GeneratesNoIncomeWarning()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var recurring = CreateRecurringTransaction(
            accountId,
            "Rent",
            -1200m,
            RecurrenceFrequency.Monthly);

        this._recurringRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });

        var service = this.CreateService();

        // Act
        var result = await service.GetAllocationSummaryAsync(RecurrenceFrequency.BiWeekly);

        // Assert
        Assert.True(result.HasWarnings);
        Assert.Contains(result.Warnings, w => w.Type == "NoIncomeConfigured");
        Assert.Null(result.PaycheckAmount);
        Assert.Null(result.TotalAnnualIncome);
    }

    [Fact]
    public async Task GetAllocationSummaryAsync_FiltersByAccount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var recurring = CreateRecurringTransaction(
            accountId,
            "Rent",
            -1200m,
            RecurrenceFrequency.Monthly);

        this._recurringRepo
            .Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });

        var service = this.CreateService();

        // Act
        var result = await service.GetAllocationSummaryAsync(
            RecurrenceFrequency.BiWeekly,
            accountId: accountId);

        // Assert
        Assert.Single(result.Allocations);
        this._recurringRepo.Verify(
            r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>()),
            Times.Once);
        this._recurringRepo.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAllocationSummaryAsync_ExcludesPositiveAmounts()
    {
        // Arrange - Include an income transaction (positive amount)
        var accountId = Guid.NewGuid();
        var bill = CreateRecurringTransaction(accountId, "Rent", -1200m, RecurrenceFrequency.Monthly);
        var income = CreateRecurringTransaction(accountId, "Salary", 5000m, RecurrenceFrequency.BiWeekly);

        this._recurringRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { bill, income });

        var service = this.CreateService();

        // Act
        var result = await service.GetAllocationSummaryAsync(RecurrenceFrequency.BiWeekly);

        // Assert - Only the bill should be included
        Assert.Single(result.Allocations);
        Assert.Equal("Rent", result.Allocations[0].Description);
    }

    [Fact]
    public async Task GetAllocationSummaryAsync_ExcludesInactiveRecurringTransactions()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var activeRecurring = CreateRecurringTransaction(accountId, "Active Bill", -100m, RecurrenceFrequency.Monthly);
        var inactiveRecurring = CreateRecurringTransaction(accountId, "Inactive Bill", -200m, RecurrenceFrequency.Monthly);

        // Pause the second one (makes it inactive)
        inactiveRecurring.Pause();

        this._recurringRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { activeRecurring, inactiveRecurring });

        var service = this.CreateService();

        // Act
        var result = await service.GetAllocationSummaryAsync(RecurrenceFrequency.BiWeekly);

        // Assert - Only the active bill should be included
        Assert.Single(result.Allocations);
        Assert.Equal("Active Bill", result.Allocations[0].Description);
    }

    [Theory]
    [InlineData(RecurrenceFrequency.Weekly)]
    [InlineData(RecurrenceFrequency.BiWeekly)]
    [InlineData(RecurrenceFrequency.Monthly)]
    public async Task GetAllocationSummaryAsync_SupportsAllPaycheckFrequencies(RecurrenceFrequency frequency)
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var recurring = CreateRecurringTransaction(accountId, "Rent", -1200m, RecurrenceFrequency.Monthly);

        this._recurringRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });

        var service = this.CreateService();

        // Act
        var result = await service.GetAllocationSummaryAsync(frequency);

        // Assert
        Assert.Equal(frequency.ToString(), result.PaycheckFrequency);
        Assert.Single(result.Allocations);
    }

    #endregion

    #region Helper Methods

    private static RecurringTransaction CreateRecurringTransaction(
        Guid accountId,
        string description,
        decimal amount,
        RecurrenceFrequency frequency)
    {
        var pattern = frequency switch
        {
            RecurrenceFrequency.Daily => RecurrencePattern.CreateDaily(),
            RecurrenceFrequency.Weekly => RecurrencePattern.CreateWeekly(1, DayOfWeek.Monday),
            RecurrenceFrequency.BiWeekly => RecurrencePattern.CreateBiWeekly(DayOfWeek.Friday),
            RecurrenceFrequency.Monthly => RecurrencePattern.CreateMonthly(1, 15),
            RecurrenceFrequency.Quarterly => RecurrencePattern.CreateQuarterly(15),
            RecurrenceFrequency.Yearly => RecurrencePattern.CreateYearly(15, 1),
            _ => RecurrencePattern.CreateMonthly(1, 15),
        };

        return RecurringTransaction.Create(
            accountId,
            description,
            MoneyValue.Create("USD", amount),
            pattern,
            DateOnly.FromDateTime(DateTime.UtcNow));
    }

    #endregion
}
