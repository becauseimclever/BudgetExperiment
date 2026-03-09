// <copyright file="TransactionDescriptionCleaner.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Cleans and normalizes raw bank transaction descriptions for AI prompt consumption.
/// Strips common bank prefixes, trailing noise (card numbers, dates, reference codes),
/// and normalizes whitespace. Original descriptions are preserved in the domain —
/// only cleaned versions are used in prompts.
/// </summary>
public static partial class TransactionDescriptionCleaner
{
    /// <summary>
    /// Cleans a raw bank transaction description by stripping noise and normalizing.
    /// </summary>
    /// <param name="rawDescription">The raw description from the bank.</param>
    /// <returns>A cleaned, uppercase, trimmed description.</returns>
    public static string Clean(string rawDescription)
    {
        if (string.IsNullOrWhiteSpace(rawDescription))
        {
            return string.Empty;
        }

        var cleaned = rawDescription.Trim();

        // Decode HTML entities (e.g., &amp; → &)
        cleaned = cleaned.Replace("&amp;", "&");

        // Apply bank-specific cleaning patterns
        cleaned = CleanUhcuDescription(cleaned);
        cleaned = CleanCapitalOneDescription(cleaned);
        cleaned = CleanBoaDescription(cleaned);

        // Strip vendor order codes after asterisk (e.g., "AMAZON MKTPL*AB3CD5EF0" → "AMAZON MKTPL")
        cleaned = StripAsteriskCodes(cleaned);

        // Normalize whitespace and uppercase
        cleaned = NormalizeWhitespace(cleaned);
        cleaned = cleaned.ToUpperInvariant();

        return cleaned;
    }

    private static string CleanUhcuDescription(string description)
    {
        // UHCU: "Withdrawal DEBIT CARD/MERCHANT Date MM/DD/YY ... Card XXXX Merchant Category Code: NNNN"
        var uhcuDebitMatch = UhcuDebitCardPattern().Match(description);
        if (uhcuDebitMatch.Success)
        {
            var merchant = uhcuDebitMatch.Groups["merchant"].Value.Trim();
            return CleanMerchantName(merchant);
        }

        // UHCU: "Withdrawal ACH NAME/TYPE: DESC ID: ... CO: ... NAME: ..."
        var uhcuAchMatch = UhcuAchPattern().Match(description);
        if (uhcuAchMatch.Success)
        {
            var name = uhcuAchMatch.Groups["name"].Value.Trim();
            var type = uhcuAchMatch.Groups["type"].Value.Trim();
            return $"{name} {type}";
        }

        // UHCU: "Withdrawal POS #.../POS MERCHANT # NNN CITY ST Card XXXX Merchant Category Code: NNNN"
        var uhcuPosMatch = UhcuPosPattern().Match(description);
        if (uhcuPosMatch.Success)
        {
            var merchant = uhcuPosMatch.Groups["merchant"].Value.Trim();

            // Strip store numbers like "# 0192"
            merchant = StoreNumberPattern().Replace(merchant, string.Empty).Trim();
            return merchant;
        }

        return description;
    }

    private static string CleanCapitalOneDescription(string description)
    {
        // Capital One: "Debit Card Purchase - MERCHANT"
        var debitPurchaseMatch = CapitalOneDebitPurchasePattern().Match(description);
        if (debitPurchaseMatch.Success)
        {
            return debitPurchaseMatch.Groups["merchant"].Value.Trim();
        }

        // Capital One: "Digital Card Purchase - MERCHANT"
        var digitalPurchaseMatch = CapitalOneDigitalPurchasePattern().Match(description);
        if (digitalPurchaseMatch.Success)
        {
            return digitalPurchaseMatch.Groups["merchant"].Value.Trim();
        }

        // Capital One: "Withdrawal from NAME"
        var withdrawalMatch = CapitalOneWithdrawalPattern().Match(description);
        if (withdrawalMatch.Success)
        {
            return withdrawalMatch.Groups["name"].Value.Trim();
        }

        // Capital One: "Deposit from NAME"
        var depositMatch = CapitalOneDepositPattern().Match(description);
        if (depositMatch.Success)
        {
            return depositMatch.Groups["name"].Value.Trim();
        }

        // Capital One: "Zelle money sent to NAME"
        var zelleMatch = CapitalOneZellePattern().Match(description);
        if (zelleMatch.Success)
        {
            return $"ZELLE {zelleMatch.Groups["name"].Value.Trim()}";
        }

        // Capital One: "Prenote NAME DATE"
        var prenoteMatch = CapitalOnePrenotePattern().Match(description);
        if (prenoteMatch.Success)
        {
            return prenoteMatch.Groups["name"].Value.Trim();
        }

        return description;
    }

    private static string CleanBoaDescription(string description)
    {
        // BoA ACH: "NAME DES:TYPE ID:... INDN:... CO ID:... PPD"
        var achMatch = BoaAchPattern().Match(description);
        if (achMatch.Success)
        {
            var name = achMatch.Groups["name"].Value.Trim();
            var type = achMatch.Groups["type"].Value.Trim();
            return $"{name} {type}";
        }

        // BoA: "MERCHANT MM/DD PURCHASE ..." — strip the date and everything after
        var purchaseMatch = BoaPurchaseDatePattern().Match(description);
        if (purchaseMatch.Success)
        {
            return purchaseMatch.Groups["merchant"].Value.Trim();
        }

        // BoA mobile deposit: "BKOFAMERICA MOBILE MM/DD XXXXX... DEPOSIT *MOBILE MO"
        var mobileMatch = BoaMobileDepositPattern().Match(description);
        if (mobileMatch.Success)
        {
            return "BKOFAMERICA MOBILE DEPOSIT";
        }

        return description;
    }

