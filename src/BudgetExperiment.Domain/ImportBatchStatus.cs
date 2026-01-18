// <copyright file="ImportBatchStatus.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Represents the status of an import batch.
/// </summary>
public enum ImportBatchStatus
{
    /// <summary>
    /// Import batch is pending execution.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Import batch completed successfully with all rows imported.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Import batch completed with some errors or skipped rows.
    /// </summary>
    PartiallyCompleted = 2,

    /// <summary>
    /// Import batch has been deleted/undone.
    /// </summary>
    Deleted = 3,
}
