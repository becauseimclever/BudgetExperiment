// <copyright file="AccessibilityHelper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

namespace BudgetExperiment.E2E.Tests.Helpers;

/// <summary>
/// Helper class for axe-core accessibility testing with Playwright.
/// Provides utilities for scanning pages and asserting WCAG compliance.
/// </summary>
public static class AccessibilityHelper
{
    /// <summary>
    /// Analyzes a page for accessibility violations using axe-core.
    /// </summary>
    /// <param name="page">The Playwright page to analyze.</param>
    /// <param name="include">Optional CSS selectors to include in the analysis.</param>
    /// <param name="exclude">Optional CSS selectors to exclude from the analysis.</param>
    /// <returns>The axe-core analysis result.</returns>
    public static async Task<AxeResult> AnalyzePageAsync(
        IPage page,
        string[]? include = null,
        string[]? exclude = null)
    {
        // Configure run options for WCAG 2.0/2.1 AA
        var options = new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions
            {
                Type = "tag",
                Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"],
            },
        };

        // Build context if include/exclude specified
        if (include != null || exclude != null)
        {
            var context = new AxeRunContext();

            if (include != null)
            {
                context.Include = include.Select(s => new AxeSelector(s)).ToList();
            }

            if (exclude != null)
            {
                context.Exclude = exclude.Select(s => new AxeSelector(s)).ToList();
            }

            return await page.RunAxe(context, options);
        }

        return await page.RunAxe(options);
    }

    /// <summary>
    /// Asserts that no accessibility violations were found.
    /// Throws an exception with detailed violation information if violations exist.
    /// </summary>
    /// <param name="result">The axe-core analysis result.</param>
    /// <param name="pageName">Optional page name for error messages.</param>
    public static void AssertNoViolations(AxeResult result, string? pageName = null)
    {
        if (result.Violations == null || result.Violations.Length == 0)
        {
            return;
        }

        var message = FormatViolations(result.Violations, pageName);
        throw new Xunit.Sdk.XunitException(message);
    }

    /// <summary>
    /// Asserts that no serious or critical violations were found.
    /// Allows minor and moderate violations to pass.
    /// </summary>
    /// <param name="result">The axe-core analysis result.</param>
    /// <param name="pageName">Optional page name for error messages.</param>
    public static void AssertNoSeriousViolations(AxeResult result, string? pageName = null)
    {
        if (result.Violations == null || result.Violations.Length == 0)
        {
            return;
        }

        var seriousViolations = result.Violations
            .Where(v => v.Impact == "critical" || v.Impact == "serious")
            .ToArray();

        if (seriousViolations.Length == 0)
        {
            return;
        }

        var message = FormatViolations(seriousViolations, pageName, "Serious/Critical");
        throw new Xunit.Sdk.XunitException(message);
    }

    /// <summary>
    /// Gets a summary of the analysis result.
    /// </summary>
    /// <param name="result">The axe-core analysis result.</param>
    /// <returns>A summary string.</returns>
    public static string GetSummary(AxeResult result)
    {
        var violations = result.Violations?.Length ?? 0;
        var passes = result.Passes?.Length ?? 0;
        var incomplete = result.Incomplete?.Length ?? 0;
        var inapplicable = result.Inapplicable?.Length ?? 0;

        return $"Violations: {violations}, Passes: {passes}, Incomplete: {incomplete}, Inapplicable: {inapplicable}";
    }

    /// <summary>
    /// Formats violations into a readable error message.
    /// </summary>
    private static string FormatViolations(AxeResultItem[] violations, string? pageName = null, string? violationType = null)
    {
        var sb = new StringBuilder();

        var header = violationType != null
            ? $"{violationType} accessibility violations"
            : "Accessibility violations";

        if (pageName != null)
        {
            sb.AppendLine($"{header} found on {pageName}:");
        }
        else
        {
            sb.AppendLine($"{header} found:");
        }

        sb.AppendLine();

        foreach (var violation in violations)
        {
            sb.AppendLine($"[{violation.Impact?.ToUpperInvariant()}] {violation.Id}: {violation.Description}");
            sb.AppendLine($"  Help: {violation.Help}");
            sb.AppendLine($"  WCAG: {string.Join(", ", violation.Tags ?? [])}");
            sb.AppendLine($"  More info: {violation.HelpUrl}");

            if (violation.Nodes != null && violation.Nodes.Length > 0)
            {
                sb.AppendLine($"  Affected elements ({violation.Nodes.Length}):");
                foreach (var node in violation.Nodes.Take(5)) // Limit to first 5 for readability
                {
                    var target = node.Target?.ToString() ?? "unknown";
                    sb.AppendLine($"    - {target}");
                    sb.AppendLine($"      HTML: {node.Html}");
                }

                if (violation.Nodes.Length > 5)
                {
                    sb.AppendLine($"    ... and {violation.Nodes.Length - 5} more");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
