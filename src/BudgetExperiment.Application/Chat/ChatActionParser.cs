// <copyright file="ChatActionParser.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Chat;

/// <summary>
/// Parses AI response JSON into <see cref="ChatAction"/> objects.
/// Chain-of-responsibility coordinator: extracts JSON, routes by intent, and
/// delegates to the appropriate per-action-type parser.
/// </summary>
public static class ChatActionParser
{
    /// <summary>
    /// Parses a raw AI response string into a <see cref="ParseResult"/>.
    /// Extracts JSON, determines intent, and constructs the appropriate action.
    /// </summary>
    /// <param name="content">The raw AI response text.</param>
    /// <param name="accounts">Available accounts for name-to-ID resolution.</param>
    /// <param name="categories">Available categories for name-to-ID resolution.</param>
    /// <param name="context">Optional chat context with defaults.</param>
    /// <returns>The parsed result.</returns>
    public static ParseResult ParseResponse(
        string content,
        IReadOnlyList<AccountInfo> accounts,
        IReadOnlyList<CategoryInfo> categories,
        ChatContext? context)
    {
        try
        {
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');

            if (jsonStart < 0 || jsonEnd < 0)
            {
                return new ParseResult(
                    Success: false,
                    Action: null,
                    ResponseText: "I couldn't understand that. Could you rephrase?",
                    ErrorMessage: "No JSON found in AI response");
            }

            var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var intent = root.GetProperty("intent").GetString() ?? "unknown";
            var confidence = root.TryGetProperty("confidence", out var confProp) ? confProp.GetDecimal() : 0.5m;
            var responseText = root.TryGetProperty("response", out var respProp)
                ? respProp.GetString() ?? "I processed your request."
                : "I processed your request.";

            if (root.TryGetProperty("clarification", out var clarProp) &&
                clarProp.TryGetProperty("needed", out var neededProp) &&
                neededProp.GetBoolean())
            {
                var clarificationAction = ClarificationActionParser.Parse(clarProp);
                return new ParseResult(
                    Success: true,
                    Action: clarificationAction,
                    ResponseText: responseText,
                    Confidence: confidence);
            }

            var data = root.TryGetProperty("data", out var dataProp) ? dataProp : default;
            ChatAction? action = intent switch
            {
                "transaction" => TransactionActionParser.Parse(data, accounts, categories, context),
                "transfer" => TransferActionParser.Parse(data, accounts, context),
                "recurring_transaction" => RecurringTransactionActionParser.Parse(data, accounts, categories),
                "recurring_transfer" => RecurringTransferActionParser.Parse(data, accounts),
                "unknown" => null,
                _ => null,
            };

            if (action == null && intent != "unknown")
            {
                return new ParseResult(
                    Success: false,
                    Action: null,
                    ResponseText: responseText,
                    ErrorMessage: $"Failed to parse {intent} action");
            }

            return new ParseResult(
                Success: action != null,
                Action: action,
                ResponseText: responseText,
                Confidence: confidence);
        }
        catch (JsonException ex)
        {
            return new ParseResult(
                Success: false,
                Action: null,
                ResponseText: "I had trouble understanding that. Could you try again?",
                ErrorMessage: $"JSON parse error: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses a clarification element. Preserved for call-sites; delegates to
    /// <see cref="ClarificationActionParser.Parse"/>.
    /// </summary>
    /// <param name="clarProp">The clarification JSON element.</param>
    /// <returns>The clarification action.</returns>
    internal static ClarificationNeededAction ParseClarification(JsonElement clarProp) =>
        ClarificationActionParser.Parse(clarProp);

    /// <summary>
    /// Parses a transaction action. Preserved for call-sites; delegates to
    /// <see cref="TransactionActionParser.Parse"/>.
    /// </summary>
    internal static CreateTransactionAction? ParseTransactionAction(
        JsonElement data,
        IReadOnlyList<AccountInfo> accounts,
        IReadOnlyList<CategoryInfo> categories,
        ChatContext? context) =>
        TransactionActionParser.Parse(data, accounts, categories, context);

    /// <summary>
    /// Parses a transfer action. Preserved for call-sites; delegates to
    /// <see cref="TransferActionParser.Parse"/>.
    /// </summary>
    internal static CreateTransferAction? ParseTransferAction(
        JsonElement data,
        IReadOnlyList<AccountInfo> accounts,
        ChatContext? context) =>
        TransferActionParser.Parse(data, accounts, context);

    /// <summary>
    /// Parses a recurring transaction action. Preserved for call-sites; delegates to
    /// <see cref="RecurringTransactionActionParser.Parse"/>.
    /// </summary>
    internal static CreateRecurringTransactionAction? ParseRecurringTransactionAction(
        JsonElement data,
        IReadOnlyList<AccountInfo> accounts,
        IReadOnlyList<CategoryInfo> categories) =>
        RecurringTransactionActionParser.Parse(data, accounts, categories);

    /// <summary>
    /// Parses a recurring transfer action. Preserved for call-sites; delegates to
    /// <see cref="RecurringTransferActionParser.Parse"/>.
    /// </summary>
    internal static CreateRecurringTransferAction? ParseRecurringTransferAction(
        JsonElement data,
        IReadOnlyList<AccountInfo> accounts) =>
        RecurringTransferActionParser.Parse(data, accounts);

    /// <summary>
    /// Parses a recurrence pattern. Preserved for call-sites; delegates to
    /// <see cref="ChatParserHelpers.ParseRecurrencePattern"/>.
    /// </summary>
    internal static RecurrencePatternValue? ParseRecurrencePattern(JsonElement data) =>
        ChatParserHelpers.ParseRecurrencePattern(data);

    /// <summary>
    /// Parses a GUID property. Preserved for call-sites; delegates to
    /// <see cref="ChatParserHelpers.ParseGuid"/>.
    /// </summary>
    internal static Guid? ParseGuid(JsonElement element, string propertyName) =>
        ChatParserHelpers.ParseGuid(element, propertyName);

    /// <summary>
    /// Parses a date property. Preserved for call-sites; delegates to
    /// <see cref="ChatParserHelpers.ParseDate"/>.
    /// </summary>
    internal static DateOnly? ParseDate(JsonElement element, string propertyName) =>
        ChatParserHelpers.ParseDate(element, propertyName);
}
