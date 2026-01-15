// <copyright file="PaycheckAllocationSummaryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the <see cref="PaycheckAllocationSummary"/> value object.
/// </summary>
public class PaycheckAllocationSummaryTests
{
    [Fact]
    public void Create_WithValidInputs_CreatesSummary()
    {
        // Arrange
        var allocations = new List<PaycheckAllocation>();
        var totalPerPaycheck = MoneyValue.Create("USD", 646.16m);
        var totalAnnualBills = MoneyValue.Create("USD", 16800m);
        var paycheckFrequency = RecurrenceFrequency.BiWeekly;
        var warnings = new List<PaycheckAllocationWarning>();

        // Act
        var summary = PaycheckAllocationSummary.Create(
            allocations,
            totalPerPaycheck,
            totalAnnualBills,
            paycheckFrequency,
            warnings);

        // Assert
        Assert.Empty(summary.Allocations);
        Assert.Equal(totalPerPaycheck, summary.TotalPerPaycheck);
        Assert.Equal(totalAnnualBills, summary.TotalAnnualBills);
        Assert.Equal(paycheckFrequency, summary.PaycheckFrequency);
        Assert.Empty(summary.Warnings);
        Assert.False(summary.HasWarnings);
        Assert.False(summary.CannotReconcile);
        Assert.Null(summary.PaycheckAmount);
        Assert.Null(summary.TotalAnnualIncome);
    }

    [Fact]
    public void Create_WithPaycheckAmount_CalculatesRemainingAndShortfall()
    {
        // Arrange
        var bill = BillInfo.Create("Rent", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Monthly);
        var allocation = PaycheckAllocation.Create(
            bill,
            MoneyValue.Create("USD", 553.85m),
            MoneyValue.Create("USD", 14400m));
        var allocations = new List<PaycheckAllocation> { allocation };
        var totalPerPaycheck = MoneyValue.Create("USD", 553.85m);
        var totalAnnualBills = MoneyValue.Create("USD", 14400m);
        var paycheckFrequency = RecurrenceFrequency.BiWeekly;
        var paycheckAmount = MoneyValue.Create("USD", 2000m);
        var totalAnnualIncome = MoneyValue.Create("USD", 52000m);
        var warnings = new List<PaycheckAllocationWarning>();

        // Act
        var summary = PaycheckAllocationSummary.Create(
            allocations,
            totalPerPaycheck,
            totalAnnualBills,
            paycheckFrequency,
            warnings,
            paycheckAmount,
            totalAnnualIncome);

        // Assert
        Assert.Equal(paycheckAmount, summary.PaycheckAmount);
        Assert.Equal(totalAnnualIncome, summary.TotalAnnualIncome);
        Assert.Equal(1446.15m, summary.RemainingPerPaycheck.Amount);
        Assert.Equal(0m, summary.Shortfall.Amount);
    }

    [Fact]
    public void Create_WithInsufficientIncome_ShowsShortfall()
    {
        // Arrange
        var bill = BillInfo.Create("Rent", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Monthly);
        var allocation = PaycheckAllocation.Create(
            bill,
            MoneyValue.Create("USD", 553.85m),
            MoneyValue.Create("USD", 14400m));
        var allocations = new List<PaycheckAllocation> { allocation };
        var totalPerPaycheck = MoneyValue.Create("USD", 553.85m);
        var totalAnnualBills = MoneyValue.Create("USD", 14400m);
        var paycheckFrequency = RecurrenceFrequency.BiWeekly;
        var paycheckAmount = MoneyValue.Create("USD", 400m); // Less than allocation
        var totalAnnualIncome = MoneyValue.Create("USD", 10400m);
        var warnings = new List<PaycheckAllocationWarning>();

        // Act
        var summary = PaycheckAllocationSummary.Create(
            allocations,
            totalPerPaycheck,
            totalAnnualBills,
            paycheckFrequency,
            warnings,
            paycheckAmount,
            totalAnnualIncome);

        // Assert
        Assert.Equal(0m, summary.RemainingPerPaycheck.Amount);
        Assert.Equal(153.85m, summary.Shortfall.Amount);
    }

