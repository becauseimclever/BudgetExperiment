// <copyright file="TransactionConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Configurations;

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

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
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

        // Indexes for calendar queries
        builder.HasIndex(t => t.Date);
        builder.HasIndex(t => t.AccountId);
        builder.HasIndex(t => new { t.AccountId, t.Date });
        builder.HasIndex(t => t.RecurringTransactionId);
        builder.HasIndex(t => t.TransferId);
        builder.HasIndex(t => t.RecurringTransferId);
        builder.HasIndex(t => t.CategoryId);

        // Scope properties for multi-user support
        builder.Property(t => t.Scope)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.OwnerUserId);

        builder.Property(t => t.CreatedByUserId)
            .IsRequired();

        // Indexes for scope filtering
        builder.HasIndex(t => t.Scope);
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
    }
}
