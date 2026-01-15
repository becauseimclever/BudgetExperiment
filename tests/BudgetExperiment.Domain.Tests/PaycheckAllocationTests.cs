// <copyright file="PaycheckAllocationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the <see cref="PaycheckAllocation"/> value object.
/// </summary>
public class PaycheckAllocationTests
{
    [Fact]
    public void Create_WithValidInputs_CreatesAllocation()
    {
        // Arrange
        var bill = BillInfo.Create("Rent", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Monthly);
        var amountPerPaycheck = MoneyValue.Create("USD", 553.85m);
        var annualAmount = MoneyValue.Create("USD", 14400m);

        // Act
        var allocation = PaycheckAllocation.Create(bill, amountPerPaycheck, annualAmount);

        // Assert
        Assert.Equal(bill, allocation.Bill);
        Assert.Equal(amountPerPaycheck, allocation.AmountPerPaycheck);
        Assert.Equal(annualAmount, allocation.AnnualAmount);
    }

    [Fact]
    public void Create_WithNullBill_ThrowsArgumentNullException()
    {
        // Arrange
        var amountPerPaycheck = MoneyValue.Create("USD", 100m);
        var annualAmount = MoneyValue.Create("USD", 1200m);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => PaycheckAllocation.Create(null!, amountPerPaycheck, annualAmount));
    }

    [Fact]
    public void Create_WithNullAmountPerPaycheck_ThrowsArgumentNullException()
    {
        // Arrange
        var bill = BillInfo.Create("Rent", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Monthly);
        var annualAmount = MoneyValue.Create("USD", 14400m);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => PaycheckAllocation.Create(bill, null!, annualAmount));
    }

    [Fact]
    public void Create_WithNullAnnualAmount_ThrowsArgumentNullException()
    {
        // Arrange
        var bill = BillInfo.Create("Rent", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Monthly);
        var amountPerPaycheck = MoneyValue.Create("USD", 553.85m);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => PaycheckAllocation.Create(bill, amountPerPaycheck, null!));
    }

    [Fact]
    public void Allocation_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var bill = BillInfo.Create("Rent", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Monthly);
        var amountPerPaycheck = MoneyValue.Create("USD", 553.85m);
        var annualAmount = MoneyValue.Create("USD", 14400m);

        var allocation1 = PaycheckAllocation.Create(bill, amountPerPaycheck, annualAmount);
        var allocation2 = PaycheckAllocation.Create(bill, amountPerPaycheck, annualAmount);

        // Act & Assert
        Assert.Equal(allocation1, allocation2);
    }

    [Fact]
    public void Allocation_DifferentValues_NotEqual()
    {
        // Arrange
        var bill1 = BillInfo.Create("Rent", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Monthly);
        var bill2 = BillInfo.Create("Insurance", MoneyValue.Create("USD", 600m), RecurrenceFrequency.Quarterly);
        var amountPerPaycheck = MoneyValue.Create("USD", 553.85m);
        var annualAmount = MoneyValue.Create("USD", 14400m);

        var allocation1 = PaycheckAllocation.Create(bill1, amountPerPaycheck, annualAmount);
        var allocation2 = PaycheckAllocation.Create(bill2, amountPerPaycheck, annualAmount);

        // Act & Assert
        Assert.NotEqual(allocation1, allocation2);
    }
}
