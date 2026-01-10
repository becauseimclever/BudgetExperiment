// <copyright file="RecurringTransactionExceptionConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for the RecurringTransactionException entity.
/// </summary>
internal sealed class RecurringTransactionExceptionConfiguration : IEntityTypeConfiguration<RecurringTransactionException>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RecurringTransactionException> builder)
    {
        builder.ToTable("RecurringTransactionExceptions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.RecurringTransactionId)
            .IsRequired();

        builder.Property(e => e.OriginalDate)
            .IsRequired();

        builder.Property(e => e.ExceptionType)
            .IsRequired()
            .HasConversion<int>();

        // MoneyValue as owned type (nullable)
        builder.OwnsOne(e => e.ModifiedAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("ModifiedAmount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("ModifiedCurrency")
                .HasMaxLength(3);
        });

        builder.Property(e => e.ModifiedDescription)
            .HasMaxLength(500);

        builder.Property(e => e.ModifiedDate);

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.Property(e => e.UpdatedAtUtc)
            .IsRequired();

        // Foreign key to RecurringTransaction
        builder.HasOne<RecurringTransaction>()
            .WithMany()
            .HasForeignKey(e => e.RecurringTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one exception per (RecurringTransactionId, OriginalDate)
        builder.HasIndex(e => new { e.RecurringTransactionId, e.OriginalDate })
            .IsUnique();

        // Index for date range queries
        builder.HasIndex(e => e.RecurringTransactionId);
    }
}
