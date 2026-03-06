// <copyright file="DependencyInjection.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Domain;

/// <summary>
/// Domain layer DI registration.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adds domain services (currently a no-op placeholder for consistency).</summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Same collection for chaining.</returns>
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        return services;
    }
}
