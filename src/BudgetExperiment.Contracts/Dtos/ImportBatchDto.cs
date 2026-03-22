// <copyright file="ImportBatchDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for import batch history.
/// </summary>
public sealed record ImportBatchDto
{
    /// <summary>
    /// Gets the batch ID.
    /// </summary>
    public Guid Id
    {
        get; init;
    }

    /// <summary>
    /// Gets the account ID the batch was imported to.
    /// </summary>
    public Guid AccountId
    {
        get; init;
    }

    /// <summary>
    /// Gets the account name.
    /// </summary>
    public string AccountName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the original file name.
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of transactions imported.
    /// </summary>
    public int TransactionCount
    {
        get; init;
    }

    /// <summary>
    /// Gets the batch status.
    /// </summary>
    public ImportBatchStatus Status
    {
        get; init;
    }

    /// <summary>
    /// Gets the import timestamp.
    /// </summary>
    public DateTime ImportedAtUtc
    {
        get; init;
    }

    /// <summary>
    /// Gets the mapping ID used (if any).
    /// </summary>
    public Guid? MappingId
    {
        get; init;
    }

    /// <summary>
    /// Gets the mapping name used (if any).
    /// </summary>
    public string? MappingName
    {
        get; init;
    }
}
