// <copyright file="LearnedMerchantMappingConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the LearnedMerchantMapping entity.
/// </summary>
internal sealed class LearnedMerchantMappingConfiguration : IEntityTypeConfiguration<LearnedMerchantMapping>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LearnedMerchantMapping> builder)
    {
        builder.ToTable("LearnedMerchantMappings");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.MerchantPattern)
            .IsRequired()
            .HasMaxLength(LearnedMerchantMapping.MaxPatternLength);

        builder.Property(m => m.CategoryId)
            .IsRequired();

        builder.Property(m => m.OwnerId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(m => m.LearnCount)
            .IsRequired();

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();

        builder.Property(m => m.UpdatedAtUtc)
            .IsRequired();

        // Unique constraint on pattern + owner
        builder.HasIndex(m => new { m.MerchantPattern, m.OwnerId })
            .IsUnique();

        // Index for querying by owner
        builder.HasIndex(m => m.OwnerId);

        // Index for querying by category
        builder.HasIndex(m => m.CategoryId);

        // Foreign key to BudgetCategory
        builder.HasOne<BudgetCategory>()
            .WithMany()
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
