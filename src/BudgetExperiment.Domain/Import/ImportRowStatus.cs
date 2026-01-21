// <copyright file="ImportRowStatus.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Import;

/// <summary>
/// Status of a row during import preview validation.
/// </summary>
public enum ImportRowStatus
{
    /// <summary>
    /// Row is valid and ready for import.
    /// </summary>
    Valid = 0,

    /// <summary>
    /// Row has warnings but can still be imported.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Row has errors and cannot be imported.
    /// </summary>
    Error = 2,

    /// <summary>
    /// Row appears to be a duplicate of an existing transaction.
    /// </summary>
    Duplicate = 3,
}
