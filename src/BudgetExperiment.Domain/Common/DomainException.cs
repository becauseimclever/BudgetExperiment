namespace BudgetExperiment.Domain.Common;

/// <summary>
/// Represents a domain rule violation.
/// </summary>
public sealed class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public DomainException(string message)
        : base(message)
    {
    }
}
