// <copyright file="DescriptionGroup.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Represents a group of transactions sharing a similar cleaned description,
/// annotated with frequency and amount range for AI prompt enrichment.
/// </summary>
/// <param name="RepresentativeDescription">The cleaned, normalized description representing this group.</param>
/// <param name="Count">Number of transactions matching this description.</param>
/// <param name="MinAmount">Minimum absolute transaction amount in the group (null if amounts unavailable).</param>
/// <param name="MaxAmount">Maximum absolute transaction amount in the group (null if amounts unavailable).</param>
public sealed record DescriptionGroup(
    string RepresentativeDescription,
    int Count,
    decimal? MinAmount,
    decimal? MaxAmount);
