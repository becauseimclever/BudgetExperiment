// <copyright file="ImportMappingConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;
using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the ImportMapping entity.
/// </summary>
internal sealed class ImportMappingConfiguration : IEntityTypeConfiguration<ImportMapping>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ImportMapping> builder)
    {
        builder.ToTable("ImportMappings");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.UserId)
            .IsRequired();

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(ImportMapping.MaxNameLength);

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();

        builder.Property(m => m.UpdatedAtUtc)
            .IsRequired();

        builder.Property(m => m.LastUsedAtUtc);

        // Store ColumnMappings as JSON
        builder.Property(m => m.ColumnMappings)
            .HasColumnName("ColumnMappingsJson")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<ColumnMappingValue>>(v, JsonOptions) ?? new List<ColumnMappingValue>(),
                new ValueComparer<IReadOnlyList<ColumnMappingValue>>(
                    (c1, c2) => JsonSerializer.Serialize(c1, JsonOptions) == JsonSerializer.Serialize(c2, JsonOptions),
                    c => JsonSerializer.Serialize(c, JsonOptions).GetHashCode(),
                    c => JsonSerializer.Deserialize<List<ColumnMappingValue>>(JsonSerializer.Serialize(c, JsonOptions), JsonOptions) ?? new List<ColumnMappingValue>()));

        builder.Property(m => m.DateFormat)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.AmountMode)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Store DuplicateSettings as JSON
        builder.Property(m => m.DuplicateSettings)
            .HasColumnName("DuplicateSettingsJson")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<DuplicateDetectionSettingsValue>(v, JsonOptions) ?? new DuplicateDetectionSettingsValue(),
                new ValueComparer<DuplicateDetectionSettingsValue>(
                    (s1, s2) => JsonSerializer.Serialize(s1, JsonOptions) == JsonSerializer.Serialize(s2, JsonOptions),
                    s => JsonSerializer.Serialize(s, JsonOptions).GetHashCode(),
                    s => JsonSerializer.Deserialize<DuplicateDetectionSettingsValue>(JsonSerializer.Serialize(s, JsonOptions), JsonOptions) ?? new DuplicateDetectionSettingsValue()));

        // Store SkipRowsSettingsValue as JSON
        builder.Property(m => m.SkipRowsSettingsValue)
            .HasColumnName("SkipRowsSettingsJson")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<SkipRowsSettingsValue>(v, JsonOptions) ?? SkipRowsSettingsValue.Default,
                new ValueComparer<SkipRowsSettingsValue>(
                    (s1, s2) => JsonSerializer.Serialize(s1, JsonOptions) == JsonSerializer.Serialize(s2, JsonOptions),
                    s => JsonSerializer.Serialize(s, JsonOptions).GetHashCode(),
                    s => JsonSerializer.Deserialize<SkipRowsSettingsValue>(JsonSerializer.Serialize(s, JsonOptions), JsonOptions) ?? SkipRowsSettingsValue.Default));

        // Store IndicatorSettings as JSON
        builder.Property(m => m.IndicatorSettings)
            .HasColumnName("IndicatorSettingsJson")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<DebitCreditIndicatorSettingsValue>(v, JsonOptions) ?? DebitCreditIndicatorSettingsValue.Disabled,
                new ValueComparer<DebitCreditIndicatorSettingsValue>(
                    (s1, s2) => JsonSerializer.Serialize(s1, JsonOptions) == JsonSerializer.Serialize(s2, JsonOptions),
                    s => JsonSerializer.Serialize(s, JsonOptions).GetHashCode(),
                    s => JsonSerializer.Deserialize<DebitCreditIndicatorSettingsValue>(JsonSerializer.Serialize(s, JsonOptions), JsonOptions) ?? DebitCreditIndicatorSettingsValue.Disabled));

        // Indexes
        builder.HasIndex(m => m.UserId)
            .HasDatabaseName("IX_ImportMappings_UserId");

        builder.HasIndex(m => new { m.UserId, m.Name })
            .HasDatabaseName("IX_ImportMappings_UserId_Name");

        // Optimistic concurrency token (PostgreSQL xmin)
        builder.Property<uint>("xmin")
            .IsConcurrencyToken();
    }
}
