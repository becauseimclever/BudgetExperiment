// Copyright (c) BecauseImClever. All rights reserved.

using BudgetExperiment.Domain.Accounts;
using BudgetExperiment.Domain.Budgeting;
using BudgetExperiment.Domain.Categorization;
using BudgetExperiment.Domain.Common;
using BudgetExperiment.Domain.Recurring;
using BudgetExperiment.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Performance.Tests.Infrastructure;

/// <summary>
/// Seeds realistic data volumes for performance testing.
/// </summary>
public static class TestDataSeeder
{
    private static readonly string[] AccountNames = ["Checking", "Savings", "Credit Card", "Cash"];
    private static readonly AccountType[] AccountTypes = [AccountType.Checking, AccountType.Savings, AccountType.CreditCard, AccountType.Cash];

    private static readonly string[] CategoryNames = ["Groceries", "Rent", "Utilities", "Transportation", "Dining Out", "Entertainment", "Healthcare", "Insurance"];
    private static readonly string[] TransactionDescriptions =
    [
        "Walmart Grocery", "Target Purchase", "Amazon Order", "Gas Station",
        "Electric Bill", "Water Bill", "Internet Service", "Phone Bill",
        "Restaurant Dinner", "Coffee Shop", "Movie Theater", "Gym Membership",
        "Doctor Visit", "Pharmacy", "Car Insurance", "Home Insurance",
    ];

    /// <summary>
    /// Gets the ID of the first seeded account (Checking). Available after <see cref="SeedAsync"/> completes.
    /// </summary>
    public static Guid FirstAccountId
    {
        get; private set;
    }

    /// <summary>
    /// Seeds the database with realistic data for performance testing.
    /// When <see cref="PerformanceWebApplicationFactory.UseRealDb"/> is true,
    /// skips seeding but resolves <see cref="FirstAccountId"/> from the existing data.
    /// </summary>
    /// <param name="factory">The web application factory providing the service scope.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SeedAsync(PerformanceWebApplicationFactory factory)
    {
        if (PerformanceWebApplicationFactory.UseRealDb)
        {
            using var realScope = factory.Services.CreateScope();
            var realDb = realScope.ServiceProvider.GetRequiredService<BudgetDbContext>();
            var account = await realDb.Accounts.FirstOrDefaultAsync();
            FirstAccountId = account?.Id ?? Guid.Empty;
            return;
        }

        using var seedScope = factory.Services.CreateScope();
        var db = seedScope.ServiceProvider.GetRequiredService<BudgetDbContext>();

        var categories = SeedCategories(db);
        var accounts = SeedAccounts(db);
        SeedTransactions(db, accounts, categories);
        SeedRecurringTransactions(db, accounts, categories);
        SeedBudgetGoals(db, categories);

        await db.SaveChangesAsync();

        FirstAccountId = accounts[0].Id;
    }

    private static List<BudgetCategory> SeedCategories(BudgetDbContext db)
    {
        var categories = new List<BudgetCategory>();
        foreach (var name in CategoryNames)
        {
            var category = BudgetCategory.Create(name, CategoryType.Expense);
            db.BudgetCategories.Add(category);
            categories.Add(category);
        }

        return categories;
    }

    private static List<Account> SeedAccounts(BudgetDbContext db)
    {
        var accounts = new List<Account>();
        for (int i = 0; i < AccountNames.Length; i++)
        {
            var account = Account.CreateShared(
                AccountNames[i],
                AccountTypes[i],
                PerformanceWebApplicationFactory.TestUserId,
                MoneyValue.Create("USD", 1000m * (i + 1)),
                new DateOnly(2025, 7, 1));
            db.Accounts.Add(account);
            accounts.Add(account);
        }

        return accounts;
    }

    private static void SeedTransactions(BudgetDbContext db, List<Account> accounts, List<BudgetCategory> categories)
    {
        var random = new Random(42); // Deterministic seed for reproducibility
        var startDate = new DateOnly(2025, 9, 1);
        var endDate = new DateOnly(2026, 3, 15);
        var totalDays = endDate.DayNumber - startDate.DayNumber;

        for (int i = 0; i < 750; i++)
        {
            var account = accounts[random.Next(accounts.Count)];
            var category = categories[random.Next(categories.Count)];
            var description = TransactionDescriptions[random.Next(TransactionDescriptions.Length)];
            var date = startDate.AddDays(random.Next(totalDays));
            var amount = MoneyValue.Create("USD", -(random.Next(500, 15000) / 100m));

            account.AddTransaction(amount, date, description, category.Id);
        }
    }

    private static void SeedRecurringTransactions(BudgetDbContext db, List<Account> accounts, List<BudgetCategory> categories)
    {
        var checkingAccount = accounts[0];
        var recurringItems = new (string Desc, decimal Amount, int DayOfMonth)[]
        {
            ("Rent", -1500m, 1),
            ("Electric Bill", -120m, 15),
            ("Internet Service", -79.99m, 5),
            ("Car Insurance", -150m, 20),
            ("Gym Membership", -49.99m, 1),
            ("Phone Bill", -85m, 10),
            ("Water Bill", -45m, 15),
            ("Streaming Service", -15.99m, 1),
            ("Car Payment", -350m, 25),
            ("Savings Transfer", -500m, 1),
            ("Grocery Budget", -400m, 1),
            ("Gas Budget", -200m, 1),
        };

        foreach (var (desc, amount, dayOfMonth) in recurringItems)
        {
            var category = categories[0]; // Use first category for simplicity
            var recurring = RecurringTransaction.Create(
                checkingAccount.Id,
                desc,
                MoneyValue.Create("USD", amount),
                RecurrencePatternValue.CreateMonthly(1, dayOfMonth),
                new DateOnly(2025, 9, 1),
                categoryId: category.Id);
            db.RecurringTransactions.Add(recurring);
        }
    }

    private static void SeedBudgetGoals(BudgetDbContext db, List<BudgetCategory> categories)
    {
        // Create goals for current and next few months
        for (int month = 1; month <= 6; month++)
        {
            foreach (var category in categories)
            {
                var goal = BudgetGoal.Create(
                    category.Id,
                    2026,
                    month,
                    MoneyValue.Create("USD", 500m));
                db.BudgetGoals.Add(goal);
            }
        }
    }
}
