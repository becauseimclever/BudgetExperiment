// <copyright file="ChatServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using Shouldly;
using Xunit;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ChatService"/>.
/// These tests focus on the core ChatService orchestration logic.
/// </summary>
public class ChatServiceTests
{
    private readonly MockChatSessionRepository _sessionRepo;
    private readonly MockChatMessageRepository _messageRepo;
    private readonly MockAccountRepository _accountRepo;
    private readonly MockBudgetCategoryRepository _categoryRepo;
    private readonly MockNaturalLanguageParser _parser;
    private readonly MockTransactionService _transactionService;
    private readonly MockTransferService _transferService;
    private readonly MockRecurringTransactionService _recurringTransactionService;
    private readonly MockRecurringTransferService _recurringTransferService;
    private readonly MockUnitOfWork _unitOfWork;
    private readonly ChatService _service;

    public ChatServiceTests()
    {
        this._sessionRepo = new MockChatSessionRepository();
        this._messageRepo = new MockChatMessageRepository();
        this._accountRepo = new MockAccountRepository();
        this._categoryRepo = new MockBudgetCategoryRepository();
        this._parser = new MockNaturalLanguageParser();
        this._transactionService = new MockTransactionService();
        this._transferService = new MockTransferService();
        this._recurringTransactionService = new MockRecurringTransactionService();
        this._recurringTransferService = new MockRecurringTransferService();
        this._unitOfWork = new MockUnitOfWork();

        this._service = new ChatService(
            this._sessionRepo,
            this._messageRepo,
            this._accountRepo,
            this._categoryRepo,
            this._parser,
            this._transactionService,
            this._transferService,
            this._recurringTransactionService,
            this._recurringTransferService,
            this._unitOfWork);
    }

