// <copyright file="RuleSuggestionConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for the RuleSuggestion entity.
/// </summary>
internal sealed class RuleSuggestionConfiguration : IEntityTypeConfiguration<RuleSuggestion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RuleSuggestion> builder)
    {
        builder.ToTable("RuleSuggestions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(RuleSuggestion.MaxTitleLength);

        builder.Property(s => s.Description)
            .IsRequired();

        builder.Property(s => s.Reasoning)
            .IsRequired();

        builder.Property(s => s.Confidence)
            .IsRequired()
            .HasPrecision(3, 2);

        builder.Property(s => s.SuggestedPattern)
            .HasMaxLength(RuleSuggestion.MaxPatternLength);

        builder.Property(s => s.SuggestedMatchType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.SuggestedCategoryId);

        builder.Property(s => s.TargetRuleId);

        builder.Property(s => s.OptimizedPattern)
            .HasMaxLength(RuleSuggestion.MaxPatternLength);

        // Store list of Guids as JSON with value comparer
        var guidListComparer = new ValueComparer<IReadOnlyList<Guid>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property(s => s.ConflictingRuleIds)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Guid>())
            .Metadata.SetValueComparer(guidListComparer);

        builder.Property(s => s.AffectedTransactionCount)
            .IsRequired();

        // Store list of strings as JSON with value comparer
        var stringListComparer = new ValueComparer<IReadOnlyList<string>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property(s => s.SampleDescriptions)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(stringListComparer);

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        builder.Property(s => s.ReviewedAtUtc);

        builder.Property(s => s.DismissalReason)
            .HasMaxLength(RuleSuggestion.MaxDismissalReasonLength);

        builder.Property(s => s.UserFeedbackPositive);

        // Indexes for efficient querying
        builder.HasIndex(s => new { s.Status, s.CreatedAtUtc })
            .HasDatabaseName("IX_RuleSuggestions_Status");

        builder.HasIndex(s => new { s.Type, s.Status })
            .HasDatabaseName("IX_RuleSuggestions_Type");

        builder.HasIndex(s => s.SuggestedPattern)
            .HasDatabaseName("IX_RuleSuggestions_Pattern");
    }
}
