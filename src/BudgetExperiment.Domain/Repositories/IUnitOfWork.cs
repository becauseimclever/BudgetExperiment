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
}
