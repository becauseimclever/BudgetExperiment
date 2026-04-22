# Phase 1B Daily Review Template

**Date:** [YYYY-MM-DD]  
**Reviewer:** Barbara (Tester)  
**Tests Reviewed:** [Test IDs, e.g., B.1, B.2, B.3]  
**Owner:** [Tim/Lucius/Cassandra]  

---

## Tests Reviewed Today

| Test ID | Test Name | Status | Verdict | Issues Found |
|---------|-----------|--------|---------|--------------|
| [ID] | [Name] | [🟢 Pass / ⚠️ Violation] | [PASS/FAIL] | [Description or "None"] |

---

## Vic's Guardrails Quality Check

For each test, validate:

### ✅ AAA Pattern (Arrange/Act/Assert)
- [ ] Clear section separation (comments or blank lines between sections)
- [ ] Arrange: All test data created before Act
- [ ] Act: Single method call or operation under test
- [ ] Assert: Verification of behavior (not nested logic)

**Violations found:** [List test IDs with issues, or "None"]

---

### ✅ Culture-Aware Setup
- [ ] Tests with currency formatting use `CultureInfo.GetCultureInfo("en-US")` in constructor
- [ ] Tests with number formatting use `CultureInfo.GetCultureInfo("en-US")` in constructor
- [ ] Tests with date formatting use explicit format strings or culture setup

**Violations found:** [List test IDs with issues, or "None"]

---

### ✅ Single Assertion Intent
- [ ] Each test proves ONE behavior (logical grouping allowed, e.g., verify transaction updated AND UOW.SaveChanges called)
- [ ] No tests with 5+ unrelated assertions (indicates multiple behaviors tested)
- [ ] Test name reveals the single assertion intent

**Violations found:** [List test IDs with issues, or "None"]

---

### ✅ No Trivial Assertions
- [ ] No tests with ONLY `Assert.NotNull(service)` (must assert behavior)
- [ ] No tests that would pass on a blank implementation
- [ ] Assertions verify actual behavior, not just object creation

**Violations found:** [List test IDs with issues, or "None"]

---

### ✅ Guard Clauses Over Nested Conditionals
- [ ] Test logic prefers early returns over nested `if` statements
- [ ] Arrange section uses guard clauses for setup validation
- [ ] Act section has no nested conditionals (should be single method call)

**Violations found:** [List test IDs with issues, or "None"]

---

### ✅ Moq Mocks with `.Verifiable()`
- [ ] Mock setups include `.Verifiable()` where method call verification is critical
- [ ] Tests call `mockRepository.Verify()` after Act to prove method was called
- [ ] Mock setups use `It.IsAny<T>()` or specific values appropriately

**Violations found:** [List test IDs with issues, or "None"]

---

### ✅ No Banned Libraries
- [ ] No FluentAssertions (`Should().Be()`, `Should().NotBeNull()`)
- [ ] No AutoFixture (`fixture.Create<T>()`)
- [ ] Uses xUnit `Assert` or Shouldly only

**Violations found:** [List test IDs with issues, or "None"]

---

### ✅ Descriptive Test Names
- [ ] Test name reveals behavior intent (not generic like `Test1`, `TestService`)
- [ ] Test name follows pattern: `MethodName_Scenario_ExpectedBehavior`
- [ ] Test name includes key edge case or condition being tested

**Violations found:** [List test IDs with issues, or "None"]

---

### ✅ No Skipped Tests
- [ ] No `[Skip]` attribute
- [ ] No `[Ignore]` attribute
- [ ] No `Skip="reason"` parameter in `[Fact]`/`[Theory]`

**Violations found:** [List test IDs with issues, or "None"]

---

### ✅ No Commented-Out Code
- [ ] No commented-out test code (use TODO with date/owner if temporary)
- [ ] No commented-out assertions
- [ ] No dead code that should be deleted

**Violations found:** [List test IDs with issues, or "None"]

---

## Mutation Testing Perspective

**Would these tests catch mutations?**

For each test, ask:
- If I deleted a guard clause in production code, would this test fail?
- If I inverted a boolean condition, would this test fail?
- If I removed a repository call, would this test fail?
- If I changed a calculation (e.g., `+` to `-`), would this test fail?

**Tests that would NOT catch mutations:** [List test IDs, or "None"]

**Recommended improvements:** [Suggest how to make tests more mutation-resistant]

---

## Coverage Delta Estimate

**Tests reviewed today:** [X tests]  
**Estimated coverage gain:** [+Y%]  
**Service(s) affected:** [BudgetProgressService, CategorySuggestionService, etc.]  
**Cumulative Phase 1B coverage (estimated):** [Current %]  

**On track for 60%+ target?** [Yes / No / At Risk]

**If at risk, recommended actions:** [List high-impact tests to prioritize]

---

## Actions Required

### Tests Requiring Fixes (Violations)

| Test ID | Issue | Fix Required | Owner | Deadline |
|---------|-------|--------------|-------|----------|
| [ID] | [Description] | [What needs to change] | [Tim/Lucius/Cassandra] | [Date] |

**Example:**
| B.2 | 5 assertions (should be 3) | Remove trivial `Assert.NotNull` calls | Tim | 2026-01-12 |

---

### Tests Approved (Ready to Merge)

| Test ID | Test Name | PR# | Merge Status |
|---------|-----------|-----|--------------|
| [ID] | [Name] | [PR link] | [Approved / Merged] |

**Example:**
| B.1 | `GetSuggestionsAsync_NoHistoricalData_ReturnsEmptyList` | PR #245 | ✅ Approved |

---

## Notes & Observations

**General feedback for team:**
- [Any patterns noticed across multiple tests]
- [Positive reinforcement for excellent test design]
- [Common pitfalls to avoid in remaining tests]

**Phase 1B trajectory insights:**
- [Are we on track for 60%+ coverage?]
- [Which services need more focus?]
- [Any blockers emerging?]

---

## Follow-Up Items

- [ ] [Action item 1]
- [ ] [Action item 2]
- [ ] [Action item 3]

**Next review date:** [YYYY-MM-DD]

---

**Barbara's Verdict:** [APPROVED / NEEDS REVISION / BLOCKED]

**Summary:** [1-2 sentence summary of today's review findings]

---

**Status:** [✅ COMPLETE / ⏳ IN PROGRESS / ⚠️ ISSUES FOUND]
