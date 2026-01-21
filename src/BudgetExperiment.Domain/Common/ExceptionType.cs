// <copyright file="ExceptionType.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Common;

/// <summary>
/// Defines the type of exception for a recurring transaction instance.
/// </summary>
public enum ExceptionType
{
    /// <summary>
    /// The instance has custom values (amount, description, or date).
    /// </summary>
    Modified = 0,

    /// <summary>
    /// The instance is excluded from generation (skipped).
    /// </summary>
    Skipped = 1,
}
