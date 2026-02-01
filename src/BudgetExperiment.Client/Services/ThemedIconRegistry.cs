// -----------------------------------------------------------------------
// <copyright file="ThemedIconRegistry.cs" company="Budget Experiment">
//     Copyright (c) Budget Experiment. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BudgetExperiment.Client.Services;

using System.Collections.Frozen;

/// <summary>
/// Registry that maps themes to custom icon sets.
/// Each theme can optionally define replacements for standard icon names.
/// </summary>
public static class ThemedIconRegistry
{
    /// <summary>
    /// Maps theme names to their custom icon sets.
    /// Each icon set maps standard icon names to themed icon names.
    /// </summary>
    private static readonly FrozenDictionary<string, FrozenDictionary<string, string>> IconSets =
        new Dictionary<string, FrozenDictionary<string, string>>
        {
            ["win95"] = new Dictionary<string, string>
            {
                ["calendar"] = "win95-calendar",
                ["home"] = "win95-home",
                ["settings"] = "win95-settings",
                ["refresh"] = "win95-refresh",
                ["repeat"] = "win95-repeat",
                ["check-circle"] = "win95-check",
                ["arrows-horizontal"] = "win95-transfer",
                ["calculator"] = "win95-calculator",
                ["tag"] = "win95-tag",
                ["filter"] = "win95-filter",
                ["pie-chart"] = "win95-chart",
                ["bar-chart"] = "win95-chart",
                ["bank"] = "win95-bank",
                ["upload"] = "win95-upload",
                ["download"] = "win95-download",
                ["plus"] = "win95-plus",
                ["wallet"] = "win95-wallet",
                ["credit-card"] = "win95-card",
                ["user"] = "win95-user",
                ["sparkles"] = "win95-sparkles",
            }.ToFrozenDictionary(),

            ["geocities"] = new Dictionary<string, string>
            {
                ["calendar"] = "geo-calendar",
                ["home"] = "geo-home",
                ["settings"] = "geo-settings",
                ["sparkles"] = "geo-sparkles",
                ["refresh"] = "geo-refresh",
            }.ToFrozenDictionary(),

            ["crayons"] = new Dictionary<string, string>
            {
                ["calendar"] = "crayon-calendar",
                ["home"] = "crayon-home",
                ["settings"] = "crayon-settings",
                ["tag"] = "crayon-tag",
                ["plus"] = "crayon-plus",
            }.ToFrozenDictionary(),
        }.ToFrozenDictionary();

    /// <summary>
    /// Gets the themed icon name for the specified theme and standard icon name.
    /// </summary>
    /// <param name="theme">The current theme name.</param>
    /// <param name="iconName">The standard icon name.</param>
    /// <returns>
    /// The themed icon name if one exists for the theme, otherwise the original icon name.
    /// </returns>
    public static string GetThemedIcon(string theme, string iconName)
    {
        if (string.IsNullOrEmpty(theme) || string.IsNullOrEmpty(iconName))
        {
            return iconName;
        }

        if (IconSets.TryGetValue(theme, out var iconSet) &&
            iconSet.TryGetValue(iconName.ToLowerInvariant(), out var themedIcon))
        {
            return themedIcon;
        }

        return iconName;
    }

    /// <summary>
    /// Gets all available themed icon sets.
    /// </summary>
    /// <returns>A collection of theme names that have custom icon sets.</returns>
    public static IEnumerable<string> GetThemesWithCustomIcons() => IconSets.Keys;

    /// <summary>
    /// Checks whether a theme has a custom icon set defined.
    /// </summary>
    /// <param name="theme">The theme name to check.</param>
    /// <returns>True if the theme has custom icons, otherwise false.</returns>
    public static bool HasCustomIcons(string theme)
    {
        return !string.IsNullOrEmpty(theme) && IconSets.ContainsKey(theme);
    }
}
