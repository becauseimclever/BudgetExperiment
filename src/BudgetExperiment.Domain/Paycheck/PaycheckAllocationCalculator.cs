// <copyright file="PaycheckAllocationCalculator.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Paycheck;

/// <summary>
/// Domain service for calculating paycheck allocations for bills.
/// </summary>
public sealed class PaycheckAllocationCalculator
{
    /// <summary>
    /// Calculates the allocation for a single bill.
    /// </summary>
    /// <param name="bill">The bill to calculate allocation for.</param>
    /// <param name="paycheckFrequency">The paycheck frequency.</param>
    /// <returns>The calculated <see cref="PaycheckAllocation"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when bill is null.</exception>
    public PaycheckAllocation CalculateAllocation(BillInfo bill, RecurrenceFrequency paycheckFrequency)
    {
        ArgumentNullException.ThrowIfNull(bill);

        var annualMultiplier = GetAnnualMultiplier(bill.Frequency);
        var periodsPerYear = GetPeriodsPerYear(paycheckFrequency);

        var annualAmount = bill.Amount.Amount * annualMultiplier;
        var amountPerPaycheck = Math.Round(annualAmount / periodsPerYear, 2, MidpointRounding.AwayFromZero);

        return PaycheckAllocation.Create(
            bill,
            MoneyValue.Create(bill.Amount.Currency, amountPerPaycheck),
            MoneyValue.Create(bill.Amount.Currency, annualAmount));
    }

    /// <summary>
    /// Calculates the allocation summary for multiple bills.
    /// </summary>
    /// <param name="bills">The bills to calculate allocations for.</param>
    /// <param name="paycheckFrequency">The paycheck frequency.</param>
    /// <param name="paycheckAmount">Optional paycheck amount for income calculations.</param>
    /// <returns>The calculated <see cref="PaycheckAllocationSummary"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when bills is null.</exception>
    public PaycheckAllocationSummary CalculateAllocationSummary(
        IEnumerable<BillInfo> bills,
        RecurrenceFrequency paycheckFrequency,
        MoneyValue? paycheckAmount = null)
    {
        ArgumentNullException.ThrowIfNull(bills);

        var billsList = bills.ToList();
        var warnings = new List<PaycheckAllocationWarning>();
        var currency = paycheckAmount?.Currency ?? "USD";

        // Handle empty bills
        if (billsList.Count == 0)
        {
            warnings.Add(PaycheckAllocationWarning.NoBillsConfigured());

            return PaycheckAllocationSummary.Create(
                Array.Empty<PaycheckAllocation>(),
                MoneyValue.Zero(currency),
                MoneyValue.Zero(currency),
                paycheckFrequency,
                warnings,
                paycheckAmount,
                paycheckAmount is not null ? CalculateAnnualIncome(paycheckAmount, paycheckFrequency) : null);
        }

        // Use currency from first bill if no paycheck amount provided
        currency = billsList[0].Amount.Currency;

        // Calculate allocations for each bill
        var allocations = billsList
            .Select(bill => this.CalculateAllocation(bill, paycheckFrequency))
            .ToList();

        // Calculate totals
        var totalAnnualBills = MoneyValue.Create(
            currency,
            allocations.Sum(a => a.AnnualAmount.Amount));

        var totalPerPaycheck = MoneyValue.Create(
            currency,
            allocations.Sum(a => a.AmountPerPaycheck.Amount));

        // Calculate annual income if paycheck amount provided
        MoneyValue? totalAnnualIncome = null;
        if (paycheckAmount is not null)
        {
            totalAnnualIncome = CalculateAnnualIncome(paycheckAmount, paycheckFrequency);

            // Check for cannot reconcile (annual bills > annual income)
            if (totalAnnualBills.Amount > totalAnnualIncome.Amount)
            {
                warnings.Add(PaycheckAllocationWarning.CannotReconcile(totalAnnualBills, totalAnnualIncome));
            }

            // Check for insufficient income per paycheck
            if (totalPerPaycheck.Amount > paycheckAmount.Amount)
            {
                var shortfall = MoneyValue.Create(currency, totalPerPaycheck.Amount - paycheckAmount.Amount);
                warnings.Add(PaycheckAllocationWarning.InsufficientIncome(shortfall));
            }
        }
        else
        {
            // No income configured warning
            warnings.Add(PaycheckAllocationWarning.NoIncomeConfigured());
        }

        return PaycheckAllocationSummary.Create(
            allocations,
            totalPerPaycheck,
            totalAnnualBills,
            paycheckFrequency,
            warnings,
            paycheckAmount,
            totalAnnualIncome);
    }

    /// <summary>
    /// Gets the annual multiplier for a recurrence frequency.
    /// </summary>
    /// <param name="frequency">The frequency.</param>
    /// <returns>The number of occurrences per year.</returns>
    private static int GetAnnualMultiplier(RecurrenceFrequency frequency) => frequency switch
    {
        RecurrenceFrequency.Daily => 365,
        RecurrenceFrequency.Weekly => 52,
        RecurrenceFrequency.BiWeekly => 26,
        RecurrenceFrequency.Monthly => 12,
        RecurrenceFrequency.Quarterly => 4,
        RecurrenceFrequency.Yearly => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unknown recurrence frequency."),
    };

    /// <summary>
    /// Gets the number of pay periods per year for a paycheck frequency.
    /// </summary>
    /// <param name="frequency">The paycheck frequency.</param>
    /// <returns>The number of pay periods per year.</returns>
    private static int GetPeriodsPerYear(RecurrenceFrequency frequency) => frequency switch
    {
        RecurrenceFrequency.Weekly => 52,
        RecurrenceFrequency.BiWeekly => 26,
        RecurrenceFrequency.Monthly => 12,
        RecurrenceFrequency.Quarterly => 4,
        RecurrenceFrequency.Yearly => 1,
        RecurrenceFrequency.Daily => 365,
        _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unknown paycheck frequency."),
    };

    /// <summary>
    /// Calculates the annual income based on paycheck amount and frequency.
    /// </summary>
    /// <param name="paycheckAmount">The paycheck amount.</param>
    /// <param name="paycheckFrequency">The paycheck frequency.</param>
    /// <returns>The calculated annual income.</returns>
    private static MoneyValue CalculateAnnualIncome(MoneyValue paycheckAmount, RecurrenceFrequency paycheckFrequency)
    {
        var periodsPerYear = GetPeriodsPerYear(paycheckFrequency);
        return MoneyValue.Create(paycheckAmount.Currency, paycheckAmount.Amount * periodsPerYear);
    }
}
