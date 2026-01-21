// <copyright file="ChatMapper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Mappers for chat-related domain entities to DTOs.
/// </summary>
public static class ChatMapper
{
    /// <summary>
    /// Maps a <see cref="ChatSession"/> to a <see cref="ChatSessionDto"/>.
    /// </summary>
    /// <param name="session">The chat session entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static ChatSessionDto ToDto(ChatSession session)
    {
        return new ChatSessionDto
        {
            Id = session.Id,
            IsActive = session.IsActive,
            CreatedAtUtc = session.CreatedAtUtc,
            LastMessageAtUtc = session.LastMessageAtUtc,
            Messages = session.Messages.Select(ToDto).ToList(),
        };
    }

    /// <summary>
    /// Maps a <see cref="ChatMessage"/> to a <see cref="ChatMessageDto"/>.
    /// </summary>
    /// <param name="message">The chat message entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static ChatMessageDto ToDto(ChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            SessionId = message.SessionId,
            Role = message.Role,
            Content = message.Content,
            CreatedAtUtc = message.CreatedAtUtc,
            Action = message.Action != null ? ToDto(message.Action) : null,
            ActionStatus = message.ActionStatus,
        };
    }

    /// <summary>
    /// Maps a <see cref="ChatAction"/> to a <see cref="ChatActionDto"/>.
    /// </summary>
    /// <param name="action">The chat action entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static ChatActionDto ToDto(ChatAction action)
    {
        var dto = new ChatActionDto
        {
            Type = action.Type,
            PreviewSummary = action.GetPreviewSummary(),
        };

        switch (action)
        {
            case CreateTransactionAction txn:
                dto.AccountId = txn.AccountId;
                dto.AccountName = txn.AccountName;
                dto.Amount = txn.Amount;
                dto.Date = txn.Date;
                dto.Description = txn.Description;
                dto.CategoryId = txn.CategoryId;
                dto.CategoryName = txn.Category;
                break;

            case CreateTransferAction transfer:
                dto.FromAccountId = transfer.FromAccountId;
                dto.FromAccountName = transfer.FromAccountName;
                dto.ToAccountId = transfer.ToAccountId;
                dto.ToAccountName = transfer.ToAccountName;
                dto.Amount = transfer.Amount;
                dto.Date = transfer.Date;
                dto.Description = transfer.Description;
                break;

            case CreateRecurringTransactionAction recTxn:
                dto.AccountId = recTxn.AccountId;
                dto.AccountName = recTxn.AccountName;
                dto.Amount = recTxn.Amount;
                dto.Date = recTxn.StartDate;
                dto.Description = recTxn.Description;
                dto.CategoryName = recTxn.Category;
                break;

            case CreateRecurringTransferAction recTransfer:
                dto.FromAccountId = recTransfer.FromAccountId;
                dto.FromAccountName = recTransfer.FromAccountName;
                dto.ToAccountId = recTransfer.ToAccountId;
                dto.ToAccountName = recTransfer.ToAccountName;
                dto.Amount = recTransfer.Amount;
                dto.Date = recTransfer.StartDate;
                dto.Description = recTransfer.Description;
                break;

            case ClarificationNeededAction clarification:
                dto.ClarificationQuestion = clarification.Question;
                dto.ClarificationFieldName = clarification.FieldName;
                dto.Options = clarification.Options.Select(o => new ClarificationOptionDto
                {
                    Label = o.Label,
                    Value = o.Value,
                    EntityId = o.EntityId,
                }).ToList();
                break;
        }

        return dto;
    }
}
