// <copyright file="IAccountRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Repository interface for Account aggregate root.
/// </summary>
public interface IAccountRepository : IReadRepository<Account>, IWriteRepository<Account>
{
    /// <summary>
    /// Gets an account by ID including its transactions.
    /// </summary>
    /// <param name="id">Account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account with transactions or null.</returns>
    Task<Account?> GetByIdWithTransactionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all accounts (for selection/dropdown).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All accounts.</returns>
    Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default);
}
