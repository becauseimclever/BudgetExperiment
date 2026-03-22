// <copyright file="RequestContext.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api.Observability;

/// <summary>
/// Request metadata included in the debug bundle.
/// </summary>
public sealed record RequestContext
{
    /// <summary>
    /// Gets the HTTP method (GET, POST, etc.).
    /// </summary>
    public required string Method
    {
        get; init;
    }

    /// <summary>
    /// Gets the route template (e.g., /api/v1/accounts/{id}).
    /// </summary>
    public required string RouteTemplate
    {
        get; init;
    }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public required int StatusCode
    {
        get; init;
    }

    /// <summary>
    /// Gets the elapsed time in milliseconds.
    /// </summary>
    public double? ElapsedMs
    {
        get; init;
    }
}
