# Financial Accuracy Audit Framework

> Reference specification for accuracy testing. Maintained by Alfred (Lead).

## 1. Precision Standard

| Rule | Enforcement |
|------|-------------|
| All monetary values use `decimal` | `MoneyValue` wraps `decimal` — no `float`/`double` anywhere in the money path |
| 2 decimal places, `MidpointRounding.AwayFromZero` | `MoneyValue.Create()` normalises every amount on construction |
| Currency mismatch is a hard error | `operator +` / `operator -` throw `DomainException` on mixed currencies |
| No implicit conversions | `MoneyValue` is an immutable sealed record; no widening to `double` |

**What "accurate" means:** For every computation involving money, the result must be identical to hand-calculating the same operation with `decimal` arithmetic rounded to 2 places. No cumulative drift. No floating-point surprises.

---

## 2. Financial Invariants

Each invariant below is a property that must hold **at all times**, regardless of how the system arrived at the current state.

### INV-1: Account Balance Identity

```
∀ account A:
  Balance(A) = A.InitialBalance.Amount + Σ(t.Amount.Amount) for t ∈ A.Transactions
```

Applies after any create/update/delete of transactions, including imports and auto-realizations.

### INV-2: Transfer Net-Zero

```
∀ transfer T (identified by TransferId):
  Let S = source transaction, D = destination transaction
  S.Amount.Amount + D.Amount.Amount = 0
  S.Amount.Currency = D.Amount.Currency
  S.TransferId = D.TransferId
  S.Date = D.Date
```

A transfer moves money; it never creates or destroys it. The combined balance of all accounts is unchanged.

### INV-3: MoneyValue Arithmetic Closure

```
∀ a, b : MoneyValue where a.Currency = b.Currency:
  (a + b).Amount = Round(a.Amount + b.Amount, 2, AwayFromZero)
  (a - b).Amount = Round(a.Amount - b.Amount, 2, AwayFromZero)
  (a + b).Currency = a.Currency
```

Mixed-currency operations are rejected, not silently coerced.

### INV-4: Budget Progress Consistency

```
∀ BudgetProgress P:
  P.RemainingAmount = P.TargetAmount - P.SpentAmount
  P.PercentUsed = Round((P.SpentAmount / P.TargetAmount) × 100, 0, AwayFromZero)
    (when TargetAmount > 0; 0 otherwise)
  P.Status = f(P.PercentUsed)  // OnTrack < 80, Warning ∈ [80,100), OverBudget ≥ 100
```

### INV-5: Paycheck Allocation Conservation

```
∀ PaycheckAllocationSummary S:
  S.TotalPerPaycheck.Amount = Σ(a.AmountPerPaycheck.Amount) for a ∈ S.Allocations
  S.TotalAnnualBills.Amount = Σ(a.AnnualAmount.Amount) for a ∈ S.Allocations

  When PaycheckAmount is provided:
    S.RemainingPerPaycheck + S.Shortfall + S.TotalPerPaycheck = S.PaycheckAmount
    (Remaining or Shortfall is zero — never both positive)
```

No money is created or lost during allocation arithmetic.

### INV-6: Paycheck Per-Bill Calculation Identity

```
∀ PaycheckAllocationValue V:
  V.AnnualAmount.Amount = V.Bill.Amount.Amount × AnnualMultiplier(V.Bill.Frequency)
  V.AmountPerPaycheck.Amount = Round(V.AnnualAmount.Amount / PeriodsPerYear(paycheckFreq), 2, AwayFromZero)
```

### INV-7: Recurring Projection No-Double-Count

```
∀ recurring transaction R, date range [from, to]:
  If a realized transaction T exists for occurrence date D:
    The projected instance for D must NOT appear in projection output
  Projected count + Realized count for R in [from, to] = Expected occurrence count
```

Auto-realization converts a projected instance into a real transaction. The same charge must never be counted twice.

### INV-8: Kakeibo Category Assignment Completeness

```
∀ BudgetCategory C where C.Type = Expense:
  C.KakeiboCategory ∈ {Essentials, Wants, Culture, Unexpected} ∨ C.KakeiboCategory = null

∀ BudgetCategory C where C.Type ∈ {Income, Transfer}:
  C.KakeiboCategory = null  // enforced by domain — SetKakeiboCategory throws
```

When Kakeibo grouping is active, every expense-category transaction maps to exactly one bucket. Income/Transfer are excluded.

