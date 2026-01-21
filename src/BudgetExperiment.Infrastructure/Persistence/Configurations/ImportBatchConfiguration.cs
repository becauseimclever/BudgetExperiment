// <copyright file="ImportBatchConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the ImportBatch entity.
/// </summary>
internal sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("ImportBatches");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedNever();

        builder.Property(b => b.UserId)
            .IsRequired();

        builder.Property(b => b.AccountId)
            .IsRequired();

        builder.Property(b => b.MappingId);

        builder.Property(b => b.FileName)
            .IsRequired()
            .HasMaxLength(ImportBatch.MaxFileNameLength);

        builder.Property(b => b.TotalRows)
            .IsRequired();

        builder.Property(b => b.ImportedCount)
            .IsRequired();

        builder.Property(b => b.SkippedCount)
            .IsRequired();

        builder.Property(b => b.ErrorCount)
            .IsRequired();

        builder.Property(b => b.ImportedAtUtc)
            .IsRequired();

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Foreign keys
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(b => b.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ImportMapping>()
            .WithMany()
            .HasForeignKey(b => b.MappingId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(b => b.UserId)
            .HasDatabaseName("IX_ImportBatches_UserId");

        builder.HasIndex(b => b.AccountId)
            .HasDatabaseName("IX_ImportBatches_AccountId");

        builder.HasIndex(b => b.MappingId)
            .HasDatabaseName("IX_ImportBatches_MappingId");

        builder.HasIndex(b => b.ImportedAtUtc)
            .HasDatabaseName("IX_ImportBatches_ImportedAtUtc");
    }
}
