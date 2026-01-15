// <copyright file="PaycheckAllocation.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Result of calculating allocation for a single bill.
/// </summary>
public sealed record PaycheckAllocation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaycheckAllocation"/> class.
    /// </summary>
    private PaycheckAllocation()
    {
    }

    /// <summary>
    /// Gets the bill information.
    /// </summary>
    public BillInfo Bill { get; private init; } = null!;

    /// <summary>
    /// Gets the amount to allocate per paycheck.
    /// </summary>
    public MoneyValue AmountPerPaycheck { get; private init; } = null!;

    /// <summary>
    /// Gets the annual amount for this bill.
    /// </summary>
    public MoneyValue AnnualAmount { get; private init; } = null!;

    /// <summary>
    /// Creates a new <see cref="PaycheckAllocation"/> instance.
    /// </summary>
    /// <param name="bill">The bill information.</param>
    /// <param name="amountPerPaycheck">The amount per paycheck.</param>
    /// <param name="annualAmount">The annual amount.</param>
    /// <returns>A new <see cref="PaycheckAllocation"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public static PaycheckAllocation Create(
        BillInfo bill,
        MoneyValue amountPerPaycheck,
        MoneyValue annualAmount)
    {
        ArgumentNullException.ThrowIfNull(bill);
        ArgumentNullException.ThrowIfNull(amountPerPaycheck);
        ArgumentNullException.ThrowIfNull(annualAmount);

        return new PaycheckAllocation
        {
            Bill = bill,
            AmountPerPaycheck = amountPerPaycheck,
            AnnualAmount = annualAmount,
        };
    }
}
