using BudgetExperiment.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

    /// <summary>Gets unified adhoc transactions set.</summary>
    public DbSet<AdhocTransaction> AdhocTransactions => this.Set<AdhocTransaction>();

    /// <summary>Gets recurring schedules set.</summary>
    public DbSet<RecurringSchedule> RecurringSchedules => this.Set<RecurringSchedule>();

    /// <inheritdoc />
    public override int SaveChanges()
    {
        this.ApplyTimestamps();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.ApplyTimestamps();
        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // AdhocTransaction mapping (unified Expense and AdhocPayment replacement)
        modelBuilder.Entity<AdhocTransaction>(a =>
        {
            a.HasKey(x => x.Id);
            a.Property(x => x.Description).IsRequired().HasMaxLength(500);
            a.Property(x => x.TransactionType).IsRequired();
            a.OwnsOne(typeof(MoneyValue), "Money", mv =>
            {
                mv.Property("Currency").HasColumnName("MoneyCurrency").HasMaxLength(3).IsRequired();
                mv.Property("Amount").HasColumnName("MoneyValue").HasColumnType("numeric(18,2)").IsRequired();
            });
            a.Property(x => x.Date).HasConversion(new DateOnlyConverter()).IsRequired();
            a.Property(x => x.Category).HasMaxLength(100);
            a.Property(x => x.CreatedUtc).IsRequired();
            a.Property(x => x.UpdatedUtc);
            a.HasIndex(x => x.Date); // Index for date-based queries
            a.HasIndex(x => x.TransactionType); // Index for filtering by income/expense
                    a.HasIndex(x => new { x.Date, x.TransactionType })
                        .HasDatabaseName("IX_AdhocTransactions_Date_TransactionType"); // Composite index for duplicate detection
        });

        // RecurringSchedule mapping (unified PaySchedule and BillSchedule replacement)
        modelBuilder.Entity<RecurringSchedule>(r =>
        {
            r.HasKey(x => x.Id);
            r.Property(x => x.Name).HasMaxLength(200); // Optional for income schedules
            r.Property(x => x.Anchor).HasConversion(new DateOnlyConverter()).IsRequired();
            r.Property(x => x.Recurrence).IsRequired();
            r.Property(x => x.ScheduleType).IsRequired();
            r.Property(x => x.DaysInterval);
            r.OwnsOne(typeof(MoneyValue), "Amount", mv =>
            {
                mv.Property("Currency").HasColumnName("AmountCurrency").HasMaxLength(3).IsRequired();
                mv.Property("Amount").HasColumnName("AmountValue").HasColumnType("numeric(18,2)").IsRequired();
            });
            r.Property(x => x.CreatedUtc).IsRequired();
            r.Property(x => x.UpdatedUtc);
            r.HasIndex(x => x.ScheduleType); // Index for filtering by income/expense
            r.HasIndex(x => x.Anchor); // Index for occurrence calculations
        });
    }

    private void ApplyTimestamps()
    {
        foreach (var entry in this.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is AdhocTransaction adhocTransaction)
                {
                    adhocTransaction.MarkUpdated();
                }
                else if (entry.Entity is RecurringSchedule recurringSchedule)
                {
                    recurringSchedule.MarkUpdated();
                }
            }
        }
    }

    /// <summary>
    /// Converter for <see cref="DateOnly"/> to <see cref="DateTime"/> storage.
    /// </summary>
    private sealed class DateOnlyConverter : ValueConverter<DateOnly, DateTime>
    {
        public DateOnlyConverter()
            : base(
                d => DateTime.SpecifyKind(d.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
                dt => DateOnly.FromDateTime(DateTime.SpecifyKind(dt, DateTimeKind.Utc)))
        {
        }
    }
}
