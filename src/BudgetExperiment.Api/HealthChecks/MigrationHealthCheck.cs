// <copyright file="MigrationHealthCheck.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BudgetExperiment.Api.HealthChecks;

/// <summary>
/// Health check that reports the status of database migrations.
/// Returns Healthy when no pending migrations exist, Degraded when migrations are pending.
/// </summary>
public sealed class MigrationHealthCheck : IHealthCheck
{
    private readonly BudgetDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationHealthCheck"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public MigrationHealthCheck(BudgetDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pending = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
            var pendingList = pending.ToList();

            if (pendingList.Count > 0)
            {
                return HealthCheckResult.Degraded(
                    $"Database has {pendingList.Count} pending migration(s): {string.Join(", ", pendingList)}");
            }

            return HealthCheckResult.Healthy("Database schema is up to date.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("relational"))
        {
            // In-memory/non-relational database (e.g., for integration tests)
            return HealthCheckResult.Healthy("Non-relational database - migrations not applicable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cannot check database migration status.", ex);
        }
    }
}
