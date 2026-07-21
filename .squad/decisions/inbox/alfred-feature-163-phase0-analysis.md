# Phase 0 Analysis: Feature 163 - Data Encryption for User Financial Data

**Date:** 2026-01-10  
**Analyst:** Alfred (Lead Architect)  
**Requestor:** Fortinbra  
**Feature Document:** `docs/163-data-encryption-user-data.md`

---

## Executive Summary

Feature 163 proposes AES-256-GCM encryption at rest for sensitive financial data via EF Core value converters. **Feasibility: 8/10**. Technical approach is sound and aligns with Clean Architecture. Key risks: query performance degradation on encrypted columns and operational key management complexity. Recommend **Phase 1 MVP: Description + ChatMessages encryption only** (defer Amount encryption pending performance validation). Estimated complexity: **Medium-High** (comparable to Feature 161 Phase 1-2 combined). Requires Lucius (Infrastructure), Barbara (testing), and Vic (security audit).

---

## 1. Feasibility Assessment

### 1.1 Technical Approach: EF Core Value Converters vs. PostgreSQL pgcrypto

**Recommendation: ✅ EF Core Value Converters (Option A)**

**Rationale:**
- ✅ **Clean Architecture alignment:** Encryption logic encapsulated in Infrastructure layer; Domain remains POCO (no `EncryptedString` leakage)
- ✅ **Testability:** Value converters are unit-testable independently; integration tests use real PostgreSQL (Testcontainers)
- ✅ **Portability:** Not PostgreSQL-specific (though we're committed to Postgres, future cloud migrations benefit)
- ✅ **Per-column control:** Can selectively encrypt sensitive columns (e.g., Description, ChatMessages) without encrypting query-critical columns (Date, CategoryId)
- ✅ **Existing patterns:** Similar to owned-type configurations (e.g., `MoneyValue` in `TransactionConfiguration.cs`)
- ⚠️ **Key rotation complexity:** Requires application restart and dual-column migration strategy (acceptable trade-off for Phase 1)

**Why NOT pgcrypto:**
- ❌ Harder to test (requires PostgreSQL-specific SQL in integration tests)
- ❌ Key management still required (pgcrypto functions need encryption keys passed as parameters)
- ❌ Less control for future enhancements (e.g., per-user key derivation requires application-level logic anyway)
- 🟡 Native performance advantage minimal (AES-GCM overhead ~10-20µs per row vs. pgcrypto ~5-10µs — negligible at <1000 rows)

**Implementation Notes:**
- Use `AesGcm` class from `System.Security.Cryptography` (available in .NET 10)
- Store as Base64-encoded TEXT: `[IV:12 bytes][Ciphertext:variable][Tag:16 bytes]`
- Implement as singleton `AesGcmEncryptionConverter` in `Infrastructure/Encryption/`
- Register encryption key provider in `DependencyInjection.cs` (loaded from config)

**Feasibility Score: 9/10** — Well-understood pattern, no blockers.

---

### 1.2 Migration Path: Zero-Downtime Encryption

**Proposed Strategy: Dual-Column Parallel Approach**

**Phase 1: Add Encrypted Columns (Nullable)**
```sql
ALTER TABLE Transactions ADD COLUMN DescriptionEncrypted TEXT NULL;
ALTER TABLE ChatMessages ADD COLUMN ContentEncrypted TEXT NULL;
-- etc.
```

**Phase 2: Background Encryption Job**
- Hosted service (`BackgroundEncryptionService`) encrypts existing plaintext → writes to `*Encrypted` columns
- Batch processing: 1000 rows per iteration, resumable on restart
- Progress tracking: `EncryptionMigrationStatus` table (records `{TableName, LastProcessedId, CompletedAt}`)

**Phase 3: Cutover (Dual-Read Support)**
```csharp
// EF configuration reads encrypted column first, falls back to plaintext if null
builder.Property(t => t.Description)
    .HasComputedColumnSql("COALESCE(DescriptionEncrypted, Description)", stored: false);
```

**Phase 4: Drop Plaintext Columns (After 100% Migration)**
```sql
ALTER TABLE Transactions DROP COLUMN Description; -- Plaintext column removed
ALTER TABLE Transactions RENAME COLUMN DescriptionEncrypted TO Description; -- Encrypted column becomes primary
```

**Can we achieve zero-downtime?**
- ✅ **Yes, if dual-read strategy implemented correctly**
- ⚠️ **Risk:** Background job must complete before Phase 4 migration (monitor via admin endpoint)
- ⚠️ **Rollback:** Keep plaintext columns for 1 release cycle (safety net if decryption issues discovered)

**Feasibility Score: 7/10** — Dual-column approach is standard practice, but requires careful orchestration. Risk: incomplete migration blocks Phase 4.

---

### 1.3 Performance Impact: Which Columns Affect Queries Most?

**Critical Analysis:**

| Column | Current Queries | Encrypted? | Query Impact | Mitigation |
|--------|-----------------|------------|--------------|------------|
| **Transactions.Date** | Calendar grid filters, date range queries | ❌ NO | N/A | Always keep plaintext |
| **Transactions.CategoryId** | Filtering, grouping (reports) | ❌ NO | N/A | Always keep plaintext |
| **Transactions.AccountId** | Scoped queries (multi-user isolation) | ❌ NO | N/A | Always keep plaintext |
| **Transactions.Description** | Display only (Calendar, lists) | ✅ YES (Phase 1) | **Low** — No filtering/sorting on description in hot paths | Safe to encrypt |
| **Transactions.Amount** | Balance calculations, sorting (reports) | 🟡 DEFER | **HIGH** — Reconciliation queries, balance aggregations require decryption | Phase 2+ only |
| **ChatMessages.Content** | Display only (chat history) | ✅ YES (Phase 1) | **Low** — No search/filtering (sequential read) | Safe to encrypt |
| **Accounts.Name** | Display only (dropdowns) | ✅ YES (Phase 1) | **Low** — < 20 accounts per user, no filtering | Safe to encrypt |
| **MonthlyReflections.Reflection** | Display only (Kakeibo journal) | ✅ YES (Phase 1) | **Low** — One record per month, no queries | Safe to encrypt |

**Most Impactful Decision: Should we encrypt `Transactions.Amount`?**

**❌ NO — Defer to Phase 2 (after performance benchmarks)**

**Rationale:**
1. **Reconciliation queries** (`ReconciliationService.GetUnreconciledTransactionsAsync`) filter by date range and calculate balances — encrypting Amount forces full-table decryption for `SUM(Amount)` aggregations
2. **Calendar grid** displays transaction amounts (currently indexed queries) — encryption breaks PostgreSQL query planner optimizations
3. **Bulk import** (1000 txns) writes encrypted amounts — acceptable overhead (~20-30µs per row = +30ms total), but reads suffer
4. **Risk:** If calendar grid load time degrades from 200ms → 800ms, user experience suffers (acceptance threshold: <500ms)

**Phase 1 MVP Decision:**
- ✅ **Encrypt:** Description, ChatMessages.Content, Accounts.Name, MonthlyReflections.Reflection, KaizenGoals.Description
- ❌ **Defer:** Amount (until Feature 113 performance environment available for rigorous benchmarking)

**Feasibility Score: 8/10** — Low-risk columns identified; high-risk columns deferred.

---

### 1.4 Key Management: User Secrets + Environment Variables Secure Enough?

**Proposed Strategy:**
```bash
# Local development
dotnet user-secrets set "Encryption:MasterKey" "BASE64_ENCODED_32_BYTE_KEY" --project src/BudgetExperiment.Api

# Docker deployment (.env file, gitignored)
ENCRYPTION_MASTER_KEY=BASE64_ENCODED_32_BYTE_KEY
```

**Security Assessment:**

| Threat | Mitigated? | Rationale |
|--------|------------|-----------|
| **Database dump leak** | ✅ YES | Encrypted data unreadable without master key |
| **Backup theft** | ✅ YES (with GPG) | `pg_dump` output encrypted via GPG before off-site storage |
| **Physical Pi access** | ✅ YES | Attacker with SD card cannot read financial data (key in env, not on disk) |
| **Insider threat (DBA)** | ✅ YES | Even with PostgreSQL access, data is encrypted |
| **Compromised API server** | ❌ NO | Attacker with API access has key in memory (accepts runtime risk) |
| **Key loss** | ⚠️ CRITICAL | Permanent data loss — requires backup strategy |

**Is user-secrets + env vars secure enough for self-hosted?**

**✅ YES, with caveats:**

1. **Acceptable for self-hosted single-user deployment** (primary use case: Raspberry Pi home server)
2. **NOT acceptable for multi-tenant cloud** (would require Azure Key Vault / AWS KMS — Phase 3 enhancement)
3. **Critical mitigation:** Documented backup strategy
   - Store master key in password manager (1Password, Bitwarden)
   - Print backup in safe (offline disaster recovery)
   - Test restore process quarterly

**Missing Guardrails (ADD TO IMPLEMENTATION):**
- ❌ No key rotation process documented (manual re-encryption required in Phase 1)
- ❌ No key validation on startup (invalid Base64 crashes app — add validation + graceful error)
- ❌ No audit logging of key access (Phase 2 feature — log when `EncryptionKeyProvider` initialized)

**Feasibility Score: 7/10** — Secure enough for stated use case (self-hosted), but operational complexity high (key backup, rotation, loss = permanent data loss).

---

## 2. Scope Decomposition

### 2.1 Phase 1 (MVP) — Low-Risk Column Encryption

**Goal:** Encrypt display-only sensitive columns without query performance impact.

**Acceptance Criteria (Subset from Feature Doc):**

1. ✅ **Encryption infrastructure:**
   - [x] `AesGcmEncryptionConverter` class (Infrastructure/Encryption/)
   - [x] `EncryptionKeyProvider` singleton (loads key from config)
   - [x] Key generation utility (`dotnet run --project tools/GenerateEncryptionKey`)
   - [x] DI registration in `Infrastructure.DependencyInjection.AddInfrastructure()`

2. ✅ **Encrypted columns (Phase 1 MVP):**
   - [x] `Transactions.Description` — encrypted (TEXT, Base64)
   - [x] `ChatMessages.Content` — encrypted
   - [x] `Accounts.Name` — encrypted
   - [x] `MonthlyReflections.Reflection` — encrypted
   - [x] `KaizenGoals.Description` — encrypted

3. ✅ **Migration:**
   - [x] EF Core migration adds `*Encrypted` columns (nullable)
   - [x] Background job (`BackgroundEncryptionService`) encrypts existing data
   - [x] Dual-read support (read encrypted first, fallback to plaintext)
   - [x] Progress tracking table (`EncryptionMigrationStatus`)

4. ✅ **Key management:**
   - [x] Master key configurable via user secrets (local)
   - [x] Environment variable override (`ENCRYPTION_MASTER_KEY`) for Docker
   - [x] Validation: reject non-Base64 or != 32-byte keys with clear error
   - [x] Documentation: key backup strategy in `docs/SECURITY-ENCRYPTION.md`

5. ✅ **Testing:**
   - [x] Unit tests: `AesGcmEncryptionConverter` round-trip (ASCII, Unicode, empty, null)
   - [x] Integration tests: Create encrypted transaction → read decrypted (Testcontainers)
   - [x] Integration tests: Background encryption job (migrates 1000 plaintext → encrypted)
   - [x] Manual test: Query encrypted data via pgAdmin (verify Base64 ciphertext visible)

6. ✅ **Documentation:**
   - [x] `docs/SECURITY-ENCRYPTION.md` (architecture, key management, backup/restore)
   - [x] GDPR compliance notes (encryption at rest satisfies Article 32)
   - [x] Key rotation process documented (manual for Phase 1)

**Out of Scope (Phase 1):**
- ❌ `Transactions.Amount` encryption (deferred to Phase 2 pending performance validation)
- ❌ Per-user key derivation (Phase 2+ enhancement)
- ❌ Automatic key rotation (manual re-encryption required)
- ❌ Azure Key Vault integration (Phase 3)
- ❌ Searchable encryption / tokenization (Phase 3+)

**Estimated Work Items:** 18 stories (6 infrastructure, 5 migration, 4 testing, 3 documentation)

---

### 2.2 Phase 2 — Amount Encryption + Performance Validation

**Prerequisites:**
- ✅ Feature 113 (Dedicated Performance Environment) — required for accurate benchmarks
- ✅ Phase 1 deployed to production for 1+ month (stability validation)

**Scope:**
- Encrypt `Transactions.Amount` (currency + decimal)
- Performance benchmarks (before/after):
  - Calendar grid (30 days, ~100 txns): < 500ms
  - Reconciliation balance calculation: < 300ms
  - Bulk import (1000 txns): < 10s
- If benchmarks fail → rollback Amount encryption, document limitation

**Risk:** If `Amount` encryption degrades reconciliation performance unacceptably, feature may be **partially complete** (descriptions encrypted, amounts plaintext).

---

### 2.3 Phase 3+ — Advanced Features (Deferred)

- Per-user key derivation (master key + userId HKDF)
- Automatic key rotation (dual-key support during transition)
- Azure Key Vault / AWS KMS integration (cloud deployments)
- Searchable encryption (tokenization for `Description LIKE` queries)
- Audit logging (log key access, encryption/decryption events)

---

### 2.4 Discovery Work Needed Before Phase 1

**Required Spikes:**

1. ✅ **EF Core Value Converter Prototype (2-3 hours)**
   - Create simple `AesGcmEncryptionConverter` in spike branch
   - Apply to `Transactions.Description` in test DbContext
   - Verify EF Core query translation doesn't break (LINQ → SQL)
   - **Deliverable:** Proof-of-concept code snippet

2. ✅ **Key Management Configuration Test (1 hour)**
   - Load Base64 key from `IConfiguration`
   - Inject into `EncryptionKeyProvider` singleton
   - Verify key accessible in `TransactionConfiguration.Configure()`
   - **Deliverable:** DI wiring pattern confirmed

3. ✅ **Migration Dual-Read Strategy Validation (2 hours)**
   - Create test migration with `Description` + `DescriptionEncrypted` columns
   - Implement computed column fallback (`COALESCE(DescriptionEncrypted, Description)`)
   - Verify EF Core reads from computed column correctly
   - **Deliverable:** Migration pattern validated

**Total Discovery Effort:** 5-6 hours (Lucius, before Phase 1 starts)

**Blocker if spikes fail?**
- If EF Core cannot handle value converters on owned types (`MoneyValue.Amount`) → fallback to pgcrypto for Amount encryption only
- If dual-read computed columns break concurrency tokens → use application-layer fallback logic (read encrypted first, then plaintext)

---

## 3. Team & Timeline Estimate

### 3.1 Squad Member Assignments

| Role | Agent | Responsibilities | Estimated Effort |
|------|-------|------------------|------------------|
| **Infrastructure Lead** | Lucius | Value converters, DI wiring, migrations, background job | 8-10 days |
| **Test Lead** | Barbara | Unit + integration tests, coverage validation, manual testing | 6-8 days |
| **Security Auditor** | Vic | Key management review, threat model validation, compliance sign-off | 2-3 days |
| **Architecture Review** | Alfred | Design review, SOLID compliance, decision gates | 2-3 days (intermittent) |

**Why Lucius?**
- ✅ Infrastructure expert (EF Core, migrations, DbContext configuration)
- ✅ Previous experience: Feature 111 (AsNoTracking optimizations), Feature 161 (BudgetScope removal — migration experience)
- ✅ Familiar with Testcontainers (integration tests with real PostgreSQL)

**Why Barbara?**
- ✅ Test coverage enforcement (Feature 163 targets 85%+ Application, 70%+ Infrastructure)
- ✅ Integration test expertise (Testcontainers, background service testing)
- ✅ Quality gate enforcement (no coverage gaming, meaningful assertions)

**Why Vic?**
- ⚠️ **CRITICAL:** Security/compliance implications require independent audit
- ✅ Threat model validation (key management strategy, backup encryption)
- ✅ GDPR compliance review (encryption at rest satisfies Article 32)
- ✅ Key rotation + backup process documentation review

---

### 3.2 Complexity Comparison: Feature 161 vs. Feature 163

**Feature 161 (BudgetScope Removal):**
- **Phase 1:** UI slice (ScopeSwitcher removal, ScopeService locked to Shared) — **3-4 days**
- **Phase 2:** API slice (remove scope header parsing, drop DB columns) — **4-5 days**
- **Total:** ~8-9 days (Lucius 6d, Barbara 2-3d)
- **Complexity:** Low-Medium (refactoring existing code, no new infrastructure)

**Feature 163 (Data Encryption) — Phase 1 MVP:**
- **Infrastructure:** Value converters, key management, DI wiring — **5-6 days**
- **Migration:** Dual-column approach, background job, progress tracking — **3-4 days**
- **Testing:** Unit + integration + manual + performance benchmarks — **6-8 days**
- **Documentation:** Security docs, key management, GDPR compliance — **2 days**
- **Total:** ~16-20 days (Lucius 8-10d, Barbara 6-8d, Vic 2-3d)
- **Complexity:** **Medium-High** (new infrastructure, cryptography, operational key management)

**Verdict:** Feature 163 Phase 1 is **~2x complexity** of Feature 161 Phase 1-2 combined.

---

### 3.3 Timeline Estimate

**Assumptions:**
- No major blockers during discovery spikes
- PostgreSQL Testcontainers stable (intermittent failures resolved)
- Key management strategy approved by Vic before implementation starts

**Aggressive (Best Case): 3 weeks**
- Week 1: Discovery spikes (1d) + infrastructure implementation (4d)
- Week 2: Migration + background job (4d) + unit tests (2d)
- Week 3: Integration tests (3d) + Vic audit (2d) + docs (2d)

**Realistic (Expected): 4 weeks**
- Week 1: Discovery spikes (2d) + infrastructure implementation (3d)
- Week 2: Infrastructure completion (2d) + migration implementation (3d)
- Week 3: Testing (unit 2d, integration 3d)
- Week 4: Manual testing (1d) + Vic audit (2d) + docs (2d) + buffer (1d)

**Conservative (With Risks): 5-6 weeks**
- If key rotation complexity requires redesign: +1 week
- If background encryption job has performance issues: +3-5 days
- If Vic requires key management architecture changes: +1 week

**Recommended Timeline:** **4-5 weeks** (plan for realistic, buffer for conservative)

---

## 4. Risks & Blockers

### 4.1 Risk Matrix

| Risk | Severity | Probability | Mitigation | Owner |
|------|----------|-------------|------------|-------|
| **Key loss = permanent data loss** | 🔴 HIGH | Low | Documented backup strategy (password manager + paper safe), quarterly restore drills | Fortinbra (operations) |
| **Query performance degradation** | 🟡 MEDIUM | Medium | Phase 1 encrypts display-only columns; defer Amount encryption to Phase 2 | Lucius + Barbara (benchmarks) |
| **Background encryption job incomplete** | 🟡 MEDIUM | Low | Progress tracking table, admin endpoint to monitor status, resumable design | Lucius |
| **EF Core query translation breaks** | 🟠 MEDIUM-HIGH | Low | Discovery spike validates converter compatibility; fallback to pgcrypto if needed | Lucius |
| **Key rotation complexity underestimated** | 🟡 MEDIUM | Medium | Phase 1 documents manual re-encryption; automate in Phase 2+ | Alfred (scope control) |
| **Backup encryption adds operational burden** | 🟡 MEDIUM | High | Document `pg_dump | gpg` pipeline, automate in backup scripts | Lucius (docs) |
| **GDPR compliance misunderstood** | 🟢 LOW | Low | Vic audit validates compliance claims, encryption != substitute for access control | Vic |
| **Testcontainer flakiness blocks CI** | 🟠 MEDIUM-HIGH | Medium | Already known issue (Feature 161 audit noted); resolve in parallel or use SQLite for unit tests | Lucius |

---

### 4.2 Architectural Conflicts with SOLID/Clean Code?

**Assessment: ✅ NO CONFLICTS**

**Clean Architecture Compliance:**
- ✅ **Domain layer unchanged:** Entities remain POCO (`Transaction.Description` is `string`, no `EncryptedString` type)
- ✅ **Infrastructure encapsulation:** Encryption logic isolated in `Infrastructure/Encryption/` (value converters, key provider)
- ✅ **Application layer unaware:** Services work with plaintext (encryption transparent)
- ✅ **Dependency inversion respected:** `IConfiguration` → `EncryptionKeyProvider` → value converters (dependencies flow inward)

**SOLID Compliance:**
- ✅ **SRP:** `AesGcmEncryptionConverter` has one reason to change (encryption algorithm upgrade)
- ✅ **OCP:** Adding new encrypted columns extends `TransactionConfiguration`, doesn't modify existing converters
- ✅ **LSP:** Value converters are transparent substitutes (EF Core treats as `string` → `string`)
- ✅ **ISP:** No interfaces introduced (value converters are infrastructure detail)
- ✅ **DIP:** Higher layers depend on abstraction (`string` domain property), Infrastructure supplies encryption implementation

**Potential Concern:** Dual-column migration adds temporary complexity (computed columns, fallback logic) — **ACCEPTABLE** as transitional state, removed in Phase 4.

---

### 4.3 Performance Gotchas

**Encrypted Balance Queries During Reconciliation:**

**Current (Plaintext):**
```sql
SELECT SUM(Amount) FROM Transactions WHERE AccountId = @id AND Date <= @date;
-- PostgreSQL uses index on (AccountId, Date), aggregates plaintext decimals
```

**After Amount Encryption (Deferred to Phase 2):**
```sql
-- EF Core decrypts Amount via value converter AFTER query
SELECT DescriptionEncrypted, AmountEncrypted FROM Transactions WHERE AccountId = @id AND Date <= @date;
-- Application decrypts each AmountEncrypted, then sums in-memory
```

**Impact:**
- ❌ **Cannot use PostgreSQL aggregation functions** (SUM, AVG) on encrypted columns
- ⚠️ **Requires in-memory decryption + aggregation** (acceptable for <1000 rows, problematic for >10k)
- 🟡 **Calendar grid loads 30 days (~100 txns)** — minimal impact
- 🔴 **Year-end reports (12 months, ~5000 txns)** — potential 2-5x slowdown

**Mitigation:**
1. ✅ **Phase 1:** Skip Amount encryption entirely (only descriptions)
2. ✅ **Phase 2:** Benchmark Amount encryption with Feature 113 performance environment
3. 🟡 **Fallback:** If performance unacceptable, keep Amount plaintext (document as limitation)

---

## 5. Decision Points Requiring User Confirmation

### 5.1 CRITICAL: Should we encrypt `Transactions.Amount`?

**Current Feature Doc Position:** Yes (Acceptance Criteria #1)

**Alfred Recommendation:** ❌ **NO — Defer to Phase 2 (after benchmarks)**

**Reasoning:**
- Reconciliation queries aggregate balances → encryption forces in-memory decryption + sum
- Calendar grid displays amounts → acceptable (display only, <100 rows)
- Year-end reports → potentially unacceptable performance (5000+ rows decrypted in-memory)
- **Risk:** If encryption degrades reconciliation >500ms, user experience suffers

**User Confirmation Required:**
- [ ] **APPROVE:** Phase 1 encrypts Description + ChatMessages only (defer Amount to Phase 2)
- [ ] **OVERRIDE:** Encrypt Amount in Phase 1 (accept risk of performance degradation)

**If OVERRIDE chosen:** Lucius must implement performance kill-switch (feature flag to disable Amount encryption if benchmarks fail).

---

### 5.2 Per-User Key Derivation: Phase 1 or Phase 2?

**Feature Doc Position:** Phase 2+ (out of scope for MVP)

**Alfred Recommendation:** ✅ **DEFER to Phase 2**

**Reasoning:**
- Adds complexity: `EncryptionKeyProvider` must access `ClaimsPrincipal` to derive user-specific keys
- Requires `OwnerUserId` in plaintext (encryption cannot obfuscate ownership — contradicts multi-user isolation goal)
- Phase 1 MVP already complex (18 stories); adding per-user keys → +5-7 stories

**User Confirmation Required:**
- [ ] **APPROVE:** Use single master key for Phase 1 (simplest, acceptable for self-hosted single-user)
- [ ] **OVERRIDE:** Implement per-user derivation in Phase 1 (adds 1-2 weeks to timeline)

---

### 5.3 Backup Encryption Strategy: Application-Level or pgcrypto?

**Feature Doc Position:** `pg_dump | gpg` (application-level post-dump encryption)

**Alfred Recommendation:** ✅ **APPROVE `pg_dump | gpg` approach**

**Reasoning:**
- pgcrypto TDE (Transparent Data Encryption) requires PostgreSQL extensions enabled + key management at DB level
- `pg_dump | gpg` is simpler: backup scripts pipe dump through GPG before writing to file
- Restore process documented: `gpg -d backup.sql.gpg | psql`

**User Confirmation Required:**
- [ ] **APPROVE:** Use `pg_dump | gpg` for backup encryption
- [ ] **OVERRIDE:** Investigate pgcrypto TDE (adds 2-3 days for Lucius to research + implement)

---

### 5.4 Calendar Grid Performance Threshold: <500ms Acceptable?

**Feature Doc Position:** Calendar grid load time <500ms after encryption (Acceptance Criteria #6)

**Alfred Recommendation:** ✅ **APPROVE 500ms threshold**

**Reasoning:**
- Current plaintext performance: ~150-200ms (30 days, ~100 txns)
- Encryption overhead: ~10-20µs per row × 100 rows = +2ms (negligible)
- **Real overhead:** Network latency, EF Core query translation, JSON serialization (~50-100ms)
- 500ms threshold allows 2-3x buffer (conservative)

**User Confirmation Required:**
- [ ] **APPROVE:** 500ms calendar grid threshold
- [ ] **OVERRIDE:** Tighten to 300ms (requires more aggressive optimization, may block Amount encryption)

---

## 6. Open Questions for Squad Discussion

1. **Should we add encryption audit logging in Phase 1?**
   - Log when `EncryptionKeyProvider` loads key (startup)
   - Log decryption failures (tampered ciphertext)
   - **Trade-off:** Adds 2-3 days (Lucius), but improves security posture

2. **Do we need a key rotation dry-run in Phase 1?**
   - Document manual re-encryption process
   - Provide `dotnet run --project tools/RotateEncryptionKey` CLI tool (dry-run mode)
   - **Trade-off:** Adds 1-2 days (Lucius), but reduces operational risk

3. **Should background encryption job run automatically on startup or manually triggered?**
   - **Auto-run:** Simpler deployment (migration runs automatically after schema update)
   - **Manual trigger:** Safer (admin controls when encryption starts via admin endpoint)
   - **Alfred Recommendation:** Manual trigger (controlled rollout)

4. **Coverage targets for Feature 163?**
   - Infrastructure: 70%+ (current baseline)
   - Application: 85%+ (encryption service tests)
   - **Barbara's approval required** before merge

---

## 7. Recommended Next Steps

1. ⏳ **Fortinbra: Approve/reject decision points** (Section 5.1-5.4)
2. ⏳ **Vic: Security/compliance pre-review** (key management strategy, threat model)
3. ✅ **Lucius: Execute discovery spikes** (3 spikes, 5-6 hours total)
   - EF Core value converter prototype
   - Key management DI wiring test
   - Dual-read migration strategy validation
4. ✅ **Alfred: Finalize Phase 1 scope** (based on spike results + user decisions)
5. ✅ **Barbara: Prepare test plan** (unit + integration test categories, coverage targets)
6. 🚦 **GATE:** If all spikes pass + Vic approves → proceed to implementation
7. 🚦 **GATE:** If any spike fails → revisit technical approach (fallback to pgcrypto?)

---

## Appendix A: Effort Breakdown (Phase 1 MVP)

| Category | Stories | Estimated Hours | Owner |
|----------|---------|-----------------|-------|
| **Discovery Spikes** | 3 | 6h | Lucius |
| **Infrastructure** | 6 | 32-40h | Lucius |
| - Value converter implementation | 1 | 6h | Lucius |
| - Key provider + DI wiring | 1 | 4h | Lucius |
| - EF Core configuration (5 entities) | 1 | 8h | Lucius |
| - Key generation CLI tool | 1 | 3h | Lucius |
| - Validation + error handling | 1 | 5h | Lucius |
| - Admin monitoring endpoint | 1 | 6h | Lucius |
| **Migration** | 5 | 24-32h | Lucius |
| - Add encrypted columns migration | 1 | 4h | Lucius |
| - Background encryption service | 1 | 12h | Lucius |
| - Progress tracking table | 1 | 4h | Lucius |
| - Dual-read configuration | 1 | 6h | Lucius |
| - Drop plaintext columns migration | 1 | 2h | Lucius |
| **Testing** | 4 | 40-48h | Barbara |
| - Unit tests (converters, key provider) | 1 | 12h | Barbara |
| - Integration tests (Testcontainers) | 1 | 20h | Barbara |
| - Manual testing (pgAdmin, backups) | 1 | 8h | Barbara |
| - Performance benchmarks | 1 | 8h | Barbara |
| **Documentation** | 3 | 16h | Lucius + Alfred |
| - SECURITY-ENCRYPTION.md | 1 | 8h | Lucius |
| - GDPR compliance notes | 1 | 4h | Vic |
| - Key rotation process | 1 | 4h | Alfred |
| **Architecture Review** | 2 | 16h | Alfred + Vic |
| - Design review (SOLID compliance) | 1 | 8h | Alfred |
| - Security audit (key management) | 1 | 8h | Vic |
| **TOTAL** | **23 stories** | **134-142h** | **17-18 days** |

**Team Allocation:**
- Lucius: 62-78h (8-10 days)
- Barbara: 40-48h (6-8 days)
- Alfred: 8h (1 day, intermittent)
- Vic: 12h (2 days)

---

## Appendix B: Comparison with Similar Projects

**Reference:** Feature 161 (BudgetScope Removal) — similar migration complexity

| Metric | Feature 161 | Feature 163 |
|--------|-------------|-------------|
| **Phases** | 2 (UI slice, API slice) | 3+ (MVP, Amount, Advanced) |
| **Database Changes** | Drop 2 columns | Add 5-7 encrypted columns |
| **Migration Strategy** | Dual-read (2 weeks) | Dual-column (4 weeks) |
| **Testing Effort** | ~20 tests | ~40-50 tests |
| **Documentation** | 1 doc (CONTRIBUTING.md) | 3 docs (SECURITY, GDPR, key rotation) |
| **Team Size** | 2 (Lucius, Barbara) | 4 (Lucius, Barbara, Alfred, Vic) |
| **Timeline** | 2 weeks | 4-5 weeks |
| **Complexity** | Low-Medium | Medium-High |

**Verdict:** Feature 163 is **2-2.5x complexity** of Feature 161.

---

*Analysis complete. Awaiting Fortinbra decision on Section 5 decision points before proceeding to discovery spikes.*
