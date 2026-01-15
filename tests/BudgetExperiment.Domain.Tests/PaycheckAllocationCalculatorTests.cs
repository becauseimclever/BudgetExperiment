// <copyright file="PaycheckAllocationCalculatorTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the <see cref="PaycheckAllocationCalculator"/> domain service.
/// </summary>
public class PaycheckAllocationCalculatorTests
{
    private readonly PaycheckAllocationCalculator _calculator = new();

    #region CalculateAllocation Tests

    [Fact]
    public void CalculateAllocation_MonthlyBill_BiweeklyPaycheck_CalculatesCorrectly()
    {
        // Arrange - Example 1 from spec: $1,200/month rent with biweekly paycheck
        var bill = BillInfo.Create("Rent", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Monthly);

        // Act
        var allocation = this._calculator.CalculateAllocation(bill, RecurrenceFrequency.BiWeekly);

        // Assert
        Assert.Equal(14400m, allocation.AnnualAmount.Amount); // $1,200 × 12
        Assert.Equal(553.85m, allocation.AmountPerPaycheck.Amount); // $14,400 ÷ 26
    }

    [Fact]
    public void CalculateAllocation_QuarterlyBill_BiweeklyPaycheck_CalculatesCorrectly()
    {
        // Arrange - Example 2 from spec: $600/quarter insurance with biweekly paycheck
        var bill = BillInfo.Create("Car Insurance", MoneyValue.Create("USD", 600m), RecurrenceFrequency.Quarterly);

        // Act
        var allocation = this._calculator.CalculateAllocation(bill, RecurrenceFrequency.BiWeekly);

        // Assert
        Assert.Equal(2400m, allocation.AnnualAmount.Amount); // $600 × 4
        Assert.Equal(92.31m, allocation.AmountPerPaycheck.Amount); // $2,400 ÷ 26
    }

    [Fact]
    public void CalculateAllocation_WeeklyBill_BiweeklyPaycheck_CalculatesCorrectly()
    {
        // Arrange - Example 3 from spec: $200/week groceries with biweekly paycheck
        var bill = BillInfo.Create("Groceries", MoneyValue.Create("USD", 200m), RecurrenceFrequency.Weekly);

        // Act
        var allocation = this._calculator.CalculateAllocation(bill, RecurrenceFrequency.BiWeekly);

        // Assert
        Assert.Equal(10400m, allocation.AnnualAmount.Amount); // $200 × 52
        Assert.Equal(400m, allocation.AmountPerPaycheck.Amount); // $10,400 ÷ 26
    }

    [Fact]
    public void CalculateAllocation_YearlyBill_MonthlyPaycheck_CalculatesCorrectly()
    {
        // Arrange - Annual property tax with monthly salary
        var bill = BillInfo.Create("Property Tax", MoneyValue.Create("USD", 3600m), RecurrenceFrequency.Yearly);

        // Act
        var allocation = this._calculator.CalculateAllocation(bill, RecurrenceFrequency.Monthly);

        // Assert
        Assert.Equal(3600m, allocation.AnnualAmount.Amount); // $3,600 × 1
        Assert.Equal(300m, allocation.AmountPerPaycheck.Amount); // $3,600 ÷ 12
    }

    [Fact]
    public void CalculateAllocation_DailyBill_WeeklyPaycheck_CalculatesCorrectly()
    {
        // Arrange - Daily expense with weekly paycheck
        var bill = BillInfo.Create("Coffee", MoneyValue.Create("USD", 5m), RecurrenceFrequency.Daily);

        // Act
        var allocation = this._calculator.CalculateAllocation(bill, RecurrenceFrequency.Weekly);

        // Assert
        Assert.Equal(1825m, allocation.AnnualAmount.Amount); // $5 × 365
        Assert.Equal(35.10m, allocation.AmountPerPaycheck.Amount); // $1,825 ÷ 52
    }

