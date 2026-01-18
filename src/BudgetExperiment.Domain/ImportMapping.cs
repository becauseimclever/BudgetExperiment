// <copyright file="ImportMapping.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Represents a saved column mapping configuration for CSV imports.
/// </summary>
public sealed class ImportMapping
{
    /// <summary>
    /// Maximum length for the mapping name.
    /// </summary>
    public const int MaxNameLength = 200;

    private List<ColumnMapping> _columnMappings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportMapping"/> class.
    /// </summary>
    /// <remarks>Private constructor for EF Core and factory method.</remarks>
    private ImportMapping()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who owns this mapping.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the name of this mapping configuration.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the UTC timestamp when this mapping was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this mapping was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this mapping was last used for an import.
    /// </summary>
    public DateTime? LastUsedAtUtc { get; private set; }

    /// <summary>
    /// Gets the column mappings configuration.
    /// </summary>
    public IReadOnlyList<ColumnMapping> ColumnMappings => this._columnMappings.AsReadOnly();

    /// <summary>
    /// Gets the date format string for parsing dates.
    /// </summary>
    public string DateFormat { get; private set; } = "MM/dd/yyyy";

    /// <summary>
    /// Gets the mode for parsing amounts.
    /// </summary>
    public AmountParseMode AmountMode { get; private set; } = AmountParseMode.NegativeIsExpense;

    /// <summary>
    /// Gets the duplicate detection settings.
    /// </summary>
    public DuplicateDetectionSettings DuplicateSettings { get; private set; } = new();

    /// <summary>
    /// Creates a new import mapping configuration.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="name">The name for this mapping.</param>
    /// <param name="mappings">The column mappings.</param>
    /// <returns>A new <see cref="ImportMapping"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static ImportMapping Create(Guid userId, string name, IReadOnlyList<ColumnMapping> mappings)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User ID is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Name is required.");
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > MaxNameLength)
        {
            throw new DomainException($"Name cannot exceed {MaxNameLength} characters.");
        }

        if (mappings is null || mappings.Count == 0)
        {
            throw new DomainException("At least one column mapping is required.");
        }

        var now = DateTime.UtcNow;

        return new ImportMapping
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = trimmedName,
            _columnMappings = mappings.ToList(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Updates the mapping configuration.
    /// </summary>
    /// <param name="name">The new name.</param>
    /// <param name="mappings">The new column mappings.</param>
    /// <param name="dateFormat">The new date format.</param>
    /// <param name="amountMode">The new amount parse mode.</param>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public void Update(string name, IReadOnlyList<ColumnMapping> mappings, string dateFormat, AmountParseMode amountMode)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Name is required.");
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > MaxNameLength)
        {
            throw new DomainException($"Name cannot exceed {MaxNameLength} characters.");
        }

        if (mappings is null || mappings.Count == 0)
        {
            throw new DomainException("At least one column mapping is required.");
        }

        if (string.IsNullOrWhiteSpace(dateFormat))
        {
            throw new DomainException("Date format is required.");
        }

        this.Name = trimmedName;
        this._columnMappings = mappings.ToList();
        this.DateFormat = dateFormat.Trim();
        this.AmountMode = amountMode;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the duplicate detection settings.
    /// </summary>
    /// <param name="settings">The new duplicate detection settings.</param>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public void UpdateDuplicateSettings(DuplicateDetectionSettings settings)
    {
        if (settings is null)
        {
            throw new DomainException("Duplicate detection settings are required.");
        }

        this.DuplicateSettings = settings;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this mapping as recently used.
    /// </summary>
    public void MarkUsed()
    {
        this.LastUsedAtUtc = DateTime.UtcNow;
    }
}
