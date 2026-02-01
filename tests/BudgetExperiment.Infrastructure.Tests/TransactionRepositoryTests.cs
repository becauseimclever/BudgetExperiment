// <copyright file="TransactionRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Persistence.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="TransactionRepository"/>.
/// </summary>
[Collection("InMemoryDb")]
public class TransactionRepositoryTests
{
    private readonly InMemoryDbFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared in-memory database fixture.</param>
    public TransactionRepositoryTests(InMemoryDbFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Transaction()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var transactionRepo = new TransactionRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Transaction Test Account", AccountType.Checking);
        var transaction = account.AddTransaction(
            MoneyValue.Create("USD", 50.00m),
            new DateOnly(2026, 1, 9),
            "Test Transaction");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
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
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Date Range Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 1, 5), "Before Range");
        account.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 1, 10), "In Range 1");
        account.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 1, 15), "In Range 2");
        account.AddTransaction(MoneyValue.Create("USD", 40m), new DateOnly(2026, 1, 25), "After Range");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
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
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account1 = Account.Create("Account Filter Test 1", AccountType.Checking);
        account1.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 2, 15), "Account 1 Trans");

        var account2 = Account.Create("Account Filter Test 2", AccountType.Savings);
        account2.AddTransaction(MoneyValue.Create("USD", 200m), new DateOnly(2026, 2, 15), "Account 2 Trans");

        await accountRepo.AddAsync(account1);
        await accountRepo.AddAsync(account2);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
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
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Daily Totals Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 3, 10), "Day 10 Trans 1");
        account.AddTransaction(MoneyValue.Create("USD", 50m), new DateOnly(2026, 3, 10), "Day 10 Trans 2");
        account.AddTransaction(MoneyValue.Create("USD", 75m), new DateOnly(2026, 3, 15), "Day 15 Trans");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
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
    public async Task GetDailyTotalsAsync_Filters_By_Month()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Month Filter Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 4, 15), "April Trans");
        account.AddTransaction(MoneyValue.Create("USD", 200m), new DateOnly(2026, 5, 15), "May Trans");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
        var aprilTotals = await transactionRepo.GetDailyTotalsAsync(2026, 4, account.Id);

        // Assert
        Assert.Single(aprilTotals);
        Assert.Equal(new DateOnly(2026, 4, 15), aprilTotals[0].Date);
    }

    [Fact]
    public async Task ListAsync_Returns_Transactions_Ordered_By_Date_Descending()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("List Order Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 6, 1), "First");
        account.AddTransaction(MoneyValue.Create("USD", 200m), new DateOnly(2026, 6, 15), "Middle");
        account.AddTransaction(MoneyValue.Create("USD", 300m), new DateOnly(2026, 6, 30), "Last");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
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
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var transactionRepo = new TransactionRepository(context, FakeUserContext.CreateDefault());

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

    #region GetUncategorizedPagedAsync Tests

    [Fact]
    public async Task GetUncategorizedPagedAsync_Returns_Only_Uncategorized_Transactions()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
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
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
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
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Date Filter Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 8, 1), "Before Range");
        account.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 8, 10), "In Range");
        account.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 8, 20), "After Range");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
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
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Amount Filter Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 8, 1), "Too Small");
        account.AddTransaction(MoneyValue.Create("USD", 50m), new DateOnly(2026, 8, 2), "In Range");
        account.AddTransaction(MoneyValue.Create("USD", 200m), new DateOnly(2026, 8, 3), "Too Large");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
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
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Description Filter Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 8, 1), "AMAZON Purchase");
        account.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 8, 2), "Walmart Groceries");
        account.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 8, 3), "Amazon Prime");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
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
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account1 = Account.Create("Account 1", AccountType.Checking);
        account1.AddTransaction(MoneyValue.Create("USD", 50m), new DateOnly(2026, 8, 1), "Account 1 Trans");

        var account2 = Account.Create("Account 2", AccountType.Savings);
        account2.AddTransaction(MoneyValue.Create("USD", 75m), new DateOnly(2026, 8, 2), "Account 2 Trans");

        await accountRepo.AddAsync(account1);
        await accountRepo.AddAsync(account2);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
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
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Sort Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 8, 1), "Oldest");
        account.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 8, 15), "Middle");
        account.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 8, 30), "Newest");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
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
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Amount Sort Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 8, 1), "Large");
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 8, 2), "Small");
        account.AddTransaction(MoneyValue.Create("USD", 50m), new DateOnly(2026, 8, 3), "Medium");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
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
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Description Sort Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 10m), new DateOnly(2026, 8, 1), "Charlie");
        account.AddTransaction(MoneyValue.Create("USD", 20m), new DateOnly(2026, 8, 2), "Alpha");
        account.AddTransaction(MoneyValue.Create("USD", 30m), new DateOnly(2026, 8, 3), "Bravo");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
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
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Paging Test", AccountType.Checking);
        for (int i = 1; i <= 10; i++)
        {
            account.AddTransaction(MoneyValue.Create("USD", i * 10m), new DateOnly(2026, 8, i), $"Trans {i}");
        }

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
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
        await using var context = this._fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Combined Filter Test", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 25m), new DateOnly(2026, 8, 5), "Amazon - Wrong Date");
        account.AddTransaction(MoneyValue.Create("USD", 25m), new DateOnly(2026, 8, 15), "Amazon - Right");
        account.AddTransaction(MoneyValue.Create("USD", 500m), new DateOnly(2026, 8, 15), "Amazon - Too Expensive");
        account.AddTransaction(MoneyValue.Create("USD", 25m), new DateOnly(2026, 8, 15), "Walmart - Wrong Name");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
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
        await using var context = this._fixture.CreateContext();
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
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var transactionRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault());
        var (items, totalCount) = await transactionRepo.GetUncategorizedPagedAsync();

        // Assert
        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }

    #endregion
}
