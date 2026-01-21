// <copyright file="DebitCreditIndicatorSettings.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Import;

/// <summary>
/// Settings for interpreting a debit/credit indicator column.
/// </summary>
public sealed record DebitCreditIndicatorSettings
{
    /// <summary>
    /// Gets the column index of the indicator column (-1 if not used).
    /// </summary>
    public int IndicatorColumnIndex { get; init; } = -1;

    /// <summary>
    /// Gets the values that indicate a debit (expense) transaction.
    /// </summary>
    public IReadOnlyList<string> DebitIndicators { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the values that indicate a credit (income) transaction.
    /// </summary>
    public IReadOnlyList<string> CreditIndicators { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets a value indicating whether indicator matching is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; init; }

    /// <summary>
    /// Gets a value indicating whether this indicator is enabled.
    /// </summary>
    public bool IsEnabled => this.IndicatorColumnIndex >= 0
        && this.DebitIndicators.Count > 0
        && this.CreditIndicators.Count > 0;

    /// <summary>
    /// Creates debit/credit indicator settings.
    /// </summary>
    /// <param name="columnIndex">The indicator column index.</param>
    /// <param name="debitIndicators">Values indicating debit transactions.</param>
    /// <param name="creditIndicators">Values indicating credit transactions.</param>
    /// <param name="caseSensitive">Whether matching is case-sensitive.</param>
    /// <returns>A new <see cref="DebitCreditIndicatorSettings"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static DebitCreditIndicatorSettings Create(
        int columnIndex,
        IReadOnlyList<string> debitIndicators,
        IReadOnlyList<string> creditIndicators,
        bool caseSensitive = false)
    {
        if (columnIndex < 0)
        {
            throw new DomainException("Column index must be non-negative.");
        }

        if (debitIndicators == null || debitIndicators.Count == 0)
        {
            throw new DomainException("At least one debit indicator value is required.");
        }

        if (creditIndicators == null || creditIndicators.Count == 0)
        {
            throw new DomainException("At least one credit indicator value is required.");
        }

        // Validate no overlap between debit and credit indicators
        var comparison = caseSensitive
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase;

        var debitSet = new HashSet<string>(debitIndicators.Select(d => d.Trim()), comparison);
        var creditSet = new HashSet<string>(creditIndicators.Select(c => c.Trim()), comparison);

        if (debitSet.Overlaps(creditSet))
        {
            throw new DomainException("Debit and credit indicators cannot overlap.");
        }

        return new DebitCreditIndicatorSettings
        {
            IndicatorColumnIndex = columnIndex,
            DebitIndicators = debitIndicators.Select(d => d.Trim()).ToList(),
            CreditIndicators = creditIndicators.Select(c => c.Trim()).ToList(),
            CaseSensitive = caseSensitive,
        };
    }

    /// <summary>
    /// Gets disabled settings (no indicator column).
    /// </summary>
    public static DebitCreditIndicatorSettings Disabled => new();

    /// <summary>
    /// Determines the sign multiplier for an indicator value.
    /// </summary>
    /// <param name="indicatorValue">The indicator value from the CSV.</param>
    /// <returns>-1 for debit, 1 for credit, null if not matched.</returns>
    public int? GetSignMultiplier(string indicatorValue)
    {
        if (!this.IsEnabled || string.IsNullOrWhiteSpace(indicatorValue))
        {
            return null;
        }

        var comparison = this.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        var trimmedValue = indicatorValue.Trim();

        if (this.DebitIndicators.Any(d => string.Equals(d, trimmedValue, comparison)))
        {
            return -1; // Debit = expense = negative
        }

        if (this.CreditIndicators.Any(c => string.Equals(c, trimmedValue, comparison)))
        {
            return 1; // Credit = income = positive
        }

        return null; // Unrecognized indicator
    }
}
