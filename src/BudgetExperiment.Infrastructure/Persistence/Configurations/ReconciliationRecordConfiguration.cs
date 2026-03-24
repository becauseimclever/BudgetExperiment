// <copyright file="ReconciliationRecordConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="ReconciliationRecord"/> entity.
/// </summary>
internal sealed class ReconciliationRecordConfiguration : IEntityTypeConfiguration<ReconciliationRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ReconciliationRecord> builder)
    {
        builder.ToTable("ReconciliationRecords");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.AccountId)
            .IsRequired();

        builder.Property(r => r.StatementDate)
            .IsRequired();

        builder.OwnsOne(r => r.StatementBalance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("StatementBalance_Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("StatementBalance_Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(r => r.ClearedBalance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("ClearedBalance_Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("ClearedBalance_Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(r => r.TransactionCount)
            .IsRequired();

        builder.Property(r => r.CompletedAtUtc)
            .IsRequired();

        builder.Property(r => r.CompletedByUserId)
            .IsRequired();

        builder.Property(r => r.Scope)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.OwnerUserId);

        builder.HasIndex(r => r.AccountId);
        builder.HasIndex(r => new { r.AccountId, r.StatementDate });
        builder.HasIndex(r => new { r.Scope, r.OwnerUserId });
    }
}
