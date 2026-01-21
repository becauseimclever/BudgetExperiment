// <copyright file="IRecurringTransferRealizationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Service interface for realizing recurring transfers into actual transfers.
/// </summary>
public interface IRecurringTransferRealizationService
{
    /// <summary>
    /// Realizes a recurring transfer instance, converting it to actual transfer transactions.
    /// </summary>
    /// <param name="recurringTransferId">The recurring transfer identifier.</param>
    /// <param name="request">The realization request with optional overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created transfer response DTO.</returns>
    /// <exception cref="BudgetExperiment.Domain.Common.DomainException">Thrown when the recurring transfer is not found or already realized.</exception>
    Task<TransferResponse> RealizeInstanceAsync(
        Guid recurringTransferId,
        RealizeRecurringTransferRequest request,
        CancellationToken cancellationToken = default);
}
