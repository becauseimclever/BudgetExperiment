using BudgetExperiment.Domain;
using BudgetExperiment.Domain.FeatureFlags;
using BudgetExperiment.Domain.Kaizen;
using BudgetExperiment.Domain.Reconciliation;
using BudgetExperiment.Domain.Reflection;
using BudgetExperiment.Domain.Services;
using BudgetExperiment.Infrastructure.Persistence.Converters;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for budgeting aggregates.
/// </summary>
public sealed class BudgetDbContext : DbContext, IUnitOfWork
{
    private readonly IServiceProvider? _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetDbContext"/> class.
    /// </summary>
    /// <param name="options">DbContext options.</param>
    /// <param name="serviceProvider">Service provider for accessing encryption service.</param>
    public BudgetDbContext(
        DbContextOptions<BudgetDbContext> options,
        IServiceProvider serviceProvider)
        : base(options)
    {
        _serviceProvider = serviceProvider;
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

    /// <summary>
    /// Gets the import mappings.
    /// </summary>
    public DbSet<ImportMapping> ImportMappings => this.Set<ImportMapping>();

    /// <summary>
    /// Gets the import batches.
    /// </summary>
    public DbSet<ImportBatch> ImportBatches => this.Set<ImportBatch>();

    /// <summary>
    /// Gets the chat sessions.
    /// </summary>
    public DbSet<ChatSession> ChatSessions => this.Set<ChatSession>();

    /// <summary>
    /// Gets the chat messages.
    /// </summary>
    public DbSet<ChatMessage> ChatMessages => this.Set<ChatMessage>();

    /// <summary>
    /// Gets the reconciliation matches.
    /// </summary>
    public DbSet<ReconciliationMatch> ReconciliationMatches => this.Set<ReconciliationMatch>();

    /// <summary>
    /// Gets the AI-generated category suggestions.
    /// </summary>
    public DbSet<CategorySuggestion> CategorySuggestions => this.Set<CategorySuggestion>();

    /// <summary>
    /// Gets the custom report layouts.
    /// </summary>
    public DbSet<CustomReportLayout> CustomReportLayouts => this.Set<CustomReportLayout>();

    /// <summary>
    /// Gets the learned merchant mappings.
    /// </summary>
    public DbSet<LearnedMerchantMapping> LearnedMerchantMappings => this.Set<LearnedMerchantMapping>();

    /// <summary>
    /// Gets the dismissed suggestion patterns.
    /// </summary>
    public DbSet<DismissedSuggestionPattern> DismissedSuggestionPatterns => this.Set<DismissedSuggestionPattern>();

    /// <summary>
    /// Gets the recurring charge suggestions.
    /// </summary>
    public DbSet<RecurringChargeSuggestion> RecurringChargeSuggestions => this.Set<RecurringChargeSuggestion>();

    /// <summary>
    /// Gets the reconciliation records.
    /// </summary>
    public DbSet<ReconciliationRecord> ReconciliationRecords => this.Set<ReconciliationRecord>();

    /// <summary>
    /// Gets the statement balances.
    /// </summary>
    public DbSet<StatementBalance> StatementBalances => this.Set<StatementBalance>();

    /// <summary>Gets the dismissed outliers.</summary>
    public DbSet<DismissedOutlier> DismissedOutliers => this.Set<DismissedOutlier>();

    /// <summary>Gets the feature flags.</summary>
    public DbSet<FeatureFlag> FeatureFlags => this.Set<FeatureFlag>();

    /// <summary>Gets the monthly Kakeibo reflections.</summary>
    public DbSet<MonthlyReflection> MonthlyReflections => this.Set<MonthlyReflection>();

    /// <summary>Gets the Kaizen micro-goals.</summary>
    public DbSet<KaizenGoal> KaizenGoals => this.Set<KaizenGoal>();

    /// <inheritdoc />
    public string? GetConcurrencyToken<T>(T entity)
        where T : class
    {
        var entry = this.Entry(entity);
        var property = entry.Properties.FirstOrDefault(p => p.Metadata.IsConcurrencyToken && p.Metadata.Name == "xmin");
        return property?.CurrentValue?.ToString();
    }

    /// <inheritdoc />
    public void SetExpectedConcurrencyToken<T>(T entity, string token)
        where T : class
    {
        var entry = this.Entry(entity);
        var property = entry.Properties.FirstOrDefault(p => p.Metadata.IsConcurrencyToken && p.Metadata.Name == "xmin");
        if (property is not null && uint.TryParse(token, out var xmin))
        {
            property.OriginalValue = xmin;
        }
    }

    /// <inheritdoc />
    public void MarkAsModified<T>(T entity)
        where T : class
    {
        this.Entry(entity).State = EntityState.Modified;
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetDbContext).Assembly);

        // Apply encryption converters to sensitive fields
        var encryptionService = _serviceProvider?.GetService<IEncryptionService>();
        if (encryptionService is not null)
        {
            var converter = new EncryptedStringConverter(encryptionService);
            var nullableConverter = new EncryptedNullableStringConverter(encryptionService);

            // Account.Name (non-nullable)
            modelBuilder.Entity<Account>()
                .Property(a => a.Name)
                .HasConversion(converter);

            // Transaction.Description (non-nullable)
            modelBuilder.Entity<Transaction>()
                .Property(t => t.Description)
                .HasConversion(converter);

            // ChatMessage.Content (non-nullable)
            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.Content)
                .HasConversion(converter);

            // MonthlyReflection text fields (nullable)
            modelBuilder.Entity<MonthlyReflection>()
                .Property(r => r.IntentionText)
                .HasConversion(nullableConverter);

            modelBuilder.Entity<MonthlyReflection>()
                .Property(r => r.GratitudeText)
                .HasConversion(nullableConverter);

            modelBuilder.Entity<MonthlyReflection>()
                .Property(r => r.ImprovementText)
                .HasConversion(nullableConverter);

            // KaizenGoal.Description (non-nullable)
            modelBuilder.Entity<KaizenGoal>()
                .Property(g => g.Description)
                .HasConversion(converter);

            // CategorizationRule.Pattern (non-nullable, MerchantPattern)
            modelBuilder.Entity<CategorizationRule>()
                .Property(r => r.Pattern)
                .HasConversion(converter);
        }

        // Apply PostgreSQL-specific xmin configuration for optimistic concurrency.
        // Entity configurations mark shadow property "xmin" as IsConcurrencyToken();
        // here we map it to the PostgreSQL system column so the DB auto-manages the value.
        if (this.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var xminProp = entityType.FindProperty("xmin");
                if (xminProp is not null)
                {
                    xminProp.SetColumnName("xmin");
                    xminProp.SetColumnType("xid");
                    xminProp.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate;
                }
            }
        }
    }
}
