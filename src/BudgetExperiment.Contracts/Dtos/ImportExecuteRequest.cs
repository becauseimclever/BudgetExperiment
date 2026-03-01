// <copyright file="ImportExecuteRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request to execute an import.
/// </summary>
public sealed record ImportExecuteRequest
{
    /// <summary>
    /// Gets the target account ID.
    /// </summary>
    public Guid AccountId { get; init; }

    /// <summary>
    /// Gets the optional saved mapping ID used for this import.
    /// </summary>
    public Guid? MappingId { get; init; }

    /// <summary>
    /// Gets the original file name.
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the transactions to import (from preview, possibly with user modifications).
    /// </summary>
    public IReadOnlyList<ImportTransactionData> Transactions { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether to run reconciliation matching after import.
    /// When enabled, imported transactions will be matched against expected recurring transaction instances.
    /// </summary>
    public bool RunReconciliation { get; init; }
}
