namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Write operations for aggregates.
/// </summary>
/// <typeparam name="T">Aggregate root type.</typeparam>
public interface IWriteRepository<T>
{
    /// <summary>
    /// Adds the aggregate to the store.
    /// </summary>
    /// <param name="entity">Aggregate instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the aggregate from the store.
    /// </summary>
    /// <param name="entity">Aggregate instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync(T entity, CancellationToken cancellationToken = default);
}
