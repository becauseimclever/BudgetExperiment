// <copyright file="RecurringChargeSuggestionConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the RecurringChargeSuggestion entity.
/// </summary>
internal sealed class RecurringChargeSuggestionConfiguration : IEntityTypeConfiguration<RecurringChargeSuggestion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RecurringChargeSuggestion> builder)
    {
        builder.ToTable("RecurringChargeSuggestions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.AccountId)
            .IsRequired();

        builder.Property(s => s.NormalizedDescription)
            .IsRequired()
            .HasMaxLength(RecurringChargeSuggestion.MaxDescriptionLength);

        builder.Property(s => s.SampleDescription)
            .IsRequired()
            .HasMaxLength(RecurringChargeSuggestion.MaxDescriptionLength);

        builder.OwnsOne(s => s.AverageAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("AverageAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("AverageAmountCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(s => s.DetectedFrequency)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.DetectedInterval)
            .IsRequired();

        builder.Property(s => s.Confidence)
            .IsRequired()
            .HasPrecision(3, 2);

        builder.Property(s => s.MatchingTransactionCount)
            .IsRequired();

        builder.Property(s => s.FirstOccurrence)
            .IsRequired();

        builder.Property(s => s.LastOccurrence)
            .IsRequired();

        builder.Property(s => s.CategoryId);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.AcceptedRecurringTransactionId);

        builder.Property(s => s.Scope)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.OwnerUserId);

        builder.Property(s => s.CreatedByUserId)
            .IsRequired();

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        builder.Property(s => s.UpdatedAtUtc)
            .IsRequired();

        // Optimistic concurrency (PostgreSQL xmin)
        builder.Property<uint>("xmin")
            .IsConcurrencyToken();

        // Indexes
        builder.HasIndex(s => new { s.AccountId, s.Status });
        builder.HasIndex(s => new { s.NormalizedDescription, s.AccountId });
    }
}
