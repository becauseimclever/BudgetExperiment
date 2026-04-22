// <copyright file="TransactionConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Transaction entity.
/// </summary>
internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.AccountId)
            .IsRequired();

        // MoneyValue as owned type
        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(t => t.Date)
            .IsRequired();

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(500);

        // Category FK (nullable)
        builder.Property(t => t.CategoryId);

        builder.HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();

        builder.Property(t => t.UpdatedAtUtc)
            .IsRequired();

        // Recurring transaction link (nullable FK)
        builder.Property(t => t.RecurringTransactionId);

        builder.Property(t => t.RecurringInstanceDate);

        builder.HasOne<RecurringTransaction>()
            .WithMany()
            .HasForeignKey(t => t.RecurringTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Transfer link (nullable, logical grouping only - no FK)
        builder.Property(t => t.TransferId);

        builder.Property(t => t.TransferDirection)
            .HasConversion<int?>();

        // Recurring transfer link (nullable FK)
        builder.Property(t => t.RecurringTransferId);

        builder.Property(t => t.RecurringTransferInstanceDate);

        builder.HasOne<RecurringTransfer>()
            .WithMany()
            .HasForeignKey(t => t.RecurringTransferId)
            .OnDelete(DeleteBehavior.SetNull);

        // Optimistic concurrency token (PostgreSQL-specific xmin config applied in DbContext.OnModelCreating)
        builder.Property<uint>("xmin")
            .IsConcurrencyToken();

        // Indexes for calendar queries
        builder.HasIndex(t => t.Date);
        builder.HasIndex(t => t.AccountId);
        builder.HasIndex(t => new { t.AccountId, t.Date });
        builder.HasIndex(t => t.RecurringTransactionId);
        builder.HasIndex(t => t.TransferId);
        builder.HasIndex(t => t.RecurringTransferId);
        builder.HasIndex(t => t.CategoryId);

        // Ownership properties for multi-user support
        builder.Property(t => t.OwnerUserId);

        builder.Property(t => t.CreatedByUserId)
            .IsRequired();

        // Indexes for ownership filtering
        builder.HasIndex(t => t.OwnerUserId);

        // Import batch link (nullable FK)
        builder.Property(t => t.ImportBatchId);

        builder.Property(t => t.ExternalReference)
            .HasMaxLength(Transaction.MaxExternalReferenceLength);

        builder.HasOne<ImportBatch>()
            .WithMany()
            .HasForeignKey(t => t.ImportBatchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.ImportBatchId)
            .HasDatabaseName("IX_Transactions_ImportBatchId");

        // Location owned type — columns added in VS-3 migration;
        // configure as owned here so EF Core doesn't treat it as an entity.
        builder.OwnsOne(t => t.Location, loc =>
        {
            loc.OwnsOne(l => l.Coordinates, coord =>
            {
                coord.Property(c => c.Latitude)
                    .HasColumnName("Location_Latitude")
                    .HasPrecision(9, 6);
                coord.Property(c => c.Longitude)
                    .HasColumnName("Location_Longitude")
                    .HasPrecision(9, 6);
            });

            loc.Property(l => l.City)
                .HasColumnName("Location_City")
                .HasMaxLength(100);
            loc.Property(l => l.StateOrRegion)
                .HasColumnName("Location_StateOrRegion")
                .HasMaxLength(100);
            loc.Property(l => l.Country)
                .HasColumnName("Location_Country")
                .HasMaxLength(2);
            loc.Property(l => l.PostalCode)
                .HasColumnName("Location_PostalCode")
                .HasMaxLength(20);
            loc.Property(l => l.Source)
                .HasColumnName("Location_Source")
                .HasConversion<int>();
        });

        // Cleared state — Feature 125
        builder.Property(t => t.IsCleared)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.ClearedDate);

        builder.Property(t => t.ReconciliationRecordId);

        // Filtered index on cleared transactions per account for reconciliation queries
        builder.HasIndex(t => new { t.AccountId, t.IsCleared })
            .HasDatabaseName("IX_Transactions_AccountId_IsCleared")
            .HasFilter("\"IsCleared\" = TRUE");

        builder.HasIndex(t => t.ReconciliationRecordId)
            .HasDatabaseName("IX_Transactions_ReconciliationRecordId")
            .HasFilter("\"ReconciliationRecordId\" IS NOT NULL");

        builder.Property(t => t.KakeiboOverride)
            .HasConversion<int?>()
            .IsRequired(false);

        builder.Property(t => t.DeletedAtUtc);

        // Query filter to exclude soft-deleted transactions
        builder.HasQueryFilter(t => t.DeletedAtUtc == null);
    }
}
