// <copyright file="ChatServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

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
    private readonly MockChatActionExecutor _actionExecutor;
    private readonly MockUnitOfWork _unitOfWork;
    private readonly ChatService _service;

    public ChatServiceTests()
    {
        _sessionRepo = new MockChatSessionRepository();
        _messageRepo = new MockChatMessageRepository();
        _accountRepo = new MockAccountRepository();
        _categoryRepo = new MockBudgetCategoryRepository();
        _parser = new MockNaturalLanguageParser();
        _actionExecutor = new MockChatActionExecutor();
        _unitOfWork = new MockUnitOfWork();

        _service = new ChatService(
            _sessionRepo,
            _messageRepo,
            _accountRepo,
            _categoryRepo,
            _parser,
            _actionExecutor,
            _unitOfWork);
    }

    [Fact]
    public async Task GetOrCreateSessionAsync_Creates_New_Session_When_None_Active()
    {
        // Arrange
        _sessionRepo.SetActiveSession(null);

        // Act
        var result = await _service.GetOrCreateSessionAsync("user123");

        // Assert
        result.ShouldNotBeNull();
        result.IsActive.ShouldBeTrue();
        _sessionRepo.AddedSessions.Count.ShouldBe(1);
        _unitOfWork.SaveChangesCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task GetOrCreateSessionAsync_Returns_Existing_Active_Session()
    {
        // Arrange
        var existingSession = ChatSession.Create();
        _sessionRepo.SetActiveSession(existingSession);

        // Act
        var result = await _service.GetOrCreateSessionAsync("user123");

        // Assert
        result.ShouldBe(existingSession);
        _sessionRepo.AddedSessions.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetSessionAsync_Returns_Session_When_Found()
    {
        // Arrange
        var session = ChatSession.Create();
        _sessionRepo.AddSession(session);

        // Act
        var result = await _service.GetSessionAsync(session.Id);

        // Assert
        result.ShouldBe(session);
    }

    [Fact]
    public async Task GetSessionAsync_Returns_Null_When_Not_Found()
    {
        // Act
        var result = await _service.GetSessionAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SendMessageAsync_Returns_Failure_When_Session_Not_Found()
    {
        // Act
        var result = await _service.SendMessageAsync(Guid.NewGuid(), "Hello");

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
        _sessionRepo.AddSession(session);

        // Act
        var result = await _service.SendMessageAsync(session.Id, "Hello");

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
        _sessionRepo.AddSession(session);

        _parser.SetupResult(new ParseResult(
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
        var result = await _service.SendMessageAsync(session.Id, "Add $50 groceries");

        // Assert
        result.Success.ShouldBeTrue();
        result.UserMessage.ShouldNotBeNull();
        result.UserMessage.Content.ShouldBe("Add $50 groceries");
        result.UserMessage.Role.ShouldBe(ChatRole.User);
        result.AssistantMessage.ShouldNotBeNull();
        result.AssistantMessage.Content.ShouldContain("create that transaction");
        result.AssistantMessage.Action.ShouldNotBeNull();
        _messageRepo.AddedMessages.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SendMessageAsync_PassesContextToParser()
    {
        // Arrange
        var session = ChatSession.Create();
        _sessionRepo.AddSession(session);

        _parser.SetupResult(new ParseResult(
            Success: true,
            Action: null,
            ResponseText: "Ok"));

        var context = new ChatContext(
            CurrentAccountName: "Checking",
            CurrentDate: new DateOnly(2026, 2, 10),
            CurrentPage: "calendar");

        // Act
        var result = await _service.SendMessageAsync(session.Id, "Add $50 groceries", context);

        // Assert
        result.Success.ShouldBeTrue();
        _parser.LastContext.ShouldNotBeNull();
        _parser.LastContext!.CurrentAccountName.ShouldBe("Checking");
        _parser.LastContext.CurrentDate.ShouldBe(new DateOnly(2026, 2, 10));
        _parser.LastContext.CurrentPage.ShouldBe("calendar");
    }

    [Fact]
    public async Task SendMessageAsync_Returns_Parse_Error_When_Parser_Fails()
    {
        // Arrange
        var session = ChatSession.Create();
        _sessionRepo.AddSession(session);

        _parser.SetupResult(new ParseResult(
            Success: false,
            Action: null,
            ResponseText: "I couldn't understand that.",
            ErrorMessage: "Parse failed"));

        // Act
        var result = await _service.SendMessageAsync(session.Id, "What's the weather?");

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
        var result = await _service.ConfirmActionAsync(Guid.NewGuid());

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
        _messageRepo.AddMessage(message);

        // Act
        var result = await _service.ConfirmActionAsync(message.Id);

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
        _messageRepo.AddMessage(message);

        // Act
        var result = await _service.ConfirmActionAsync(message.Id);

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("not pending");
    }

    [Fact]
    public async Task CancelActionAsync_Returns_False_When_Message_Not_Found()
    {
        // Act
        var result = await _service.CancelActionAsync(Guid.NewGuid());

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
        _messageRepo.AddMessage(message);

        // Act
        var result = await _service.CancelActionAsync(message.Id);

        // Assert
        result.ShouldBeTrue();
        message.ActionStatus.ShouldBe(ChatActionStatus.Cancelled);
        _unitOfWork.SaveChangesCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task CloseSessionAsync_Returns_False_When_Not_Found()
    {
        // Act
        var result = await _service.CloseSessionAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task CloseSessionAsync_Closes_Active_Session()
    {
        // Arrange
        var session = ChatSession.Create();
        _sessionRepo.AddSession(session);

        // Act
        var result = await _service.CloseSessionAsync(session.Id);

        // Assert
        result.ShouldBeTrue();
        session.IsActive.ShouldBeFalse();
        _unitOfWork.SaveChangesCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task CloseSessionAsync_Returns_False_When_Already_Closed()
    {
        // Arrange
        var session = ChatSession.Create();
        session.Close();
        _sessionRepo.AddSession(session);

        // Act
        var result = await _service.CloseSessionAsync(session.Id);

        // Assert
        result.ShouldBeFalse();
    }

    private sealed class MockChatSessionRepository : IChatSessionRepository
    {
        private readonly Dictionary<Guid, ChatSession> _sessions = new();
        private ChatSession? _activeSession;

        public List<ChatSession> AddedSessions { get; } = new();

        public void SetActiveSession(ChatSession? session)
        {
            _activeSession = session;
        }

        public void AddSession(ChatSession session)
        {
            _sessions[session.Id] = session;
        }

        public Task<ChatSession?> GetActiveSessionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_activeSession);

        public Task<ChatSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_sessions.GetValueOrDefault(id));

        public Task<ChatSession?> GetWithMessagesAsync(Guid sessionId, int messageLimit = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult(_sessions.GetValueOrDefault(sessionId));

        public Task<IReadOnlyList<ChatSession>> ListAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChatSession>>(_sessions.Values.Skip(skip).Take(take).ToList());

        public Task<long> CountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((long)_sessions.Count);

        public Task AddAsync(ChatSession entity, CancellationToken cancellationToken = default)
        {
            _sessions[entity.Id] = entity;
            this.AddedSessions.Add(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(ChatSession entity, CancellationToken cancellationToken = default)
        {
            _sessions.Remove(entity.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class MockChatMessageRepository : IChatMessageRepository
    {
        private readonly Dictionary<Guid, ChatMessage> _messages = new();

        public List<ChatMessage> AddedMessages { get; } = new();

        public void AddMessage(ChatMessage message)
        {
            _messages[message.Id] = message;
        }

        public Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_messages.GetValueOrDefault(id));

        public Task<IReadOnlyList<ChatMessage>> GetBySessionAsync(Guid sessionId, int limit = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChatMessage>>(
                _messages.Values.Where(m => m.SessionId == sessionId).Take(limit).ToList());

        public Task<IReadOnlyList<ChatMessage>> GetPendingActionsAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChatMessage>>(
                _messages.Values
                    .Where(m => m.SessionId == sessionId && m.ActionStatus == ChatActionStatus.Pending)
                    .ToList());

        public Task<IReadOnlyList<ChatMessage>> ListAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ChatMessage>>(_messages.Values.Skip(skip).Take(take).ToList());

        public Task<long> CountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((long)_messages.Count);

        public Task AddAsync(ChatMessage entity, CancellationToken cancellationToken = default)
        {
            _messages[entity.Id] = entity;
            this.AddedMessages.Add(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(ChatMessage entity, CancellationToken cancellationToken = default)
        {
            _messages.Remove(entity.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class MockAccountRepository : IAccountRepository
    {
        private readonly List<Account> _accounts = new();

        public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.FirstOrDefault(a => a.Id == id));

        public Task<Account?> GetByIdWithTransactionsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_accounts.FirstOrDefault(a => a.Id == id));

        public Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Account>>(_accounts);

        public Task<IReadOnlyList<Account>> ListAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Account>>(_accounts.Skip(skip).Take(take).ToList());

        public Task<long> CountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((long)_accounts.Count);

        public Task AddAsync(Account entity, CancellationToken cancellationToken = default)
        {
            _accounts.Add(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Account entity, CancellationToken cancellationToken = default)
        {
            _accounts.Remove(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class MockBudgetCategoryRepository : IBudgetCategoryRepository
    {
        private readonly List<BudgetCategory> _categories = new();

        public Task<BudgetCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_categories.FirstOrDefault(c => c.Id == id));

        public Task<BudgetCategory?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(_categories.FirstOrDefault(c => c.Name == name));

        public Task<IReadOnlyList<BudgetCategory>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BudgetCategory>>(_categories.Where(c => c.IsActive).ToList());

        public Task<IReadOnlyList<BudgetCategory>> GetByTypeAsync(CategoryType type, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BudgetCategory>>(_categories.Where(c => c.Type == type).ToList());

        public Task<IReadOnlyList<BudgetCategory>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BudgetCategory>>(_categories);

        public Task<IReadOnlyList<BudgetCategory>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.ToList();
            return Task.FromResult<IReadOnlyList<BudgetCategory>>(_categories.Where(c => idList.Contains(c.Id)).ToList());
        }

        public Task<IReadOnlyList<BudgetCategory>> ListAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BudgetCategory>>(_categories.Skip(skip).Take(take).ToList());

        public Task<long> CountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult((long)_categories.Count);

        public Task AddAsync(BudgetCategory entity, CancellationToken cancellationToken = default)
        {
            _categories.Add(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(BudgetCategory entity, CancellationToken cancellationToken = default)
        {
            _categories.Remove(entity);
            return Task.CompletedTask;
        }
    }

    private sealed class MockNaturalLanguageParser : INaturalLanguageParser
    {
        private ParseResult _result = new(false, null, "Not configured");

        public ChatContext? LastContext
        {
            get; private set;
        }

        public void SetupResult(ParseResult result)
        {
            _result = result;
        }

        public Task<ParseResult> ParseCommandAsync(
            string input,
            IReadOnlyList<AccountInfo> accounts,
            IReadOnlyList<CategoryInfo> categories,
            ChatContext? context = null,
            CancellationToken cancellationToken = default)
        {
            this.LastContext = context;
            return Task.FromResult(_result);
        }
    }

    private sealed class MockChatActionExecutor : IChatActionExecutor
    {
        private ActionExecutionResult? _result;

        public void SetupResult(ActionExecutionResult result)
        {
            _result = result;
        }

        public Task<ActionExecutionResult> ExecuteActionAsync(ChatAction action, CancellationToken cancellationToken = default)
        {
            if (_result is not null)
            {
                return Task.FromResult(_result);
            }

            return Task.FromResult(new ActionExecutionResult(
                Success: true,
                ActionType: action.Type,
                CreatedEntityId: Guid.NewGuid(),
                Message: "Action executed."));
        }
    }

    private sealed class MockUnitOfWork : IUnitOfWork
    {
        public bool SaveChangesCalled
        {
            get; private set;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            this.SaveChangesCalled = true;
            return Task.FromResult(1);
        }

        public string? GetConcurrencyToken<T>(T entity)
            where T : class => null;

        public void SetExpectedConcurrencyToken<T>(T entity, string token)
            where T : class
        {
        }

        public void MarkAsModified<T>(T entity)
            where T : class
        {
        }
    }
}
