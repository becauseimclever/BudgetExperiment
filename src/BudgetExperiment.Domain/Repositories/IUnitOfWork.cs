namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Unit of work abstraction for committing persistence changes.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves changes to the underlying store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the concurrency token for a tracked entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The tracked entity.</param>
    /// <returns>The concurrency token as a string, or null if not configured.</returns>
    string? GetConcurrencyToken<T>(T entity)
        where T : class;

    /// <summary>
    /// Sets the expected concurrency token for optimistic concurrency checks.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The tracked entity.</param>
    /// <param name="token">The expected concurrency token value.</param>
    void SetExpectedConcurrencyToken<T>(T entity, string token)
        where T : class;
}
