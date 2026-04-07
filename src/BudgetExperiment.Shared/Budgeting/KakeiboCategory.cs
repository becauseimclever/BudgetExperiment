// <copyright file="KakeiboCategory.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Shared.Budgeting;

/// <summary>
/// The four Kakeibo spending buckets used to categorize expenses by philosophical intent.
/// Only applicable to Expense-type budget categories; Income and Transfer remain null.
/// </summary>
public enum KakeiboCategory
{
    /// <summary>
    /// 必要 (Hitsuyou) — Things needed to live: housing, utilities, groceries, healthcare, transportation.
    /// </summary>
    Essentials = 1,

    /// <summary>
    /// 欲しい (Hoshii) — Things enjoyed but not essential: dining, entertainment, shopping, subscriptions.
    /// </summary>
    Wants = 2,

    /// <summary>
    /// 文化 (Bunka) — Things that enrich mind and spirit: education, books, charity, cultural experiences.
    /// </summary>
    Culture = 3,

    /// <summary>
    /// 予期しない (Yoki shinai) — Things that were not planned: emergency repairs, unexpected medical bills.
    /// </summary>
    Unexpected = 4,
}
