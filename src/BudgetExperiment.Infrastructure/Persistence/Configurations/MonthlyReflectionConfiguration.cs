// <copyright file="MonthlyReflectionConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="MonthlyReflection"/> entity.
/// </summary>
internal sealed class MonthlyReflectionConfiguration : IEntityTypeConfiguration<MonthlyReflection>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MonthlyReflection> builder)
    {
        builder.ToTable("MonthlyReflections");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.Year)
            .IsRequired();

        builder.Property(r => r.Month)
            .IsRequired();

        builder.Property(r => r.SavingsGoal)
            .HasPrecision(19, 2)
            .IsRequired();

        builder.Property(r => r.ActualSavings)
            .HasPrecision(19, 2)
            .IsRequired(false);

        builder.Property(r => r.IntentionText)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(r => r.GratitudeText)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(r => r.ImprovementText)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .IsRequired();

        // Composite unique index: one reflection per user per month
        builder.HasIndex(r => new { r.UserId, r.Year, r.Month })
            .IsUnique();
    }
}
