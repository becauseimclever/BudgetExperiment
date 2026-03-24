// <copyright file="StatementBalanceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the <see cref="StatementBalance"/> aggregate.
/// </summary>
public class StatementBalanceTests
{
    // AC-125b-06
    [Fact]
    public void UpdateBalance_OnCompletedStatementBalance_ThrowsDomainException()
    {
        // Arrange
        var balance = StatementBalance.Create(
            Guid.NewGuid(),
            new DateOnly(2026, 1, 31),
            MoneyValue.Create("USD", 1000.00m));
        balance.MarkCompleted();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            balance.UpdateBalance(MoneyValue.Create("USD", 1200.00m)));

        Assert.Equal(DomainExceptionType.InvalidOperation, ex.ExceptionType);
    }

    [Fact]
    public void Create_SetsAllFieldsCorrectly()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var statementDate = new DateOnly(2026, 1, 31);
        var amount = MoneyValue.Create("USD", 500.00m);

        // Act
        var balance = StatementBalance.Create(accountId, statementDate, amount);

        // Assert
        Assert.NotEqual(Guid.Empty, balance.Id);
        Assert.Equal(accountId, balance.AccountId);
        Assert.Equal(statementDate, balance.StatementDate);
        Assert.Equal(amount, balance.Balance);
        Assert.False(balance.IsCompleted);
    }

    [Fact]
    public void UpdateBalance_OnActiveStatementBalance_UpdatesBalance()
    {
        // Arrange
        var balance = StatementBalance.Create(
            Guid.NewGuid(),
            new DateOnly(2026, 1, 31),
            MoneyValue.Create("USD", 1000.00m));
        var newBalance = MoneyValue.Create("USD", 1200.00m);

        // Act
        balance.UpdateBalance(newBalance);

        // Assert
        Assert.Equal(newBalance, balance.Balance);
    }
}
