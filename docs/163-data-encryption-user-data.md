# Feature 128: Data Encryption for User Financial Data

**Status:** `Planning`  
**Priority:** Medium  
**Complexity:** Medium

## Problem Statement

Budget Experiment stores sensitive financial data in PostgreSQL without encryption at rest. While authentication and authorization protect access, **database-level encryption would add a critical defense-in-depth layer** for financial data privacy and compliance. A database breach, backup leak, or compromised snapshot could expose:

- Account names and balances
- Transaction descriptions (merchant names, purchase details)
- Personal notes (chat messages, Kakeibo reflections, monthly goals)
- User settings (currency preferences, time zones)
- Category names and budget goals
- Recurring patterns and spending habits

As a **self-hosted financial application**, users trust Budget Experiment with their complete financial history. Encryption at rest would ensure that even with physical database access, data remains protected.

### Current State

- **No encryption at rest** — all user data is stored in plaintext in PostgreSQL
- **Authentication via Authentik (OIDC)** — access control only; data readable in database
- **HTTPS in transit** — data encrypted over the wire, but not in storage
- **Docker deployment on Raspberry Pi** — physical access to SD card = full data access
- **Database backups unencrypted** — PostgreSQL dumps contain plaintext financial data
- **Multi-user support** — OwnerUserId/CreatedByUserId scope filtering isolates user data logically, but not cryptographically

### Target State

- **Sensitive columns encrypted at rest** — account balances, transaction descriptions, personal notes protected
- **Transparent to application logic** — encryption/decryption handled automatically via EF Core value converters or PostgreSQL pgcrypto
- **Key management strategy** — secure key storage (user secrets, environment variables, or Azure Key Vault for future cloud deployments)
- **Searchable where necessary** — balance on queries that require filtering/sorting encrypted fields
- **Migration path** — encrypt existing data without downtime (background migration + dual-read support)
- **Backup encryption** — PostgreSQL dumps protected via transparent encryption or post-dump encryption
- **Compliance-ready** — GDPR "right to erasure", potential PCI-DSS relevance if payment data added

---

## Strategic Context

Budget Experiment positions itself as a **privacy-first, self-hosted alternative** to cloud budget trackers like YNAB or Mint. While we avoid third-party cloud services, **we currently lack one critical privacy feature: encryption at rest**.

### User Trust & Positioning

- README emphasizes: *"Self-hoster who wants full control over your financial data — no third-party cloud"*
- Encryption strengthens the value proposition: **your data is not only yours, it's unreadable without your keys**
- Competitive differentiation vs. cloud services: even the host cannot read your financial data

### Compliance & Future-Proofing

- **GDPR:** Encryption at rest supports Article 32 (security of processing) and "privacy by design"
- **PCI-DSS:** If we ever add payment card storage (unlikely but possible), encryption is mandatory
- **Data breach notification laws:** Encrypted data may reduce regulatory obligations in some jurisdictions

### Technical Maturity

- Application architecture is **clean and modular** — encryption can be added at Infrastructure layer without touching Domain/Application
- **EF Core 10 value converters** provide a clean mechanism for transparent encryption
- **PostgreSQL pgcrypto extension** offers native TDE (Transparent Data Encryption) alternative

---

## Goals

- 🔒 **Protect sensitive financial data at rest** — account balances, transaction descriptions, personal notes
- 🎯 **Transparent to application logic** — Domain/Application layers unaware of encryption (handled in Infrastructure)
- 🔑 **Secure key management** — keys stored securely outside database, rotatable without full re-encryption
- ⚖️ **Balance security vs. performance** — encrypt sensitive columns only, preserve query performance where needed
- 🚀 **Zero-downtime migration** — encrypt existing production data without service interruption
- 📦 **Backup encryption** — ensure PostgreSQL dumps are protected
- 🧪 **Test coverage** — encryption/decryption verified via integration tests with real PostgreSQL

---

## Acceptance Criteria

1. **Sensitive columns encrypted at rest:**
   - `Accounts.Name` — account names encrypted
   - `Transactions.Description` — merchant names, purchase details encrypted
   - `Transactions.Amount` (currency + decimal) — balances encrypted (note: query impact)
   - `ChatMessages.Content` — personal financial conversations encrypted
   - `MonthlyReflections.Reflection` — Kakeibo monthly reflections encrypted
   - `KaizenGoals.Description` — improvement goal descriptions encrypted
   - `CategorizationRules.MerchantPattern` — learned merchant patterns encrypted
   
