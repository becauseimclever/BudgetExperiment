using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Application;

/// <summary>
/// Application layer DI registration.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adds application services.</summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Same collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Services will be registered as new features are implemented
        return services;
    }
}
