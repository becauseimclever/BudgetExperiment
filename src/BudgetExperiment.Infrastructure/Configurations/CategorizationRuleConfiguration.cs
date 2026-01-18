// <copyright file="CategorizationRuleConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for the CategorizationRule entity.
/// </summary>
internal sealed class CategorizationRuleConfiguration : IEntityTypeConfiguration<CategorizationRule>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CategorizationRule> builder)
    {
        builder.ToTable("CategorizationRules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(CategorizationRule.MaxNameLength);

        builder.Property(r => r.MatchType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.Pattern)
            .IsRequired()
            .HasMaxLength(CategorizationRule.MaxPatternLength);

        builder.Property(r => r.CategoryId)
            .IsRequired();

        builder.Property(r => r.Priority)
            .IsRequired();

        builder.Property(r => r.IsActive)
            .IsRequired();

        builder.Property(r => r.CaseSensitive)
            .IsRequired();

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .IsRequired();

        // Foreign key to BudgetCategory
        builder.HasOne(r => r.Category)
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for efficient querying
        builder.HasIndex(r => new { r.IsActive, r.Priority })
            .HasDatabaseName("IX_CategorizationRules_Priority");

        builder.HasIndex(r => r.CategoryId)
            .HasDatabaseName("IX_CategorizationRules_CategoryId");

        builder.HasIndex(r => r.Name);
    }
}
