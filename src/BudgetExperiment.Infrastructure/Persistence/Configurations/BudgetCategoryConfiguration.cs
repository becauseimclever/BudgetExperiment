// <copyright file="BudgetCategoryConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the BudgetCategory entity.
/// </summary>
internal sealed class BudgetCategoryConfiguration : IEntityTypeConfiguration<BudgetCategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BudgetCategory> builder)
    {
        builder.ToTable("BudgetCategories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(BudgetCategory.MaxNameLength);

        builder.Property(c => c.Icon)
            .HasMaxLength(50);

        builder.Property(c => c.Color)
            .HasMaxLength(10);

        builder.Property(c => c.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.SortOrder)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc)
            .IsRequired();

        // Navigation to Goals
        builder.HasMany(c => c.Goals)
            .WithOne(g => g.Category)
            .HasForeignKey(g => g.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.Type);
        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => c.SortOrder);

        // Scope properties for multi-user support
        builder.Property(c => c.Scope)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.OwnerUserId);

        builder.Property(c => c.CreatedByUserId)
            .IsRequired();

        // Indexes for scope filtering
        builder.HasIndex(c => c.Scope);
        builder.HasIndex(c => c.OwnerUserId);

        // Unique constraint on name within scope
        builder.HasIndex(c => new { c.Name, c.Scope, c.OwnerUserId }).IsUnique();
    }
}
