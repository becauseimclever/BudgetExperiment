// <copyright file="StubApiErrorContext.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;

namespace BudgetExperiment.Client.Tests.TestHelpers;

/// <summary>
/// Stub implementation of <see cref="IApiErrorContext"/> for unit tests.
/// </summary>
internal sealed class StubApiErrorContext : IApiErrorContext
{
    /// <inheritdoc/>
    public string? LastTraceId
    {
        get; set;
    }

    /// <inheritdoc/>
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
