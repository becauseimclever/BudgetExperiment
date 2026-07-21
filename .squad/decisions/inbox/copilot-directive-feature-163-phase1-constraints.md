### 2026-04-25T23:05: Feature 163 Phase 1 Constraints
**By:** Fortinbra (via Copilot)

**Decisions Locked In:**

1. **No query-sensitive encryption in Phase 1**
   - Do NOT encrypt `Transactions.Amount` 
   - Defer to Phase 2 after performance benchmarks
   - Calendar UI performance is a hard constraint — must remain <500ms

2. **No per-user key derivation**
   - Phase 1: Single master key, applied uniformly to all user data
   - Phase 2 exploration: Per-user key derivation (if needed)

3. **Standard PostgreSQL backup encryption**
   - Use native PostgreSQL backup encryption (pgBackRest or pg_dump + native tools)
   - Do NOT implement custom backup-level encryption layer
   - Standard Postgres ops team practices

4. **Performance is non-negotiable**
   - Calendar grid (primary UI) must stay <500ms
   - No breaking changes to transaction query performance
   - Benchmark encrypted vs. unencrypted queries before Phase 2

**Phase 1 Scope Implications:**
- Encrypt: Descriptions, ChatMessages, AccountName, Reflection, KaizenGoals.Description, CategorizationRules.MerchantPattern
- Don't encrypt: Amount (Phase 2), Dates, CategoryId, enums, functional settings
- Key management: User secrets (dev) + environment variable override (Docker)

**Why:** Keeps Phase 1 focused on core encryption functionality without query performance risk. Calendar is the primary UI — responsiveness is user-critical.
