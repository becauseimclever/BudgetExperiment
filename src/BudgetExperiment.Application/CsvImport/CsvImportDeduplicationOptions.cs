using System;

namespace BudgetExperiment.Application.CsvImport;

/// <summary>
/// Configuration for CSV import deduplication heuristics.
/// </summary>
public sealed class CsvImportDeduplicationOptions
{
    /// <summary>
    /// Gets or sets the fuzzy date window in days (±N days).
    /// </summary>
    public int FuzzyDateWindowDays { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum allowed Levenshtein distance.
    /// </summary>
    public int MaxLevenshteinDistance { get; set; } = 5;

    /// <summary>
    /// Gets or sets the minimum required Jaccard similarity (0-1).
    /// </summary>
    public double MinJaccardSimilarity { get; set; } = 0.6;

    internal int EffectiveDateWindowDays => this.FuzzyDateWindowDays > 0 ? this.FuzzyDateWindowDays : 3;
    internal int EffectiveMaxLevenshtein => this.MaxLevenshteinDistance > 0 ? this.MaxLevenshteinDistance : 5;
    internal double EffectiveMinJaccard => this.MinJaccardSimilarity > 0 ? this.MinJaccardSimilarity : 0.6;
}