    [Fact]
    public async Task GetOrCreateSessionAsync_Creates_New_Session_When_None_Active()
    {
        // Arrange
        this._sessionRepo.SetActiveSession(null);

        // Act
        var result = await this._service.GetOrCreateSessionAsync("user123");

        // Assert
        result.ShouldNotBeNull();
        result.IsActive.ShouldBeTrue();
        this._sessionRepo.AddedSessions.Count.ShouldBe(1);
        this._unitOfWork.SaveChangesCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task GetOrCreateSessionAsync_Returns_Existing_Active_Session()
    {
        // Arrange
        var existingSession = ChatSession.Create();
        this._sessionRepo.SetActiveSession(existingSession);

        // Act
        var result = await this._service.GetOrCreateSessionAsync("user123");

        // Assert
        result.ShouldBe(existingSession);
        this._sessionRepo.AddedSessions.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetSessionAsync_Returns_Session_When_Found()
    {
        // Arrange
        var session = ChatSession.Create();
        this._sessionRepo.AddSession(session);

        // Act
        var result = await this._service.GetSessionAsync(session.Id);

        // Assert
        result.ShouldBe(session);
    }

    [Fact]
    public async Task GetSessionAsync_Returns_Null_When_Not_Found()
    {
        // Act
        var result = await this._service.GetSessionAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SendMessageAsync_Returns_Failure_When_Session_Not_Found()
    {
        // Act
        var result = await this._service.SendMessageAsync(Guid.NewGuid(), "Hello");

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("Session not found");
    }

    [Fact]
    public async Task SendMessageAsync_Returns_Failure_When_Session_Closed()
    {
        // Arrange
        var session = ChatSession.Create();
        session.Close();
        this._sessionRepo.AddSession(session);

        // Act
        var result = await this._service.SendMessageAsync(session.Id, "Hello");

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("closed");
    }

    [Fact]
    public async Task SendMessageAsync_Creates_Messages_And_Returns_Result()
    {
        // Arrange
        var session = ChatSession.Create();
        this._sessionRepo.AddSession(session);

        this._parser.SetupResult(new ParseResult(
            Success: true,
            Action: new CreateTransactionAction
            {
                AccountId = Guid.NewGuid(),
                AccountName = "Checking",
                Amount = -50m,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Description = "Test purchase",
            },
            ResponseText: "I'll create that transaction for you."));

        // Act
        var result = await this._service.SendMessageAsync(session.Id, "Add $50 groceries");

        // Assert
        result.Success.ShouldBeTrue();
        result.UserMessage.ShouldNotBeNull();
        result.UserMessage.Content.ShouldBe("Add $50 groceries");
        result.UserMessage.Role.ShouldBe(ChatRole.User);
        result.AssistantMessage.ShouldNotBeNull();
        result.AssistantMessage.Content.ShouldContain("create that transaction");
        result.AssistantMessage.Action.ShouldNotBeNull();
        this._messageRepo.AddedMessages.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SendMessageAsync_Returns_Parse_Error_When_Parser_Fails()
    {
        // Arrange
        var session = ChatSession.Create();
        this._sessionRepo.AddSession(session);

        this._parser.SetupResult(new ParseResult(
            Success: false,
            Action: null,
            ResponseText: "I couldn't understand that.",
            ErrorMessage: "Parse failed"));

        // Act
        var result = await this._service.SendMessageAsync(session.Id, "What's the weather?");

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Parse failed");
        result.AssistantMessage.ShouldNotBeNull();
        result.AssistantMessage.Action.ShouldBeNull();
    }

    [Fact]
    public async Task ConfirmActionAsync_Returns_Failure_When_Message_Not_Found()
    {
        // Act
        var result = await this._service.ConfirmActionAsync(Guid.NewGuid());

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("not found");
    }

    [Fact]
    public async Task ConfirmActionAsync_Returns_Failure_When_No_Action()
    {
        // Arrange
        var session = ChatSession.Create();
        var message = session.AddUserMessage("Hello");
        this._messageRepo.AddMessage(message);

        // Act
        var result = await this._service.ConfirmActionAsync(message.Id);

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("No action");
    }

    [Fact]
    public async Task ConfirmActionAsync_Returns_Failure_When_Action_Already_Confirmed()
    {
        // Arrange
        var session = ChatSession.Create();
        var action = new CreateTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = -50m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Description = "Test",
        };
        var message = session.AddAssistantMessage("Creating...", action);
        message.MarkActionConfirmed(Guid.NewGuid()); // Already confirmed
        this._messageRepo.AddMessage(message);

        // Act
        var result = await this._service.ConfirmActionAsync(message.Id);

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("not pending");
    }

    [Fact]
    public async Task CancelActionAsync_Returns_False_When_Message_Not_Found()
    {
        // Act
        var result = await this._service.CancelActionAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task CancelActionAsync_Cancels_Pending_Action()
    {
        // Arrange
        var session = ChatSession.Create();
        var action = new CreateTransactionAction
        {
            AccountId = Guid.NewGuid(),
            AccountName = "Checking",
            Amount = -50m,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Description = "Test",
        };
        var message = session.AddAssistantMessage("Creating...", action);
        this._messageRepo.AddMessage(message);

        // Act
        var result = await this._service.CancelActionAsync(message.Id);

        // Assert
        result.ShouldBeTrue();
        message.ActionStatus.ShouldBe(ChatActionStatus.Cancelled);
        this._unitOfWork.SaveChangesCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task CloseSessionAsync_Returns_False_When_Not_Found()
    {
        // Act
        var result = await this._service.CloseSessionAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task CloseSessionAsync_Closes_Active_Session()
    {
        // Arrange
        var session = ChatSession.Create();
        this._sessionRepo.AddSession(session);

        // Act
        var result = await this._service.CloseSessionAsync(session.Id);

        // Assert
        result.ShouldBeTrue();
        session.IsActive.ShouldBeFalse();
        this._unitOfWork.SaveChangesCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task CloseSessionAsync_Returns_False_When_Already_Closed()
    {
        // Arrange
        var session = ChatSession.Create();
        session.Close();
        this._sessionRepo.AddSession(session);

        // Act
        var result = await this._service.CloseSessionAsync(session.Id);

        // Assert
        result.ShouldBeFalse();
    }

    #region Mock Implementations

    private sealed class MockChatSessionRepository : IChatSessionRepository
    {
        private readonly Dictionary<Guid, ChatSession> _sessions = new();
        private ChatSession? _activeSession;

        public List<ChatSession> AddedSessions { get; } = new();

        public void SetActiveSession(ChatSession? session)
        {
            this._activeSession = session;
        }

        public void AddSession(ChatSession session)
        {
            this._sessions[session.Id] = session;
        }

        public Task<ChatSession?> GetActiveSessionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(this._activeSession);

        public Task<ChatSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(this._sessions.GetValueOrDefault(id));

        public Task<ChatSession?> GetWithMessagesAsync(Guid sessionId, int messageLimit = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult(this._sessions.GetValueOrDefault(sessionId));

        public Task<IReadOnlyList<ChatSession>> ListAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChatSession>>(this._sessions.Values.Skip(skip).Take(take).ToList());

        public Task<long> CountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((long)this._sessions.Count);

        public Task AddAsync(ChatSession entity, CancellationToken cancellationToken = default)
        {
            this._sessions[entity.Id] = entity;
            this.AddedSessions.Add(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(ChatSession entity, CancellationToken cancellationToken = default)
        {
            this._sessions.Remove(entity.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class MockChatMessageRepository : IChatMessageRepository
    {
        private readonly Dictionary<Guid, ChatMessage> _messages = new();

        public List<ChatMessage> AddedMessages { get; } = new();

        public void AddMessage(ChatMessage message)
        {
            this._messages[message.Id] = message;
        }

        public Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(this._messages.GetValueOrDefault(id));

        public Task<IReadOnlyList<ChatMessage>> GetBySessionAsync(Guid sessionId, int limit = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChatMessage>>(
                this._messages.Values.Where(m => m.SessionId == sessionId).Take(limit).ToList());

        public Task<IReadOnlyList<ChatMessage>> GetPendingActionsAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChatMessage>>(
                this._messages.Values
                    .Where(m => m.SessionId == sessionId && m.ActionStatus == ChatActionStatus.Pending)
                    .ToList());

        public Task<IReadOnlyList<ChatMessage>> ListAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChatMessage>>(this._messages.Values.Skip(skip).Take(take).ToList());

        public Task<long> CountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((long)this._messages.Count);

        public Task AddAsync(ChatMessage entity, CancellationToken cancellationToken = default)
        {
            this._messages[entity.Id] = entity;
            this.AddedMessages.Add(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(ChatMessage entity, CancellationToken cancellationToken = default)
        {
            this._messages.Remove(entity.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class MockAccountRepository : IAccountRepository
    {
        private readonly List<Account> _accounts = new();

        public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(this._accounts.FirstOrDefault(a => a.Id == id));

        public Task<Account?> GetByIdWithTransactionsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(this._accounts.FirstOrDefault(a => a.Id == id));

        public Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Account>>(this._accounts);

        public Task<IReadOnlyList<Account>> ListAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Account>>(this._accounts.Skip(skip).Take(take).ToList());

        public Task<long> CountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((long)this._accounts.Count);

        public Task AddAsync(Account entity, CancellationToken cancellationToken = default)
        {
            this._accounts.Add(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Account entity, CancellationToken cancellationToken = default)
        {
            this._accounts.Remove(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class MockBudgetCategoryRepository : IBudgetCategoryRepository
    {
        private readonly List<BudgetCategory> _categories = new();

        public Task<BudgetCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(this._categories.FirstOrDefault(c => c.Id == id));

        public Task<BudgetCategory?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(this._categories.FirstOrDefault(c => c.Name == name));

        public Task<IReadOnlyList<BudgetCategory>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BudgetCategory>>(this._categories.Where(c => c.IsActive).ToList());

        public Task<IReadOnlyList<BudgetCategory>> GetByTypeAsync(CategoryType type, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BudgetCategory>>(this._categories.Where(c => c.Type == type).ToList());

        public Task<IReadOnlyList<BudgetCategory>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BudgetCategory>>(this._categories);

        public Task<IReadOnlyList<BudgetCategory>> ListAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BudgetCategory>>(this._categories.Skip(skip).Take(take).ToList());

        public Task<long> CountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((long)this._categories.Count);

        public Task AddAsync(BudgetCategory entity, CancellationToken cancellationToken = default)
        {
            this._categories.Add(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(BudgetCategory entity, CancellationToken cancellationToken = default)
        {
            this._categories.Remove(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class MockNaturalLanguageParser : INaturalLanguageParser
    {
        private ParseResult _result = new(false, null, "Not configured");

        public void SetupResult(ParseResult result)
        {
            this._result = result;
        }

        public Task<ParseResult> ParseCommandAsync(
            string input,
            IReadOnlyList<AccountInfo> accounts,
            IReadOnlyList<CategoryInfo> categories,
            ChatContext? context = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(this._result);
    }

    private sealed class MockTransferService : ITransferService
    {
        public Task<TransferResponse> CreateAsync(CreateTransferRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TransferResponse
            {
                TransferId = Guid.NewGuid(),
                SourceAccountId = request.SourceAccountId,
                DestinationAccountId = request.DestinationAccountId,
                Amount = request.Amount,
                Currency = request.Currency,
                Date = request.Date,
            });
        }

        public Task<TransferResponse?> GetByIdAsync(Guid transferId, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransferResponse?>(null);

        public Task<IReadOnlyList<TransferListItemResponse>> ListAsync(Guid? accountId = null, DateOnly? fromDate = null, DateOnly? toDate = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TransferListItemResponse>>(new List<TransferListItemResponse>());

        public Task<TransferResponse?> UpdateAsync(Guid transferId, UpdateTransferRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransferResponse?>(null);

        public Task<bool> DeleteAsync(Guid transferId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class MockRecurringTransactionService : IRecurringTransactionService
    {
        public Task<RecurringTransactionDto> CreateAsync(RecurringTransactionCreateDto dto, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RecurringTransactionDto
            {
                Id = Guid.NewGuid(),
                AccountId = dto.AccountId,
                AccountName = "Test Account",
                Description = dto.Description,
                Amount = dto.Amount,
                Frequency = dto.Frequency,
                StartDate = dto.StartDate,
            });
        }

        public Task<RecurringTransactionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransactionDto?>(null);

        public Task<IReadOnlyList<RecurringTransactionDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RecurringTransactionDto>>(new List<RecurringTransactionDto>());

        public Task<IReadOnlyList<RecurringTransactionDto>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RecurringTransactionDto>>(new List<RecurringTransactionDto>());

        public Task<RecurringTransactionDto?> UpdateAsync(Guid id, RecurringTransactionUpdateDto dto, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransactionDto?>(null);

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<RecurringTransactionDto?> PauseAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransactionDto?>(null);

        public Task<RecurringTransactionDto?> ResumeAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransactionDto?>(null);
    }

    private sealed class MockRecurringTransferService : IRecurringTransferService
    {
        public Task<RecurringTransferDto> CreateAsync(RecurringTransferCreateDto dto, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RecurringTransferDto
            {
                Id = Guid.NewGuid(),
                SourceAccountId = dto.SourceAccountId,
                SourceAccountName = "Source Account",
                DestinationAccountId = dto.DestinationAccountId,
                DestinationAccountName = "Dest Account",
                Description = dto.Description,
                Amount = dto.Amount,
                Frequency = dto.Frequency,
                StartDate = dto.StartDate,
            });
        }

        public Task<RecurringTransferDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RecurringTransferDto?>(null);

        public Task<IReadOnlyList<RecurringTransferDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RecurringTransferDto>>(new List<RecurringTransferDto>());

        public Task<IReadOnlyList<RecurringTransferDto>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RecurringTransferDto>>(new List<RecurringTransferDto>());

        public Task<IReadOnlyList<RecurringTransferDto>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RecurringTransferDto>>(new List<RecurringTransferDto>());
    }

    private sealed class MockTransactionService : ITransactionService
    {
        public Task<TransactionDto> CreateAsync(TransactionCreateDto dto, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TransactionDto
            {
                Id = Guid.NewGuid(),
                AccountId = dto.AccountId,
                Amount = dto.Amount,
                Date = dto.Date,
                Description = dto.Description,
            });
        }

        public Task<TransactionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransactionDto?>(null);

        public Task<IReadOnlyList<TransactionDto>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TransactionDto>>(new List<TransactionDto>());

        public Task<TransactionDto?> UpdateAsync(Guid id, TransactionUpdateDto dto, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransactionDto?>(null);
    }

    private sealed class MockUnitOfWork : IUnitOfWork
    {
        public bool SaveChangesCalled { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            this.SaveChangesCalled = true;
            return Task.FromResult(1);
        }
    }

    #endregion
}
