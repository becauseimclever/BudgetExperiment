// <copyright file="AccountBalanceAccuracyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Tests.Accuracy;

/// <summary>
/// Accuracy tests asserting account balance invariants from the application layer.
/// Focuses on order invariance and compound scenarios not covered in the domain-level
/// <c>AccountBalanceAccuracyTests</c>.
/// </summary>
[Trait("Category", "Accuracy")]
public class AccountBalanceAccuracyTests
{
    private static readonly DateOnly Jan1 = new(2026, 1, 1);

    [Fact]
    public void Balance_TransactionInsertionOrder_DoesNotAffectFinalBalance()
    {
        // Arrange
        var accountA = Account.Create("Checking A", AccountType.Checking, MoneyValue.Create("USD", 1000m), Jan1);
        var accountB = Account.Create("Checking B", AccountType.Checking, MoneyValue.Create("USD", 1000m), Jan1);

        // Act — add in forward order to accountA
        accountA.AddTransaction(MoneyValue.Create("USD", -200m), Jan1.AddDays(1), "Rent");
        accountA.AddTransaction(MoneyValue.Create("USD", 500m), Jan1.AddDays(2), "Salary");
        accountA.AddTransaction(MoneyValue.Create("USD", -75m), Jan1.AddDays(3), "Groceries");
        accountA.AddTransaction(MoneyValue.Create("USD", -30m), Jan1.AddDays(4), "Gas");

        // Act — add in reverse order to accountB
        accountB.AddTransaction(MoneyValue.Create("USD", -30m), Jan1.AddDays(4), "Gas");
        accountB.AddTransaction(MoneyValue.Create("USD", -75m), Jan1.AddDays(3), "Groceries");
        accountB.AddTransaction(MoneyValue.Create("USD", 500m), Jan1.AddDays(2), "Salary");
        accountB.AddTransaction(MoneyValue.Create("USD", -200m), Jan1.AddDays(1), "Rent");

        // Assert — balance must be identical regardless of insertion order
        Assert.Equal(ComputeBalance(accountA), ComputeBalance(accountB));
        Assert.Equal(1195m, ComputeBalance(accountA));
    }

    [Fact]
    public void FullJourneyScenario_OpenDepositWithdrawDeposit_FinalBalanceIsExact()
    {
        // Arrange — open account with no initial balance
        var account = Account.Create("Savings", AccountType.Savings);

        // Act
        account.AddTransaction(MoneyValue.Create("USD", 2500m), Jan1.AddDays(1), "Opening deposit");
        account.AddTransaction(MoneyValue.Create("USD", -800m), Jan1.AddDays(5), "Rent");
        account.AddTransaction(MoneyValue.Create("USD", -150m), Jan1.AddDays(7), "Groceries");
        account.AddTransaction(MoneyValue.Create("USD", 2500m), Jan1.AddDays(15), "Second deposit");
        account.AddTransaction(MoneyValue.Create("USD", -99.99m), Jan1.AddDays(20), "Utilities");

        // Assert: 0 + 2500 − 800 − 150 + 2500 − 99.99 = 3950.01
        Assert.Equal(3950.01m, ComputeBalance(account));
    }

    [Fact]
    public void Balance_RemovingTransactionFromMiddle_RemainingTransactionsAreUnaffected()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking, MoneyValue.Create("USD", 500m), Jan1);

        account.AddTransaction(MoneyValue.Create("USD", -100m), Jan1.AddDays(1), "Coffee");
        var targetTx = account.AddTransaction(MoneyValue.Create("USD", -200m), Jan1.AddDays(2), "Rent — to delete");
        account.AddTransaction(MoneyValue.Create("USD", -50m), Jan1.AddDays(3), "Gas");

        // Act — remove only the middle transaction
        account.RemoveTransaction(targetTx.Id);

        // Assert: 500 − 100 − 50 = 350
        Assert.Equal(350m, ComputeBalance(account));
    }

    [Fact]
    public void Balance_LargeInitialBalance_WithPrecisionTransactions_IsDecimalExact()
    {
        // Arrange — large initial balance with sub-dollar amounts
        var account = Account.Create(
            "Investment",
            AccountType.Savings,
            MoneyValue.Create("USD", 100000.00m),
            Jan1);

        // Act — 10 transactions totalling exactly −$0.10
        for (var i = 0; i < 10; i++)
        {
            account.AddTransaction(MoneyValue.Create("USD", -0.01m), Jan1.AddDays(i + 1), $"Fee {i}");
        }

        // Assert — $100,000.00 − $0.10 = $99,999.90
        Assert.Equal(99999.90m, ComputeBalance(account));
    }

    [Fact]
    public void Balance_NegativeInitialBalance_CreditTransactionsConvergeTowardsZero()
    {
        // Arrange — credit-card style account starting in debt
        var card = Account.Create(
            "Credit Card",
            AccountType.CreditCard,
            MoneyValue.Create("USD", -5000m),
            Jan1);

        // Act — three payments that together exactly cover the debt
        card.AddTransaction(MoneyValue.Create("USD", 2000m), Jan1.AddDays(5), "Payment 1");
        card.AddTransaction(MoneyValue.Create("USD", 2000m), Jan1.AddDays(20), "Payment 2");
        card.AddTransaction(MoneyValue.Create("USD", 1000m), Jan1.AddDays(30), "Payment 3");

        // Assert
        Assert.Equal(0m, ComputeBalance(card));
    }

    private static decimal ComputeBalance(Account account) =>
        account.InitialBalance.Amount + account.Transactions.Sum(t => t.Amount.Amount);
}
