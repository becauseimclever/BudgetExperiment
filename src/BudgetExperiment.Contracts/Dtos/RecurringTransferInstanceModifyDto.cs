// <copyright file="RecurringTransferInstanceModifyDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for modifying a single instance of a recurring transfer.
/// </summary>
public sealed class RecurringTransferInstanceModifyDto
{
    /// <summary>Gets or sets the modified amount (null = use series amount).</summary>
    public MoneyDto? Amount
    {
        get; set;
    }

    /// <summary>Gets or sets the modified description (null = use series description).</summary>
    public string? Description
    {
        get; set;
    }

    /// <summary>Gets or sets the modified date for rescheduling (null = use original date).</summary>
    public DateOnly? Date
    {
        get; set;
    }
}