    [Fact]
    public void HasWarnings_WithWarnings_ReturnsTrue()
    {
        // Arrange
        var totalPerPaycheck = MoneyValue.Create("USD", 0m);
        var totalAnnualBills = MoneyValue.Create("USD", 0m);
        var paycheckFrequency = RecurrenceFrequency.BiWeekly;
        var warnings = new List<PaycheckAllocationWarning>
        {
            PaycheckAllocationWarning.NoBillsConfigured(),
        };

        // Act
        var summary = PaycheckAllocationSummary.Create(
            new List<PaycheckAllocation>(),
            totalPerPaycheck,
            totalAnnualBills,
            paycheckFrequency,
            warnings);

        // Assert
        Assert.True(summary.HasWarnings);
        Assert.Single(summary.Warnings);
    }

    [Fact]
    public void CannotReconcile_WhenAnnualBillsExceedIncome_ReturnsTrue()
    {
        // Arrange
        var totalPerPaycheck = MoneyValue.Create("USD", 1000m);
        var totalAnnualBills = MoneyValue.Create("USD", 60000m);
        var paycheckFrequency = RecurrenceFrequency.BiWeekly;
        var paycheckAmount = MoneyValue.Create("USD", 2000m);
        var totalAnnualIncome = MoneyValue.Create("USD", 52000m); // Less than bills
        var cannotReconcileWarning = PaycheckAllocationWarning.CannotReconcile(
            totalAnnualBills,
            totalAnnualIncome);
        var warnings = new List<PaycheckAllocationWarning> { cannotReconcileWarning };

        // Act
        var summary = PaycheckAllocationSummary.Create(
            new List<PaycheckAllocation>(),
            totalPerPaycheck,
            totalAnnualBills,
            paycheckFrequency,
            warnings,
            paycheckAmount,
            totalAnnualIncome);

        // Assert
        Assert.True(summary.CannotReconcile);
    }

    [Fact]
    public void Allocations_IsImmutableCollection()
    {
        // Arrange
        var bill = BillInfo.Create("Rent", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Monthly);
        var allocation = PaycheckAllocation.Create(
            bill,
            MoneyValue.Create("USD", 553.85m),
            MoneyValue.Create("USD", 14400m));
        var originalList = new List<PaycheckAllocation> { allocation };
        var totalPerPaycheck = MoneyValue.Create("USD", 553.85m);
        var totalAnnualBills = MoneyValue.Create("USD", 14400m);
        var paycheckFrequency = RecurrenceFrequency.BiWeekly;

        // Act
        var summary = PaycheckAllocationSummary.Create(
            originalList,
            totalPerPaycheck,
            totalAnnualBills,
            paycheckFrequency,
            new List<PaycheckAllocationWarning>());

        // Modify the original list
        originalList.Clear();

        // Assert - summary should still have the allocation
        Assert.Single(summary.Allocations);
    }

    [Fact]
    public void Create_WithNullAllocations_ThrowsArgumentNullException()
    {
        // Arrange
        var totalPerPaycheck = MoneyValue.Create("USD", 0m);
        var totalAnnualBills = MoneyValue.Create("USD", 0m);
        var paycheckFrequency = RecurrenceFrequency.BiWeekly;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PaycheckAllocationSummary.Create(
            null!,
            totalPerPaycheck,
            totalAnnualBills,
            paycheckFrequency,
            new List<PaycheckAllocationWarning>()));
    }

    [Fact]
    public void Create_WithNullTotalPerPaycheck_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PaycheckAllocationSummary.Create(
            new List<PaycheckAllocation>(),
            null!,
            MoneyValue.Create("USD", 0m),
            RecurrenceFrequency.BiWeekly,
            new List<PaycheckAllocationWarning>()));
    }

    [Fact]
    public void Create_WithNullWarnings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PaycheckAllocationSummary.Create(
            new List<PaycheckAllocation>(),
            MoneyValue.Create("USD", 0m),
            MoneyValue.Create("USD", 0m),
            RecurrenceFrequency.BiWeekly,
            null!));
    }
}
