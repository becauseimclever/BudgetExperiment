// <copyright file="TransactionRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Domain.DataHealth;
using BudgetExperiment.Infrastructure.Persistence.Repositories;

using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="TransactionRepository"/>.
/// </summary>
[Collection("PostgreSqlDb")]
public class TransactionRepositoryTests
{
    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL database fixture.</param>
    public TransactionRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Transaction()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var transactionRepo = new TransactionRepository(context, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var account = Account.Create("Transaction Test Account", AccountType.Checking);
        var transaction = account.AddTransaction(
            MoneyValue.Create("USD", 50.00m),
            new DateOnly(2026, 1, 9),
            "Test Transaction");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var retrieved = await verifyRepo.GetByIdAsync(transaction.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(transaction.Id, retrieved.Id);
        Assert.Equal("Test Transaction", retrieved.Description);
        Assert.Equal(50.00m, retrieved.Amount.Amount);
        Assert.Equal("USD", retrieved.Amount.Currency);
    }

    [Fact]
    public async Task GetByDateRangeAsync_Returns_Transactions_In_Range()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Date Range Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 1, 5), "Before Range");
        account.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 1, 10), "In Range 1");
        account.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 1, 15), "In Range 2");
        account.AddTransaction(MoneyValue.Create("USD", 40m), new DateOnly(2026, 1, 25), "After Range");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var results = await transactionRepo.GetByDateRangeAsync(
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 1, 20));

        // Assert
        Assert.Contains(results, t => t.Description == "In Range 1");
        Assert.Contains(results, t => t.Description == "In Range 2");
        Assert.DoesNotContain(results, t => t.Description == "Before Range");
        Assert.DoesNotContain(results, t => t.Description == "After Range");
    }

    [Fact]
    public async Task GetByDateRangeAsync_Filters_By_AccountId()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account1 = Account.Create("Account Filter Test 1", AccountType.Checking);
        account1.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 2, 15), "Account 1 Trans");

        var account2 = Account.Create("Account Filter Test 2", AccountType.Savings);
        account2.AddTransaction(MoneyValue.Create("USD", 200m), new DateOnly(2026, 2, 15), "Account 2 Trans");

        await accountRepo.AddAsync(account1);
        await accountRepo.AddAsync(account2);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var results = await transactionRepo.GetByDateRangeAsync(
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 2, 28),
            account1.Id);

        // Assert
        Assert.Contains(results, t => t.Description == "Account 1 Trans");
        Assert.DoesNotContain(results, t => t.Description == "Account 2 Trans");
    }

    [Fact]
    public async Task GetDailyTotalsAsync_Returns_Correct_Totals()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Daily Totals Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 3, 10), "Day 10 Trans 1");
        account.AddTransaction(MoneyValue.Create("USD", 50m), new DateOnly(2026, 3, 10), "Day 10 Trans 2");
        account.AddTransaction(MoneyValue.Create("USD", 75m), new DateOnly(2026, 3, 15), "Day 15 Trans");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var dailyTotals = await transactionRepo.GetDailyTotalsAsync(2026, 3, account.Id);

        // Assert
        var day10Total = dailyTotals.FirstOrDefault(d => d.Date == new DateOnly(2026, 3, 10));
        var day15Total = dailyTotals.FirstOrDefault(d => d.Date == new DateOnly(2026, 3, 15));

        Assert.NotNull(day10Total);
        Assert.Equal(150m, day10Total.Total.Amount);
        Assert.Equal(2, day10Total.TransactionCount);

        Assert.NotNull(day15Total);
        Assert.Equal(75m, day15Total.Total.Amount);
        Assert.Equal(1, day15Total.TransactionCount);
    }

    [Fact]
    public async Task GetDailyTotalsAsync_WithNullAccountId_AggregatesAcrossAccountsForSameDay()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var accountA = Account.Create("Aggregate A", AccountType.Checking);
        accountA.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 3, 10), "A day 10");

        var accountB = Account.Create("Aggregate B", AccountType.Savings);
        accountB.AddTransaction(MoneyValue.Create("USD", -40m), new DateOnly(2026, 3, 10), "B day 10");

        await accountRepo.AddAsync(accountA);
        await accountRepo.AddAsync(accountB);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var dailyTotals = await transactionRepo.GetDailyTotalsAsync(2026, 3, null);

        // Assert
        var day10Total = dailyTotals.Single(d => d.Date == new DateOnly(2026, 3, 10));
        Assert.Equal(60m, day10Total.Total.Amount);
        Assert.Equal(2, day10Total.TransactionCount);
    }

    [Fact]
    public async Task GetDailyTotalsAsync_WithPairedTransferAcrossAccounts_NetsZeroForAggregateScope()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var sourceAccount = Account.Create("Transfer Source", AccountType.Checking);
        sourceAccount.AddTransaction(MoneyValue.Create("USD", -300m), new DateOnly(2026, 3, 11), "Transfer out");

        var destinationAccount = Account.Create("Transfer Destination", AccountType.Savings);
        destinationAccount.AddTransaction(MoneyValue.Create("USD", 300m), new DateOnly(2026, 3, 11), "Transfer in");

        await accountRepo.AddAsync(sourceAccount);
        await accountRepo.AddAsync(destinationAccount);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var dailyTotals = await transactionRepo.GetDailyTotalsAsync(2026, 3, null);

        // Assert
        var transferDayTotal = dailyTotals.Single(d => d.Date == new DateOnly(2026, 3, 11));
        Assert.Equal(0m, transferDayTotal.Total.Amount);
        Assert.Equal(2, transferDayTotal.TransactionCount);
    }

    [Fact]
    public async Task GetDailyTotalsAsync_AfterDelete_RecomputeIsStable()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var transactionRepo = new TransactionRepository(context, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var account = Account.Create("Delete Stability", AccountType.Checking);
        var toDelete = account.AddTransaction(
            MoneyValue.Create("USD", 50m),
            new DateOnly(2026, 3, 12),
            "Delete me");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        await transactionRepo.RemoveAsync(toDelete);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var firstRead = await verifyRepo.GetDailyTotalsAsync(2026, 3, account.Id);
        var secondRead = await verifyRepo.GetDailyTotalsAsync(2026, 3, account.Id);

        // Assert
        Assert.DoesNotContain(firstRead, d => d.Date == new DateOnly(2026, 3, 12));
        Assert.Equal(firstRead.Count, secondRead.Count);
        Assert.Equal(
            firstRead.Select(d => (d.Date, d.Total.Amount, d.TransactionCount)).ToList(),
            secondRead.Select(d => (d.Date, d.Total.Amount, d.TransactionCount)).ToList());
    }

    [Fact]
    public async Task GetDailyTotalsAsync_AfterLateInsert_RecomputeIncludesInsertedEarlierDate()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var transactionRepo = new TransactionRepository(context, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var account = Account.Create("Late Insert", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 3, 20), "Original later day");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        var firstRead = await transactionRepo.GetDailyTotalsAsync(2026, 3, account.Id);
        Assert.Single(firstRead);
        Assert.Equal(new DateOnly(2026, 3, 20), firstRead[0].Date);

        account.AddTransaction(MoneyValue.Create("USD", 25m), new DateOnly(2026, 3, 10), "Late inserted earlier day");
        await context.SaveChangesAsync();

        // Act
        var secondRead = await transactionRepo.GetDailyTotalsAsync(2026, 3, account.Id);

        // Assert
        Assert.Equal(2, secondRead.Count);
        Assert.Contains(secondRead, d => d.Date == new DateOnly(2026, 3, 10) && d.Total.Amount == 25m);
        Assert.Contains(secondRead, d => d.Date == new DateOnly(2026, 3, 20) && d.Total.Amount == 10m);
    }

    [Fact]
    public async Task GetDailyTotalsAsync_Filters_By_Month()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Month Filter Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 4, 15), "April Trans");
        account.AddTransaction(MoneyValue.Create("USD", 200m), new DateOnly(2026, 5, 15), "May Trans");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var aprilTotals = await transactionRepo.GetDailyTotalsAsync(2026, 4, account.Id);

        // Assert
        Assert.Single(aprilTotals);
        Assert.Equal(new DateOnly(2026, 4, 15), aprilTotals[0].Date);
    }

    [Fact]
    public async Task ListAsync_Returns_Transactions_Ordered_By_Date_Descending()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("List Order Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 6, 1), "First");
        account.AddTransaction(MoneyValue.Create("USD", 200m), new DateOnly(2026, 6, 15), "Middle");
        account.AddTransaction(MoneyValue.Create("USD", 300m), new DateOnly(2026, 6, 30), "Last");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var transactions = await transactionRepo.ListAsync(0, 100);

        // Assert - most recent first
        Assert.Equal(3, transactions.Count);
        Assert.Equal("Last", transactions[0].Description);
        Assert.Equal("Middle", transactions[1].Description);
        Assert.Equal("First", transactions[2].Description);
    }

    [Fact]
    public async Task CountAsync_Returns_Correct_Count()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var transactionRepo = new TransactionRepository(context, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var initialCount = await transactionRepo.CountAsync();
        Assert.Equal(0, initialCount);

        var account = Account.Create("Count Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 7, 1), "Count Test Trans");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        var newCount = await transactionRepo.CountAsync();

        // Assert
        Assert.Equal(1, newCount);
    }

    [Fact]
    public async Task GetUncategorizedPagedAsync_Returns_Only_Uncategorized_Transactions()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var categoryRepo = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());

        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        await categoryRepo.AddAsync(category);

        var account = Account.Create("Uncategorized Test", AccountType.Checking);
        var uncategorizedTrans = account.AddTransaction(MoneyValue.Create("USD", 50m), new DateOnly(2026, 8, 1), "No Category");
        var categorizedTrans = account.AddTransaction(MoneyValue.Create("USD", 75m), new DateOnly(2026, 8, 2), "Has Category");
        categorizedTrans.UpdateCategory(category.Id);

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var (items, totalCount) = await transactionRepo.GetUncategorizedPagedAsync();

        // Assert
        Assert.Equal(1, totalCount);
        Assert.Single(items);
        Assert.Equal("No Category", items[0].Description);
    }

    [Fact]
    public async Task GetUncategorizedPagedAsync_Filters_By_DateRange()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Date Filter Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 8, 1), "Before Range");
        account.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 8, 10), "In Range");
        account.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 8, 20), "After Range");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var (items, totalCount) = await transactionRepo.GetUncategorizedPagedAsync(
            startDate: new DateOnly(2026, 8, 5),
            endDate: new DateOnly(2026, 8, 15));

        // Assert
        Assert.Equal(1, totalCount);
        Assert.Single(items);
        Assert.Equal("In Range", items[0].Description);
    }

    [Fact]
    public async Task GetUncategorizedPagedAsync_Filters_By_AmountRange()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Amount Filter Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 8, 1), "Too Small");
        account.AddTransaction(MoneyValue.Create("USD", 50m), new DateOnly(2026, 8, 2), "In Range");
        account.AddTransaction(MoneyValue.Create("USD", 200m), new DateOnly(2026, 8, 3), "Too Large");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var (items, totalCount) = await transactionRepo.GetUncategorizedPagedAsync(
            minAmount: 25m,
            maxAmount: 100m);

        // Assert
        Assert.Equal(1, totalCount);
        Assert.Single(items);
        Assert.Equal("In Range", items[0].Description);
    }

    [Fact]
    public async Task GetUncategorizedPagedAsync_Filters_By_Description_Contains()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Description Filter Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 8, 1), "AMAZON Purchase");
        account.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 8, 2), "Walmart Groceries");
        account.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 8, 3), "Amazon Prime");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var (items, totalCount) = await transactionRepo.GetUncategorizedPagedAsync(
            descriptionContains: "amazon");

        // Assert
        Assert.Equal(2, totalCount);
        Assert.All(items, t => Assert.Contains("Amazon", t.Description, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetUncategorizedPagedAsync_Filters_By_AccountId()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account1 = Account.Create("Account 1", AccountType.Checking);
        account1.AddTransaction(MoneyValue.Create("USD", 50m), new DateOnly(2026, 8, 1), "Account 1 Trans");

        var account2 = Account.Create("Account 2", AccountType.Savings);
        account2.AddTransaction(MoneyValue.Create("USD", 75m), new DateOnly(2026, 8, 2), "Account 2 Trans");

        await accountRepo.AddAsync(account1);
        await accountRepo.AddAsync(account2);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var (items, totalCount) = await transactionRepo.GetUncategorizedPagedAsync(
            accountId: account1.Id);

        // Assert
        Assert.Equal(1, totalCount);
        Assert.Single(items);
        Assert.Equal("Account 1 Trans", items[0].Description);
    }

    [Fact]
    public async Task GetUncategorizedPagedAsync_Sorts_By_Date_Descending_By_Default()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Sort Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 8, 1), "Oldest");
        account.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 8, 15), "Middle");
        account.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 8, 30), "Newest");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var (items, _) = await transactionRepo.GetUncategorizedPagedAsync();

        // Assert
        Assert.Equal("Newest", items[0].Description);
        Assert.Equal("Middle", items[1].Description);
        Assert.Equal("Oldest", items[2].Description);
    }

    [Fact]
    public async Task GetUncategorizedPagedAsync_Sorts_By_Amount_Ascending()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Amount Sort Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 8, 1), "Large");
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 8, 2), "Small");
        account.AddTransaction(MoneyValue.Create("USD", 50m), new DateOnly(2026, 8, 3), "Medium");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var (items, _) = await transactionRepo.GetUncategorizedPagedAsync(
            sortBy: "Amount",
            sortDescending: false);

        // Assert
        Assert.Equal("Small", items[0].Description);
        Assert.Equal("Medium", items[1].Description);
        Assert.Equal("Large", items[2].Description);
    }

    [Fact]
    public async Task GetUncategorizedPagedAsync_Sorts_By_Description_Ascending()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Description Sort Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 8, 1), "Charlie");
        account.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 8, 2), "Alpha");
        account.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 8, 3), "Bravo");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var (items, _) = await transactionRepo.GetUncategorizedPagedAsync(
            sortBy: "Description",
            sortDescending: false);

        // Assert
        Assert.Equal("Alpha", items[0].Description);
        Assert.Equal("Bravo", items[1].Description);
        Assert.Equal("Charlie", items[2].Description);
    }

    [Fact]
    public async Task GetUncategorizedPagedAsync_Applies_Paging()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Paging Test", AccountType.Checking);
        for (int i = 1; i <= 10; i++)
        {
            account.AddTransaction(MoneyValue.Create("USD", i * 10m), new DateOnly(2026, 8, i), $"Trans {i}");
        }

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var (page1Items, totalCount) = await transactionRepo.GetUncategorizedPagedAsync(
            skip: 0,
            take: 3);
        var (page2Items, _) = await transactionRepo.GetUncategorizedPagedAsync(
            skip: 3,
            take: 3);

        // Assert
        Assert.Equal(10, totalCount);
        Assert.Equal(3, page1Items.Count);
        Assert.Equal(3, page2Items.Count);

        // Verify page 1 and page 2 have different items (sorted by date desc, so page1 has Trans 10, 9, 8)
        Assert.DoesNotContain(page1Items, t => page2Items.Any(p2 => p2.Id == t.Id));
    }

    [Fact]
    public async Task GetUncategorizedPagedAsync_Combines_Multiple_Filters()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Combined Filter Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 25m), new DateOnly(2026, 8, 5), "Amazon - Wrong Date");
        account.AddTransaction(MoneyValue.Create("USD", 25m), new DateOnly(2026, 8, 15), "Amazon - Right");
        account.AddTransaction(MoneyValue.Create("USD", 500m), new DateOnly(2026, 8, 15), "Amazon - Too Expensive");
        account.AddTransaction(MoneyValue.Create("USD", 25m), new DateOnly(2026, 8, 15), "Walmart - Wrong Name");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var (items, totalCount) = await transactionRepo.GetUncategorizedPagedAsync(
            startDate: new DateOnly(2026, 8, 10),
            endDate: new DateOnly(2026, 8, 20),
            minAmount: 10m,
            maxAmount: 100m,
            descriptionContains: "Amazon");

        // Assert
        Assert.Equal(1, totalCount);
        Assert.Single(items);
        Assert.Equal("Amazon - Right", items[0].Description);
    }

    [Fact]
    public async Task GetUncategorizedPagedAsync_Returns_Empty_When_No_Matches()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var categoryRepo = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());

        var category = BudgetCategory.Create("All Categorized", CategoryType.Expense);
        await categoryRepo.AddAsync(category);

        var account = Account.Create("All Categorized Test", AccountType.Checking);
        var trans = account.AddTransaction(MoneyValue.Create("USD", 50m), new DateOnly(2026, 8, 1), "Categorized");
        trans.UpdateCategory(category.Id);

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var (items, totalCount) = await transactionRepo.GetUncategorizedPagedAsync();

        // Assert
        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetUncategorizedAsync_WithMaxCount_ReturnsAtMostMaxCountItems()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Max Count Test", AccountType.Checking);
        for (var i = 1; i <= 5; i++)
        {
            account.AddTransaction(
                MoneyValue.Create("USD", i * 10m),
                new DateOnly(2026, 9, i),
                $"Uncategorized {i}");
        }

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var results = await transactionRepo.GetUncategorizedAsync(maxCount: 2, cancellationToken: CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetUncategorizedDescriptionsAsync_ReturnsOnlyDescriptionStrings()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var categoryRepo = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());

        var category = BudgetCategory.Create("Utilities", CategoryType.Expense);
        await categoryRepo.AddAsync(category);

        var account = Account.Create("Descriptions Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 12m), new DateOnly(2026, 9, 1), "Coffee");
        account.AddTransaction(MoneyValue.Create("USD", 24m), new DateOnly(2026, 9, 2), "Rent");
        var categorized = account.AddTransaction(MoneyValue.Create("USD", 36m), new DateOnly(2026, 9, 3), "Electric");
        categorized.UpdateCategory(category.Id);

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var descriptions = await transactionRepo.GetUncategorizedDescriptionsAsync(cancellationToken: CancellationToken.None);

        // Assert
        Assert.Equal(new[] { "Coffee", "Rent" }, descriptions);
    }

    [Fact]
    public async Task GetTransactionProjectionsForDuplicateDetectionAsync_ReturnsExpectedShape()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Duplicate Projection Test", AccountType.Checking);
        var transaction = account.AddTransaction(
            MoneyValue.Create("USD", 12.34m),
            new DateOnly(2026, 9, 10),
            "Grocery Store");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var projections = await transactionRepo.GetTransactionProjectionsForDuplicateDetectionAsync(CancellationToken.None);

        // Assert
        var projection = Assert.Single(projections);
        Assert.Equal(transaction.Id, projection.Id);
        Assert.Equal(account.Id, projection.AccountId);
        Assert.Equal(transaction.Date, projection.Date);
        Assert.Equal(transaction.Amount.Amount, projection.Amount);
        Assert.Equal(transaction.Description, projection.Description);
    }

    [Fact]
    public async Task GetTransactionDatesForGapAnalysisAsync_ReturnsDistinctAccountDates()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var accountA = Account.Create("Gap Account A", AccountType.Checking);
        accountA.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 9, 5), "A1");
        accountA.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 9, 5), "A2");
        accountA.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 9, 6), "A3");

        var accountB = Account.Create("Gap Account B", AccountType.Savings);
        accountB.AddTransaction(MoneyValue.Create("USD", 40m), new DateOnly(2026, 9, 5), "B1");

        await accountRepo.AddAsync(accountA);
        await accountRepo.AddAsync(accountB);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var dates = await transactionRepo.GetTransactionDatesForGapAnalysisAsync(CancellationToken.None);

        // Assert
        var expected = new List<DateGapProjection>
        {
            new(accountA.Id, new DateOnly(2026, 9, 5)),
            new(accountA.Id, new DateOnly(2026, 9, 6)),
            new(accountB.Id, new DateOnly(2026, 9, 5)),
        };

        Assert.Equal(expected.Count, dates.Count);
        foreach (var item in expected)
        {
            Assert.Contains(item, dates);
        }
    }

    [Fact]
    public async Task GetTransactionAmountsForOutlierAnalysisAsync_ReturnsIdDescriptionAmount()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Outlier Projection Test", AccountType.Checking);
        var transaction = account.AddTransaction(
            MoneyValue.Create("USD", -99.99m),
            new DateOnly(2026, 9, 12),
            "Hardware Store");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var projections = await transactionRepo.GetTransactionAmountsForOutlierAnalysisAsync(CancellationToken.None);

        // Assert
        var projection = Assert.Single(projections);
        Assert.Equal(transaction.Id, projection.Id);
        Assert.Equal(transaction.Description, projection.Description);
        Assert.Equal(transaction.Amount.Amount, projection.Amount);
    }

    [Fact]
    public async Task GetAllDescriptionsAsync_WithPrefix_ReturnsOnlyMatchingDescriptions()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Description Prefix Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 9, 1), "Amazon Fresh");
        account.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 9, 2), "Amazon Prime");
        account.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 9, 3), "Target");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var descriptions = await transactionRepo.GetAllDescriptionsAsync("Amazon", cancellationToken: CancellationToken.None);

        // Assert
        Assert.Equal(new[] { "Amazon Fresh", "Amazon Prime" }, descriptions);
    }

    [Fact]
    public async Task GetAllDescriptionsAsync_WithLargeDataSet_ReturnsAtMostMaxResults()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Description Limit Test", AccountType.Checking);
        for (var i = 0; i < 40; i++)
        {
            account.AddTransaction(
                MoneyValue.Create("USD", i + 1),
                new DateOnly(2026, 10, 1).AddDays(i),
                $"Merchant {i:00}");
        }

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var descriptions = await transactionRepo.GetAllDescriptionsAsync(maxResults: 15, cancellationToken: CancellationToken.None);

        // Assert
        Assert.Equal(15, descriptions.Count);
    }

    [Fact]
    public async Task GetByDateRangeAsync_Personal_Scope_Excludes_Shared_Transactions()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var defaultUserContext = FakeUserContext.CreateDefault();
        var accountRepo = new AccountRepository(context, defaultUserContext);

        var personalAccount = Account.CreatePersonal("Personal Acct", AccountType.Checking, userId);
        personalAccount.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 3, 15), "Personal Trans");

        var sharedAccount = Account.CreateShared("Shared Acct", AccountType.Checking, userId);
        sharedAccount.AddTransaction(MoneyValue.Create("USD", 200m), new DateOnly(2026, 3, 15), "Shared Trans");

        await accountRepo.AddAsync(personalAccount);
        await accountRepo.AddAsync(sharedAccount);
        await context.SaveChangesAsync();

        // Act — query with Personal scope
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var personalContext = FakeUserContext.CreateForPersonalScope(userId);
        var transactionRepo = new TransactionRepository(verifyContext, personalContext, NullLogger<TransactionRepository>.Instance);
        var results = await transactionRepo.GetByDateRangeAsync(
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 31));

        // Assert
        Assert.Single(results);
        Assert.Equal("Personal Trans", results[0].Description);
    }

    [Fact]
    public async Task GetByDateRangeAsync_Shared_Scope_Excludes_Personal_Transactions()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var defaultUserContext = FakeUserContext.CreateDefault();
        var accountRepo = new AccountRepository(context, defaultUserContext);

        var personalAccount = Account.CreatePersonal("Personal Acct", AccountType.Checking, userId);
        personalAccount.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 4, 15), "Personal Trans");

        var sharedAccount = Account.CreateShared("Shared Acct", AccountType.Checking, userId);
        sharedAccount.AddTransaction(MoneyValue.Create("USD", 200m), new DateOnly(2026, 4, 15), "Shared Trans");

        await accountRepo.AddAsync(personalAccount);
        await accountRepo.AddAsync(sharedAccount);
        await context.SaveChangesAsync();

        // Act — query with Shared scope
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var sharedContext = FakeUserContext.CreateForSharedScope();
        var transactionRepo = new TransactionRepository(verifyContext, sharedContext, NullLogger<TransactionRepository>.Instance);
        var results = await transactionRepo.GetByDateRangeAsync(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 30));

        // Assert
        Assert.Single(results);
        Assert.Equal("Shared Trans", results[0].Description);
    }

    [Fact]
    public async Task GetByDateRangeAsync_All_Scope_Returns_Both_Personal_And_Shared()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var defaultUserContext = FakeUserContext.CreateDefault();
        var accountRepo = new AccountRepository(context, defaultUserContext);

        var personalAccount = Account.CreatePersonal("Personal Acct", AccountType.Checking, userId);
        personalAccount.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 5, 15), "Personal Trans");

        var sharedAccount = Account.CreateShared("Shared Acct", AccountType.Checking, userId);
        sharedAccount.AddTransaction(MoneyValue.Create("USD", 200m), new DateOnly(2026, 5, 15), "Shared Trans");

        await accountRepo.AddAsync(personalAccount);
        await accountRepo.AddAsync(sharedAccount);
        await context.SaveChangesAsync();

        // Act — query with null scope (All)
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var allContext = FakeUserContext.CreateDefault(); // null scope = show all
        var transactionRepo = new TransactionRepository(verifyContext, allContext, NullLogger<TransactionRepository>.Instance);
        var results = await transactionRepo.GetByDateRangeAsync(
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 31));

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, t => t.Description == "Personal Trans");
        Assert.Contains(results, t => t.Description == "Shared Trans");
    }

    [Fact]
    public async Task GetByDateRangeAsync_Personal_Scope_Excludes_Other_Users_Personal_Transactions()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var otherUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var defaultUserContext = FakeUserContext.CreateDefault();
        var accountRepo = new AccountRepository(context, defaultUserContext);

        var myPersonalAccount = Account.CreatePersonal("My Personal", AccountType.Checking, userId);
        myPersonalAccount.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 6, 15), "My Trans");

        var otherPersonalAccount = Account.CreatePersonal("Other Personal", AccountType.Checking, otherUserId);
        otherPersonalAccount.AddTransaction(MoneyValue.Create("USD", 200m), new DateOnly(2026, 6, 15), "Other Trans");

        await accountRepo.AddAsync(myPersonalAccount);
        await accountRepo.AddAsync(otherPersonalAccount);
        await context.SaveChangesAsync();

        // Act — query with Personal scope for default user
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var personalContext = FakeUserContext.CreateForPersonalScope(userId);
        var transactionRepo = new TransactionRepository(verifyContext, personalContext, NullLogger<TransactionRepository>.Instance);
        var results = await transactionRepo.GetByDateRangeAsync(
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 30));

        // Assert
        Assert.Single(results);
        Assert.Equal("My Trans", results[0].Description);
    }

    [Fact]
    public async Task GetSpendingByCategoriesAsync_Returns_Spending_Aggregated_By_Category()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var categoryRepo = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());

        var groceries = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var utilities = BudgetCategory.Create("Utilities", CategoryType.Expense);
        var income = BudgetCategory.Create("Salary", CategoryType.Income);

        await categoryRepo.AddAsync(groceries);
        await categoryRepo.AddAsync(utilities);
        await categoryRepo.AddAsync(income);

        var account = Account.Create("Spending Test", AccountType.Checking);

        // Groceries expenses (negative amounts in domain)
        var groceryTrans1 = account.AddTransaction(
            MoneyValue.Create("USD", -50m),
            new DateOnly(2026, 1, 5),
            "Grocery Store");
        groceryTrans1.UpdateCategory(groceries.Id);

        var groceryTrans2 = account.AddTransaction(
            MoneyValue.Create("USD", -75m),
            new DateOnly(2026, 1, 15),
            "Farmer's Market");
        groceryTrans2.UpdateCategory(groceries.Id);

        // Utilities expenses
        var utilitiesTrans = account.AddTransaction(
            MoneyValue.Create("USD", -120m),
            new DateOnly(2026, 1, 10),
            "Electric Bill");
        utilitiesTrans.UpdateCategory(utilities.Id);

        // Income (positive, should be excluded from spending)
        var incomeTrans = account.AddTransaction(
            MoneyValue.Create("USD", 3000m),
            new DateOnly(2026, 1, 1),
            "Monthly Salary");
        incomeTrans.UpdateCategory(income.Id);

        // Uncategorized expense (should be excluded)
        account.AddTransaction(
            MoneyValue.Create("USD", -25m),
            new DateOnly(2026, 1, 20),
            "Unknown Merchant");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var spending = await transactionRepo.GetSpendingByCategoriesAsync(2026, 1, BudgetScope.Shared);

        // Assert
        Assert.Equal(2, spending.Count);
        Assert.True(spending.ContainsKey(groceries.Id), "Groceries category should be in results");
        Assert.True(spending.ContainsKey(utilities.Id), "Utilities category should be in results");
        Assert.Equal(125m, spending[groceries.Id]); // 50 + 75 (absolute values)
        Assert.Equal(120m, spending[utilities.Id]);
        Assert.False(spending.ContainsKey(income.Id), "Income category should be excluded");
    }
}
