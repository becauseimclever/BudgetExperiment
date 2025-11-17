using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.AdhocTransactions;

/// <summary>
/// Application service implementation for managing adhoc transactions.
/// Replaces both ExpenseService and AdhocPaymentService.
/// </summary>
public sealed class AdhocTransactionService : IAdhocTransactionService
{
    private readonly IAdhocTransactionReadRepository _readRepository;
    private readonly IAdhocTransactionWriteRepository _writeRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdhocTransactionService"/> class.
    /// </summary>
    /// <param name="readRepository">Read repository.</param>
    /// <param name="writeRepository">Write repository.</param>
    /// <param name="unitOfWork">Unit of work.</param>
    public AdhocTransactionService(
        IAdhocTransactionReadRepository readRepository,
        IAdhocTransactionWriteRepository writeRepository,
        IUnitOfWork unitOfWork)
    {
        this._readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
        this._writeRepository = writeRepository ?? throw new ArgumentNullException(nameof(writeRepository));
        this._unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    // Income transaction creation methods

    /// <inheritdoc />
    public async Task<AdhocTransactionResponse> CreateIncomeAsync(CreateIncomeTransactionRequest request, CancellationToken cancellationToken = default)
                {
            // Check for duplicates
            var duplicates = await this._readRepository.FindDuplicatesAsync(
                request.Date,
                request.Description,
                request.Amount,
                TransactionType.Income,
                cancellationToken).ConfigureAwait(false);

            if (duplicates.Count > 0)
            {
                throw new DomainException($"A duplicate income transaction already exists on {request.Date:MM/dd/yyyy} for '{request.Description}' with amount ${request.Amount:F2}.");
            }

        var money = MoneyValue.Create(request.Currency, request.Amount);
        var transaction = AdhocTransaction.CreateIncome(request.Description, money, request.Date, request.Category);

        await this._writeRepository.AddAsync(transaction, cancellationToken).ConfigureAwait(false);
        await this._unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ToResponse(transaction);
    }

    // Expense transaction creation methods

    /// <inheritdoc />
    public async Task<AdhocTransactionResponse> CreateExpenseAsync(CreateExpenseTransactionRequest request, CancellationToken cancellationToken = default)
                {
            // Check for duplicates
            var duplicates = await this._readRepository.FindDuplicatesAsync(
                request.Date,
                request.Description,
                request.Amount,
                TransactionType.Expense,
                cancellationToken).ConfigureAwait(false);

            if (duplicates.Count > 0)
            {
                throw new DomainException($"A duplicate expense transaction already exists on {request.Date:MM/dd/yyyy} for '{request.Description}' with amount ${request.Amount:F2}.");
            }

        var money = MoneyValue.Create(request.Currency, request.Amount);
        var transaction = AdhocTransaction.CreateExpense(request.Description, money, request.Date, request.Category);

        await this._writeRepository.AddAsync(transaction, cancellationToken).ConfigureAwait(false);
        await this._unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ToResponse(transaction);
    }

    // Query methods

    /// <inheritdoc />
    public async Task<AdhocTransactionResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await this._readRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return transaction != null ? ToResponse(transaction) : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdhocTransactionResponse>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var transactions = await this._readRepository.GetByDateAsync(date, cancellationToken).ConfigureAwait(false);
        return transactions.Select(ToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdhocTransactionResponse>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var transactions = await this._readRepository.GetByDateRangeAsync(startDate, endDate, cancellationToken).ConfigureAwait(false);
        return transactions.Select(ToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<AdhocTransactionResponse> Items, int Total)> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;
        var items = await this._readRepository.ListAsync(skip, pageSize, cancellationToken).ConfigureAwait(false);
        var total = await this._readRepository.CountAsync(cancellationToken).ConfigureAwait(false);
        var dtos = items.Select(ToResponse).ToList();
        return (dtos, total);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<AdhocTransactionResponse> Items, int Total)> ListIncomeTransactionsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, total) = await this._readRepository.GetIncomeTransactionsAsync(page, pageSize, cancellationToken).ConfigureAwait(false);
        var dtos = items.Select(ToResponse).ToList();
        return (dtos, total);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<AdhocTransactionResponse> Items, int Total)> ListExpenseTransactionsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, total) = await this._readRepository.GetExpenseTransactionsAsync(page, pageSize, cancellationToken).ConfigureAwait(false);
        var dtos = items.Select(ToResponse).ToList();
        return (dtos, total);
    }

    // Update and delete methods

    /// <inheritdoc />
    public async Task<AdhocTransactionResponse?> UpdateAsync(Guid id, UpdateAdhocTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var transaction = await this._readRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (transaction == null)
        {
            return null;
        }

        var money = MoneyValue.Create(request.Currency, request.Amount);
        transaction.Update(request.Description, money, request.Date, request.Category);

        // Entity is already being tracked by EF Core, just save changes
        await this._unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ToResponse(transaction);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await this._readRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (transaction == null)
        {
            return false;
        }

        await this._writeRepository.RemoveAsync(transaction, cancellationToken).ConfigureAwait(false);
        await this._unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static AdhocTransactionResponse ToResponse(AdhocTransaction transaction)
    {
        return new AdhocTransactionResponse(
            transaction.Id,
            transaction.Description,
            transaction.Money.Currency,
            transaction.Money.Amount,
            transaction.Date,
            transaction.Category,
            transaction.TransactionType,
            transaction.CreatedUtc,
            transaction.UpdatedUtc);
    }
}
