// <copyright file="ChartColorProvider.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Pure colour resolution for chart categories and semantic transaction types.
/// All values mirror the light-mode baseline tokens in
/// <c>wwwroot/css/design-system/tokens.css</c>.
/// No external dependencies — all colour logic is deterministic and stateless.
/// </summary>
public sealed class ChartColorProvider : IChartColorProvider
{
    // Eight-colour palette sourced from the BudgetExperiment design tokens.
    // Ordered to maximise contrast between adjacent categories.
    private static readonly string[] Palette =
    [
        "#107c10", // income green     (--color-income)
        "#d13438", // expense red      (--color-expense)
        "#0078d4", // transfer blue    (--color-transfer)
        "#8764b8", // recurring purple (--color-recurring)
        "#ffb900", // amber            (--color-warning)
        "#038387", // teal
        "#05a6f0", // sky blue
        "#498205", // lime
    ];

    /// <inheritdoc/>
    public string GetIncomeColor() => "#107c10";

    /// <inheritdoc/>
    public string GetExpenseColor() => "#d13438";

    /// <inheritdoc/>
    public string GetTransferColor() => "#0078d4";

    /// <inheritdoc/>
    public string[] GetDefaultPalette() => Palette;

    /// <inheritdoc/>
    public string GetCategoryColor(string categoryName)
    {
        if (string.IsNullOrEmpty(categoryName))
        {
            return Palette[0];
        }

        var index = Math.Abs(categoryName.GetHashCode() % Palette.Length);
        return Palette[index];
    }
}
