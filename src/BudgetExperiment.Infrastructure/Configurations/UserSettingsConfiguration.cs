// <copyright file="UserSettingsConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for the UserSettings entity.
/// </summary>
internal sealed class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("UserSettings");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.UserId)
            .IsRequired();

        builder.Property(u => u.DefaultScope)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.AutoRealizePastDueItems)
            .IsRequired();

        builder.Property(u => u.PastDueLookbackDays)
            .IsRequired()
            .HasDefaultValue(30);

        builder.Property(u => u.PreferredCurrency)
            .HasMaxLength(3);

        builder.Property(u => u.TimeZoneId)
            .HasMaxLength(100);

        builder.Property(u => u.CreatedAtUtc)
            .IsRequired();

        builder.Property(u => u.UpdatedAtUtc)
            .IsRequired();

        // Unique constraint: one settings record per user
        builder.HasIndex(u => u.UserId)
            .IsUnique();
    }
}
