// <copyright file="ExtractedColumnValues.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Holds the raw string values extracted from a CSV row by <see cref="ImportFieldExtractor"/>.
/// </summary>
internal readonly record struct ExtractedColumnValues(
    string? DateStr,
    string? Description,
    string? AmountStr,
    string? DebitStr,
    string? CreditStr,
    string? CategoryName,
    string? Reference,
    string? IndicatorValue);
