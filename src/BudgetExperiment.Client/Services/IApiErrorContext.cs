// <copyright file="IApiErrorContext.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Captures the traceId from the most recent API error response (ProblemDetails).
/// </summary>
public interface IApiErrorContext
{
    /// <summary>
    /// Gets the traceId from the most recent API error response, or null if none.
    /// </summary>
    string? LastTraceId { get; }

    /// <summary>
    /// Clears the stored traceId.
    /// </summary>
    void Clear();
}
