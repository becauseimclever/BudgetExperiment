// <copyright file="TransactionDescriptionCleanerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Unit tests for <see cref="TransactionDescriptionCleaner"/>.
/// Uses real-world bank description samples from sample data files.
/// </summary>
public class TransactionDescriptionCleanerTests
{
    [Theory]
    [InlineData(
        "Debit Card Purchase - AMAZON MKTPL AMZN COM BIL WA",
        "AMAZON MKTPL AMZN COM BIL WA")]
    [InlineData(
        "Digital Card Purchase - AMAZON MKTPL AMZN COM BIL WA",
        "AMAZON MKTPL AMZN COM BIL WA")]
    [InlineData(
        "Debit Card Purchase - GROCERY STORE CITY ST",
        "GROCERY STORE CITY ST")]
    public void Clean_Strips_CapitalOne_DebitCardPurchase_Prefix(string raw, string expected)
    {
        var result = TransactionDescriptionCleaner.Clean(raw);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(
        "Withdrawal from INSURANCE COMPANY PAYMENT",
        "INSURANCE COMPANY PAYMENT")]
    [InlineData(
        "Withdrawal from CAPITAL ONE MOBILE PMT",
        "CAPITAL ONE MOBILE PMT")]
    [InlineData(
        "Withdrawal from MORTGAGE COMPANY",
        "MORTGAGE COMPANY")]
    public void Clean_Strips_CapitalOne_WithdrawalFrom_Prefix(string raw, string expected)
    {
        var result = TransactionDescriptionCleaner.Clean(raw);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Deposit from EMPLOYER PAYROLL", "EMPLOYER PAYROLL")]
    public void Clean_Strips_CapitalOne_DepositFrom_Prefix(string raw, string expected)
    {
        var result = TransactionDescriptionCleaner.Clean(raw);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Zelle money sent to JOHN DOE", "ZELLE JOHN DOE")]
    public void Clean_Strips_CapitalOne_Zelle_Prefix(string raw, string expected)
    {
        var result = TransactionDescriptionCleaner.Clean(raw);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(
        "AMAZON MKTPL*AB3CD5EF0 10/01 PURCHASE Amzn.com/bill WA",
        "AMAZON MKTPL")]
    [InlineData(
        "AMAZON RETA* LM7QS9TU4 10/08 PURCHASE WWW.AMAZON.CO WA",
        "AMAZON RETA")]
    [InlineData(
        "Audible*PQ8RT4VW2 10/04 PURCHASE Amzn.com/bill NJ",
        "AUDIBLE")]
    [InlineData(
        "CHEWY.COM 10/07 PURCHASE 800-672-4399 FL",
        "CHEWY.COM")]
    [InlineData(
        "CONSUMER CELLULAR INC 09/30 PURCHASE XXX-XX91237 OR",
        "CONSUMER CELLULAR INC")]
    public void Clean_Strips_BoA_DateAndPurchaseTrailer(string raw, string expected)
    {
        var result = TransactionDescriptionCleaner.Clean(raw);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(
        "ACME CORP DES:PAYROLL ID:58247 INDN:JOHN SMITH CO ID:XXXXX92451 PPD",
        "ACME CORP PAYROLL")]
    [InlineData(
        "UPSTART NETWORK DES:JEFFERIES ID:9482156 INDN:JOHN SMITH CO ID:78392641PQ WEB PMT INFO:FM XXXXX5728 BORROWER_PAYMT",
        "UPSTART NETWORK JEFFERIES")]
    [InlineData(
        "OPTUM BANK DES:REIMBUR ID:XXXXX3294 INDN:JOHN M SMITH CO ID:XXXXX67819 PPD",
        "OPTUM BANK REIMBUR")]
    public void Clean_Strips_BoA_ACH_Metadata(string raw, string expected)
    {
        var result = TransactionDescriptionCleaner.Clean(raw);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(
        "Withdrawal DEBIT CARD/AMAZON MKTPL*B82LH69L1 Amzn.com/bill WA Date 11/16/25 24692165320109351897026 Card 1234Merchant Category Code: 5942",
        "AMAZON MKTPL")]
    [InlineData(
        "Withdrawal DEBIT CARD/QT 4150 INSIDE ANYTOWN TX Date 11/14/25 24692165320109748411721 Card 1234Merchant Category Code: 5541",
        "QT 4150 INSIDE ANYTOWN TX")]
    [InlineData(
        "Withdrawal DEBIT CARD/ROSAS CAFE & TORTILLA F ANYTOWN TX Date 11/10/25 24013395315002183354435 Card 1234Merchant Category Code: 5814",
        "ROSAS CAFE & TORTILLA F ANYTOWN TX")]
    [InlineData(
        "Withdrawal DEBIT CARD/VENMO* John Doe Visa Direct NY Date 11/06/25 24248185310307357073572 Card 1234Merchant Category Code: 6051",
        "VENMO")]
    public void Clean_Strips_UHCU_DebitCard_Wrapper(string raw, string expected)
    {
        var result = TransactionDescriptionCleaner.Clean(raw);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(
        "Withdrawal ACH CAPITAL ONE/TYPE: MOBILE PMT ID: 9279744380 CO: CAPITAL ONE NAME: John Doe",
        "CAPITAL ONE MOBILE PMT")]
    public void Clean_Strips_UHCU_ACH_Metadata(string raw, string expected)
    {
        var result = TransactionDescriptionCleaner.Clean(raw);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(
        "Withdrawal POS #531023837611/POS PETSMART # 0192 ANYTOWN TX Card 1234 Merchant Category Code: 5995",
        "PETSMART ANYTOWN TX")]
    public void Clean_Strips_UHCU_POS_Wrapper(string raw, string expected)
    {
        var result = TransactionDescriptionCleaner.Clean(raw);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(
        "BKOFAMERICA MOBILE 10/01 XXXXX82945 DEPOSIT *MOBILE MO",
        "BKOFAMERICA MOBILE DEPOSIT")]
    public void Clean_Strips_BoA_MobileDeposit_Noise(string raw, string expected)
    {
        var result = TransactionDescriptionCleaner.Clean(raw);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(
        "VENMO* John 10/04 PMNT SENT Visa Direct NY",
        "VENMO")]
    [InlineData(
        "SQ *TEMP-PACK: FORMERLY 10/10 PURCHASE gosq.com TX",
        "SQ TEMP-PACK: FORMERLY")]
    public void Clean_Strips_BoA_Vendor_Transaction_Noise(string raw, string expected)
    {
        var result = TransactionDescriptionCleaner.Clean(raw);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Clean_Returns_Empty_For_Null()
    {
        var result = TransactionDescriptionCleaner.Clean(null!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Clean_Returns_Empty_For_WhiteSpace()
    {
        var result = TransactionDescriptionCleaner.Clean("   ");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Clean_Normalizes_Whitespace()
    {
        var result = TransactionDescriptionCleaner.Clean("WALMART   STORE   #123");

        Assert.Equal("WALMART STORE #123", result);
    }

    [Fact]
    public void Clean_Uppercases_Output()
    {
        var result = TransactionDescriptionCleaner.Clean("Amazon Purchase");

        Assert.Equal("AMAZON PURCHASE", result);
    }

    [Theory]
    [InlineData("WALMART SUPERCENTER", "WALMART SUPERCENTER")]
    [InlineData("COSTCO WHOLESALE", "COSTCO WHOLESALE")]
    public void Clean_Leaves_Simple_Descriptions_Unchanged(string raw, string expected)
    {
        var result = TransactionDescriptionCleaner.Clean(raw);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Clean_Strips_HtmlEntities()
    {
        // UHCU data contains &amp; for &
        var result = TransactionDescriptionCleaner.Clean("ROSAS CAFE &amp; TORTILLA");

        Assert.Equal("ROSAS CAFE & TORTILLA", result);
    }

    [Theory]
    [InlineData("Prenote INSURANCE COMPANY PAYMENT NOV 12", "INSURANCE COMPANY PAYMENT")]
    public void Clean_Strips_CapitalOne_Prenote_Prefix(string raw, string expected)
    {
        var result = TransactionDescriptionCleaner.Clean(raw);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(
        "AMAZON MKTPL*AB3CD5EF0",
        "AMAZON MKTPL")]
    [InlineData(
        "VENMO*",
        "VENMO")]
    [InlineData(
        "Audible*PQ8RT4VW2",
        "AUDIBLE")]
    public void Clean_Strips_Asterisk_OrderCodes(string raw, string expected)
    {
        var result = TransactionDescriptionCleaner.Clean(raw);

        Assert.Equal(expected, result);
    }
}
