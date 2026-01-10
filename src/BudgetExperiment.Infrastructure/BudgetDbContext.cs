using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure;

/// <summary>
/// EF Core DbContext for budgeting aggregates.
/// </summary>
public sealed class BudgetDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetDbContext"/> class.
    /// </summary>
    /// <param name="options">DbContext options.</param>
    public BudgetDbContext(DbContextOptions<BudgetDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Entity configurations will be added as new domain models are implemented
    }
}
