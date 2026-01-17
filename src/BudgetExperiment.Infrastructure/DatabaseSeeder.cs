// <copyright file="DatabaseSeeder.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BudgetExperiment.Infrastructure;

/// <summary>
/// Seeds the database with sample data for development and testing.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// A well-known system user ID used for seeding shared data.
    /// This represents data created by the system/seeder, not by any real user.
    /// </summary>
    public static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Seeds the database with sample accounts and transactions.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>A task representing the async operation.</returns>
    public static async Task SeedAsync(BudgetDbContext context, ILogger? logger = null)
    {
        // Only seed if database is empty
        if (await context.Accounts.AnyAsync())
        {
            logger?.LogInformation("Database already contains data, skipping seed.");
            return;
        }

        logger?.LogInformation("Seeding database with sample data...");

        // Create shared accounts (visible to all authenticated users)
        var checking = Account.CreateShared("Primary Checking", AccountType.Checking, SystemUserId);
        var savings = Account.CreateShared("Emergency Fund", AccountType.Savings, SystemUserId);
        var creditCard = Account.CreateShared("Rewards Card", AccountType.CreditCard, SystemUserId);
        var cash = Account.CreateShared("Wallet Cash", AccountType.Cash, SystemUserId);

        // Add transactions to checking account
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);

        // Checking account transactions (income and expenses)
        checking.AddTransaction(
            MoneyValue.Create("USD", 3500.00m),
            startOfMonth,
            "Paycheck - Direct Deposit");

        checking.AddTransaction(
            MoneyValue.Create("USD", -1200.00m),
            startOfMonth.AddDays(1),
            "Rent Payment");

        checking.AddTransaction(
            MoneyValue.Create("USD", -85.50m),
            startOfMonth.AddDays(3),
            "Electric Bill");

        checking.AddTransaction(
            MoneyValue.Create("USD", -120.00m),
            startOfMonth.AddDays(5),
            "Grocery Store");

        checking.AddTransaction(
            MoneyValue.Create("USD", -45.00m),
            startOfMonth.AddDays(7),
            "Gas Station");

        checking.AddTransaction(
            MoneyValue.Create("USD", 3500.00m),
            startOfMonth.AddDays(15),
            "Paycheck - Direct Deposit");

        checking.AddTransaction(
            MoneyValue.Create("USD", -65.00m),
            startOfMonth.AddDays(16),
            "Internet Bill");

        checking.AddTransaction(
            MoneyValue.Create("USD", -150.00m),
            startOfMonth.AddDays(18),
            "Grocery Store");

        checking.AddTransaction(
            MoneyValue.Create("USD", -500.00m),
            startOfMonth.AddDays(20),
            "Transfer to Savings");

        // Savings account transactions
        savings.AddTransaction(
            MoneyValue.Create("USD", 500.00m),
            startOfMonth.AddDays(20),
            "Transfer from Checking");

        savings.AddTransaction(
            MoneyValue.Create("USD", 25.00m),
            startOfMonth.AddDays(25),
            "Interest Payment");

        // Credit card transactions
        creditCard.AddTransaction(
            MoneyValue.Create("USD", -89.99m),
            startOfMonth.AddDays(2),
            "Amazon Purchase");

        creditCard.AddTransaction(
            MoneyValue.Create("USD", -35.00m),
            startOfMonth.AddDays(4),
            "Restaurant Dinner");

        creditCard.AddTransaction(
            MoneyValue.Create("USD", -12.99m),
            startOfMonth.AddDays(6),
            "Netflix Subscription");

        creditCard.AddTransaction(
            MoneyValue.Create("USD", -15.99m),
            startOfMonth.AddDays(6),
            "Spotify Subscription");

        creditCard.AddTransaction(
            MoneyValue.Create("USD", -250.00m),
            startOfMonth.AddDays(10),
            "Credit Card Payment");

        creditCard.AddTransaction(
            MoneyValue.Create("USD", -42.50m),
            startOfMonth.AddDays(12),
            "Coffee Shop");

        creditCard.AddTransaction(
            MoneyValue.Create("USD", -199.00m),
            startOfMonth.AddDays(14),
            "New Shoes");

        // Cash transactions
        cash.AddTransaction(
            MoneyValue.Create("USD", 100.00m),
            startOfMonth.AddDays(1),
            "ATM Withdrawal");

        cash.AddTransaction(
            MoneyValue.Create("USD", -25.00m),
            startOfMonth.AddDays(8),
            "Farmers Market");

        cash.AddTransaction(
            MoneyValue.Create("USD", -15.00m),
            startOfMonth.AddDays(15),
            "Parking");

        // Add all accounts to the context
        context.Accounts.AddRange(checking, savings, creditCard, cash);

        await context.SaveChangesAsync();

        logger?.LogInformation(
            "Database seeded with {AccountCount} accounts and {TransactionCount} transactions.",
            4,
            checking.Transactions.Count + savings.Transactions.Count +
            creditCard.Transactions.Count + cash.Transactions.Count);
    }
}
