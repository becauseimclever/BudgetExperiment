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

    /// <summary>
    /// Gets the recurring transactions.
    /// </summary>
    public DbSet<RecurringTransaction> RecurringTransactions => this.Set<RecurringTransaction>();

    /// <summary>
    /// Gets the recurring transaction exceptions.
    /// </summary>
    public DbSet<RecurringTransactionException> RecurringTransactionExceptions => this.Set<RecurringTransactionException>();

    /// <summary>
    /// Gets the recurring transfers.
    /// </summary>
    public DbSet<RecurringTransfer> RecurringTransfers => this.Set<RecurringTransfer>();

    /// <summary>
    /// Gets the recurring transfer exceptions.
    /// </summary>
    public DbSet<RecurringTransferException> RecurringTransferExceptions => this.Set<RecurringTransferException>();

    /// <summary>
    /// Gets the application settings.
    /// </summary>
    public DbSet<AppSettings> AppSettings => this.Set<AppSettings>();

    /// <summary>
    /// Gets the budget categories.
    /// </summary>
    public DbSet<BudgetCategory> BudgetCategories => this.Set<BudgetCategory>();

    /// <summary>
    /// Gets the budget goals.
    /// </summary>
    public DbSet<BudgetGoal> BudgetGoals => this.Set<BudgetGoal>();

    /// <summary>
    /// Gets the user settings.
    /// </summary>
    public DbSet<UserSettings> UserSettings => this.Set<UserSettings>();

    /// <summary>
    /// Gets the categorization rules.
    /// </summary>
    public DbSet<CategorizationRule> CategorizationRules => this.Set<CategorizationRule>();

    /// <summary>
    /// Gets the AI-generated rule suggestions.
    /// </summary>
    public DbSet<RuleSuggestion> RuleSuggestions => this.Set<RuleSuggestion>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetDbContext).Assembly);
    }
}
