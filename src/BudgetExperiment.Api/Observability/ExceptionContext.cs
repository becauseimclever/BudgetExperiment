// <copyright file="ExceptionContext.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api.Observability;

/// <summary>
/// Exception details included in the debug bundle (sanitized).
/// </summary>
public sealed record ExceptionContext
{
    /// <summary>
    /// Gets the fully-qualified exception type name.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the sanitized exception message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the full stack trace (safe — contains only code paths).
    /// </summary>
    public string? StackTrace { get; init; }
}
