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

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        // Navigation to Transactions
        builder.HasMany(a => a.Transactions)
            .WithOne()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for efficient listing
        builder.HasIndex(a => a.Name);
        builder.HasIndex(a => a.Type);
    }
}
