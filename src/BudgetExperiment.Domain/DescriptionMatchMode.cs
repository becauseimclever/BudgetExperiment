// <copyright file="DescriptionMatchMode.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Specifies how transaction descriptions are matched for duplicate detection.
/// </summary>
public enum DescriptionMatchMode
{
    /// <summary>
    /// Descriptions must match exactly (case-insensitive).
    /// </summary>
    Exact = 0,

    /// <summary>
    /// One description must contain the other.
    /// </summary>
    Contains = 1,

    /// <summary>
    /// Fuzzy matching allowing minor differences.
    /// </summary>
    Fuzzy = 2,
}
