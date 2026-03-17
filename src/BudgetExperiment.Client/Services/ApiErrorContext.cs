// <copyright file="ApiErrorContext.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Scoped service that captures the traceId from the most recent API error response.
/// </summary>
public sealed class ApiErrorContext : IApiErrorContext
{
    /// <inheritdoc/>
    public string? LastTraceId { get; private set; }

    /// <summary>
    /// Sets the traceId from a ProblemDetails response.
    /// </summary>
    /// <param name="traceId">The trace identifier.</param>
    public void SetTraceId(string traceId)
    {
        this.LastTraceId = traceId;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        this.LastTraceId = null;
    }
}
