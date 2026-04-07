// <copyright file="KakeiboDefaultSeeder.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Persistence;
using BudgetExperiment.Shared.Budgeting;

using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Seeding;

/// <summary>
/// Seeds smart Kakeibo default routing for existing expense categories.
/// Runs once at startup; idempotent (only updates rows with NULL KakeiboCategory).
/// </summary>
public static class KakeiboDefaultSeeder
{
    private static readonly Dictionary<KakeiboCategory, IReadOnlyList<string>> Defaults = new()
    {
        [KakeiboCategory.Essentials] = ["Groceries", "Utilities", "Gas", "Transportation", "Healthcare", "Insurance", "Housing", "Rent", "Electric", "Water", "Internet", "Phone", "Medical"],
        [KakeiboCategory.Wants] = ["Dining", "Restaurants", "Entertainment", "Shopping", "Subscriptions", "Health & Fitness", "Fitness", "Gym", "Pets", "Travel", "Clothing", "Personal Care", "Beauty", "Coffee"],
        [KakeiboCategory.Culture] = ["Education", "Books", "Charity", "Donations", "Museum", "Arts", "Learning", "Courses"],
        [KakeiboCategory.Unexpected] = ["Unexpected", "Emergency", "Repairs", "Maintenance", "Medical Emergency"],
    };

    /// <summary>
    /// Applies smart Kakeibo defaults to expense categories with no routing.
    /// Categories not matching any name pattern are assigned <see cref="KakeiboCategory.Wants"/> as fallback.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SeedAsync(BudgetDbContext context)
    {
        foreach (var (bucket, names) in Defaults)
        {
            foreach (var name in names)
            {
                await context.Database.ExecuteSqlRawAsync(
                    """
                    UPDATE "BudgetCategories"
                    SET "KakeiboCategory" = {0}
                    WHERE "KakeiboCategory" IS NULL
                      AND "Type" = 'Expense'
                      AND LOWER("Name") LIKE LOWER({1})
                    """,
                    (int)bucket,
                    $"%{name}%");
            }
        }

        await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE "BudgetCategories"
            SET "KakeiboCategory" = {0}
            WHERE "KakeiboCategory" IS NULL
              AND "Type" = 'Expense'
            """,
            (int)KakeiboCategory.Wants);
    }
}
