// <copyright file="ReconciliationMatchConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for the ReconciliationMatch entity.
/// </summary>
internal sealed class ReconciliationMatchConfiguration : IEntityTypeConfiguration<ReconciliationMatch>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ReconciliationMatch> builder)
    {
        builder.ToTable("ReconciliationMatches");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.ImportedTransactionId)
            .IsRequired();

        builder.Property(m => m.RecurringTransactionId)
            .IsRequired();

        builder.Property(m => m.RecurringInstanceDate)
            .IsRequired();

        builder.Property(m => m.ConfidenceScore)
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(m => m.ConfidenceLevel)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.AmountVariance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(m => m.DateOffsetDays)
            .IsRequired();

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();

        builder.Property(m => m.ResolvedAtUtc);

        builder.Property(m => m.Scope)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.OwnerUserId);

        // Foreign keys
        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(m => m.ImportedTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RecurringTransaction>()
            .WithMany()
            .HasForeignKey(m => m.RecurringTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for efficient queries
        builder.HasIndex(m => m.ImportedTransactionId);

        builder.HasIndex(m => new { m.RecurringTransactionId, m.RecurringInstanceDate });

        builder.HasIndex(m => m.Status);

        builder.HasIndex(m => new { m.Scope, m.OwnerUserId });

        // Unique constraint: one match suggestion per transaction + recurring instance combination
        builder.HasIndex(m => new { m.ImportedTransactionId, m.RecurringTransactionId, m.RecurringInstanceDate })
            .IsUnique();
    }
}