2. **Non-encrypted columns (query-critical or low-sensitivity):**
   - `Transactions.Date` — required for date range queries, filtering, calendar grid
   - `Transactions.CategoryId` — foreign key join performance
   - `Accounts.Type` — enum, not sensitive
   - `BudgetCategories.Name` — category names (debatable, start unencrypted)
   - `UserSettings.PreferredCurrency`, `UserSettings.TimeZoneId` — functional, low sensitivity

3. **Key management implemented:**
   - Master encryption key stored in **user secrets** (local development)
   - Environment variable override (`ENCRYPTION_MASTER_KEY`) for Docker deployment
   - Key rotation strategy documented (with re-encryption process)
   - Per-user key derivation explored (phase 2 enhancement — use master key to derive user-specific keys)

4. **Encryption mechanism chosen and implemented:**
   - **Option A (preferred):** EF Core value converters with AES-256-GCM encryption
   - **Option B:** PostgreSQL pgcrypto extension (TDE at database level)
   - **Option C:** Hybrid (TDE for tables + column-level for high-sensitivity fields)

5. **Migration strategy:**
   - EF Core migration adds encrypted columns (parallel to existing)
   - Background job encrypts existing data
   - Application reads from encrypted columns when present, falls back to plaintext
   - Cutover: switch to encrypted-only reads, drop plaintext columns

6. **Performance validated:**
   - Calendar grid load time (30 days of transactions) < 500ms (with encrypted descriptions)
   - Transaction list filtering (date range + category) performance acceptable
   - Benchmark report comparing encrypted vs. plaintext query performance

7. **Backup encryption:**
   - `pg_dump` output encrypted via `gpg` or `openssl` in backup scripts
   - Restore process documented with decryption step

8. **Documentation & compliance:**
   - Encryption architecture documented in `docs/SECURITY-ENCRYPTION.md`
   - Key management, rotation, and backup procedures documented
   - GDPR compliance notes updated (encryption at rest satisfies Article 32)

---

## Scope

### In Scope

- Encrypt high-sensitivity columns (account names, transaction descriptions, amounts, chat messages, reflections, goals)
- EF Core value converter implementation (AES-256-GCM)
- Master key storage via user secrets / environment variables
- Migration strategy for existing data (dual-column approach)
- Integration tests with encrypted data (real PostgreSQL via Testcontainers)
- Performance benchmarks (before/after encryption)
- Backup encryption documentation

### Out of Scope (Future Enhancements)

- **Per-user encryption keys** — Phase 2 (derive keys from master key + userId hash)
- **Searchable encryption** — homomorphic encryption or tokenization for queries (complex, low ROI)
- **Azure Key Vault integration** — Phase 3 (cloud key management for future Azure deployments)
- **Full-disk encryption** — OS-level, outside application scope (recommend in deployment docs)
- **Database connection encryption** — separate concern, already handled by `sslmode=require` in production
- **Audit logging of key access** — Phase 2 (log when encryption keys are loaded/used)

---

## Technical Design

### Architecture: Column-Level Encryption via EF Core Value Converters

**Why Value Converters?**
- Clean separation: Domain entities remain POCO, encryption lives in Infrastructure
- Transparent to Application/Domain layers (no `EncryptedString` leaking into business logic)
- Testable: can unit test converters independently
- EF Core native: no third-party encryption libraries