### INV-9: Report Aggregate Consistency

```
∀ MonthlyCategoryReport R for month M:
  R.TotalExpenses.Amount = Σ(category.Total.Amount) for expense categories
  R.TotalIncome.Amount = Σ(category.Total.Amount) for income categories
  Each category total = Σ(transaction amounts) for transactions in that category during M
```

### INV-10: Reconciliation Integrity

```
∀ ReconciliationMatch M:
  M.ConfidenceScore ∈ [0, 1]
  M.ConfidenceLevel = f(M.ConfidenceScore)  // High ≥ 0.85, Medium ≥ 0.60, Low < 0.60
  A transaction can be linked to at most one recurring instance (no many-to-many double counting)
```

---

## 3. Test Categories

### CAT-1: Domain Arithmetic (`Domain.Tests`)

Tests that `MoneyValue` operations are exact.

| Test Pattern | What It Proves |
|---|---|
| `MoneyValue_Add_SameCurrency_SumsExactly` | INV-3 |
| `MoneyValue_Add_DifferentCurrency_Throws` | INV-3 |
| `MoneyValue_Subtract_ProducesExactDifference` | INV-3 |
| `MoneyValue_Create_RoundsToTwoDecimalsAwayFromZero` | Precision standard |
| `MoneyValue_HundredPennies_SumToOneDollar` | No cumulative drift |
| `MoneyValue_Abs_And_Negate_PreserveAmount` | INV-3 |

**Status: ✅ Covered** — `MoneyValueTests.cs` covers all of the above.

### CAT-2: Account Balance (`Domain.Tests`)

Tests that the balance identity (INV-1) holds under all mutations.

| Test Pattern | What It Proves |
|---|---|
| `AccountBalance_NewAccount_EqualsInitialBalance` | INV-1 |
| `AccountBalance_AfterDebit_ReducesByExactAmount` | INV-1 |
| `AccountBalance_AfterCredit_IncreasesByExactAmount` | INV-1 |
| `AccountBalance_AfterMultipleTransactions_EqualsInitialPlusSum` | INV-1 |
| `AccountBalance_DebitAndMatchingCredit_NetZero` | INV-1 |
| `AccountBalance_UpdateAmount_ReflectsNewValue` | INV-1 |
| `AccountBalance_RemoveTransaction_RestoresBalance` | INV-1 |
| `AccountBalance_ManySmallAmounts_NoDecimalDrift` | INV-1 + precision |

**Status: ✅ Covered** — `AccountBalanceAccuracyTests.cs` covers all of the above.

### CAT-3: Transfer Symmetry (`Application.Tests/Accuracy`)

Tests that transfers are net-zero (INV-2).

| Test Pattern | What It Proves |
|---|---|
| `Transfer_SourceAndDestination_SumToZero` | INV-2 |
| `Transfer_SourceHasNegativeAmount` | INV-2 |
| `Transfer_DestinationHasPositiveAmount` | INV-2 |
| `Transfer_NetBalanceAcrossAccounts_Unchanged` | INV-2 |
| `Transfer_BothTransactions_MatchCurrency` | INV-2 |
| `Transfer_BothTransactions_ShareTransferId` | INV-2 |
| `Transfer_SameAccount_Rejected` | INV-2 guard |

**Status: ✅ Covered** — `TransferNetZeroAccuracyTests.cs` covers all of the above.

### CAT-4: Balance Calculation Service (`Application.Tests/Accuracy`)

Tests the service-layer balance computation (INV-1 at service level).

| Test Pattern | What It Proves |
|---|---|
| `BalanceAsOfDate_NoAccounts_ReturnsZero` | INV-1 |
| `BalanceAsOfDate_TransactionsOnDate_Included` | INV-1 boundary |
| `BalanceAsOfDate_TransactionsAfterDate_Excluded` | INV-1 boundary |
| `BalanceAsOfDate_MultipleAccounts_SumsCorrectly` | INV-1 aggregation |
| `BalanceAsOfDate_ManySmallTransactions_Exact` | INV-1 + precision |
| `BalanceAsOfDate_AccountStartsAfterDate_ContributesZero` | INV-1 boundary |

**Status: ✅ Covered** — `BalanceCalculationAccuracyTests.cs`.

### CAT-5: Budget Progress (`Domain.Tests`)

Tests BudgetProgress arithmetic (INV-4).

