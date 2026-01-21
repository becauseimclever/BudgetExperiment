// <copyright file="DismissedSuggestionPatternConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the DismissedSuggestionPattern entity.
/// </summary>
internal sealed class DismissedSuggestionPatternConfiguration : IEntityTypeConfiguration<DismissedSuggestionPattern>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DismissedSuggestionPattern> builder)
    {
        builder.ToTable("DismissedSuggestionPatterns");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.Pattern)
            .IsRequired()
            .HasMaxLength(DismissedSuggestionPattern.MaxPatternLength);

        builder.Property(p => p.OwnerId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.DismissedAtUtc)
            .IsRequired();

        // Unique constraint on pattern + owner
        builder.HasIndex(p => new { p.Pattern, p.OwnerId })
            .IsUnique();

        // Index for querying by owner
        builder.HasIndex(p => p.OwnerId);
    }
}
