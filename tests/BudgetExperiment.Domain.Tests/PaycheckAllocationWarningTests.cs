// <copyright file="PaycheckAllocationWarningTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the <see cref="PaycheckAllocationWarning"/> value object.
/// </summary>
public class PaycheckAllocationWarningTests
{
    [Fact]
    public void InsufficientIncome_CreatesWarningWithCorrectType()
    {
        // Arrange
        var shortfall = MoneyValue.Create("USD", 500m);

        // Act
        var warning = PaycheckAllocationWarning.InsufficientIncome(shortfall);

        // Assert
        Assert.Equal(AllocationWarningType.InsufficientIncome, warning.Type);
        Assert.Contains("500", warning.Message);
        Assert.Equal(shortfall, warning.Amount);
    }

    [Fact]
    public void CannotReconcile_CreatesWarningWithCorrectType()
    {
        // Arrange
        var annualBills = MoneyValue.Create("USD", 60000m);
        var annualIncome = MoneyValue.Create("USD", 50000m);

        // Act
        var warning = PaycheckAllocationWarning.CannotReconcile(annualBills, annualIncome);

        // Assert
        Assert.Equal(AllocationWarningType.CannotReconcile, warning.Type);
        Assert.Contains("60000", warning.Message);
        Assert.Contains("50000", warning.Message);
        Assert.NotNull(warning.Amount);
    }

    [Fact]
    public void NoBillsConfigured_CreatesWarningWithCorrectType()
    {
        // Act
        var warning = PaycheckAllocationWarning.NoBillsConfigured();

        // Assert
        Assert.Equal(AllocationWarningType.NoBillsConfigured, warning.Type);
        Assert.Contains("bill", warning.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(warning.Amount);
    }

    [Fact]
    public void NoIncomeConfigured_CreatesWarningWithCorrectType()
    {
        // Act
        var warning = PaycheckAllocationWarning.NoIncomeConfigured();

        // Assert
        Assert.Equal(AllocationWarningType.NoIncomeConfigured, warning.Type);
        Assert.Contains("income", warning.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(warning.Amount);
    }

    [Fact]
    public void Create_WithAllParameters_CreatesWarning()
    {
        // Arrange
        var type = AllocationWarningType.InsufficientIncome;
        var message = "Custom warning message";
        var amount = MoneyValue.Create("USD", 100m);

        // Act
        var warning = PaycheckAllocationWarning.Create(type, message, amount);

        // Assert
        Assert.Equal(type, warning.Type);
        Assert.Equal(message, warning.Message);
        Assert.Equal(amount, warning.Amount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyMessage_ThrowsDomainException(string? message)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(
            () => PaycheckAllocationWarning.Create(AllocationWarningType.InsufficientIncome, message!));
        Assert.Contains("message", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Warning_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var warning1 = PaycheckAllocationWarning.NoBillsConfigured();
        var warning2 = PaycheckAllocationWarning.NoBillsConfigured();

        // Act & Assert
        Assert.Equal(warning1, warning2);
    }
}
