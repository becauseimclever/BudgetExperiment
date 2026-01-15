// <copyright file="BillInfoTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the <see cref="BillInfo"/> value object.
/// </summary>
public class BillInfoTests
{
    [Fact]
    public void Create_WithValidInputs_CreatesBillInfo()
    {
        // Arrange
        var description = "Monthly Rent";
        var amount = MoneyValue.Create("USD", 1200m);
        var frequency = RecurrenceFrequency.Monthly;

        // Act
        var bill = BillInfo.Create(description, amount, frequency);

        // Assert
        Assert.Equal(description, bill.Description);
        Assert.Equal(amount, bill.Amount);
        Assert.Equal(frequency, bill.Frequency);
        Assert.Null(bill.SourceRecurringTransactionId);
    }

    [Fact]
    public void Create_WithSourceId_IncludesSourceId()
    {
        // Arrange
        var description = "Car Insurance";
        var amount = MoneyValue.Create("USD", 600m);
        var frequency = RecurrenceFrequency.Quarterly;
        var sourceId = Guid.NewGuid();

        // Act
        var bill = BillInfo.Create(description, amount, frequency, sourceId);

        // Assert
        Assert.Equal(description, bill.Description);
        Assert.Equal(amount, bill.Amount);
        Assert.Equal(frequency, bill.Frequency);
        Assert.Equal(sourceId, bill.SourceRecurringTransactionId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyDescription_ThrowsDomainException(string? description)
    {
        // Arrange
        var amount = MoneyValue.Create("USD", 100m);
        var frequency = RecurrenceFrequency.Monthly;

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => BillInfo.Create(description!, amount, frequency));
        Assert.Contains("description", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithNullAmount_ThrowsArgumentNullException()
    {
        // Arrange
        var description = "Test Bill";
        var frequency = RecurrenceFrequency.Monthly;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BillInfo.Create(description, null!, frequency));
    }

    [Fact]
    public void FromRecurringTransaction_CreatesCorrectBillInfo()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var pattern = RecurrencePattern.CreateMonthly(1, 15);
        var recurring = RecurringTransaction.Create(
            accountId,
            "Netflix Subscription",
            MoneyValue.Create("USD", -15.99m),
            pattern,
            DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var bill = BillInfo.FromRecurringTransaction(recurring);

        // Assert
        Assert.Equal("Netflix Subscription", bill.Description);
        Assert.Equal(15.99m, bill.Amount.Amount); // Should use absolute value for bills
        Assert.Equal(RecurrenceFrequency.Monthly, bill.Frequency);
        Assert.Equal(recurring.Id, bill.SourceRecurringTransactionId);
    }

    [Fact]
    public void FromRecurringTransaction_WithNullTransaction_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BillInfo.FromRecurringTransaction(null!));
    }

    [Fact]
    public void BillInfo_IsImmutable_RecordEquality()
    {
        // Arrange
        var amount = MoneyValue.Create("USD", 500m);
        var bill1 = BillInfo.Create("Electric Bill", amount, RecurrenceFrequency.Monthly);
        var bill2 = BillInfo.Create("Electric Bill", amount, RecurrenceFrequency.Monthly);

        // Act & Assert - record equality
        Assert.Equal(bill1, bill2);
    }

    [Fact]
    public void BillInfo_DifferentValues_NotEqual()
    {
        // Arrange
        var amount = MoneyValue.Create("USD", 500m);
        var bill1 = BillInfo.Create("Electric Bill", amount, RecurrenceFrequency.Monthly);
        var bill2 = BillInfo.Create("Gas Bill", amount, RecurrenceFrequency.Monthly);

        // Act & Assert
        Assert.NotEqual(bill1, bill2);
    }
}