| Test Pattern | What It Proves |
|---|---|
| `BudgetProgress_Remaining_IsTargetMinusSpent` | INV-4 |
| `BudgetProgress_PercentUsed_CalculatedCorrectly` | INV-4 |
| `BudgetProgress_StatusThresholds_Correct` | INV-4 |
| `BudgetProgress_ZeroTarget_PercentIsZero` | INV-4 edge |
| `BudgetProgress_NoBudgetSet_StatusCorrect` | INV-4 edge |

**Status: ✅ Covered** — `BudgetProgressTests.cs`.

### CAT-6: Paycheck Allocation (`Domain.Tests`)

Tests allocation calculator arithmetic (INV-5, INV-6).

| Test Pattern | What It Proves |
|---|---|
| `Allocation_MonthlyBill_BiweeklyPaycheck_Correct` | INV-6 |
| `Allocation_AllFrequencyCombinations_AnnualMultipliersCorrect` | INV-6 |
| `AllocationSummary_TotalPerPaycheck_EqualsSumOfAllocations` | INV-5 |
| `AllocationSummary_RemainingPlusShortfallPlusTotal_EqualsPaycheck` | INV-5 |
| `AllocationSummary_InsufficientIncome_WarnsCorrectly` | INV-5 guard |
| `AllocationSummary_CannotReconcile_WhenBillsExceedIncome` | INV-5 guard |

**Status: ⚠️ Partially Covered** — `PaycheckAllocationCalculatorTests.cs` covers INV-6 and most of INV-5. **Gap:** No explicit test that `RemainingPerPaycheck + Shortfall + TotalPerPaycheck = PaycheckAmount` as a single assertion (conservation law).

### CAT-7: Recurring Projection Accuracy (`Application.Tests`)

Tests that projected and realized instances don't double-count (INV-7).

| Test Pattern | What It Proves |
|---|---|
| `Projection_SkippedException_ExcludesInstance` | INV-7 |
| `Projection_InactiveRecurring_ExcludesAll` | INV-7 |
| `Projection_RealizationReplacesProjection_NoDuplicates` | INV-7 |
| `AutoRealize_CreatesTransaction_AdvancesNextOccurrence` | INV-7 |
| `AutoRealize_AlreadyRealized_DoesNotDuplicate` | INV-7 |

**Status: ⚠️ Partially Covered** — `RecurringInstanceProjectorTests.cs` and `AutoRealizeServiceTests.cs` exist. **Gap:** No explicit accuracy test asserting `projected_count + realized_count = expected_occurrences` for a date range. No cross-cutting test that verifies a realized instance is excluded from the projector output for the same date.

### CAT-8: Kakeibo Allocation (`Domain.Tests`)

Tests Kakeibo bucket assignment rules (INV-8).

| Test Pattern | What It Proves |
|---|---|
| `KakeiboCategory_OnExpense_AcceptsAllBuckets` | INV-8 |
| `KakeiboCategory_OnIncome_ThrowsDomainException` | INV-8 |
| `KakeiboCategory_OnTransfer_ThrowsDomainException` | INV-8 |

**Status: ⚠️ Partially Covered** — `BudgetCategoryTests.cs` tests the domain enforcement. **Gap:** No test verifying that when Kakeibo grouping is active in reports, every expense transaction maps to exactly one bucket with no orphans and no duplicates.

### CAT-9: Report Aggregate Accuracy (`Application.Tests`)

Tests that report totals equal the sum of their parts (INV-9).

| Test Pattern | What It Proves |
|---|---|
| `MonthlyCategoryReport_TotalExpenses_EqualsSumOfExpenseCategories` | INV-9 |
| `MonthlyCategoryReport_TotalIncome_EqualsSumOfIncomeCategories` | INV-9 |
| `MonthlyCategoryReport_CategoryTotal_EqualsSumOfTransactions` | INV-9 |
| `MonthlyCategoryReport_AllTransactionsAccountedFor_NoneOrphaned` | INV-9 |

**Status: ❌ Gap** — `ReportServiceTests.cs` exists and tests report generation, but no dedicated accuracy tests asserting the sum-of-parts identity.

### CAT-10: Reconciliation Integrity (`Domain.Tests`)

Tests reconciliation match constraints (INV-10).

| Test Pattern | What It Proves |
|---|---|
| `ReconciliationMatch_ConfidenceScore_BoundsEnforced` | INV-10 |
| `ReconciliationMatch_ConfidenceLevel_MatchesScore` | INV-10 |
| `ReconciliationMatch_ResolvedMatch_CannotBeModified` | INV-10 |

