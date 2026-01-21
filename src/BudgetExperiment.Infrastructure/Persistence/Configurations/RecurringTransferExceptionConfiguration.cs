// <copyright file="RecurringTransferExceptionConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the RecurringTransferException entity.
/// </summary>
internal sealed class RecurringTransferExceptionConfiguration : IEntityTypeConfiguration<RecurringTransferException>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RecurringTransferException> builder)
    {
        builder.ToTable("RecurringTransferExceptions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.RecurringTransferId)
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

        // Foreign key to RecurringTransfer
        builder.HasOne<RecurringTransfer>()
            .WithMany()
            .HasForeignKey(e => e.RecurringTransferId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one exception per (RecurringTransferId, OriginalDate)
        builder.HasIndex(e => new { e.RecurringTransferId, e.OriginalDate })
            .IsUnique();

        // Index for date range queries
        builder.HasIndex(e => e.RecurringTransferId);
    }
}
