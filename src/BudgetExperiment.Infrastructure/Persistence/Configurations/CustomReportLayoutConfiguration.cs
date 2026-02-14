// <copyright file="CustomReportLayoutConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for custom report layouts.
/// </summary>
internal sealed class CustomReportLayoutConfiguration : IEntityTypeConfiguration<CustomReportLayout>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CustomReportLayout> builder)
    {
        builder.ToTable("CustomReportLayouts");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .ValueGeneratedNever();

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(CustomReportLayout.MaxNameLength);

        builder.Property(l => l.LayoutJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(l => l.CreatedAtUtc)
            .IsRequired();

        builder.Property(l => l.UpdatedAtUtc)
            .IsRequired();

        builder.Property(l => l.Scope)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(l => l.OwnerUserId);

        builder.Property(l => l.CreatedByUserId)
            .IsRequired();

        builder.HasIndex(l => l.Scope);
        builder.HasIndex(l => l.OwnerUserId);
        builder.HasIndex(l => l.CreatedByUserId);
    }
}
