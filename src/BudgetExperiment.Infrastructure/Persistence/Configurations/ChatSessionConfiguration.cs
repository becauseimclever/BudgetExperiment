// <copyright file="ChatSessionConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the ChatSession entity.
/// </summary>
internal sealed class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("ChatSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        builder.Property(s => s.LastMessageAtUtc)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired();

        // Configure one-to-many relationship with ChatMessages
        builder.HasMany(s => s.Messages)
            .WithOne()
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for finding active sessions
        builder.HasIndex(s => s.IsActive)
            .HasFilter("\"IsActive\" = true");

        // Index for ordering by last message
        builder.HasIndex(s => s.LastMessageAtUtc);
    }
}
