// <copyright file="AccountConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for the Account entity.
/// </summary>
internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // Initial balance as owned type
        builder.OwnsOne(a => a.InitialBalance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("InitialBalance")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("InitialBalanceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(a => a.InitialBalanceDate)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        // Scope properties for multi-user support
        builder.Property(a => a.Scope)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.OwnerUserId);

        builder.Property(a => a.CreatedByUserId)
            .IsRequired();

        // Navigation to Transactions
        builder.HasMany(a => a.Transactions)
            .WithOne()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for efficient listing
        builder.HasIndex(a => a.Name);
        builder.HasIndex(a => a.Type);

        // Indexes for scope filtering
        builder.HasIndex(a => a.Scope);
        builder.HasIndex(a => a.OwnerUserId);
    }
}
