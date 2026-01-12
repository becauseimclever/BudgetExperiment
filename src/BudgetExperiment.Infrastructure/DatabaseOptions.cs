// <copyright file="DatabaseOptions.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

namespace BudgetExperiment.Infrastructure;

/// <summary>
/// Configuration options for database initialization and migrations.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Gets or sets a value indicating whether migrations should run automatically at startup.
    /// Default: true.
    /// </summary>
    public bool AutoMigrate { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout for migration operations in seconds.
    /// Default: 300 (5 minutes).
    /// </summary>
    public int MigrationTimeoutSeconds { get; set; } = 300;
}
