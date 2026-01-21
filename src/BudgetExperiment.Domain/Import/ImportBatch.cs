// <copyright file="ImportBatch.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Import;

/// <summary>
/// Represents a batch of imported transactions from a CSV file.
/// </summary>
public sealed class ImportBatch
{
    /// <summary>
    /// Maximum length for the file name.
    /// </summary>
    public const int MaxFileNameLength = 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportBatch"/> class.
    /// </summary>
    /// <remarks>Private constructor for EF Core and factory method.</remarks>
    private ImportBatch()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who performed this import.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the identifier of the account transactions were imported into.
    /// </summary>
    public Guid AccountId { get; private set; }

    /// <summary>
    /// Gets the identifier of the import mapping used (null if ad-hoc mapping).
    /// </summary>
    public Guid? MappingId { get; private set; }

    /// <summary>
    /// Gets the name of the imported file.
    /// </summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the total number of data rows in the CSV file.
    /// </summary>
    public int TotalRows { get; private set; }

    /// <summary>
    /// Gets the number of transactions successfully imported.
    /// </summary>
    public int ImportedCount { get; private set; }

    /// <summary>
    /// Gets the number of rows skipped (e.g., duplicates).
    /// </summary>
    public int SkippedCount { get; private set; }

    /// <summary>
    /// Gets the number of rows with errors.
    /// </summary>
    public int ErrorCount { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this import was performed.
    /// </summary>
    public DateTime ImportedAtUtc { get; private set; }

    /// <summary>
    /// Gets the current status of this import batch.
    /// </summary>
    public ImportBatchStatus Status { get; private set; }

    /// <summary>
    /// Creates a new import batch.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="accountId">The target account identifier.</param>
    /// <param name="fileName">The name of the CSV file.</param>
    /// <param name="totalRows">The total number of data rows.</param>
    /// <param name="mappingId">Optional mapping identifier.</param>
    /// <returns>A new <see cref="ImportBatch"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static ImportBatch Create(Guid userId, Guid accountId, string fileName, int totalRows, Guid? mappingId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User ID is required.");
        }

        if (accountId == Guid.Empty)
        {
            throw new DomainException("Account ID is required.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainException("File name is required.");
        }

        var trimmedFileName = fileName.Trim();
        if (trimmedFileName.Length > MaxFileNameLength)
        {
            throw new DomainException($"File name cannot exceed {MaxFileNameLength} characters.");
        }

        if (totalRows <= 0)
        {
            throw new DomainException("Total rows must be greater than zero.");
        }

        return new ImportBatch
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountId = accountId,
            MappingId = mappingId,
            FileName = trimmedFileName,
            TotalRows = totalRows,
            ImportedCount = 0,
            SkippedCount = 0,
            ErrorCount = 0,
            ImportedAtUtc = DateTime.UtcNow,
            Status = ImportBatchStatus.Pending,
        };
    }

    /// <summary>
    /// Marks the batch as complete with the final counts.
    /// </summary>
    /// <param name="imported">Number of transactions imported.</param>
    /// <param name="skipped">Number of rows skipped.</param>
    /// <param name="errors">Number of rows with errors.</param>
    /// <exception cref="DomainException">Thrown when counts are invalid.</exception>
    public void Complete(int imported, int skipped, int errors)
    {
        if (imported < 0)
        {
            throw new DomainException("Imported count cannot be negative.");
        }

        if (skipped < 0)
        {
            throw new DomainException("Skipped count cannot be negative.");
        }

        if (errors < 0)
        {
            throw new DomainException("Error count cannot be negative.");
        }

        this.ImportedCount = imported;
        this.SkippedCount = skipped;
        this.ErrorCount = errors;

        // Status is PartiallyCompleted if there were any errors
        this.Status = errors > 0 ? ImportBatchStatus.PartiallyCompleted : ImportBatchStatus.Completed;
    }

    /// <summary>
    /// Marks the batch as deleted (undo operation).
    /// </summary>
    public void MarkDeleted()
    {
        this.Status = ImportBatchStatus.Deleted;
    }
}
