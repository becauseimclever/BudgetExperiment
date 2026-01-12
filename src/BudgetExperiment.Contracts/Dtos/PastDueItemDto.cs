// <copyright file="PastDueItemDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO representing a past-due recurring item.
/// </summary>
public sealed class PastDueItemDto
{
    /// <summary>Gets or sets the recurring item identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the type of recurring item (recurring-transaction or recurring-transfer).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the scheduled instance date.</summary>
    public DateOnly InstanceDate { get; set; }

    /// <summary>Gets or sets the number of days past due.</summary>
    public int DaysPastDue { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the account identifier (for transactions).</summary>
    public Guid? AccountId { get; set; }

    /// <summary>Gets or sets the account name (for transactions).</summary>
    public string? AccountName { get; set; }

    /// <summary>Gets or sets the source account identifier (for transfers).</summary>
    public Guid? SourceAccountId { get; set; }

    /// <summary>Gets or sets the source account name (for transfers).</summary>
    public string? SourceAccountName { get; set; }

    /// <summary>Gets or sets the destination account identifier (for transfers).</summary>
    public Guid? DestinationAccountId { get; set; }

    /// <summary>Gets or sets the destination account name (for transfers).</summary>
    public string? DestinationAccountName { get; set; }
}
