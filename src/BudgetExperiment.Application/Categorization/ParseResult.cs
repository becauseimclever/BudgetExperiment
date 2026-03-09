// <copyright file="ParseResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Wraps the result of parsing an AI response, including diagnostic messages
/// for parse failures or warnings (e.g., skipped suggestions with unknown categories).
/// </summary>
/// <typeparam name="T">The type of the parsed result.</typeparam>
public sealed record ParseResult<T>
{
    /// <summary>
    /// Gets the parsed result value.
    /// </summary>
    public required T Result { get; init; }

    /// <summary>
    /// Gets a value indicating whether the parse was successful.
    /// When false, <see cref="Result"/> will be a default/empty value.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets diagnostic messages produced during parsing.
    /// Includes warnings (e.g., skipped categories) even on success,
    /// and error details on failure.
    /// </summary>
    public IReadOnlyList<string> Diagnostics { get; init; } = [];

    /// <summary>
    /// Creates a successful parse result.
    /// </summary>
    /// <param name="result">The parsed value.</param>
    /// <param name="diagnostics">Optional warning messages.</param>
    /// <returns>A successful <see cref="ParseResult{T}"/>.</returns>
    public static ParseResult<T> Ok(T result, IReadOnlyList<string>? diagnostics = null)
    {
        return new ParseResult<T>
        {
            Result = result,
            Success = true,
            Diagnostics = diagnostics ?? [],
        };
    }

    /// <summary>
    /// Creates a failed parse result.
    /// </summary>
    /// <param name="defaultValue">The default/empty value for the result.</param>
    /// <param name="diagnostics">Error diagnostic messages.</param>
    /// <returns>A failed <see cref="ParseResult{T}"/>.</returns>
    public static ParseResult<T> Fail(T defaultValue, IReadOnlyList<string> diagnostics)
    {
        return new ParseResult<T>
        {
            Result = defaultValue,
            Success = false,
            Diagnostics = diagnostics,
        };
    }
}
