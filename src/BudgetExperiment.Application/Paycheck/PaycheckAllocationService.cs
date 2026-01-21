// <copyright file="PaycheckAllocationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Mapping;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Paycheck;

/// <summary>
/// Application service for calculating paycheck allocations for recurring bills.
/// </summary>
public sealed class PaycheckAllocationService : IPaycheckAllocationService
{
    private readonly IRecurringTransactionRepository _recurringTransactionRepository;
    private readonly PaycheckAllocationCalculator _calculator;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaycheckAllocationService"/> class.
    /// </summary>
    /// <param name="recurringTransactionRepository">The recurring transaction repository.</param>
    public PaycheckAllocationService(IRecurringTransactionRepository recurringTransactionRepository)
    {
        this._recurringTransactionRepository = recurringTransactionRepository;
        this._calculator = new PaycheckAllocationCalculator();
    }

    /// <inheritdoc/>
    public async Task<PaycheckAllocationSummaryDto> GetAllocationSummaryAsync(
        RecurrenceFrequency paycheckFrequency,
        decimal? paycheckAmount = null,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        // Fetch recurring transactions (optionally filtered by account)
        var recurringTransactions = accountId.HasValue
            ? await this._recurringTransactionRepository.GetByAccountIdAsync(accountId.Value, cancellationToken)
            : await this._recurringTransactionRepository.GetAllAsync(cancellationToken);

        // Filter to only active bills (negative amounts, not income)
        var bills = recurringTransactions
            .Where(r => r.IsActive && r.Amount.Amount < 0)
            .Select(BillInfo.FromRecurringTransaction)
            .ToList();

        // Convert paycheck amount to MoneyValue if provided
        MoneyValue? paycheckMoney = paycheckAmount.HasValue
            ? MoneyValue.Create("USD", paycheckAmount.Value)
            : null;

        // Calculate allocations using domain service
        var summary = this._calculator.CalculateAllocationSummary(
            bills,
            paycheckFrequency,
            paycheckMoney);

        // Map to DTO and return
        return DomainToDtoMapper.ToDto(summary);
    }
}