**Status: ✅ Covered** — `ReconciliationMatchTests.cs`.

---

## 4. Test Naming Convention

```
[Category]_[Scenario]_[ExpectedOutcome]
```

Examples:
- `AccountBalance_AfterDebitAndCredit_NetChangeIsExact`
- `Transfer_SourceAndDestinationAmounts_SumToZero`
- `PaycheckAllocation_ConservationLaw_RemainingPlusShortfallPlusTotalEqualsPaycheck`
- `RecurringProjection_RealizedAndProjected_NoDuplicatesInDateRange`

Accuracy tests use the `Accuracy` folder or filename suffix `AccuracyTests.cs`.

---

## 5. Coverage Ownership

| Invariant | Test Project | Test File / Folder |
|---|---|---|
| INV-1 (Balance Identity) | `Domain.Tests` | `AccountBalanceAccuracyTests.cs` |
| INV-2 (Transfer Net-Zero) | `Application.Tests` | `Accuracy/TransferNetZeroAccuracyTests.cs` |
| INV-3 (MoneyValue Arithmetic) | `Domain.Tests` | `MoneyValueTests.cs` |
| INV-4 (Budget Progress) | `Domain.Tests` | `BudgetProgressTests.cs` |
| INV-5 (Paycheck Conservation) | `Domain.Tests` | `PaycheckAllocationCalculatorTests.cs` |
| INV-6 (Per-Bill Calculation) | `Domain.Tests` | `PaycheckAllocationCalculatorTests.cs` |
| INV-7 (Recurring No-Double-Count) | `Application.Tests` | `Accuracy/` (new) |
| INV-8 (Kakeibo Assignment) | `Domain.Tests` + `Application.Tests` | `BudgetCategoryTests.cs` + `Accuracy/` (new for report grouping) |
| INV-9 (Report Aggregates) | `Application.Tests` | `Accuracy/` (new) |
| INV-10 (Reconciliation Integrity) | `Domain.Tests` | `ReconciliationMatchTests.cs` |

---

## 6. Gap Analysis Summary

### Already Tested (solid coverage)
- ✅ `MoneyValue` arithmetic — precision, rounding, currency enforcement
- ✅ Account balance identity — all mutation scenarios
- ✅ Transfer net-zero — source/destination symmetry
- ✅ Balance calculation service — date boundaries, multi-account aggregation
- ✅ Budget progress — remaining/percent/status calculations
- ✅ Reconciliation match — confidence scoring, state machine
- ✅ Paycheck allocation — per-bill and summary calculations

### Gaps to Fill (Barbara should prioritize)

| Priority | Gap | Invariant | Where |
|---|---|---|---|
| **P1** | Paycheck conservation law: explicit assertion that `Remaining + Shortfall + TotalPerPaycheck = PaycheckAmount` | INV-5 | `Domain.Tests` |
| **P1** | Recurring projection + realization no-double-count: cross-cutting test proving projected + realized = expected for a date range | INV-7 | `Application.Tests/Accuracy/` |
| **P2** | Report aggregate identity: sum-of-categories = total, sum-of-transactions = category total | INV-9 | `Application.Tests/Accuracy/` |
| **P2** | Kakeibo report grouping: every expense maps to exactly one bucket when grouped | INV-8 | `Application.Tests/Accuracy/` |
| **P3** | Auto-realize + projector interaction: realized instance no longer appears in projection | INV-7 | `Application.Tests/Accuracy/` |

---

## 7. Proof Strategy

For each invariant, the testing approach is:

1. **Property**: State the mathematical identity.
2. **Unit test (domain)**: Prove the identity holds for the domain object in isolation using deterministic inputs.
3. **Service test (application)**: Prove the identity holds through the service layer with mocked repositories.
4. **Integration test (infrastructure)**: Prove the identity holds end-to-end with real PostgreSQL via Testcontainers (existing API/Infrastructure test projects).
5. **Regression**: Any reported accuracy bug becomes a named test case before the fix.

The domain layer is the primary fortress. If `MoneyValue`, `Account`, `BudgetProgress`, and `PaycheckAllocationCalculator` are provably correct, the service and API layers can only break accuracy by incorrectly filtering, aggregating, or double-counting — which the service-level accuracy tests catch.
