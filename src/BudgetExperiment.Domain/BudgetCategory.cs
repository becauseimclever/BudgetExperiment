// <copyright file="BudgetCategory.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Represents a budget category for organizing transactions.
/// </summary>
public sealed class BudgetCategory
{
    /// <summary>
    /// Maximum length for category name.
    /// </summary>
    public const int MaxNameLength = 100;

    private readonly List<BudgetGoal> _goals = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetCategory"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private BudgetCategory()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the category name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the icon identifier (e.g., "shopping", "food", "transport").
    /// </summary>
    public string? Icon { get; private set; }

    /// <summary>
    /// Gets the hex color code.
    /// </summary>
    public string? Color { get; private set; }

    /// <summary>
    /// Gets the category type.
    /// </summary>
    public CategoryType Type { get; private set; }

    /// <summary>
    /// Gets the sort order for display.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the category is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets the UTC timestamp when the category was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the category was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the budget goals for this category.
    /// </summary>
    public IReadOnlyCollection<BudgetGoal> Goals => this._goals.AsReadOnly();

    /// <summary>
    /// Creates a new budget category.
    /// </summary>
    /// <param name="name">The category name.</param>
    /// <param name="type">The category type.</param>
    /// <param name="icon">Optional icon identifier.</param>
    /// <param name="color">Optional hex color code.</param>
    /// <returns>A new <see cref="BudgetCategory"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static BudgetCategory Create(string name, CategoryType type, string? icon = null, string? color = null)
    {
        ValidateName(name);

        var now = DateTime.UtcNow;
        return new BudgetCategory
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Type = type,
            Icon = icon,
            Color = color,
            SortOrder = 0,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Updates the category properties.
    /// </summary>
    /// <param name="name">The new name.</param>
    /// <param name="icon">The new icon identifier.</param>
    /// <param name="color">The new hex color code.</param>
    /// <param name="sortOrder">The new sort order.</param>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public void Update(string name, string? icon, string? color, int sortOrder)
    {
        ValidateName(name);

        this.Name = name.Trim();
        this.Icon = icon;
        this.Color = color;
        this.SortOrder = sortOrder;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the category.
    /// </summary>
    public void Deactivate()
    {
        this.IsActive = false;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the category.
    /// </summary>
    public void Activate()
    {
        this.IsActive = true;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category name is required.");
        }

        if (name.Trim().Length > MaxNameLength)
        {
            throw new DomainException($"Category name cannot exceed {MaxNameLength} characters.");
        }
    }
}
