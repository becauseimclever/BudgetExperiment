// <copyright file="PaycheckAllocationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Settings;

namespace BudgetExperiment.Application.Paycheck;

/// <summary>
/// Application service for calculating paycheck allocations for recurring bills.
/// </summary>
public sealed class PaycheckAllocationService : IPaycheckAllocationService
{
    private readonly IRecurringTransactionRepository _recurringTransactionRepository;
    private readonly PaycheckAllocationCalculator _calculator;
    private readonly ICurrencyProvider _currencyProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaycheckAllocationService"/> class.
    /// </summary>
    /// <param name="recurringTransactionRepository">The recurring transaction repository.</param>
    /// <param name="currencyProvider">The currency provider.</param>
    public PaycheckAllocationService(
        IRecurringTransactionRepository recurringTransactionRepository,
        ICurrencyProvider currencyProvider)
    {
        this._recurringTransactionRepository = recurringTransactionRepository;
        this._calculator = new PaycheckAllocationCalculator();
        this._currencyProvider = currencyProvider;
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
            .Select(BillInfoValue.FromRecurringTransaction)
            .ToList();

        // Convert paycheck amount to MoneyValue if provided
        var currency = await this._currencyProvider.GetCurrencyAsync(cancellationToken);
        MoneyValue? paycheckMoney = paycheckAmount.HasValue
            ? MoneyValue.Create(currency, paycheckAmount.Value)
            : null;

        // Calculate allocations using domain service
        var summary = this._calculator.CalculateAllocationSummary(
            bills,
            paycheckFrequency,
            paycheckMoney);

        // Map to DTO and return
        return PaycheckMapper.ToDto(summary);
    }
}
