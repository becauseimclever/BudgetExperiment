// <copyright file="CustomReportLayout.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Reports;

/// <summary>
/// Represents a saved custom report layout.
/// </summary>
public sealed class CustomReportLayout
{
    /// <summary>
    /// Maximum length for layout names.
    /// </summary>
    public const int MaxNameLength = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomReportLayout"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private CustomReportLayout()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the layout name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the JSON layout definition.
    /// </summary>
    public string LayoutJson { get; private set; } = "{}";

    /// <summary>
    /// Gets the UTC timestamp when the layout was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the layout was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the budget scope (Shared or Personal).
    /// </summary>
    public BudgetScope Scope { get; private set; }

    /// <summary>
    /// Gets the owner user ID. NULL for Shared scope, user ID for Personal scope.
    /// </summary>
    public Guid? OwnerUserId { get; private set; }

    /// <summary>
    /// Gets the user ID of who created this layout.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// Creates a new shared custom report layout.
    /// </summary>
    /// <param name="name">Layout name.</param>
    /// <param name="layoutJson">Layout JSON.</param>
    /// <param name="createdByUserId">Creating user ID.</param>
    /// <returns>A new <see cref="CustomReportLayout"/> instance.</returns>
    public static CustomReportLayout CreateShared(string name, string layoutJson, Guid createdByUserId)
    {
        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("Created by user ID is required.");
        }

        var now = DateTime.UtcNow;
        return new CustomReportLayout
        {
            Id = Guid.NewGuid(),
            Name = ValidateName(name),
            LayoutJson = NormalizeLayoutJson(layoutJson),
            Scope = BudgetScope.Shared,
            OwnerUserId = null,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Creates a new personal custom report layout.
    /// </summary>
    /// <param name="name">Layout name.</param>
    /// <param name="layoutJson">Layout JSON.</param>
    /// <param name="ownerUserId">Owner user ID.</param>
    /// <returns>A new <see cref="CustomReportLayout"/> instance.</returns>
    public static CustomReportLayout CreatePersonal(string name, string layoutJson, Guid ownerUserId)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new DomainException("Owner user ID is required.");
        }

        var now = DateTime.UtcNow;
        return new CustomReportLayout
        {
            Id = Guid.NewGuid(),
            Name = ValidateName(name),
            LayoutJson = NormalizeLayoutJson(layoutJson),
            Scope = BudgetScope.Personal,
            OwnerUserId = ownerUserId,
            CreatedByUserId = ownerUserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Updates the layout name.
    /// </summary>
    /// <param name="name">New name.</param>
    public void UpdateName(string name)
    {
        this.Name = ValidateName(name);
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the layout JSON definition.
    /// </summary>
    /// <param name="layoutJson">New layout JSON.</param>
    public void UpdateLayout(string layoutJson)
    {
        this.LayoutJson = NormalizeLayoutJson(layoutJson);
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Layout name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
        {
            throw new DomainException($"Layout name must be {MaxNameLength} characters or fewer.");
        }

        return trimmed;
    }

    private static string NormalizeLayoutJson(string layoutJson)
    {
        return string.IsNullOrWhiteSpace(layoutJson) ? "{}" : layoutJson.Trim();
    }
}
