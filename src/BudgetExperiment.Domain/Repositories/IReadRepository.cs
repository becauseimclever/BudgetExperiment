namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Read operations for aggregates.
/// </summary>
/// <typeparam name="T">Aggregate type.</typeparam>
public interface IReadRepository<T>
{
    /// <summary>
    /// Gets an aggregate by id or null if not found.
    /// </summary>
    /// <param name="id">Aggregate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregate or null.</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists aggregates (paged) in unspecified but stable order (implementation-defined).
    /// </summary>
    /// <param name="skip">Items to skip.</param>
    /// <param name="take">Items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Page of aggregates.</returns>
    Task<IReadOnlyList<T>> ListAsync(int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts total aggregates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total count.</returns>
    Task<long> CountAsync(CancellationToken cancellationToken = default);
}
