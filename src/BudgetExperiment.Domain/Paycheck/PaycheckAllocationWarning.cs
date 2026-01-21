// <copyright file="PaycheckAllocationWarning.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

namespace BudgetExperiment.Domain.Paycheck;

/// <summary>
/// Represents a warning about paycheck allocation issues.
/// </summary>
public sealed record PaycheckAllocationWarning
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaycheckAllocationWarning"/> class.
    /// </summary>
    private PaycheckAllocationWarning()
    {
    }

    /// <summary>
    /// Gets the warning type.
    /// </summary>
    public AllocationWarningType Type { get; private init; }

    /// <summary>
    /// Gets the warning message.
    /// </summary>
    public string Message { get; private init; } = string.Empty;

    /// <summary>
    /// Gets the optional amount associated with the warning (e.g., shortfall amount).
    /// </summary>
    public MoneyValue? Amount { get; private init; }

    /// <summary>
    /// Creates a new warning.
    /// </summary>
    /// <param name="type">The warning type.</param>
    /// <param name="message">The warning message.</param>
    /// <param name="amount">Optional associated amount.</param>
    /// <returns>A new <see cref="PaycheckAllocationWarning"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when message is empty.</exception>
    public static PaycheckAllocationWarning Create(AllocationWarningType type, string message, MoneyValue? amount = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new DomainException("Warning message is required.");
        }

        return new PaycheckAllocationWarning
        {
            Type = type,
            Message = message.Trim(),
            Amount = amount,
        };
    }

    /// <summary>
    /// Creates an insufficient income warning.
    /// </summary>
    /// <param name="shortfall">The shortfall amount per paycheck.</param>
    /// <returns>A new <see cref="PaycheckAllocationWarning"/> instance.</returns>
    public static PaycheckAllocationWarning InsufficientIncome(MoneyValue shortfall)
    {
        var message = string.Format(
            CultureInfo.InvariantCulture,
            "Your bills require more than your paycheck amount. Shortfall: {0} {1:F2} per paycheck.",
            shortfall.Currency,
            shortfall.Amount);

        return new PaycheckAllocationWarning
        {
            Type = AllocationWarningType.InsufficientIncome,
            Message = message,
            Amount = shortfall,
        };
    }

    /// <summary>
    /// Creates a cannot reconcile warning (annual bills exceed annual income).
    /// </summary>
    /// <param name="annualBills">The total annual bills.</param>
    /// <param name="annualIncome">The total annual income.</param>
    /// <returns>A new <see cref="PaycheckAllocationWarning"/> instance.</returns>
    public static PaycheckAllocationWarning CannotReconcile(MoneyValue annualBills, MoneyValue annualIncome)
    {
        var shortfall = annualBills - annualIncome;
        var message = string.Format(
            CultureInfo.InvariantCulture,
            "Your annual bills ({0} {1:F2}) exceed your annual income ({0} {2:F2}). Please review your recurring expenses.",
            annualBills.Currency,
            annualBills.Amount,
            annualIncome.Amount);

        return new PaycheckAllocationWarning
        {
            Type = AllocationWarningType.CannotReconcile,
            Message = message,
            Amount = shortfall,
        };
    }

    /// <summary>
    /// Creates a warning indicating no bills are configured.
    /// </summary>
    /// <returns>A new <see cref="PaycheckAllocationWarning"/> instance.</returns>
    public static PaycheckAllocationWarning NoBillsConfigured()
    {
        return new PaycheckAllocationWarning
        {
            Type = AllocationWarningType.NoBillsConfigured,
            Message = "No recurring bills are configured. Add recurring transactions to see allocation suggestions.",
            Amount = null,
        };
    }

    /// <summary>
    /// Creates a warning indicating no income amount was provided.
    /// </summary>
    /// <returns>A new <see cref="PaycheckAllocationWarning"/> instance.</returns>
    public static PaycheckAllocationWarning NoIncomeConfigured()
    {
        return new PaycheckAllocationWarning
        {
            Type = AllocationWarningType.NoIncomeConfigured,
            Message = "Enter your paycheck amount to see income-related warnings and remaining balance calculations.",
            Amount = null,
        };
    }
}
