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

        builder.Property(t => t.Category)
            .HasMaxLength(100);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired();

        // Indexes for calendar queries
        builder.HasIndex(t => t.Date);
        builder.HasIndex(t => t.AccountId);
        builder.HasIndex(t => new { t.AccountId, t.Date });
    }
}
