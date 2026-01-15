// <copyright file="PaycheckAllocationSummary.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Complete summary of all paycheck allocations.
/// </summary>
public sealed record PaycheckAllocationSummary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaycheckAllocationSummary"/> class.
    /// </summary>
    private PaycheckAllocationSummary()
    {
    }

    /// <summary>
    /// Gets the individual allocations.
    /// </summary>
    public IReadOnlyList<PaycheckAllocation> Allocations { get; private init; } = Array.Empty<PaycheckAllocation>();

    /// <summary>
    /// Gets the total amount to allocate per paycheck.
    /// </summary>
    public MoneyValue TotalPerPaycheck { get; private init; } = null!;

    /// <summary>
    /// Gets the optional paycheck amount.
    /// </summary>
    public MoneyValue? PaycheckAmount { get; private init; }

    /// <summary>
    /// Gets the remaining amount per paycheck after allocations.
    /// </summary>
    public MoneyValue RemainingPerPaycheck { get; private init; } = null!;

    /// <summary>
    /// Gets the shortfall amount per paycheck (if allocations exceed paycheck).
    /// </summary>
    public MoneyValue Shortfall { get; private init; } = null!;

    /// <summary>
    /// Gets the total annual bills.
    /// </summary>
    public MoneyValue TotalAnnualBills { get; private init; } = null!;

    /// <summary>
    /// Gets the optional total annual income.
    /// </summary>
    public MoneyValue? TotalAnnualIncome { get; private init; }

    /// <summary>
    /// Gets the warnings.
    /// </summary>
    public IReadOnlyList<PaycheckAllocationWarning> Warnings { get; private init; } = Array.Empty<PaycheckAllocationWarning>();

    /// <summary>
    /// Gets a value indicating whether there are any warnings.
    /// </summary>
    public bool HasWarnings => this.Warnings.Count > 0;

    /// <summary>
    /// Gets a value indicating whether annual bills exceed annual income.
    /// </summary>
    public bool CannotReconcile => this.Warnings.Any(w => w.Type == AllocationWarningType.CannotReconcile);

    /// <summary>
    /// Gets the paycheck frequency.
    /// </summary>
    public RecurrenceFrequency PaycheckFrequency { get; private init; }

    /// <summary>
    /// Creates a new <see cref="PaycheckAllocationSummary"/> instance.
    /// </summary>
    /// <param name="allocations">The individual allocations.</param>
    /// <param name="totalPerPaycheck">The total per paycheck.</param>
    /// <param name="totalAnnualBills">The total annual bills.</param>
    /// <param name="paycheckFrequency">The paycheck frequency.</param>
    /// <param name="warnings">The warnings.</param>
    /// <param name="paycheckAmount">Optional paycheck amount.</param>
    /// <param name="totalAnnualIncome">Optional total annual income.</param>
    /// <returns>A new <see cref="PaycheckAllocationSummary"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public static PaycheckAllocationSummary Create(
        IEnumerable<PaycheckAllocation> allocations,
        MoneyValue totalPerPaycheck,
        MoneyValue totalAnnualBills,
        RecurrenceFrequency paycheckFrequency,
        IEnumerable<PaycheckAllocationWarning> warnings,
        MoneyValue? paycheckAmount = null,
        MoneyValue? totalAnnualIncome = null)
    {
        ArgumentNullException.ThrowIfNull(allocations);
        ArgumentNullException.ThrowIfNull(totalPerPaycheck);
        ArgumentNullException.ThrowIfNull(totalAnnualBills);
        ArgumentNullException.ThrowIfNull(warnings);

        var allocationsList = allocations.ToList().AsReadOnly();
        var warningsList = warnings.ToList().AsReadOnly();

        var currency = totalPerPaycheck.Currency;
        MoneyValue remaining;
        MoneyValue shortfall;

        if (paycheckAmount is not null)
        {
            var difference = paycheckAmount.Amount - totalPerPaycheck.Amount;
            if (difference >= 0)
            {
                remaining = MoneyValue.Create(currency, difference);
                shortfall = MoneyValue.Zero(currency);
            }
            else
            {
                remaining = MoneyValue.Zero(currency);
                shortfall = MoneyValue.Create(currency, Math.Abs(difference));
            }
        }
        else
        {
            remaining = MoneyValue.Zero(currency);
            shortfall = MoneyValue.Zero(currency);
        }

        return new PaycheckAllocationSummary
        {
            Allocations = allocationsList,
            TotalPerPaycheck = totalPerPaycheck,
            PaycheckAmount = paycheckAmount,
            RemainingPerPaycheck = remaining,
            Shortfall = shortfall,
            TotalAnnualBills = totalAnnualBills,
            TotalAnnualIncome = totalAnnualIncome,
            Warnings = warningsList,
            PaycheckFrequency = paycheckFrequency,
        };
    }
}
