// <copyright file="BudgetGoalConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for the BudgetGoal entity.
/// </summary>
internal sealed class BudgetGoalConfiguration : IEntityTypeConfiguration<BudgetGoal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BudgetGoal> builder)
    {
        builder.ToTable("BudgetGoals");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .ValueGeneratedNever();

        builder.Property(g => g.CategoryId)
            .IsRequired();

        builder.Property(g => g.Year)
            .IsRequired();

        builder.Property(g => g.Month)
            .IsRequired();

        // Target amount as owned type
        builder.OwnsOne(g => g.TargetAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("TargetAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("TargetCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(g => g.CreatedAtUtc)
            .IsRequired();

        builder.Property(g => g.UpdatedAtUtc)
            .IsRequired();

        // Unique constraint: one goal per category per month
        builder.HasIndex(g => new { g.CategoryId, g.Year, g.Month })
            .IsUnique();

        // Index for month queries
        builder.HasIndex(g => new { g.Year, g.Month });

        // Scope properties for multi-user support
        builder.Property(g => g.Scope)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(g => g.OwnerUserId);

        builder.Property(g => g.CreatedByUserId)
            .IsRequired();

        // Indexes for scope filtering
        builder.HasIndex(g => g.Scope);
        builder.HasIndex(g => g.OwnerUserId);
    }
}
