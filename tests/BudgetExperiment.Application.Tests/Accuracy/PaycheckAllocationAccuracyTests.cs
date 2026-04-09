// <copyright file="PaycheckAllocationAccuracyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Tests.Accuracy;

/// <summary>
/// Accuracy tests for <see cref="PaycheckAllocationCalculator"/> verifying that
/// no money is lost or created through rounding, and that edge cases are handled
/// precisely. Complements the functional tests in <c>PaycheckAllocationCalculatorTests</c>.
/// </summary>
[Trait("Category", "Accuracy")]
public class PaycheckAllocationAccuracyTests
{
    private readonly PaycheckAllocationCalculator _calculator = new();

    [Theory]
    [InlineData(RecurrenceFrequency.Monthly, RecurrenceFrequency.BiWeekly, 26)]
    [InlineData(RecurrenceFrequency.Yearly, RecurrenceFrequency.Monthly, 12)]
    [InlineData(RecurrenceFrequency.Quarterly, RecurrenceFrequency.BiWeekly, 26)]
    [InlineData(RecurrenceFrequency.Weekly, RecurrenceFrequency.Weekly, 52)]
    public void Allocation_PerPaycheckTimesPayPeriods_WithinOneHalfCentPerPeriod(
        RecurrenceFrequency billFrequency,
        RecurrenceFrequency paycheckFrequency,
        int periodsPerYear)
    {
        // Arrange — $1,000 bill at the given frequency
        var bill = BillInfoValue.Create("Bill", MoneyValue.Create("USD", 1000m), billFrequency);

        // Act
        var allocation = _calculator.CalculateAllocation(bill, paycheckFrequency);

        // Assert — rounding error across all periods is at most $0.005 × periods.
        // AwayFromZero rounds the division result to 2 decimal places; the truncation
        // error per paycheck is at most $0.005, so total error ≤ $0.005 × periodsPerYear.
        var actualAnnualCoverage = allocation.AmountPerPaycheck.Amount * periodsPerYear;
        var error = Math.Abs(actualAnnualCoverage - allocation.AnnualAmount.Amount);
        var maxAllowedError = 0.005m * periodsPerYear;

        Assert.True(
            error <= maxAllowedError,
            $"Rounding error {error} exceeds maximum allowed {maxAllowedError} " +
            $"(AmountPerPaycheck {allocation.AmountPerPaycheck.Amount} × {periodsPerYear} = {actualAnnualCoverage}, " +
            $"AnnualAmount = {allocation.AnnualAmount.Amount})");
    }

    [Fact]
    public void Allocation_ZeroAmountBill_ProducesZeroPerPaycheck()
    {
        var bill = BillInfoValue.Create("Free service", MoneyValue.Create("USD", 0m), RecurrenceFrequency.Monthly);

        var allocation = _calculator.CalculateAllocation(bill, RecurrenceFrequency.BiWeekly);

        Assert.Equal(0m, allocation.AmountPerPaycheck.Amount);
        Assert.Equal(0m, allocation.AnnualAmount.Amount);
    }

    [Fact]
    public void AllocationSummary_TotalPerPaycheck_EqualsSumOfIndividualAllocations()
    {
        // Arrange — four different bills
        var bills = new[]
        {
            BillInfoValue.Create("Rent", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Monthly),
            BillInfoValue.Create("Car Insurance", MoneyValue.Create("USD", 600m), RecurrenceFrequency.Quarterly),
            BillInfoValue.Create("Streaming", MoneyValue.Create("USD", 15.99m), RecurrenceFrequency.Monthly),
            BillInfoValue.Create("Property Tax", MoneyValue.Create("USD", 3600m), RecurrenceFrequency.Yearly),
        };

        // Act
        var summary = _calculator.CalculateAllocationSummary(bills, RecurrenceFrequency.BiWeekly);
        var expectedTotal = summary.Allocations.Sum(a => a.AmountPerPaycheck.Amount);

        // Assert — reported total must match sum of individual allocations exactly
        Assert.Equal(expectedTotal, summary.TotalPerPaycheck.Amount);
    }

    [Fact]
    public void AllocationSummary_TotalAnnualBills_EqualsSumOfIndividualAnnualAmounts()
    {
        var bills = new[]
        {
            BillInfoValue.Create("Rent", MoneyValue.Create("USD", 1200m), RecurrenceFrequency.Monthly),
            BillInfoValue.Create("Car Insurance", MoneyValue.Create("USD", 600m), RecurrenceFrequency.Quarterly),
        };

        var summary = _calculator.CalculateAllocationSummary(bills, RecurrenceFrequency.BiWeekly);

        var expectedAnnual = summary.Allocations.Sum(a => a.AnnualAmount.Amount);
        Assert.Equal(expectedAnnual, summary.TotalAnnualBills.Amount);
    }

    [Theory]
    [InlineData(1200.00, RecurrenceFrequency.Monthly)]
    [InlineData(600.00, RecurrenceFrequency.Quarterly)]
    [InlineData(3600.00, RecurrenceFrequency.Yearly)]
    public void Allocation_RoundingError_AtMostHalfCentPerPeriod(double billAmountRaw, RecurrenceFrequency billFrequency)
    {
        // Arrange
        var bill = BillInfoValue.Create("Bill", MoneyValue.Create("USD", (decimal)billAmountRaw), billFrequency);
        const int periodsPerYear = 26; // BiWeekly

        // Act
        var allocation = _calculator.CalculateAllocation(bill, RecurrenceFrequency.BiWeekly);

        // Assert — total rounding error must not exceed $0.005 × periodsPerYear = $0.13
        var totalError = Math.Abs((allocation.AmountPerPaycheck.Amount * periodsPerYear) - allocation.AnnualAmount.Amount);
        Assert.True(
            totalError <= 0.005m * periodsPerYear,
            $"Total rounding error {totalError} exceeds maximum allowed {0.005m * periodsPerYear}");
    }
}
