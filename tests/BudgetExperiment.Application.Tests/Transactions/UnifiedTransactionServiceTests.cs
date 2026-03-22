// <copyright file="UnifiedTransactionServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Transactions;
using BudgetExperiment.Contracts.Dtos;

using Moq;

namespace BudgetExperiment.Application.Tests.Transactions;

/// <summary>
/// Unit tests for <see cref="UnifiedTransactionService"/>.
/// </summary>
public class UnifiedTransactionServiceTests
{
    [Fact]
    public async Task GetPagedAsync_Returns_Paged_Transactions_With_Account_Names()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);

        var transaction = account.AddTransaction(
            MoneyValue.Create("USD", -50m),
            new DateOnly(2026, 1, 15),
            "Grocery Store");

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo
            .Setup(r => r.GetUnifiedPagedAsync(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "date",
                true,
                0,
                50,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([transaction], 1));

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([account]);

        var service = new UnifiedTransactionService(transactionRepo.Object, accountRepo.Object);

        // Act
        var result = await service.GetPagedAsync(new UnifiedTransactionFilterDto());

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.Equal("Checking", result.Items[0].AccountName);
        Assert.Equal("Grocery Store", result.Items[0].Description);
        Assert.Equal(-50m, result.Items[0].Amount.Amount);
    }

    [Fact]
    public async Task GetPagedAsync_Passes_All_Filter_Parameters_To_Repository()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo
            .Setup(r => r.GetUnifiedPagedAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Transaction>().AsReadOnly(), 0));

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new UnifiedTransactionService(transactionRepo.Object, accountRepo.Object);
        var filter = new UnifiedTransactionFilterDto
        {
            AccountId = accountId,
            CategoryId = categoryId,
            Uncategorized = true,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
            Description = "Amazon",
            MinAmount = 10m,
            MaxAmount = 100m,
            SortBy = "amount",
            SortDescending = false,
            Page = 3,
            PageSize = 25,
        };

        // Act
        await service.GetPagedAsync(filter);

        // Assert
        transactionRepo.Verify(
            r => r.GetUnifiedPagedAsync(
                accountId,
                categoryId,
                true,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 31),
                "Amazon",
                10m,
                100m,
                "amount",
                false,
                50,
                25,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_Clamps_PageSize_To_Maximum_100()
    {
        // Arrange
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo
            .Setup(r => r.GetUnifiedPagedAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Transaction>().AsReadOnly(), 0));

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new UnifiedTransactionService(transactionRepo.Object, accountRepo.Object);

        // Act
        var result = await service.GetPagedAsync(new UnifiedTransactionFilterDto { PageSize = 500 });

        // Assert
        Assert.Equal(100, result.PageSize);
        transactionRepo.Verify(
            r => r.GetUnifiedPagedAsync(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "date",
                true,
                0,
                100,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_Clamps_Page_To_Minimum_1()
    {
        // Arrange
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo
            .Setup(r => r.GetUnifiedPagedAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Transaction>().AsReadOnly(), 0));

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new UnifiedTransactionService(transactionRepo.Object, accountRepo.Object);

        // Act
        var result = await service.GetPagedAsync(new UnifiedTransactionFilterDto { Page = -5 });

        // Assert
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetPagedAsync_Computes_Summary_From_Page_Items()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);
        var t1 = account.AddTransaction(
            MoneyValue.Create("USD", 500m),
            new DateOnly(2026, 1, 10),
            "Paycheck");
        var t2 = account.AddTransaction(
            MoneyValue.Create("USD", -75m),
            new DateOnly(2026, 1, 11),
            "Groceries");
        var t3 = account.AddTransaction(
            MoneyValue.Create("USD", -25m),
            new DateOnly(2026, 1, 12),
            "Gas");

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo
            .Setup(r => r.GetUnifiedPagedAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([t1, t2, t3], 3));

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([account]);

        var service = new UnifiedTransactionService(transactionRepo.Object, accountRepo.Object);

        // Act
        var result = await service.GetPagedAsync(new UnifiedTransactionFilterDto());

        // Assert
        Assert.NotNull(result.Summary);
        Assert.Equal(3, result.Summary.TotalCount);
        Assert.Equal(400m, result.Summary.TotalAmount.Amount);
        Assert.Equal(500m, result.Summary.IncomeTotal.Amount);
        Assert.Equal(-100m, result.Summary.ExpenseTotal.Amount);
        Assert.Equal(3, result.Summary.UncategorizedCount);
    }

    [Fact]
    public async Task GetPagedAsync_Includes_BalanceInfo_When_Single_Account_Filtered()
    {
        // Arrange
        var account = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 1));

        var t1 = account.AddTransaction(
            MoneyValue.Create("USD", -50m),
            new DateOnly(2026, 1, 10),
            "Groceries");

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo
            .Setup(r => r.GetUnifiedPagedAsync(
                account.Id,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "date",
                true,
                0,
                50,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([t1], 1));

        transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                DateOnly.MinValue,
                DateOnly.MaxValue,
                account.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([t1]);

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([account]);

        var service = new UnifiedTransactionService(transactionRepo.Object, accountRepo.Object);

        // Act
        var result = await service.GetPagedAsync(
            new UnifiedTransactionFilterDto { AccountId = account.Id });

        // Assert
        Assert.NotNull(result.BalanceInfo);
        Assert.Equal(1000m, result.BalanceInfo.InitialBalance.Amount);
        Assert.Equal(new DateOnly(2026, 1, 1), result.BalanceInfo.InitialBalanceDate);
        Assert.Equal(950m, result.BalanceInfo.CurrentBalance.Amount);
    }

    [Fact]
    public async Task GetPagedAsync_No_BalanceInfo_When_No_Account_Filter()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);
        var t1 = account.AddTransaction(
            MoneyValue.Create("USD", -50m),
            new DateOnly(2026, 1, 10),
            "Groceries");

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo
            .Setup(r => r.GetUnifiedPagedAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([t1], 1));

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([account]);

        var service = new UnifiedTransactionService(transactionRepo.Object, accountRepo.Object);

        // Act
        var result = await service.GetPagedAsync(new UnifiedTransactionFilterDto());

        // Assert
        Assert.Null(result.BalanceInfo);
    }

    [Fact]
    public async Task GetPagedAsync_Maps_Transaction_Fields_Correctly()
    {
        // Arrange
        var account = Account.Create("Savings", AccountType.Savings);
        var categoryId = Guid.NewGuid();

        var transaction = account.AddTransaction(
            MoneyValue.Create("USD", -99.99m),
            new DateOnly(2026, 3, 5),
            "Online Purchase",
            categoryId);

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo
            .Setup(r => r.GetUnifiedPagedAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([transaction], 1));

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([account]);

        var service = new UnifiedTransactionService(transactionRepo.Object, accountRepo.Object);

        // Act
        var result = await service.GetPagedAsync(new UnifiedTransactionFilterDto());

        // Assert
        var item = Assert.Single(result.Items);
        Assert.Equal(transaction.Id, item.Id);
        Assert.Equal(new DateOnly(2026, 3, 5), item.Date);
        Assert.Equal("Online Purchase", item.Description);
        Assert.Equal(-99.99m, item.Amount.Amount);
        Assert.Equal("USD", item.Amount.Currency);
        Assert.Equal(account.Id, item.AccountId);
        Assert.Equal("Savings", item.AccountName);
        Assert.Equal(categoryId, item.CategoryId);
        Assert.False(item.IsTransfer);
    }

    [Fact]
    public async Task GetPagedAsync_Counts_Uncategorized_In_Summary()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);
        var categoryId = Guid.NewGuid();

        var categorized = account.AddTransaction(
            MoneyValue.Create("USD", -50m),
            new DateOnly(2026, 1, 10),
            "Groceries",
            categoryId);
        var uncategorized = account.AddTransaction(
            MoneyValue.Create("USD", -30m),
            new DateOnly(2026, 1, 11),
            "Unknown");

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo
            .Setup(r => r.GetUnifiedPagedAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([categorized, uncategorized], 2));

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([account]);

        var service = new UnifiedTransactionService(transactionRepo.Object, accountRepo.Object);

        // Act
        var result = await service.GetPagedAsync(new UnifiedTransactionFilterDto());

        // Assert
        Assert.NotNull(result.Summary);
        Assert.Equal(1, result.Summary.UncategorizedCount);
    }

    [Fact]
    public async Task GetPagedAsync_Returns_Empty_Page_When_No_Transactions()
    {
        // Arrange
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo
            .Setup(r => r.GetUnifiedPagedAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Transaction>().AsReadOnly(), 0));

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new UnifiedTransactionService(transactionRepo.Object, accountRepo.Object);

        // Act
        var result = await service.GetPagedAsync(new UnifiedTransactionFilterDto());

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.NotNull(result.Summary);
        Assert.Equal(0, result.Summary.TotalCount);
    }

    [Fact]
    public async Task GetPagedAsync_Computes_RunningBalance_When_Single_Account_Filtered()
    {
        // Arrange
        var account = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 1));

        var t1 = account.AddTransaction(
            MoneyValue.Create("USD", -50m),
            new DateOnly(2026, 1, 10),
            "Groceries");
        var t2 = account.AddTransaction(
            MoneyValue.Create("USD", -30m),
            new DateOnly(2026, 1, 15),
            "Gas");
        var t3 = account.AddTransaction(
            MoneyValue.Create("USD", 500m),
            new DateOnly(2026, 1, 20),
            "Paycheck");

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo
            .Setup(r => r.GetUnifiedPagedAsync(
                account.Id,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "date",
                true,
                0,
                50,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([t1, t2, t3], 3));

        transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                DateOnly.MinValue,
                DateOnly.MaxValue,
                account.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([t1, t2, t3]);

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([account]);

        var service = new UnifiedTransactionService(transactionRepo.Object, accountRepo.Object);

        // Act
        var result = await service.GetPagedAsync(
            new UnifiedTransactionFilterDto { AccountId = account.Id });

        // Assert — running balance = initial (1000) + cumulative transactions
        // t1 (Jan 10, -50): 1000 + (-50) = 950
        // t2 (Jan 15, -30): 950 + (-30) = 920
        // t3 (Jan 20, +500): 920 + 500 = 1420
        var item1 = result.Items.Single(i => i.Id == t1.Id);
        var item2 = result.Items.Single(i => i.Id == t2.Id);
        var item3 = result.Items.Single(i => i.Id == t3.Id);

        Assert.NotNull(item1.RunningBalance);
        Assert.Equal(950m, item1.RunningBalance.Amount);
        Assert.Equal("USD", item1.RunningBalance.Currency);

        Assert.NotNull(item2.RunningBalance);
        Assert.Equal(920m, item2.RunningBalance.Amount);

        Assert.NotNull(item3.RunningBalance);
        Assert.Equal(1420m, item3.RunningBalance.Amount);
    }

    [Fact]
    public async Task GetPagedAsync_RunningBalance_Null_When_No_Account_Filter()
    {
        // Arrange
        var account = Account.Create("Checking", AccountType.Checking);
        var t1 = account.AddTransaction(
            MoneyValue.Create("USD", -50m),
            new DateOnly(2026, 1, 10),
            "Groceries");

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo
            .Setup(r => r.GetUnifiedPagedAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([t1], 1));

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([account]);

        var service = new UnifiedTransactionService(transactionRepo.Object, accountRepo.Object);

        // Act
        var result = await service.GetPagedAsync(new UnifiedTransactionFilterDto());

        // Assert
        Assert.Null(result.Items[0].RunningBalance);
    }

    [Fact]
    public async Task GetPagedAsync_RunningBalance_Considers_All_Account_Transactions_Not_Just_Page()
    {
        // Arrange — page shows t3 only, but running balance considers t1 and t2
        var account = Account.Create(
            "Savings",
            AccountType.Savings,
            MoneyValue.Create("USD", 2000m),
            new DateOnly(2026, 1, 1));

        var t1 = account.AddTransaction(
            MoneyValue.Create("USD", -100m),
            new DateOnly(2026, 1, 5),
            "Withdrawal 1");
        var t2 = account.AddTransaction(
            MoneyValue.Create("USD", -200m),
            new DateOnly(2026, 1, 10),
            "Withdrawal 2");
        var t3 = account.AddTransaction(
            MoneyValue.Create("USD", -300m),
            new DateOnly(2026, 1, 15),
            "Withdrawal 3");

        var transactionRepo = new Mock<ITransactionRepository>();

        // Page only returns t3 (simulating page 2 with pageSize=2, or filtered view)
        transactionRepo
            .Setup(r => r.GetUnifiedPagedAsync(
                account.Id,
                It.IsAny<Guid?>(),
                It.IsAny<bool?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(([t3], 3));

        // But GetByDateRangeAsync returns ALL transactions for the account
        transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                DateOnly.MinValue,
                DateOnly.MaxValue,
                account.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([t1, t2, t3]);

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo
            .Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([account]);

        var service = new UnifiedTransactionService(transactionRepo.Object, accountRepo.Object);

        // Act
        var result = await service.GetPagedAsync(
            new UnifiedTransactionFilterDto { AccountId = account.Id });

        // Assert — t3's running balance includes t1 and t2's effects
        // Initial 2000 + t1(-100) + t2(-200) + t3(-300) = 1400
        var item3 = result.Items.Single(i => i.Id == t3.Id);
        Assert.NotNull(item3.RunningBalance);
        Assert.Equal(1400m, item3.RunningBalance.Amount);
    }
}
