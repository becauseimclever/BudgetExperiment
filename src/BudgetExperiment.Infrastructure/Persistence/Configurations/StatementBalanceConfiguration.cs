// <copyright file="StatementBalanceConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="StatementBalance"/> entity.
/// </summary>
internal sealed class StatementBalanceConfiguration : IEntityTypeConfiguration<StatementBalance>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StatementBalance> builder)
    {
        builder.ToTable("StatementBalances");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.AccountId)
            .IsRequired();

        builder.Property(s => s.StatementDate)
            .IsRequired();

        builder.OwnsOne(s => s.Balance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Balance_Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Balance_Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(s => s.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        builder.Property(s => s.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(s => s.AccountId);

        // Filtered index: only one active statement balance per account
        builder.HasIndex(s => new { s.AccountId, s.IsCompleted })
            .HasDatabaseName("IX_StatementBalances_AccountId_IsCompleted")
            .HasFilter("\"IsCompleted\" = FALSE");
    }
}
