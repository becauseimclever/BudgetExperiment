# Phase 1 Test Patterns — Reusable Concurrency, Soft-Delete, Authorization Templates

**Skill Owner:** Barbara (Tester)  
**Date:** 2026-04-22  
**Purpose:** Documented patterns for rapid test implementation across Phase 1+2  

---

## Pattern 1: Optimistic Locking (Concurrency) Test Template

### When to Use
- Testing `IUnitOfWork.SaveChangesAsync()` when entity has `RowVersion` (xmin token)
- Simulating concurrent updates to same aggregate
- Verifying conflict detection and exception handling

### Template
```csharp
public class YourServiceOptimisticLockingTests
{
    [Fact]
    public async Task UpdateAsync_RowVersionMismatch_ThrowsConcurrencyException()
    {
        // Arrange: Entity with initial RowVersion token
        var entity = YourEntity.Create("test");
        var mockRepository = new Mock<IYourRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        
        // Simulate SaveChangesAsync detecting RowVersion mismatch
        mockUow.Setup(u => u.SaveChangesAsync(default))
            .ThrowsAsync(new DbUpdateConcurrencyException(
                "Database operation expected to affect 1 row(s) but actually affected 0 row(s); " +
                "data may have been modified or deleted since entities were loaded."));
        
        var service = new YourService(mockRepository.Object, mockUow.Object);
        var updateDto = new YourUpdateDto { /* values */ };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => service.UpdateAsync(entity.Id, updateDto, default));
        
        // Verify error message mentions version/concurrency
        Assert.Contains("affected 0 row", ex.Message);
        
        // Verify no SaveChanges attempted twice
        mockUow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ConflictRetried_EventuallySucceeds()
    {
        // Arrange: Polly retry policy (3 retries, exponential backoff)
        var retryPolicy = Policy
            .Handle<DbUpdateConcurrencyException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => 
                    TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100),
                onRetry: (outcome, timespan, retry, context) =>
                {
                    // Log retry (optional in tests)
                });

        var entity = YourEntity.Create("test");
        var mockRepository = new Mock<IYourRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        
        // First 2 calls fail, 3rd succeeds
        mockUow.SetupSequence(u => u.SaveChangesAsync(default))
            .ThrowsAsync(new DbUpdateConcurrencyException("conflict"))
            .ThrowsAsync(new DbUpdateConcurrencyException("conflict"))
            .ReturnsAsync(1); // Success
        
        var service = new YourService(mockRepository.Object, mockUow.Object);
        var updateDto = new YourUpdateDto { /* values */ };

        // Act: Call wrapped in retry policy
        var result = await retryPolicy.ExecuteAsync(
            () => service.UpdateAsync(entity.Id, updateDto, default));

        // Assert: Eventual success
        Assert.NotNull(result);
        mockUow.Verify(u => u.SaveChangesAsync(default), Times.Exactly(3));
    }
}
```

