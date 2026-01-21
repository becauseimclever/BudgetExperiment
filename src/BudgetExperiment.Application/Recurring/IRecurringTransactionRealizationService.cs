// <copyright file="IRecurringTransactionRealizationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Service interface for realizing recurring transactions into actual transactions.
/// </summary>
public interface IRecurringTransactionRealizationService
{
    /// <summary>
    /// Realizes a recurring transaction instance, converting it to an actual transaction.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="request">The realization request with optional overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created transaction DTO.</returns>
    /// <exception cref="BudgetExperiment.Domain.Common.DomainException">Thrown when the recurring transaction is not found or already realized.</exception>
    Task<TransactionDto> RealizeInstanceAsync(
        Guid recurringTransactionId,
        RealizeRecurringTransactionRequest request,
        CancellationToken cancellationToken = default);
}
