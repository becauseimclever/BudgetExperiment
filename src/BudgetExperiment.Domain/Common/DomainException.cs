namespace BudgetExperiment.Domain.Common;

/// <summary>
/// Represents a domain rule violation.
/// </summary>
public sealed class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a validation type.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public DomainException(string message)
        : this(message, DomainExceptionType.Validation)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="exceptionType">The classification of this exception.</param>
    public DomainException(string message, DomainExceptionType exceptionType)
        : base(message)
    {
        ExceptionType = exceptionType;
    }

    /// <summary>
    /// Gets the classification of this domain exception.
    /// </summary>
    public DomainExceptionType ExceptionType
    {
        get;
    }
}
