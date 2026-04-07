// <copyright file="KaizenGoalConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Kaizen;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="KaizenGoal"/> entity.
/// </summary>
internal sealed class KaizenGoalConfiguration : IEntityTypeConfiguration<KaizenGoal>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<KaizenGoal> builder)
    {
        builder.ToTable("KaizenGoals");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .ValueGeneratedNever();

        builder.Property(g => g.UserId)
            .IsRequired();

        builder.Property(g => g.WeekStartDate)
            .IsRequired();

        builder.Property(g => g.Description)
            .IsRequired()
            .HasMaxLength(KaizenGoal.MaxDescriptionLength);

        builder.Property(g => g.TargetAmount)
            .HasPrecision(19, 2)
            .IsRequired(false);

        builder.Property(g => g.KakeiboCategory)
            .HasConversion<int?>()
            .IsRequired(false);

        builder.Property(g => g.IsAchieved)
            .IsRequired();

        builder.Property(g => g.CreatedAtUtc)
            .IsRequired();

        builder.Property(g => g.UpdatedAtUtc)
            .IsRequired();

        // Composite unique constraint: one goal per user per week
        builder.HasIndex(g => new { g.UserId, g.WeekStartDate })
            .IsUnique();

        builder.HasIndex(g => g.UserId);
    }
}
