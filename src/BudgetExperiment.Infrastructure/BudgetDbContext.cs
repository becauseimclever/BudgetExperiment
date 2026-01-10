using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure;

/// <summary>
/// EF Core DbContext for budgeting aggregates.
/// </summary>
public sealed class BudgetDbContext : DbContext, IUnitOfWork
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetDbContext"/> class.
    /// </summary>
    /// <param name="options">DbContext options.</param>
    public BudgetDbContext(DbContextOptions<BudgetDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the accounts.
    /// </summary>
    public DbSet<Account> Accounts => this.Set<Account>();

    /// <summary>
    /// Gets the transactions.
    /// </summary>
    public DbSet<Transaction> Transactions => this.Set<Transaction>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetDbContext).Assembly);
    }
}
