namespace BudgetExperiment.Domain.Common;

/// <summary>
/// Immutable monetary value and currency (2 decimal rounding, away from zero).
/// Converted to reference type record to simplify EF Core owned mapping.
/// </summary>
public sealed record MoneyValue
{
    /// <summary>Initializes a new instance of the <see cref="MoneyValue"/> class.</summary>
    /// <param name="currency">Currency code.</param>
    /// <param name="amount">Amount.</param>
    private MoneyValue(string currency, decimal amount)
    {
        this.Currency = currency;
        this.Amount = amount;
    }

    /// <summary>Gets currency code (ISO upper case).</summary>
    /// <value>The ISO currency code in uppercase.</value>
    public string Currency { get; init; } = string.Empty;

    /// <summary>Gets monetary amount (scaled 2 decimals).</summary>
    /// <value>The monetary amount rounded to 2 decimal places.</value>
    public decimal Amount
    {
        get; init;
    }

    /// <summary>
    /// Adds two monetary values of the same currency.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>Summed <see cref="MoneyValue"/>.</returns>
    /// <exception cref="DomainException">Thrown when currencies differ.</exception>
    public static MoneyValue operator +(MoneyValue left, MoneyValue right)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Cannot add amounts with different currencies.");
        }

        return Create(left.Currency, left.Amount + right.Amount);
    }

    /// <summary>
    /// Subtracts two monetary values of the same currency.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>Difference <see cref="MoneyValue"/>.</returns>
    /// <exception cref="DomainException">Thrown when currencies differ.</exception>
    public static MoneyValue operator -(MoneyValue left, MoneyValue right)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Cannot subtract amounts with different currencies.");
        }

        return Create(left.Currency, left.Amount - right.Amount);
    }

    /// <summary>
    /// Returns the zero monetary value for the provided currency.
    /// </summary>
    /// <param name="currency">ISO currency code.</param>
    /// <returns>A zero <see cref="MoneyValue"/> for the currency.</returns>
    public static MoneyValue Zero(string currency) => Create(currency, 0m);

    /// <summary>
    /// Factory with validation and normalization.
    /// </summary>
    /// <param name="currency">ISO currency code.</param>
    /// <param name="amount">Amount (can be positive, negative, or zero).</param>
    /// <returns>Validated monetary value.</returns>
    /// <exception cref="DomainException">Thrown when invalid.</exception>
    public static MoneyValue Create(string currency, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Currency is required.");
        }

        return new MoneyValue(currency.Trim().ToUpperInvariant(), decimal.Round(amount, 2, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Gets the absolute value of this monetary amount.
    /// </summary>
    /// <returns>A new <see cref="MoneyValue"/> with the absolute amount.</returns>
    public MoneyValue Abs() => Create(this.Currency, Math.Abs(this.Amount));

    /// <summary>
    /// Gets the negated value of this monetary amount.
    /// </summary>
    /// <returns>A new <see cref="MoneyValue"/> with the negated amount.</returns>
    public MoneyValue Negate() => Create(this.Currency, -this.Amount);

    /// <inheritdoc />
    public override string ToString() => $"{this.Currency} {this.Amount:0.00}";
}
