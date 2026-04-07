// <copyright file="AccountBalanceAccuracyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Accuracy tests ensuring the account balance is always correct after the most common user actions.
/// Core invariant: Balance = InitialBalance.Amount + Sum(Transactions.Amount).
/// </summary>
public class AccountBalanceAccuracyTests
{
    private static readonly DateOnly Jan1 = new(2026, 1, 1);

    [Theory]
    [InlineData(0)]
    [InlineData(1000.00)]
    [InlineData(5432.10)]
    [InlineData(-2500.00)]
    public void NewAccount_WithInitialBalance_BalanceEqualsInitialBalance(decimal initial)
    {
        var account = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", initial),
            Jan1);

        Assert.Equal(initial, ComputeBalance(account));
    }

    [Fact]
    public void NewAccount_WithDefaultInitialBalance_BalanceIsZero()
    {
        var account = Account.Create("Checking", AccountType.Checking);

        Assert.Equal(0m, ComputeBalance(account));
    }

    [Theory]
    [InlineData(1000.00, -50.00, 950.00)]
    [InlineData(500.00, -500.00, 0.00)]
    [InlineData(0.00, -25.99, -25.99)]
    public void AddDebit_ReducesBalance_ByExactAmount(decimal initial, decimal debit, decimal expected)
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", initial), Jan1);

        account.AddTransaction(MoneyValue.Create("USD", debit), Jan1.AddDays(1), "Purchase");

        Assert.Equal(expected, ComputeBalance(account));
    }

    [Theory]
    [InlineData(1000.00, 200.00, 1200.00)]
    [InlineData(0.00, 1500.00, 1500.00)]
    [InlineData(-2500.00, 100.00, -2400.00)]
    public void AddCredit_IncreasesBalance_ByExactAmount(decimal initial, decimal credit, decimal expected)
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", initial), Jan1);

        account.AddTransaction(MoneyValue.Create("USD", credit), Jan1.AddDays(1), "Deposit");

        Assert.Equal(expected, ComputeBalance(account));
    }

    [Fact]
    public void AddMultipleTransactions_BalanceIsInitialPlusSumOfAll()
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 1000m), Jan1);

        account.AddTransaction(MoneyValue.Create("USD", -50m), Jan1.AddDays(1), "Coffee");
        account.AddTransaction(MoneyValue.Create("USD", -200m), Jan1.AddDays(2), "Groceries");
        account.AddTransaction(MoneyValue.Create("USD", 2000m), Jan1.AddDays(3), "Paycheck");
        account.AddTransaction(MoneyValue.Create("USD", -15m), Jan1.AddDays(4), "Subscription");

        // 1000 − 50 − 200 + 2000 − 15 = 2735
        Assert.Equal(2735m, ComputeBalance(account));
    }

    [Fact]
    public void AddDebitAndMatchingCredit_BalanceUnchanged()
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 1000m), Jan1);

        account.AddTransaction(MoneyValue.Create("USD", -300m), Jan1.AddDays(1), "Rent");
        account.AddTransaction(MoneyValue.Create("USD", 300m), Jan1.AddDays(2), "Refund");

        Assert.Equal(1000m, ComputeBalance(account));
    }

    [Fact]
    public void UpdateTransactionAmount_BalanceReflectsNewAmount()
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 1000m), Jan1);

        var transaction = account.AddTransaction(
            MoneyValue.Create("USD", -50m), Jan1.AddDays(1), "Coffee");

        transaction.UpdateAmount(MoneyValue.Create("USD", -4.50m));

        // 1000 − 4.50 = 995.50
        Assert.Equal(995.50m, ComputeBalance(account));
    }

    [Fact]
    public void SequentialAmountEdits_BalanceIsAlwaysAccurate()
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 1000m), Jan1);

        var transaction = account.AddTransaction(
            MoneyValue.Create("USD", -50m), Jan1.AddDays(1), "Purchase");

        Assert.Equal(950m, ComputeBalance(account));

        transaction.UpdateAmount(MoneyValue.Create("USD", -75m));
        Assert.Equal(925m, ComputeBalance(account));

        transaction.UpdateAmount(MoneyValue.Create("USD", -10m));
        Assert.Equal(990m, ComputeBalance(account));
    }

    [Fact]
    public void RemoveTransaction_RestoresBalanceToPreviousState()
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 1000m), Jan1);

        var balanceBefore = ComputeBalance(account);

        var transaction = account.AddTransaction(
            MoneyValue.Create("USD", -75m), Jan1.AddDays(1), "Lunch");
        account.RemoveTransaction(transaction.Id);

        Assert.Equal(balanceBefore, ComputeBalance(account));
    }

    [Fact]
    public void RemoveOneOfManyTransactions_BalanceReflectsRemovalOnly()
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 1000m), Jan1);

        var tx1 = account.AddTransaction(MoneyValue.Create("USD", -100m), Jan1.AddDays(1), "Rent");
        account.AddTransaction(MoneyValue.Create("USD", -50m), Jan1.AddDays(2), "Groceries");

        account.RemoveTransaction(tx1.Id);

        // 1000 − 50 = 950 (rent was removed, groceries remain)
        Assert.Equal(950m, ComputeBalance(account));
    }

    [Fact]
    public void CreditCard_InitialDebt_ChargesAndPayments_BalanceIsCorrect()
    {
        var card = Account.Create(
            "Credit Card",
            AccountType.CreditCard,
            MoneyValue.Create("USD", -2500m),
            Jan1);

        card.AddTransaction(MoneyValue.Create("USD", -120m), Jan1.AddDays(1), "Groceries");
        card.AddTransaction(MoneyValue.Create("USD", -45m), Jan1.AddDays(2), "Gas");
        card.AddTransaction(MoneyValue.Create("USD", 500m), Jan1.AddDays(10), "Payment");

        // −2500 − 120 − 45 + 500 = −2165
        Assert.Equal(-2165m, ComputeBalance(card));
    }

    [Fact]
    public void ManySmallTransactions_BalanceIsArithmeticallyExact()
    {
        var account = Account.Create("Checking", AccountType.Checking);

        for (var i = 0; i < 100; i++)
        {
            account.AddTransaction(MoneyValue.Create("USD", 0.01m), Jan1.AddDays(i), $"Tx {i}");
        }

        Assert.Equal(1.00m, ComputeBalance(account));
    }

    [Fact]
    public void LargeNumberOfTransactions_BalanceMatchesManualSum()
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 5000m), Jan1);

        decimal[] amounts =
        [
            -1200m, 3500m, -450m, -89.99m, 1000m,
            -230m, -15m, -99.99m, 2000m, -567.50m,
        ];

        foreach (var (amount, i) in amounts.Select((a, i) => (a, i)))
        {
            account.AddTransaction(MoneyValue.Create("USD", amount), Jan1.AddDays(i + 1), $"Tx {i}");
        }

        Assert.Equal(5000m + amounts.Sum(), ComputeBalance(account));
    }

    [Fact]
    public void ZeroAmountTransaction_BalanceIsUnchanged()
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 500m), Jan1);

        account.AddTransaction(MoneyValue.Create("USD", 0m), Jan1.AddDays(1), "Adjustment");

        Assert.Equal(500m, ComputeBalance(account));
    }

    private static decimal ComputeBalance(Account account) =>
        account.InitialBalance.Amount + account.Transactions.Sum(t => t.Amount.Amount);
}
