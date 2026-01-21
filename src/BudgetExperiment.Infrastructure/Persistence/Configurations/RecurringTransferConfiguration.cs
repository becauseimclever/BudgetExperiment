// <copyright file="RecurringTransferConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the RecurringTransfer entity.
/// </summary>
internal sealed class RecurringTransferConfiguration : IEntityTypeConfiguration<RecurringTransfer>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RecurringTransfer> builder)
    {
        builder.ToTable("RecurringTransfers");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.SourceAccountId)
            .IsRequired();

        builder.Property(r => r.DestinationAccountId)
            .IsRequired();

        builder.Property(r => r.Description)
            .IsRequired()
            .HasMaxLength(500);

        // MoneyValue as owned type
        builder.OwnsOne(r => r.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // RecurrencePattern as owned type
        builder.OwnsOne(r => r.RecurrencePattern, pattern =>
        {
            pattern.Property(p => p.Frequency)
                .HasColumnName("Frequency")
                .HasConversion<int>()
                .IsRequired();

            pattern.Property(p => p.Interval)
                .HasColumnName("Interval")
                .IsRequired()
                .HasDefaultValue(1);

            pattern.Property(p => p.DayOfMonth)
                .HasColumnName("DayOfMonth");

            pattern.Property(p => p.DayOfWeek)
                .HasColumnName("DayOfWeek")
                .HasConversion<int?>();

            pattern.Property(p => p.MonthOfYear)
                .HasColumnName("MonthOfYear");
        });

        builder.Property(r => r.StartDate)
            .IsRequired();

        builder.Property(r => r.EndDate);

        builder.Property(r => r.NextOccurrence)
            .IsRequired();

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.LastGeneratedDate);

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .IsRequired();

        // Foreign key to Source Account
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(r => r.SourceAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Destination Account
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(r => r.DestinationAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(r => r.SourceAccountId);
        builder.HasIndex(r => r.DestinationAccountId);
        builder.HasIndex(r => r.NextOccurrence);
        builder.HasIndex(r => r.IsActive);

        // Scope properties for multi-user support
        builder.Property(r => r.Scope)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.OwnerUserId);

        builder.Property(r => r.CreatedByUserId)
            .IsRequired();

        // Indexes for scope filtering
        builder.HasIndex(r => r.Scope);
        builder.HasIndex(r => r.OwnerUserId);
    }
}
