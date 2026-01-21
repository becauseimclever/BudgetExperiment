// <copyright file="ChatMessageConfiguration.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;
using BudgetExperiment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetExperiment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the ChatMessage entity.
/// </summary>
internal sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        Converters = { new RecurrencePatternJsonConverter() },
    };

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.SessionId)
            .IsRequired();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.Content)
            .IsRequired();

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();

        builder.Property(m => m.ActionStatus)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.CreatedEntityId);

        builder.Property(m => m.ErrorMessage)
            .HasMaxLength(500);

        // Store ChatAction as JSON in a jsonb column
        builder.Property(m => m.Action)
            .HasColumnType("jsonb")
            .HasConversion(
                v => SerializeAction(v),
                v => DeserializeAction(v));

        // Index for finding messages by session
        builder.HasIndex(m => new { m.SessionId, m.CreatedAtUtc });

        // Index for finding pending actions
        builder.HasIndex(m => m.ActionStatus)
            .HasFilter("\"ActionStatus\" = 1"); // Pending = 1
    }

    private static string? SerializeAction(ChatAction? action)
    {
        if (action == null)
        {
            return null;
        }

        // Serialize to a JSON object, then add the type discriminator
        var jsonNode = action switch
        {
            CreateTransactionAction a => JsonSerializer.SerializeToNode(a, JsonOptions),
            CreateTransferAction a => JsonSerializer.SerializeToNode(a, JsonOptions),
            CreateRecurringTransactionAction a => JsonSerializer.SerializeToNode(a, JsonOptions),
            CreateRecurringTransferAction a => JsonSerializer.SerializeToNode(a, JsonOptions),
            ClarificationNeededAction a => JsonSerializer.SerializeToNode(a, JsonOptions),
            _ => null,
        };

        if (jsonNode is JsonObject obj)
        {
            obj["$type"] = action.Type.ToString();
        }

        return jsonNode?.ToJsonString(JsonOptions);
    }

    private static ChatAction? DeserializeAction(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        // First, deserialize just to get the type
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("$type", out var typeElement))
        {
            return null;
        }

        var typeString = typeElement.GetString();
        if (!Enum.TryParse<ChatActionType>(typeString, out var actionType))
        {
            return null;
        }

        // Deserialize with the concrete type
        return actionType switch
        {
            ChatActionType.CreateTransaction => JsonSerializer.Deserialize<CreateTransactionAction>(json, JsonOptions),
            ChatActionType.CreateTransfer => JsonSerializer.Deserialize<CreateTransferAction>(json, JsonOptions),
            ChatActionType.CreateRecurringTransaction => JsonSerializer.Deserialize<CreateRecurringTransactionAction>(json, JsonOptions),
            ChatActionType.CreateRecurringTransfer => JsonSerializer.Deserialize<CreateRecurringTransferAction>(json, JsonOptions),
            ChatActionType.ClarificationNeeded => JsonSerializer.Deserialize<ClarificationNeededAction>(json, JsonOptions),
            _ => null,
        };
    }
}
