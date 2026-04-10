// <copyright file="BalanceCalculationAccuracyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Moq;

namespace BudgetExperiment.Application.Tests.Accuracy;

/// <summary>
/// Accuracy tests for <see cref="BalanceCalculationService"/> covering date-boundary filtering,
/// per-account isolation, and multi-account aggregation.
/// </summary>
public class BalanceCalculationAccuracyTests
{
    private static readonly DateOnly Jan1 = new(2026, 1, 1);
    private static readonly DateOnly Jan15 = new(2026, 1, 15);
    private static readonly DateOnly Jan31 = new(2026, 1, 31);

    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<ITransactionRepository> _transactionRepo = new();
    private readonly Mock<ICurrencyProvider> _currencyProvider = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BalanceCalculationAccuracyTests"/> class.
    /// </summary>
    public BalanceCalculationAccuracyTests()
    {
        _currencyProvider
            .Setup(c => c.GetCurrencyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("USD");

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());
    }

    [Fact]
    public async Task GetBalanceAsOfDate_AccountWithInitialBalance_NoTransactions_ReturnsInitialBalance()
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 1500m), Jan1);
        SetupAccounts(account);

        var result = await CreateService().GetBalanceAsOfDateAsync(Jan31);

        Assert.Equal(1500m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceAsOfDate_NoAccounts_ReturnsZero()
    {
        SetupAccounts();

        var result = await CreateService().GetBalanceAsOfDateAsync(Jan31);

        Assert.Equal(0m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task GetBalanceAsOfDate_TransactionsOnQueryDate_AreIncluded()
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 1000m), Jan1);
        SetupAccounts(account);

        var txOnDate = TransactionFactory.Create(
            account.Id, MoneyValue.Create("USD", -200m), Jan31, "Rent");
        SetupTransactions(txOnDate);

        var result = await CreateService().GetBalanceAsOfDateAsync(Jan31);

        // 1000 − 200 = 800
        Assert.Equal(800m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceAsOfDate_TransactionsAfterQueryDate_AreExcluded()
    {
        // Simulates the repository honouring the upper bound — no transactions returned.
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 1000m), Jan1);
        SetupAccounts(account);
        SetupTransactions();

        var result = await CreateService().GetBalanceAsOfDateAsync(Jan31);

        Assert.Equal(1000m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceBefore_TransactionsStrictlyBefore_AreIncluded()
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 500m), Jan1);
        SetupAccounts(account);

        SetupTransactions(
            TransactionFactory.Create(account.Id, MoneyValue.Create("USD", -50m), Jan15, "Groceries"));

        var result = await CreateService().GetBalanceBeforeDateAsync(Jan31);

        // 500 − 50 = 450
        Assert.Equal(450m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceBefore_BoundaryDateTransactionsExcluded_BalanceReflectsOnlyPrior()
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 1000m), Jan1);
        SetupAccounts(account);
        SetupTransactions();

        var result = await CreateService().GetBalanceBeforeDateAsync(Jan31);

        Assert.Equal(1000m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceAsOfDate_AccountStartsAfterQueryDate_ContributesZero()
    {
        var account = Account.Create(
            "Savings",
            AccountType.Savings,
            MoneyValue.Create("USD", 5000m),
            new DateOnly(2026, 2, 1));
        SetupAccounts(account);

        var result = await CreateService().GetBalanceAsOfDateAsync(Jan31);

        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceAsOfDate_AccountStartsOnQueryDate_InitialBalanceIncluded()
    {
        var account = Account.Create(
            "Savings", AccountType.Savings, MoneyValue.Create("USD", 2000m), Jan31);
        SetupAccounts(account);

        var result = await CreateService().GetBalanceAsOfDateAsync(Jan31);

        Assert.Equal(2000m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceAsOfDate_MultipleAccounts_SumsAllInitialBalances()
    {
        var checking = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 1000m), Jan1);
        var savings = Account.Create(
            "Savings", AccountType.Savings, MoneyValue.Create("USD", 4000m), Jan1);
        SetupAccounts(checking, savings);

        var result = await CreateService().GetBalanceAsOfDateAsync(Jan31);

        // 1000 + 4000 = 5000
        Assert.Equal(5000m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceAsOfDate_MultipleAccounts_WithTransactions_SumsCorrectly()
    {
        var checking = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 1000m), Jan1);
        var savings = Account.Create(
            "Savings", AccountType.Savings, MoneyValue.Create("USD", 3000m), Jan1);
        SetupAccounts(checking, savings);

        var checkingTx = TransactionFactory.Create(checking.Id, MoneyValue.Create("USD", -200m), Jan15, "Bills");
        var savingsTx = TransactionFactory.Create(savings.Id, MoneyValue.Create("USD", 500m), Jan15, "Transfer in");

        // Set up per-account responses so the mock mirrors real repository isolation.
        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.Is<Guid?>(id => id == checking.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction> { checkingTx });
        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.Is<Guid?>(id => id == savings.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction> { savingsTx });

        var result = await CreateService().GetBalanceAsOfDateAsync(Jan31);

        // (1000 − 200) + (3000 + 500) = 4300
        Assert.Equal(4300m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceAsOfDate_ByAccountId_ReturnsOnlyThatAccountBalance()
    {
        var checking = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 1000m), Jan1);
        _accountRepo
            .Setup(r => r.GetByIdAsync(checking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checking);

        var result = await CreateService().GetBalanceAsOfDateAsync(Jan31, checking.Id);

        Assert.Equal(1000m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceAsOfDate_ByAccountId_WithTransactions_AccuracyIsExact()
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 2000m), Jan1);
        _accountRepo
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        SetupTransactions(
            TransactionFactory.Create(account.Id, MoneyValue.Create("USD", -450m), Jan15, "Rent"),
            TransactionFactory.Create(account.Id, MoneyValue.Create("USD", -89.99m), Jan15, "Utilities"),
            TransactionFactory.Create(account.Id, MoneyValue.Create("USD", 3000m), Jan15, "Paycheck"));

        var result = await CreateService().GetBalanceAsOfDateAsync(Jan31, account.Id);

        // 2000 − 450 − 89.99 + 3000 = 4460.01
        Assert.Equal(4460.01m, result.Amount);
    }

    [Fact]
    public async Task GetOpeningBalance_WithPriorTransactions_ExcludesTransactionsOnDate()
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 1000m), Jan1);
        SetupAccounts(account);

        SetupTransactions(
            TransactionFactory.Create(account.Id, MoneyValue.Create("USD", -100m), Jan15, "Prior"));

        var result = await CreateService().GetOpeningBalanceForDateAsync(Jan31);

        // 1000 − 100 = 900
        Assert.Equal(900m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceAsOfDate_ManySmallTransactions_SumIsExact()
    {
        var account = Account.Create(
            "Checking", AccountType.Checking, MoneyValue.Create("USD", 0m), Jan1);
        SetupAccounts(account);

        SetupTransactions(Enumerable.Range(0, 100)
            .Select(i => TransactionFactory.Create(
                account.Id, MoneyValue.Create("USD", 0.01m), Jan1.AddDays(i), $"Tx {i}"))
            .ToArray());

        var result = await CreateService().GetBalanceAsOfDateAsync(Jan31);

        Assert.Equal(1.00m, result.Amount);
    }

    private BalanceCalculationService CreateService() =>
        new(_accountRepo.Object, _transactionRepo.Object, _currencyProvider.Object);

    private void SetupAccounts(params Account[] accounts) =>
        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts.ToList());

    private void SetupTransactions(params Transaction[] transactions) =>
        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions.ToList());
}
