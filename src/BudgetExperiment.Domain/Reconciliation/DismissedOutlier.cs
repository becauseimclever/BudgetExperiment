// <copyright file="DismissedOutlier.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Reconciliation;

/// <summary>Records that a transaction outlier has been dismissed by the user.</summary>
public sealed class DismissedOutlier
{
    private DismissedOutlier()
    {
    }

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id
    {
        get; private set;
    }

    /// <summary>Gets the transaction identifier that was dismissed.</summary>
    public Guid TransactionId
    {
        get; private set;
    }

    /// <summary>Gets the UTC timestamp when the outlier was dismissed.</summary>
    public DateTime DismissedAtUtc
    {
        get; private set;
    }

    /// <summary>Creates a new DismissedOutlier record.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <returns>A new <see cref="DismissedOutlier"/> instance.</returns>
    public static DismissedOutlier Create(Guid transactionId)
    {
        return new DismissedOutlier
        {
            Id = Guid.CreateVersion7(),
            TransactionId = transactionId,
            DismissedAtUtc = DateTime.UtcNow,
        };
    }
}
