// <copyright file="AccountType.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Represents the type of financial account.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// A checking/current account.
    /// </summary>
    Checking = 0,

    /// <summary>
    /// A savings account.
    /// </summary>
    Savings = 1,

    /// <summary>
    /// A credit card account.
    /// </summary>
    CreditCard = 2,

    /// <summary>
    /// A cash account.
    /// </summary>
    Cash = 3,

    /// <summary>
    /// Other account type.
    /// </summary>
    Other = 4,
}