    private static string CleanMerchantName(string merchant)
    {
        // Strip trailing transaction metadata: "Date MM/DD/YY NNNN... Card XXXX..."
        merchant = DateTrailerPattern().Replace(merchant, string.Empty).Trim();

        // Strip vendor reference codes: "AMAZON MKTPL*B82LH69L1 Amzn.com/bill WA" → "AMAZON MKTPL"
        // First check for asterisk pattern (vendor*code)
        var asteriskIdx = merchant.IndexOf('*');
        if (asteriskIdx >= 0)
        {
            merchant = merchant[..asteriskIdx].Trim();
        }

        return merchant;
    }

    private static string StripAsteriskCodes(string description)
    {
        // When * follows a non-whitespace character, it's a vendor*orderCode pattern.
        // Strip everything from * onward: "AMAZON MKTPL*AB3CD5EF0" → "AMAZON MKTPL"
        // Also handles: "RETA* LM7QS9TU4" → "RETA", "VENMO*" → "VENMO"
        var vendorCodeMatch = VendorAsteriskPattern().Match(description);
        if (vendorCodeMatch.Success)
        {
            return vendorCodeMatch.Groups["prefix"].Value.Trim();
        }

        // Handle "SQ *NAME" (space-asterisk) brand separator: strip " *" to get "SQ NAME"
        var spaceAsteriskMatch = SpaceAsteriskPattern().Match(description);
        if (spaceAsteriskMatch.Success)
        {
            var before = spaceAsteriskMatch.Groups["before"].Value.Trim();
            var after = spaceAsteriskMatch.Groups["after"].Value.Trim();
            return $"{before} {after}";
        }

        return description;
    }

    private static string NormalizeWhitespace(string text)
    {
        return MultiSpacePattern().Replace(text, " ").Trim();
    }

    // ============================================================
    // Regex patterns (source-generated for performance)
    // ============================================================

    // UHCU patterns
    [GeneratedRegex(@"^Withdrawal DEBIT CARD/(?<merchant>.+?)(?:\s+Date\s+\d{1,2}/\d{1,2}/\d{2,4})", RegexOptions.IgnoreCase)]
    private static partial Regex UhcuDebitCardPattern();

    [GeneratedRegex(@"^Withdrawal ACH (?<name>[^/]+)/TYPE:\s*(?<type>.+?)\s+ID:", RegexOptions.IgnoreCase)]
    private static partial Regex UhcuAchPattern();

    [GeneratedRegex(@"^Withdrawal POS #\d+/POS\s+(?<merchant>.+?)(?:\s+Card\s+\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex UhcuPosPattern();

    [GeneratedRegex(@"\s*#\s*\d+", RegexOptions.None)]
    private static partial Regex StoreNumberPattern();

    // Capital One patterns
    [GeneratedRegex(@"^Debit Card Purchase\s*-\s*(?<merchant>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex CapitalOneDebitPurchasePattern();

    [GeneratedRegex(@"^Digital Card Purchase\s*-\s*(?<merchant>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex CapitalOneDigitalPurchasePattern();

    [GeneratedRegex(@"^Withdrawal from\s+(?<name>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex CapitalOneWithdrawalPattern();

    [GeneratedRegex(@"^Deposit from\s+(?<name>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex CapitalOneDepositPattern();

    [GeneratedRegex(@"^Zelle money sent to\s+(?<name>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex CapitalOneZellePattern();

    [GeneratedRegex(@"^Prenote\s+(?<name>.+?)(?:\s+(?:JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)\s+\d{1,2})?$", RegexOptions.IgnoreCase)]
    private static partial Regex CapitalOnePrenotePattern();

    // BoA patterns
    [GeneratedRegex(@"^(?<name>.+?)\s+DES:(?<type>\S+)\s+ID:", RegexOptions.IgnoreCase)]
    private static partial Regex BoaAchPattern();

    [GeneratedRegex(@"^(?<merchant>.+?)\s+\d{1,2}/\d{1,2}\s+(?:PURCHASE|PMNT SENT)", RegexOptions.IgnoreCase)]
    private static partial Regex BoaPurchaseDatePattern();

    [GeneratedRegex(@"^BKOFAMERICA MOBILE\s+\d{1,2}/\d{1,2}", RegexOptions.IgnoreCase)]
    private static partial Regex BoaMobileDepositPattern();

    // General patterns
    [GeneratedRegex(@"\s+Date\s+\d{1,2}/\d{1,2}/\d{2,4}.*$", RegexOptions.IgnoreCase)]
    private static partial Regex DateTrailerPattern();

    [GeneratedRegex(@"^(?<prefix>.*\S)\*", RegexOptions.None)]
    private static partial Regex VendorAsteriskPattern();

    [GeneratedRegex(@"^(?<before>.+?)\s+\*\s*(?<after>.+)$", RegexOptions.None)]
    private static partial Regex SpaceAsteriskPattern();

    [GeneratedRegex(@"\s{2,}", RegexOptions.None)]
    private static partial Regex MultiSpacePattern();
}