**Why Not PostgreSQL pgcrypto?**
- Harder to test (requires PostgreSQL-specific SQL in integration tests)
- Less portable (ties us to PostgreSQL, though we're already committed)
- Key management still needed (pgcrypto functions require keys)
- Value converters give us more control (e.g., per-user key derivation later)

### Encryption Algorithm: AES-256-GCM

- **Algorithm:** AES-256 (Advanced Encryption Standard)
- **Mode:** GCM (Galois/Counter Mode) — provides both encryption and authentication (AEAD)
- **Key Size:** 256 bits (32 bytes)
- **IV/Nonce:** Random 12-byte nonce per encryption operation (stored with ciphertext)
- **Tag:** 16-byte authentication tag (prevents tampering)

**Storage Format (Base64-encoded):**
```
[IV:12 bytes][Ciphertext:variable][Tag:16 bytes]
```

**Rationale:**
- GCM mode prevents tampering (authenticated encryption)
- Random IV per operation ensures identical plaintext → different ciphertext
- Base64 encoding allows storage in `TEXT` columns (PostgreSQL-compatible)

### Domain Model (No Changes)

Domain entities remain **unchanged** — encryption is Infrastructure concern only.

```csharp
// Domain entity — no encryption awareness
public sealed class Transaction
{
    public string Description { get; private set; } = string.Empty;
    public MoneyValue Amount { get; private set; } = null!;
    // ...
}
```

### Infrastructure: EF Core Value Converters

**New Class: `AesGcmEncryptionConverter`**

```csharp
// src/BudgetExperiment.Infrastructure/Encryption/AesGcmEncryptionConverter.cs
public class AesGcmEncryptionConverter : ValueConverter<string, string>
{
    private readonly byte[] _key;

    public AesGcmEncryptionConverter(byte[] encryptionKey)
        : base(
            plaintext => Encrypt(plaintext, encryptionKey),
            ciphertext => Decrypt(ciphertext, encryptionKey))
    {
        _key = encryptionKey;
    }

    private static string Encrypt(string plaintext, byte[] key)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;

        byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        RandomNumberGenerator.Fill(nonce);

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Combine: [nonce][ciphertext][tag]
        byte[] combined = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length + ciphertext.Length, tag.Length);

        return Convert.ToBase64String(combined);
    }

    private static string Decrypt(string ciphertext, byte[] key)
    {
        if (string.IsNullOrEmpty(ciphertext)) return ciphertext;

        byte[] combined = Convert.FromBase64String(ciphertext);

        byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
        byte[] ciphertextBytes = new byte[combined.Length - nonce.Length - tag.Length];

        Buffer.BlockCopy(combined, 0, nonce, 0, nonce.Length);
        Buffer.BlockCopy(combined, nonce.Length, ciphertextBytes, 0, ciphertextBytes.Length);
        Buffer.BlockCopy(combined, nonce.Length + ciphertextBytes.Length, tag, 0, tag.Length);

        byte[] plaintext = new byte[ciphertextBytes.Length];

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aes.Decrypt(nonce, ciphertextBytes, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
```

**EF Core Configuration:**

```csharp
// TransactionConfiguration.cs
public void Configure(EntityTypeBuilder<Transaction> builder)
{
    // Existing configuration...

    // Encrypt Description column
    builder.Property(t => t.Description)
        .HasConversion(new AesGcmEncryptionConverter(_encryptionKey))
        .HasMaxLength(1000); // Encrypted length ~1.5x plaintext (Base64 overhead + IV + tag)

    // Encrypt Amount.Amount (decimal)
    builder.OwnsOne(t => t.Amount, money =>
    {
        money.Property(m => m.Amount)
            .HasConversion(
                v => EncryptDecimal(v, _encryptionKey),
                v => DecryptDecimal(v, _encryptionKey))
            .HasColumnName("Amount");
    });
}

private static string EncryptDecimal(decimal value, byte[] key)
{
    return AesGcmEncryptionConverter.Encrypt(value.ToString(CultureInfo.InvariantCulture), key);
}

private static decimal DecryptDecimal(string encrypted, byte[] key)
{
    string decrypted = AesGcmEncryptionConverter.Decrypt(encrypted, key);
    return decimal.Parse(decrypted, CultureInfo.InvariantCulture);
}
```

### Key Management

**Phase 1: Master Key in User Secrets / Environment Variables**

```bash
# Local development (user secrets)
dotnet user-secrets set "Encryption:MasterKey" "BASE64_ENCODED_32_BYTE_KEY" --project src/BudgetExperiment.Api

# Docker deployment (.env file, never committed)
ENCRYPTION_MASTER_KEY=BASE64_ENCODED_32_BYTE_KEY
```

**Key Generation Script:**

```csharp
// src/BudgetExperiment.Infrastructure/Encryption/KeyGenerator.cs
public static class KeyGenerator
{
    public static string GenerateMasterKey()
    {
        byte[] key = new byte[32]; // 256 bits
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}
```

**Loading Key in DI:**

```csharp
// Infrastructure.DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    string? masterKeyBase64 = configuration["Encryption:MasterKey"];
    if (string.IsNullOrEmpty(masterKeyBase64))
    {
        throw new InvalidOperationException(
            "Encryption master key not configured. " +
            "Set 'Encryption:MasterKey' in user secrets or environment variables.");
    }

    byte[] masterKey = Convert.FromBase64String(masterKeyBase64);

    // Register key as singleton (in-memory only, never persisted)
    services.AddSingleton(new EncryptionKeyProvider(masterKey));

    // Configure DbContext to use encryption
    services.AddDbContext<BudgetDbContext>((sp, options) =>
    {
        var keyProvider = sp.GetRequiredService<EncryptionKeyProvider>();
        options.UseNpgsql(connectionString)
            .UseEncryption(keyProvider.MasterKey); // Custom extension method
    });

    return services;
}
```

### Migration Strategy (Zero-Downtime)

**Phase 1: Add Encrypted Columns (Parallel)**

```bash
dotnet ef migrations add Feature128_AddEncryptedColumns --project src/BudgetExperiment.Infrastructure --startup-project src/BudgetExperiment.Api
```

Migration adds:
- `Transactions.DescriptionEncrypted` (TEXT, nullable)
- `Transactions.AmountEncrypted` (TEXT, nullable)
- `Accounts.NameEncrypted` (TEXT, nullable)
- etc.

**Phase 2: Background Encryption Job**

```csharp
// BackgroundEncryptionService.cs (hosted service)
public async Task EncryptExistingDataAsync(CancellationToken cancellationToken)
{
    // Batch process: read plaintext, encrypt, write to *Encrypted columns
    var transactions = await _context.Transactions
        .Where(t => t.DescriptionEncrypted == null)
        .Take(1000)
        .ToListAsync(cancellationToken);

    foreach (var tx in transactions)
    {
        tx.DescriptionEncrypted = Encrypt(tx.Description);
        tx.AmountEncrypted = EncryptDecimal(tx.Amount.Amount);
    }

    await _context.SaveChangesAsync(cancellationToken);
}
```

**Phase 3: Cutover (Switch Reads to Encrypted Columns)**

Update EF configuration to read from `DescriptionEncrypted`, fallback to `Description` if null:

```csharp
// Dual-read support during migration
builder.Property(t => t.Description)
    .HasComputedColumnSql("COALESCE(DescriptionEncrypted, Description)", stored: false);
```

**Phase 4: Drop Plaintext Columns**

After all data encrypted, drop `Description`, `Amount` plaintext columns in final migration.

---

## Encryption Options Comparison

| Approach | Pros | Cons | Recommendation |
|----------|------|------|----------------|
| **EF Core Value Converters** | Clean, testable, transparent to Domain, portable, per-column control | Requires application restart for key rotation, stored encrypted length ~1.5x plaintext | ✅ **Preferred** |
| **PostgreSQL pgcrypto (TDE)** | Database-native, no app code changes, transparent to EF | Harder to test, PostgreSQL-specific, limited key management flexibility | ❌ Phase 2 option |
| **Hybrid (TDE + column-level)** | Best of both worlds (TDE for tables, column encryption for high-sensitivity fields) | Complex setup, dual key management | 🟡 Future enhancement |
| **Application-layer pre-save encryption** | Full control, no EF magic | Leaks encryption logic into Application/Domain, harder to maintain | ❌ Violates clean architecture |

---

## Performance Considerations

### Query Impact

**Encrypted columns cannot be indexed or filtered efficiently:**
- ❌ `WHERE Description LIKE '%Amazon%'` — requires full table scan + decryption
- ❌ `ORDER BY Amount DESC` — requires decrypting all amounts, then sorting

**Mitigation strategies:**
1. **Encrypt descriptions, not dates/categories** — preserve query performance on filter columns
2. **Use full-text search on plaintext description hash** (separate column) for search
3. **Benchmark before/after** — if calendar grid slows significantly, reconsider encrypting amounts
4. **Pagination + limits** — always use `Take(100)` to avoid decrypting entire table

### Benchmark Targets

| Scenario | Before (Plaintext) | After (Encrypted) | Acceptance |
|----------|-------------------|-------------------|------------|
| **Calendar Grid (30 days, ~100 txns)** | < 200ms | < 500ms | ✅ < 500ms |
| **Transaction List (filter by date range)** | < 150ms | < 300ms | ✅ < 300ms |
| **Transaction Create (single)** | < 50ms | < 100ms | ✅ < 100ms |
| **Bulk Import (1000 txns)** | < 5s | < 10s | ✅ < 10s |

**If performance degrades beyond acceptance:** 
- Remove `Amount` encryption (keep descriptions only)
- Use indexed plaintext hash for search (encrypted column for display only)

---

## Dependencies & Blockers

- **None** — encryption is additive, no dependencies on other features
- **Key management decision** — user secrets (local) vs. environment variable (Docker) vs. Azure Key Vault (future)
- **Performance testing** — requires Feature 113 (dedicated performance environment) for accurate benchmarks (optional, can use local profiling)

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| **Key loss = permanent data loss** | Document backup strategy: store master key in password manager (1Password, Bitwarden) + print backup in safe |
| **Performance degradation on queries** | Encrypt selectively (descriptions, not amounts initially); benchmark early; fallback to description-only encryption |
| **Migration failure (corrupt data)** | Dual-column approach: keep plaintext during migration, verify encrypted data before cutover |
| **Backup leaks still expose data** | Encrypt `pg_dump` output with GPG; document restore process with decryption |
| **Key rotation complexity** | Phase 1: document manual re-encryption process; Phase 2: automate key rotation with dual-key support |
| **EF Core query translation issues** | Test extensively with Testcontainers (real PostgreSQL); avoid complex LINQ on encrypted columns |
| **Compliance misunderstanding** | Encryption at rest is NOT a substitute for access control — still need authentication/authorization |

---

## Testing Strategy

### Unit Tests

- [ ] `AesGcmEncryptionConverter` — encrypt/decrypt round-trip (ASCII, Unicode, empty string, null)
- [ ] `EncryptionKeyProvider` — key loading from configuration, validation (32 bytes)
- [ ] Key generation utility — generates valid Base64 32-byte keys

### Integration Tests (Testcontainers + Real PostgreSQL)

- [ ] Create encrypted transaction, read back decrypted description
- [ ] Update encrypted transaction description, verify re-encryption
- [ ] Query by date range (non-encrypted column) with encrypted descriptions in result
- [ ] Bulk import 1000 encrypted transactions, verify all decrypt correctly
- [ ] Migration test: dual-column read (encrypted + fallback to plaintext)

### Performance Tests (NBomber)

- [ ] Calendar grid load time (30 days, 100 txns) — before/after encryption
- [ ] Transaction list filter (date range, category) — before/after encryption
- [ ] Transaction create (single) — before/after encryption
- [ ] Bulk import (1000 txns) — before/after encryption

### Manual Testing

- [ ] Generate master key, configure in user secrets, restart API, verify app works
- [ ] Create account + transactions in encrypted mode, verify data readable
- [ ] Query encrypted data via pgAdmin (verify Base64 ciphertext in database)
- [ ] Backup database (`pg_dump`), restore, verify decryption works
- [ ] Simulate key loss: delete master key, restart API, verify graceful error (not crash)

---

## Migration Notes

### Database Migration

```bash
# Phase 1: Add encrypted columns (parallel)
dotnet ef migrations add Feature128_AddEncryptedColumns --project src/BudgetExperiment.Infrastructure --startup-project src/BudgetExperiment.Api

# Phase 2: Encrypt existing data (background job, no migration)
# (Run BackgroundEncryptionService as hosted service)

# Phase 3: Cutover (update EF config to read encrypted, drop plaintext)
dotnet ef migrations add Feature128_CutoverToEncrypted --project src/BudgetExperiment.Infrastructure --startup-project src/BudgetExperiment.Api

# Phase 4: Drop plaintext columns
dotnet ef migrations add Feature128_DropPlaintextColumns --project src/BudgetExperiment.Infrastructure --startup-project src/BudgetExperiment.Api
```

### Breaking Changes

**None** — encryption is backward-compatible during migration (dual-column read). After full cutover, backups from pre-encryption versions cannot be restored without migration.

---

## Security Considerations

### Threat Model

**Threats Mitigated:**
- ✅ **Database dump leak** — stolen PostgreSQL dump file is unreadable without master key
- ✅ **Backup theft** — backups encrypted via GPG before off-site storage
- ✅ **Physical access to Raspberry Pi** — attacker with SD card cannot read financial data
- ✅ **Insider threat (DBA)** — even with database access, data is encrypted

**Threats NOT Mitigated:**
- ❌ **Compromised API server** — if attacker gains API access, they have decrypted data (encryption key in memory)
- ❌ **SQL injection** — encryption does not prevent SQL injection; parameterized queries still required
- ❌ **Backup key leak** — if master key leaks alongside backup, encryption is defeated
- ❌ **Memory dump attack** — encryption key in API process memory (mitigated by OS-level protections)

### Key Management Best Practices

1. **Never commit master key to Git** — use `.gitignore` for `.env`, user secrets only
2. **Rotate keys periodically** — document annual key rotation process (Phase 2: automate)
3. **Backup master key securely** — store in password manager (1Password, Bitwarden) + paper backup in safe
4. **Separate key per environment** — dev/staging/production use different master keys
5. **Audit key access** — log when encryption key is loaded (Phase 2 feature)

---

## Performance Considerations

### Encrypted vs. Plaintext Storage

**Storage Overhead:**
- **Plaintext:** `VARCHAR(500)` = 500 bytes max
- **Encrypted (Base64):** `TEXT` = ~750 bytes (500 * 1.33 Base64 overhead + 12 IV + 16 tag)
- **Impact:** ~50% storage increase for encrypted columns

**Query Performance:**
- **Reads:** Decryption overhead ~10-20µs per row (negligible for <1000 rows)
- **Writes:** Encryption overhead ~20-30µs per row (negligible for typical batch sizes)
- **Filtering:** ❌ Cannot filter on encrypted columns efficiently (requires full table scan + decryption)

**Optimization:**
- Keep **date, categoryId, accountId unencrypted** for query performance
- **Paginate aggressively** — `Take(100)` to avoid decrypting thousands of rows
- **Consider search indexing** — if full-text search needed, use plaintext hash column + encrypted display column

---

## Future Enhancements

### Phase 2: Per-User Encryption Keys

**Goal:** Derive unique encryption key per user (master key + userId hash) → prevents cross-user data leaks even if one user key compromised.

**Approach:**
```csharp
byte[] userKey = HKDF.DeriveKey(masterKey, userId.ToString(), 32);
```

**Impact:** Requires storing `OwnerUserId` in plaintext for key derivation, value converter gets userId from DbContext.

### Phase 3: Azure Key Vault Integration

**Goal:** Store master key in Azure Key Vault for cloud deployments.

**Approach:**
```csharp
var client = new SecretClient(vaultUri, new DefaultAzureCredential());
KeyVaultSecret secret = await client.GetSecretAsync("EncryptionMasterKey");
byte[] masterKey = Convert.FromBase64String(secret.Value);
```

### Phase 4: Searchable Encryption (Tokenization)

**Goal:** Enable `WHERE Description LIKE '%Amazon%'` on encrypted data.

**Approach:** Store plaintext description hash or tokens in separate indexed column:
- `Transactions.DescriptionEncrypted` (AES-GCM, display only)
- `Transactions.DescriptionTokens` (plaintext tokens for search: `["amazon", "purchase"]`)

**Trade-off:** Leaks some information (presence of keywords), but enables search.

---

## References

- [NIST Special Publication 800-175B](https://csrc.nist.gov/publications/detail/sp/800-175b/final) — Key Management Guidelines
- [OWASP Cryptographic Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html)
- [EF Core Value Converters](https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions)
- [PostgreSQL pgcrypto Extension](https://www.postgresql.org/docs/current/pgcrypto.html)
- [AES-GCM in .NET](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm)
- [GDPR Article 32: Security of Processing](https://gdpr-info.eu/art-32-gdpr/)
- [Feature 113: Dedicated Performance Environment](./113-dedicated-performance-environment.md) — for encryption performance benchmarks
- [Feature 160: Pluggable AI Backend](./160-pluggable-ai-backend.md) — similar config-driven strategy pattern

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-10 | Initial draft — investigation phase complete | Alfred (via Copilot) |
