// <copyright file="EnvironmentContext.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Api.Observability;

/// <summary>
/// Environment metadata included in the debug bundle.
/// </summary>
public sealed record EnvironmentContext
{
    /// <summary>
    /// Gets the application version.
    /// </summary>
    public required string AppVersion
    {
        get; init;
    }

    /// <summary>
    /// Gets the .NET runtime version.
    /// </summary>
    public required string DotnetVersion
    {
        get; init;
    }

    /// <summary>
    /// Gets the OS description.
    /// </summary>
    public required string OsDescription
    {
        get; init;
    }

    /// <summary>
    /// Gets the environment name (Development, Production, etc.).
    /// </summary>
    public required string EnvironmentName
    {
        get; init;
    }

    /// <summary>
    /// Gets the machine name.
    /// </summary>
    public required string MachineName
    {
        get; init;
    }
}