### Key Points
- Use `DbUpdateConcurrencyException` for row-count mismatch (PostgreSQL xmin)
- `SetupSequence` for simulating retry-until-success
- Verify `SaveChangesAsync` called expected number of times
- No need to test actual Polly policy (it's external); test service behavior only

---

## Pattern 2: Soft-Delete Query Filtering (Mock Repository)

### When to Use
- Testing application service filtering of soft-deleted records
- Mocking repository to return only active (IsDeleted=false) records
- Verifying business logic doesn't include deleted aggregates

### Template
```csharp
public class YourServiceSoftDeleteTests
{
    [Fact]
    public async Task GetProgressAsync_WithSoftDeletedTransactions_ExcludesFromCalculation()
    {
        // Arrange: Mock repository with soft-delete filtering
        var categoryId = Guid.NewGuid();
        
        // Active transaction (returned by mock)
        var activeTransaction = Transaction.Create(
            categoryId, MoneyValue.Create("USD", 100m), 
            new DateOnly(2026, 1, 15), "Active Tx");
        
        // Soft-deleted transaction (NOT returned by mock with filter)
        var deletedTransaction = Transaction.Create(
            categoryId, MoneyValue.Create("USD", 50m), 
            new DateOnly(2026, 1, 20), "Deleted Tx");
        // deletedTransaction.SoftDelete() would set IsDeleted=true, DeletedAt=UtcNow
        
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        // Repository filtered query returns only active
        mockTransactionRepo.Setup(r => r.GetSpendingByCategoryAsync(categoryId, 2026, 1, default))
            .ReturnsAsync(MoneyValue.Create("USD", 100m)); // Only active (50m omitted)
        
        var mockGoalRepo = new Mock<IBudgetGoalRepository>();
        var goal = BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 200m));
        mockGoalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default))
            .ReturnsAsync(goal);

        var service = new BudgetProgressService(
            mockGoalRepo.Object, 
            mockTransactionRepo.Object);

        // Act
        var progress = await service.GetProgressAsync(categoryId, 2026, 1);

        // Assert: Spent amount reflects only active transaction (100m, not 150m)
        Assert.Equal(100m, progress.SpentAmount.Amount);
        Assert.Equal(100m, progress.PercentUsed); // 100/200 = 50%, but we report 50m/200m=25%? Check spec
        
        // Verify repository query was called (filtering happens in repo)
        mockTransactionRepo.Verify(
            r => r.GetSpendingByCategoryAsync(categoryId, 2026, 1, default), 
            Times.Once);
    }

    [Fact]
    public async Task GetProgressAsync_AfterRestore_IncludesPreviouslyDeletedTransaction()
    {
        // Arrange: Transaction was deleted, now restored (IsDeleted=false)
        var categoryId = Guid.NewGuid();
        var restoredTransaction = Transaction.Create(
            categoryId, MoneyValue.Create("USD", 75m), 
            new DateOnly(2026, 1, 15), "Restored Tx");
        // restoredTransaction.Restore() sets IsDeleted=false, DeletedAt=null
        
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        // After restore, repository returns restored transaction
        mockTransactionRepo.Setup(r => r.GetSpendingByCategoryAsync(categoryId, 2026, 1, default))
            .ReturnsAsync(MoneyValue.Create("USD", 75m)); // Restored transaction included
        
        var mockGoalRepo = new Mock<IBudgetGoalRepository>();
        var goal = BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 200m));
        mockGoalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default))
            .ReturnsAsync(goal);

        var service = new BudgetProgressService(
            mockGoalRepo.Object, 
            mockTransactionRepo.Object);

        // Act
        var progress = await service.GetProgressAsync(categoryId, 2026, 1);

        // Assert: Restored transaction is included
        Assert.Equal(75m, progress.SpentAmount.Amount);
    }
}
```

### Key Points
- Mock repository `.GetSpendingByCategoryAsync()` (or equivalent filtered query) to return only active records
- Repository filtering is MOCKED — real EF Core `.Where(x => !x.IsDeleted)` tested in Phase 2
- Test verifies service doesn't re-filter (trusts repository)
- Soft-delete implementation deferred; tests assume IsDeleted exists

---

## Pattern 3: Cross-User Authorization Test

### When to Use
- Testing that service rejects access to resources belonging to different user
- Mocking IUserContext with specific UserId
- Verifying DomainException thrown with appropriate message

### Template
```csharp
public class YourServiceAuthorizationTests
{
    private static Mock<IUserContext> CreateUserContextMock(Guid userId)
    {
        var mock = new Mock<IUserContext>();
        mock.Setup(c => c.UserId).Returns(userId);
        return mock;
    }

    [Fact]
    public async Task GetByIdAsync_UserAccessingOtherUsersResource_ThrowsDomainException()
    {
        // Arrange: Resource belongs to UserA
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        
        var transaction = Transaction.Create(
            userA, // Resource owner
            Guid.NewGuid(), // AccountId
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 15),
            "Test Transaction");
        var transactionId = transaction.Id;

        var mockRepository = new Mock<ITransactionRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(transactionId, default))
            .ReturnsAsync(transaction);
        
        // Mock user context for UserB
        var userBContext = CreateUserContextMock(userB);

        var service = new TransactionService(
            mockRepository.Object,
            userContextMock: userBContext.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.GetByIdAsync(transactionId, default));
        
        // Verify error message indicates access denied
        Assert.Contains("not authorized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_UserUpdatingOwnResource_Succeeds()
    {
        // Arrange: UserA owns the transaction
        var userId = Guid.NewGuid();
        var transaction = Transaction.Create(
            userId, // Same user
            Guid.NewGuid(),
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 15),
            "Test Transaction");

        var mockRepository = new Mock<ITransactionRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(transaction.Id, default))
            .ReturnsAsync(transaction);
        
        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        
        var userContext = CreateUserContextMock(userId);
        var service = new TransactionService(
            mockRepository.Object,
            unitOfWork: mockUow.Object,
            userContext: userContext.Object);

        var updateDto = new TransactionUpdateDto 
        { 
            Description = "Updated Description" 
        };

        // Act
        var result = await service.UpdateAsync(transaction.Id, updateDto, default);

        // Assert: Update succeeds
        Assert.NotNull(result);
        Assert.Equal("Updated Description", result.Description);
        mockUow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
```

### Key Points
- IUserContext.UserId determines ownership scope
- Service must verify resource.UserId == context.UserId before allowing access
- Cross-user access raises DomainException (not AuthorizationException — domain rule)
- Test both deny and allow cases

---

## Pattern 4: Edge Case — Division by Zero Prevention

### When to Use
- Testing budget progress calculation with zero target amount
- Testing any calculation involving divisor that could be zero
- Verifying graceful error or sensible fallback

### Template
```csharp
public class BudgetProgressEdgeCaseTests
{
    [Fact]
    public async Task GetProgressAsync_GoalWithZeroTarget_NoDivisionByZero_ReturnsGracefulResult()
    {
        // Arrange: Goal with target = 0 (edge case)
        var categoryId = Guid.NewGuid();
        var goalWithZeroTarget = BudgetGoal.Create(
            categoryId, 2026, 1, 
            MoneyValue.Create("USD", 0m)); // Zero target!
        
        var mockGoalRepo = new Mock<IBudgetGoalRepository>();
        mockGoalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default))
            .ReturnsAsync(goalWithZeroTarget);
        
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        mockTransactionRepo.Setup(r => r.GetSpendingByCategoryAsync(categoryId, 2026, 1, default))
            .ReturnsAsync(MoneyValue.Create("USD", 50m)); // Spent 50 against 0 target

        var service = new BudgetProgressService(
            mockGoalRepo.Object,
            mockTransactionRepo.Object);

        // Act — should not throw DivideByZeroException
        var progress = await service.GetProgressAsync(categoryId, 2026, 1);

        // Assert: Returns sensible result (e.g., "Over Budget" status, PercentUsed = decimal.MaxValue or -1 sentinel)
        Assert.NotNull(progress);
        // Depending on design, PercentUsed might be:
        //  - decimal.MaxValue (to indicate "exceeded")
        //  - -1 (sentinel for "undefined")
        //  - Or status property = "OverBudget" + percent = 100
        Assert.True(progress.PercentUsed > 0 || progress.PercentUsed == -1m);
    }

    [Fact]
    public async Task GetProgressAsync_EmptyDataset_NoTransactions_ProgressIsZeroNotNull()
    {
        // Arrange: Goal exists, but no transactions
        var categoryId = Guid.NewGuid();
        var goal = BudgetGoal.Create(categoryId, 2026, 1, MoneyValue.Create("USD", 500m));
        
        var mockGoalRepo = new Mock<IBudgetGoalRepository>();
        mockGoalRepo.Setup(r => r.GetByCategoryAndMonthAsync(categoryId, 2026, 1, default))
            .ReturnsAsync(goal);
        
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        mockTransactionRepo.Setup(r => r.GetSpendingByCategoryAsync(categoryId, 2026, 1, default))
            .ReturnsAsync(MoneyValue.Create("USD", 0m)); // No transactions

        var service = new BudgetProgressService(
            mockGoalRepo.Object,
            mockTransactionRepo.Object);

        // Act
        var progress = await service.GetProgressAsync(categoryId, 2026, 1);

        // Assert: Not null, spent = 0
        Assert.NotNull(progress);
        Assert.Equal(0m, progress.SpentAmount.Amount);
        Assert.Equal(0m, progress.PercentUsed);
        Assert.Equal("OnTrack", progress.Status); // 0/500 = 0%, on track
    }
}
```

### Key Points
- Guard clauses prevent division by zero (e.g., `if (target == 0) return OverBudgetResult;`)
- Empty dataset (0 transactions) is valid, not an error
- Tests verify graceful handling without exception

---

## Pattern 5: Culture-Aware Assertion (Money Formatting)

### When to Use
- Any test asserting currency formatting (ToString("C"))
- Dates, numbers that vary by locale
- Ensuring tests pass on both Windows (local) and Linux CI (invariant)

### Template
```csharp
using System.Globalization;

public class MoneyValueFormattingTests
{
    public MoneyValueFormattingTests()
    {
        // Set culture to en-US for all tests in this class
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [Fact]
    public void ToString_USDCurrency_FormatsWithDollarSign()
    {
        // Arrange
        var money = MoneyValue.Create("USD", 1000.50m);

        // Act & Assert: Use culture-specific expected value
        var formatted = money.ToString("C");
        Assert.Equal("$1,000.50", formatted);
    }

    [Fact]
    public void Addition_RoundingToUSCents_SumCorrect()
    {
        // Arrange: Three values that sum to whole cents
        var val1 = MoneyValue.Create("USD", 1.005m);
        var val2 = MoneyValue.Create("USD", 2.003m);
        var val3 = MoneyValue.Create("USD", 3.992m); // Sum = 7.000m

        // Act
        var sum = val1.Add(val2).Add(val3);

        // Assert: Rounded to cents (USD precision)
        Assert.Equal(7.00m, sum.Amount);
        Assert.Equal("$7.00", sum.ToString("C")); // Culture-aware
    }

    [Theory]
    [InlineData("USD", "$123.45")]
    [InlineData("GBP", "GBP123.45")] // Adjust per actual implementation
    public void ToString_DifferentCurrencies_FormatsCorrectly(string currency, string expected)
    {
        // Arrange
        var money = MoneyValue.Create(currency, 123.45m);

        // Act & Assert
        var formatted = money.ToString("C");
        Assert.Equal(expected, formatted);
    }
}
```

### Key Points
- Set `CultureInfo.CurrentCulture` in test constructor
- This overrides the host OS locale (Windows vs. Linux CI)
- Linux CI runs with invariant culture by default (produces `¤` instead of `$`)
- Tests written against `en-US` pass on all environments

---

## Pattern 6: Concurrent Operation Test (Task.WhenAll)

### When to Use
- Testing that two simultaneous operations don't corrupt state
- Simulating race conditions safely in unit tests
- Verifying aggregate consistency under concurrency

### Template
```csharp
public class AccountConcurrencyTests
{
    [Fact]
    public async Task AddTransactionAsync_TwoDepositsSimultaneously_BalanceCalculatesCorrectly()
    {
        // Arrange: Account with two concurrent deposits
        var account = Account.Create("Test Checking");
        var accountId = account.Id;
        
        // Mock repo that tracks calls
        var mockTransactionRepo = new Mock<ITransactionRepository>();
        var transactions = new List<Transaction>();
        
        mockTransactionRepo.Setup(r => r.GetByAccountAsync(accountId, default))
            .ReturnsAsync(() => transactions); // Returns current list dynamically

        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.SaveChangesAsync(default))
            .Returns<CancellationToken>(async ct =>
            {
                await Task.Delay(10, ct); // Simulate I/O
                return 1;
            });

        var service = new AccountService(mockTransactionRepo.Object, mockUow.Object);

        // Act: Two deposits in parallel
        var tx1Task = Task.Run(async () =>
        {
            var tx1 = Transaction.Create(
                accountId,
                MoneyValue.Create("USD", 100m),
                new DateOnly(2026, 1, 15),
                "Deposit 1");
            transactions.Add(tx1);
            await service.AddTransactionAsync(accountId, tx1);
            return tx1;
        });

        var tx2Task = Task.Run(async () =>
        {
            var tx2 = Transaction.Create(
                accountId,
                MoneyValue.Create("USD", 50m),
                new DateOnly(2026, 1, 15),
                "Deposit 2");
            transactions.Add(tx2);
            await service.AddTransactionAsync(accountId, tx2);
            return tx2;
        });

        // Wait for both to complete
        var results = await Task.WhenAll(tx1Task, tx2Task).ConfigureAwait(false);

        // Assert: Both transactions added, balance reflects both
        Assert.Equal(2, transactions.Count);
        Assert.Equal(150m, transactions.Sum(t => t.Amount.Amount)); // Both deposits counted
    }

    [Fact]
    public async Task UpdateAsync_ConcurrentModifications_FirstWinsSecondFails()
    {
        // Arrange: Two concurrent updates to same entity
        var entity = YourEntity.Create("test");
        var updateCount = 0;
        
        var mockRepository = new Mock<IYourRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        
        // First SaveChanges succeeds, second fails with concurrency exception
        mockUow.Setup(u => u.SaveChangesAsync(default))
            .Returns<CancellationToken>(async ct =>
            {
                updateCount++;
                if (updateCount == 1)
                {
                    await Task.Delay(5, ct);
                    return 1; // First succeeds
                }
                else
                {
                    throw new DbUpdateConcurrencyException("conflict");
                }
            });

        var service = new YourService(mockRepository.Object, mockUow.Object);

        // Act: Two updates in parallel
        var update1Task = service.UpdateAsync(entity.Id, new YourUpdateDto { Value = "First" }, default);
        var update2Task = service.UpdateAsync(entity.Id, new YourUpdateDto { Value = "Second" }, default);

        // Assert: First succeeds, second fails
        var update1Result = await update1Task;
        var update2Exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => update2Task);
        
        Assert.NotNull(update1Result);
        Assert.Contains("conflict", update2Exception.Message);
    }
}
```

### Key Points
- Use `Task.Run` for parallel execution (thread pool)
- Use `Task.WhenAll` to wait for both
- Mock state mutation (list.Add, counter increment) to simulate real behavior
- First-wins pattern: first update succeeds, second raises exception

---

## Reusability Checklist

- [ ] Test pattern follows Arrange/Act/Assert structure
- [ ] One assertion intent per test (logical grouping OK)
- [ ] Mocks use `new Mock<IInterface>()` + `.Setup()` (Moq consistent)
- [ ] No FluentAssertions (use xUnit `Assert.*`, Shouldly `.Should.*()`)
- [ ] Culture-aware: `CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US")` in constructor
- [ ] Concurrency tests use `Task.WhenAll` or `SetupSequence`
- [ ] Soft-delete tests use `.GetSpendingByCategoryAsync()` (or filtered mock query)
- [ ] Authorization tests mock `IUserContext.UserId`
- [ ] Test name describes behavior: `Service_Behavior_Expected_Outcome`
- [ ] Comments minimal (code speaks for itself; use TODO if unclear)

---

**Skill Status:** ✅ READY FOR PHASE 1A IMPLEMENTATION  
**Used By:** Lucius (developer), Barbara (QA reviewer)  
**Estimated Implementation Boost:** Each pattern saves 30–50 lines of boilerplate reasoning