    [Fact]
    public void CalculateAllocation_MonthlyBill_MonthlyPaycheck_CalculatesCorrectly()
    {
        // Arrange - Same frequency (monthly-monthly)
        var bill = BillInfo.Create("Netflix", MoneyValue.Create("USD", 15.99m), RecurrenceFrequency.Monthly);

        // Act
        var allocation = this._calculator.CalculateAllocation(bill, RecurrenceFrequency.Monthly);

        // Assert
        Assert.Equal(191.88m, allocation.AnnualAmount.Amount); // $15.99 × 12
        Assert.Equal(15.99m, allocation.AmountPerPaycheck.Amount); // $191.88 ÷ 12
    }

    [Fact]
    public void CalculateAllocation_WithNullBill_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => this._calculator.CalculateAllocation(null!, RecurrenceFrequency.BiWeekly));
    }

    #endregion

    #region CalculateAllocationSummary Tests

    [Fact]
    public void CalculateAllocationSummary_MultipleBills_CalculatesTotalCorrectly()
    {
        // Arrange
        var bills = new[]
        {
            BillInfo.Create("Rent", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Monthly),
            BillInfo.Create("Car Insurance", MoneyValue.Create("USD", 600m), RecurrenceFrequency.Quarterly),
        };

        // Act
        var summary = this._calculator.CalculateAllocationSummary(
            bills,
            RecurrenceFrequency.BiWeekly);

        // Assert
        Assert.Equal(2, summary.Allocations.Count);
        Assert.Equal(16800m, summary.TotalAnnualBills.Amount); // $14,400 + $2,400
        Assert.Equal(646.16m, summary.TotalPerPaycheck.Amount); // $553.85 + $92.31
        Assert.Equal(RecurrenceFrequency.BiWeekly, summary.PaycheckFrequency);
    }

    [Fact]
    public void CalculateAllocationSummary_WithPaycheckAmount_CalculatesRemaining()
    {
        // Arrange
        var bills = new[]
        {
            BillInfo.Create("Rent", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Monthly),
        };

        // Act
        var summary = this._calculator.CalculateAllocationSummary(
            bills,
            RecurrenceFrequency.BiWeekly,
            MoneyValue.Create("USD", 2000m));

        // Assert
        Assert.Equal(2000m, summary.PaycheckAmount?.Amount);
        Assert.Equal(52000m, summary.TotalAnnualIncome?.Amount); // $2,000 × 26
        Assert.Equal(1446.15m, summary.RemainingPerPaycheck.Amount); // $2,000 - $553.85
        Assert.Equal(0m, summary.Shortfall.Amount);
        Assert.False(summary.HasWarnings);
    }

    [Fact]
    public void CalculateAllocationSummary_InsufficientIncome_GeneratesWarning()
    {
        // Arrange
        var bills = new[]
        {
            BillInfo.Create("Rent", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Monthly),
        };
        var paycheckAmount = MoneyValue.Create("USD", 400m); // Less than $553.85 required

        // Act
        var summary = this._calculator.CalculateAllocationSummary(
            bills,
            RecurrenceFrequency.BiWeekly,
            paycheckAmount);

        // Assert
        Assert.True(summary.HasWarnings);
        Assert.Contains(summary.Warnings, w => w.Type == AllocationWarningType.InsufficientIncome);
        Assert.Equal(153.85m, summary.Shortfall.Amount); // $553.85 - $400
        Assert.Equal(0m, summary.RemainingPerPaycheck.Amount);
    }

    [Fact]
    public void CalculateAllocationSummary_CannotReconcile_GeneratesWarning()
    {
        // Arrange - Annual bills exceed annual income
        var bills = new[]
        {
            BillInfo.Create("Rent", MoneyValue.Create("USD", 5000m), RecurrenceFrequency.Monthly), // $60,000/year
        };
        var paycheckAmount = MoneyValue.Create("USD", 2000m); // $52,000/year

        // Act
        var summary = this._calculator.CalculateAllocationSummary(
            bills,
            RecurrenceFrequency.BiWeekly,
            paycheckAmount);

        // Assert
        Assert.True(summary.CannotReconcile);
        Assert.Contains(summary.Warnings, w => w.Type == AllocationWarningType.CannotReconcile);
    }

    [Fact]
    public void CalculateAllocationSummary_EmptyBills_GeneratesNoBillsWarning()
    {
        // Arrange
        var bills = Array.Empty<BillInfo>();

        // Act
        var summary = this._calculator.CalculateAllocationSummary(
            bills,
            RecurrenceFrequency.BiWeekly);

        // Assert
        Assert.True(summary.HasWarnings);
        Assert.Contains(summary.Warnings, w => w.Type == AllocationWarningType.NoBillsConfigured);
        Assert.Empty(summary.Allocations);
        Assert.Equal(0m, summary.TotalPerPaycheck.Amount);
        Assert.Equal(0m, summary.TotalAnnualBills.Amount);
    }

    [Fact]
    public void CalculateAllocationSummary_NoPaycheckAmount_GeneratesNoIncomeWarning()
    {
        // Arrange
        var bills = new[]
        {
            BillInfo.Create("Rent", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Monthly),
        };

        // Act
        var summary = this._calculator.CalculateAllocationSummary(
            bills,
            RecurrenceFrequency.BiWeekly,
            paycheckAmount: null);

        // Assert
        Assert.True(summary.HasWarnings);
        Assert.Contains(summary.Warnings, w => w.Type == AllocationWarningType.NoIncomeConfigured);
        Assert.Null(summary.PaycheckAmount);
        Assert.Null(summary.TotalAnnualIncome);
    }

    [Fact]
    public void CalculateAllocationSummary_WithNullBills_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => this._calculator.CalculateAllocationSummary(null!, RecurrenceFrequency.BiWeekly));
    }

    [Fact]
    public void CalculateAllocationSummary_PreservesSourceTransactionId()
    {
        // Arrange
        var sourceId = Guid.NewGuid();
        var bill = BillInfo.Create(
            "Subscription",
            MoneyValue.Create("USD", 10m),
            RecurrenceFrequency.Monthly,
            sourceId);

        // Act
        var summary = this._calculator.CalculateAllocationSummary(
            new[] { bill },
            RecurrenceFrequency.BiWeekly);

        // Assert
        Assert.Single(summary.Allocations);
        Assert.Equal(sourceId, summary.Allocations[0].Bill.SourceRecurringTransactionId);
    }

    #endregion

    #region GetAnnualMultiplier Tests (verified through allocation calculations)

    [Theory]
    [InlineData(RecurrenceFrequency.Daily, 365)]
    [InlineData(RecurrenceFrequency.Weekly, 52)]
    [InlineData(RecurrenceFrequency.BiWeekly, 26)]
    [InlineData(RecurrenceFrequency.Monthly, 12)]
    [InlineData(RecurrenceFrequency.Quarterly, 4)]
    [InlineData(RecurrenceFrequency.Yearly, 1)]
    public void CalculateAllocation_VerifyAnnualMultipliers(RecurrenceFrequency frequency, int expectedMultiplier)
    {
        // Arrange
        var bill = BillInfo.Create("Test", MoneyValue.Create("USD", 100m), frequency);

        // Act
        var allocation = this._calculator.CalculateAllocation(bill, RecurrenceFrequency.Yearly);

        // Assert - Since yearly has 1 period, annual amount should be 100 * multiplier
        Assert.Equal(100m * expectedMultiplier, allocation.AnnualAmount.Amount);
    }

    #endregion

    #region GetPeriodsPerYear Tests (verified through allocation calculations)

    [Theory]
    [InlineData(RecurrenceFrequency.Weekly, 52)]
    [InlineData(RecurrenceFrequency.BiWeekly, 26)]
    [InlineData(RecurrenceFrequency.Monthly, 12)]
    public void CalculateAllocation_VerifyPeriodsPerYear(RecurrenceFrequency paycheckFrequency, int expectedPeriods)
    {
        // Arrange - Use yearly bill so annual = $1200
        var bill = BillInfo.Create("Annual Bill", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Yearly);

        // Act
        var allocation = this._calculator.CalculateAllocation(bill, paycheckFrequency);

        // Assert - Per paycheck should be 1200 / periods
        var expectedPerPaycheck = Math.Round(1200m / expectedPeriods, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedPerPaycheck, allocation.AmountPerPaycheck.Amount);
    }

    #endregion
}
