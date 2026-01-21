// <copyright file="CategorySuggestionConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the CategorySuggestion entity.
/// </summary>
internal sealed class CategorySuggestionConfiguration : IEntityTypeConfiguration<CategorySuggestion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CategorySuggestion> builder)
    {
        builder.ToTable("CategorySuggestions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.SuggestedName)
            .IsRequired()
            .HasMaxLength(CategorySuggestion.MaxNameLength);

        builder.Property(s => s.SuggestedIcon)
            .HasMaxLength(50);

        builder.Property(s => s.SuggestedColor)
            .HasMaxLength(10);

        builder.Property(s => s.SuggestedType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.Confidence)
            .IsRequired()
            .HasPrecision(3, 2);

        // Store list of strings as JSON with value comparer
        var stringListComparer = new ValueComparer<IReadOnlyList<string>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property(s => s.MerchantPatterns)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(stringListComparer);

        builder.Property(s => s.MatchingTransactionCount)
            .IsRequired();

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.OwnerId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        // Index for querying by owner and status
        builder.HasIndex(s => new { s.OwnerId, s.Status });

        // Index for checking duplicate names
        builder.HasIndex(s => new { s.OwnerId, s.SuggestedName, s.Status });
    }
}
