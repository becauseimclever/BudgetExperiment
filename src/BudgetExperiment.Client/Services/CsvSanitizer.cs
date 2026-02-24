// <copyright file="CsvSanitizer.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Sanitizes CSV cell values to prevent formula injection in spreadsheet applications.
/// Cells starting with formula-trigger characters (<c>=</c>, <c>@</c>, <c>+</c>, <c>-</c>,
/// tab, or carriage return) are prefixed with a single quote to neutralize execution.
/// </summary>
public static class CsvSanitizer
{
    /// <summary>
    /// Prefixes a cell value with <c>'</c> if it starts with a formula-trigger character.
    /// Null and empty values pass through unchanged.
    /// </summary>
    /// <param name="value">The raw cell value.</param>
    /// <returns>The sanitized value safe for display or export.</returns>
    public static string SanitizeForDisplay(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        char first = value[0];
        if (first is '=' or '@' or '+' or '-' or '\t' or '\r')
        {
            return "'" + value;
        }

        return value;
    }

    /// <summary>
    /// Strips the sanitization prefix added by <see cref="SanitizeForDisplay"/> so the
    /// original value can be used for numeric/date parsing. Only removes the leading
    /// apostrophe when it is immediately followed by a known trigger character.
    /// </summary>
    /// <param name="value">A potentially sanitized cell value.</param>
    /// <returns>The original unsanitized value.</returns>
    public static string UnsanitizeForParsing(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.Length >= 2 && value[0] == '\'')
        {
            char second = value[1];
            if (second is '=' or '@' or '+' or '-' or '\t' or '\r')
            {
                return value[1..];
            }
        }

        return value;
    }
}
