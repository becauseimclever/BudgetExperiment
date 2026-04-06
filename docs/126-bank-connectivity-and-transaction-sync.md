# Feature 126: Bank Connectivity & Automatic Transaction Sync
> **Status:** On Hold — external dependency
> **Priority:** Medium  
> **Effort:** X-Large (new external integration, OAuth flow, background sync, new domain aggregates)

## Overview

Replace the manual CSV export → import workflow with direct bank connectivity through a financial data aggregation service. Users link their bank accounts once via an OAuth flow, and the system automatically fetches new transactions on a schedule. The architecture is vendor-agnostic: a `IBankConnector` abstraction in the Domain layer allows swapping providers without touching application or UI code.

## Problem Statement

### Current State

- **CSV-only import.** Every transaction import requires the user to: (1) log into their bank's website, (2) navigate to the export/download section, (3) select a date range, (4) download a CSV file, (5) open the app's import page, (6) configure column mappings, (7) preview and execute the import. This takes 5–15 minutes per account per import session.
- **No real-time data.** Transactions are only as current as the last manual import. Users forget to import for weeks, creating date gaps (exactly the problem Feature 125a's Date Gap Detection was designed to surface).
- **Mapping friction.** Different banks produce different CSV formats. The `ImportMapping` system (Feature ~027) mitigates this, but users must still configure mappings per bank and deal with format changes when banks update their export templates.
- **Duplicate detection is heuristic.** The `ImportDuplicateDetector` uses date + amount + description similarity. Without a stable external transaction ID from the bank, duplicates can slip through or valid transactions can be incorrectly flagged.
- **No transaction updates.** Once imported via CSV, transactions are static. If the bank retroactively updates a pending transaction (e.g., a hold becomes a settled charge with a different amount), the app has no way to detect or apply the change.
- **Manual effort doesn't scale.** With 3–5 bank accounts, the user is doing 15–75 minutes of import work per week. This is the single largest friction point in the app's daily usage.

### Target State

- **One-time bank linking.** User authenticates with their bank via an OAuth/redirect flow provided by the aggregation vendor. The app stores an access token (not bank credentials).
- **Automatic sync.** New transactions are fetched on a configurable schedule (daily default, manual trigger available). The sync is idempotent — bank-provided transaction IDs prevent duplicates without heuristics.
- **Transaction updates.** Pending → settled transitions are detected via the bank's transaction ID and applied automatically (amount changes, description enrichment, date finalization).
- **Vendor-agnostic architecture.** An `IBankConnector` interface in the Domain layer, with Infrastructure adapters per vendor. Swapping from Plaid to another provider requires only a new adapter — no domain, application, or UI changes.
- **Coexists with CSV import.** Bank connectivity is additive. The existing CSV import pipeline remains fully functional for accounts or institutions not supported by the connected provider.

---

## Vendor Comparison

### Evaluation Criteria

For each vendor, assessed against this project's specific constraints:
- **Personal/self-hosted app** on a Raspberry Pi — NOT a commercial SaaS product
- **US-primary** with potential future international needs
- **.NET 10 backend** — need REST API at minimum, C# SDK preferred
- **Low account volume** — 3–10 linked accounts, single household
- **Cost sensitivity** — personal project, not VC-funded
- **Self-hosted** — must work without a public internet-facing callback URL (or support polling-only flows)

---

### 1. Plaid

**What it is:** Market-leading financial data aggregation platform. Connects to 12,000+ US/Canadian/UK financial institutions via a combination of OAuth-based direct bank integrations and credential-based connections. Provides a client-side "Link" widget (JavaScript/iframe) that handles the bank authentication UX.

**Coverage:** Excellent US/Canada coverage (~95% of US bank accounts by market share). UK coverage growing. Limited outside US/Canada/UK.

**Pricing model:**
- **Development/Sandbox:** Free. 100 test Items (linked accounts), uses sandbox bank data.
- **Production:** Pay-per-Item model. An "Item" is one set of credentials (can cover multiple accounts at the same institution). Pricing tiers:
  - **Launch:** Free for first 100 Items. Transactions product included. Good for getting started.
  - **Grow:** Custom pricing, typically $0.30–$1.50 per Item/month for the Transactions product.
  - **Scale:** Volume discounts at 500+ Items.
- **For personal use (3–10 Items):** The Launch tier's free 100 Items would cover a personal app indefinitely. Beyond that, ~$3–$15/month for a small household.

**.NET / C# SDK:** No official .NET SDK. Well-documented REST API with OpenAPI spec. Community .NET wrappers exist (e.g., `Going.Plaid` NuGet package). HTTP client integration straightforward in .NET.

**Data access:**
- **Transaction history:** Up to 24 months on initial link, then incremental updates via webhooks or polling.
- **Refresh:** Supports webhooks for real-time transaction notifications (requires publicly accessible endpoint) AND polling via `/transactions/sync` endpoint (cursor-based, no webhook required).
- **Pending transactions:** Yes — includes pending transactions that update to settled.

**Pros:**
- Broadest US institution coverage
- `/transactions/sync` endpoint supports cursor-based polling (no webhook required — critical for self-hosted)
- Stable, well-funded company (IPO'd 2024)
- Sandbox mode for development with no cost
- Launch tier free for 100 Items — more than enough for personal use
- Merchant name enrichment and category data included

**Cons:**
- No official .NET SDK (REST API is clean, but requires building a client)
- Link widget is JavaScript-based — needs Blazor JS interop
- Production onboarding requires application review (but is self-service)
- Webhook support requires public URL (polling works without one)

**Suitability for this project:** ★★★★★ — Best fit. Free Launch tier covers personal use. Polling-based sync works for self-hosted without public URL. Strongest US coverage.

---

### 2. MX Technologies

**What it is:** Financial data aggregation platform focused on enterprise clients (banks, fintechs, wealth management). Strong data enrichment (transaction categorization, merchant identification, account aggregation). Uses a mix of direct API connections and credential-based access.

**Coverage:** Strong US coverage. International coverage limited.

**Pricing model:**
- **No self-service.** Requires sales contact and commercial agreement.
- **Enterprise pricing.** Minimum annual contracts, typically $10,000+ per year.
- **No free tier or personal-use plan.**

**.NET / C# SDK:** No official SDK. REST API available but documentation gated behind commercial agreement.

**Data access:** Real-time and batch. Strong data enrichment (auto-categorization, merchant normalization). Webhook and polling supported.

**Pros:**
- Excellent data enrichment (categorization, merchant normalization)
- Strong compliance and security certifications

**Cons:**
- Enterprise-only pricing — completely impractical for personal use
- No self-service signup
- Documentation not publicly accessible
- Designed for institutions embedding financial data, not end-user apps

**Suitability for this project:** ★☆☆☆☆ — Not viable. Enterprise pricing and sales-only access eliminate this for personal use.

---

### 3. Finicity (by Mastercard)

**What it is:** Open banking platform acquired by Mastercard in 2020. Strong in US market, focused on verification (income, employment, assets) and transaction data. Uses direct API connections to institutions (no screen scraping in most cases).

**Coverage:** Strong US coverage. International coverage through Mastercard's Open Banking network expanding but not as broad as Plaid for consumer accounts.

**Pricing model:**
- **Sandbox:** Free with test institutions.
- **Production:** Requires Mastercard Developer Portal registration. Pay-per-call pricing model.
- **Typical costs:** $0.10–$0.50 per API call for transaction pulls. For personal use with daily syncs across 5 accounts, roughly $15–$75/month depending on call volume.
- **No personal-use tier.** Designed for fintech companies.

**.NET / C# SDK:** No official .NET SDK. REST API with OpenAPI spec. Auto-generated clients possible.

**Data access:** Real-time pulls. Transaction history up to 24 months. Supports webhooks and polling.

**Pros:**
- Strong US institution coverage
- Mastercard backing (stability)
- Direct API connections (no screen scraping)

**Cons:**
- Per-call pricing adds up even for personal use
- No free production tier
- Designed for commercial fintech integration
- Onboarding more complex than Plaid

**Suitability for this project:** ★★☆☆☆ — Possible but expensive. Per-call pricing model is unfriendly for a daily-sync personal app.

---

### 4. Tink (by Visa)

**What it is:** European open banking platform acquired by Visa in 2022. Provides account aggregation, payment initiation, and financial data enrichment. Uses PSD2/Open Banking regulated APIs for European institutions.

**Coverage:** Excellent across EU/EEA/UK (3,400+ institutions in 18 European markets). Minimal US coverage — Visa's US strategy uses different channels.

**Pricing model:**
- **Sandbox:** Free.
- **Production:** Requires commercial agreement with Visa. Enterprise-oriented pricing.
- **No personal-use tier.**

**.NET / C# SDK:** No official SDK. REST API available.

**Data access:** Real-time via regulated Open Banking APIs. Transaction history varies by bank (typically 90 days under PSD2, some banks offer more).

**Pros:**
- Best-in-class European coverage
- PSD2-compliant direct API access (no screen scraping in EU)
- Strong data enrichment
- Visa backing

**Cons:**
- No meaningful US coverage
- Enterprise pricing, no self-service for production
- PSD2 consent expires every 90 days — requires user re-authentication
- Overkill for a personal US app

**Suitability for this project:** ★☆☆☆☆ — Not viable for US-primary. Would only be relevant as a secondary adapter if EU coverage needed.

---

### 5. Mono

**What it is:** Financial data API focused on African markets (Nigeria, Kenya, South Africa, Ghana). Provides account linking, transaction data, and identity verification for African financial institutions.

**Coverage:** Africa only — Nigeria (strongest), Kenya, South Africa, Ghana, and expanding.

**Pricing model:**
- **Sandbox:** Free.
- **Production:** Pay-per-use. $0.10–$0.50 per successful connection.
- **Accessible pricing for developers but limited market.**

**.NET / C# SDK:** No official SDK. REST API.

**Data access:** Real-time and historical. Varies by institution.

**Pros:**
- Best-in-class African market coverage
- Developer-friendly pricing
- Growing rapidly in African fintech ecosystem

**Cons:**
- Zero US/EU coverage
- Completely irrelevant for US-primary personal use

**Suitability for this project:** ☆☆☆☆☆ — Not applicable. Wrong market entirely.

---

### 6. TrueLayer

**What it is:** UK/EU open banking platform. Provides account data access and payment initiation through PSD2-regulated APIs. Strong in UK market, expanding across Europe.

**Coverage:** UK (excellent), EU (good across major markets — France, Germany, Spain, Italy, Netherlands). No US coverage.

**Pricing model:**
- **Sandbox:** Free.
- **Data API:** Pricing starts at ~£0.10 per connection/month.
- **Production:** Requires commercial agreement for data access products.
- **No personal-use tier** for data aggregation (payment initiation has more flexible pricing).

**.NET / C# SDK:** No official .NET SDK. REST API with good documentation.

**Data access:** Real-time via Open Banking APIs. PSD2 90-day consent limitation applies.

**Pros:**
- Excellent UK open banking coverage
- Clean, developer-friendly API
- Payment initiation capability (future: pay bills from app)

**Cons:**
- No US coverage
- PSD2 90-day re-consent requirement
- Commercial agreement required for production

**Suitability for this project:** ★☆☆☆☆ — Not viable for US-primary. Good secondary option for UK/EU expansion.

---

### 7. Akoya

**What it is:** US financial data network that provides direct API access to bank data without screen scraping. Founded by Fidelity, operates as a "data access network" — banks publish data directly to Akoya's network, and third-party apps consume it through a standardized API (FDX — Financial Data Exchange standard).

**Coverage:** US only. ~75% of US deposit accounts by market share (major banks: Chase, Wells Fargo, Bank of America, Citi via direct connections). Growing coverage but not as broad as Plaid for smaller institutions and credit unions.

**Pricing model:**
- **Sandbox:** Available with test data.
- **Production:** Requires commercial agreement. Pricing not publicly disclosed.
- **No personal-use tier.** Designed for fintech companies building consumer products.

**.NET / C# SDK:** No official SDK. REST API using FDX standard.

**Data access:** Real-time via direct API connections. No screen scraping — all data flows through bank-authorized channels. Strong on security and consumer consent.

**Pros:**
- No screen scraping — all direct API (highest data reliability)
- FDX standard alignment (future-proof)
- Strong with major US banks
- Consumer-consent-first model

**Cons:**
- Commercial agreement required — no self-service personal use
- Smaller institution coverage gaps (credit unions, community banks)
- Newer platform — less mature ecosystem
- Pricing not transparent

**Suitability for this project:** ★★☆☆☆ — Architecturally ideal (direct API, no scraping) but commercially inaccessible for personal use.

---

### 8. Nordigen / GoCardless Bank Account Data

**What it is:** Open banking data API, now part of GoCardless (acquired 2022). Provides free access to bank account data in Europe through PSD2-regulated APIs. Rebranded as "GoCardless Bank Account Data API." The notable differentiator: **free tier for account data access** with no per-connection fees.

**Coverage:** Europe/UK primarily — 2,300+ institutions across 31 countries. US coverage: **none** (PSD2/Open Banking is a European regulatory framework).

**Pricing model:**
- **Free tier:** Up to 50 unique end-user agreements per day. No per-connection or per-API-call fees. Truly free for personal use.
- **Premium:** Custom pricing for higher volume and additional features.
- **For personal use:** The free tier would cover a personal app indefinitely (single user, few accounts).

**.NET / C# SDK:** No official .NET SDK. Clean REST API. Community clients exist.

**Data access:** Real-time via PSD2 Open Banking APIs. Transaction history typically 90 days (PSD2 limitation, some banks offer more). Consent expires every 90 days — requires user re-authentication.

**Pros:**
- **Free for personal use** — the only vendor in this comparison with a genuinely free production tier
- No screen scraping (PSD2 direct API)
- Clean, well-documented REST API
- Self-service signup, no sales contact needed
- Good developer experience

**Cons:**
- **Zero US coverage** — European Open Banking only
- PSD2 90-day consent expiry requires periodic re-authentication
- Transaction history limited to 90 days on initial connection
- No merchant enrichment or categorization data
- GoCardless acquisition may shift focus toward payment products

**Suitability for this project:** ★★★☆☆ — Would be the top pick if the app were EU-primary. The free tier is unmatched. For US-primary use, not viable as the primary adapter. Excellent candidate as a secondary adapter for future EU support.

---

### Vendor Comparison Summary

| Vendor | US Coverage | EU Coverage | Free Tier | Personal Use | .NET SDK | Self-Hosted OK | Rating |
|--------|-------------|-------------|-----------|--------------|----------|----------------|--------|
| **Plaid** | ★★★★★ | ★★☆☆☆ | ✅ 100 Items | ✅ Launch tier | ❌ (REST) | ✅ Polling | ★★★★★ |
| **MX Technologies** | ★★★★☆ | ★☆☆☆☆ | ❌ | ❌ Enterprise | ❌ | N/A | ★☆☆☆☆ |
| **Finicity** | ★★★★☆ | ★☆☆☆☆ | ✅ Sandbox | ⚠️ Per-call $$ | ❌ (REST) | ✅ | ★★☆☆☆ |
| **Tink (Visa)** | ☆☆☆☆☆ | ★★★★★ | ✅ Sandbox | ❌ Enterprise | ❌ | N/A | ★☆☆☆☆ |
| **Mono** | ☆☆☆☆☆ | ☆☆☆☆☆ | ✅ Sandbox | ✅ | ❌ | ✅ | ☆☆☆☆☆ |
| **TrueLayer** | ☆☆☆☆☆ | ★★★★☆ | ✅ Sandbox | ❌ Commercial | ❌ | N/A | ★☆☆☆☆ |
| **Akoya** | ★★★★☆ | ☆☆☆☆☆ | ✅ Sandbox | ❌ Commercial | ❌ | N/A | ★★☆☆☆ |
| **Nordigen/GC** | ☆☆☆☆☆ | ★★★★★ | ✅ **Free** | ✅ Free | ❌ (REST) | ✅ | ★★★☆☆ |

---

## Vendor Recommendation

### Primary: Plaid (US accounts)

**Rationale:**

1. **US coverage is best-in-class.** Plaid covers ~95% of US bank accounts. For US-primary personal use, this is non-negotiable.

2. **Free for personal scale.** Plaid's Launch tier provides 100 free Items. A personal household has 3–10 linked bank Items. This means **years of free use** before any pricing concern.

3. **Polling works without public URL.** The `/transactions/sync` endpoint uses a cursor-based model: the app requests "give me all changes since cursor X." This is ideal for a Raspberry Pi behind a home router — no need for a public-facing webhook endpoint. The app simply polls on a schedule (e.g., every 6 hours via a background service).

4. **Stable, well-documented REST API.** No .NET SDK needed — Plaid's OpenAPI spec can auto-generate a typed C# client. The API is mature and breaking changes are versioned.

5. **Bank-provided transaction IDs.** Each transaction has a stable `transaction_id`. This eliminates the heuristic duplicate detection that the current CSV import relies on. Synced transactions can be matched deterministically.

6. **Merchant enrichment included.** Plaid normalizes merchant names and provides category codes — feeding directly into the app's auto-categorization pipeline.

### Secondary: Nordigen / GoCardless (EU accounts)

If the app needs European bank account support, Nordigen's free tier makes it the obvious choice for EU residents. The `IBankConnector` abstraction means adding a `NordigenBankConnector` adapter is a single Infrastructure-project change. Both Plaid and Nordigen can be active simultaneously within the same deployment — users simply select their region when linking an account.

**Key advantage:** Free tier for personal use (up to 50 unique end-user agreements per day). No per-connection fees.

### Architecture Implication

The abstraction layer (`IBankConnector`) is designed so that:
- Plaid-specific details are isolated in `Infrastructure/BankConnectivity/Plaid/`
- Nordigen-specific details (when implemented) are isolated in `Infrastructure/BankConnectivity/Nordigen/`
- Swapping to Finicity, Akoya, or any future vendor requires only a new adapter class
- The Domain and Application layers never reference vendor names directly
- The **ConnectorRegistry** (see Architecture section) manages which connectors are enabled and routes requests to the appropriate one based on user selection

---

## Goals

- **G1:** One-time bank linking via OAuth redirect flow for each institution
- **G2:** Automatic background transaction sync on a configurable schedule (default: every 6 hours)
- **G3:** Deterministic deduplication via bank-provided transaction IDs (no heuristic matching)
- **G4:** Pending → settled transaction updates tracked and applied automatically
- **G5:** Vendor-agnostic abstraction allowing adapter swap without domain/application changes
- **G6:** Manual sync trigger from the UI for immediate refresh
- **G7:** Coexistence with CSV import — bank connectivity is additive, not a replacement
- **G8:** **Multi-vendor support with user-selectable connectors.** A deployment can have multiple connectors (e.g., Plaid for US accounts, Nordigen/GoCardless for EU accounts) registered and active simultaneously. When a user links a bank account, they select their region/provider; the system automatically invokes the correct adapter. Each linked account stores which connector it uses, enabling seamless sync with the appropriate provider.

## Non-Goals

- **NG1:** Bill payment or money movement (read-only integration)
- **NG2:** Real-time push via webhooks (polling-only for self-hosted compatibility; webhook support is a future enhancement)
- **NG4:** Bank credential storage (all authentication is via OAuth — app never sees bank passwords)
- **NG5:** Account balance sync for budgeting (account balance from bank is informational; the app's own transaction-based balance remains authoritative)
- **NG6:** Investment or brokerage account sync (checking/savings/credit card transaction accounts only)
- **NG7:** Automatic categorization from bank category codes (bank categories feed into the *suggestion* pipeline, not auto-applied — user's rules remain authoritative)

---

## Architecture

### ConnectorRegistry: Managing Multiple Vendors

The architecture now supports multiple simultaneous bank connectors within a single deployment. A **ConnectorRegistry** maintains metadata about each registered connector and enables routing:

```csharp
// src/BudgetExperiment.Domain/BankConnectivity/ConnectorRegistryEntry.cs
public sealed record ConnectorRegistryEntry(
    string ConnectorType,                    // "Plaid", "Nordigen", etc.
    string DisplayName,                      // User-facing name
    string[] SupportedRegions,               // ["US", "CA"] or ["EU", "UK"]
    bool IsConfigured,                       // Has credentials been set up?
    bool IsEnabled);                         // Can users select this connector?

// src/BudgetExperiment.Domain/BankConnectivity/IBankConnectorRegistry.cs
public interface IBankConnectorRegistry
{
    /// <summary>
    /// Get all registered connectors.
    /// </summary>
    IReadOnlyList<ConnectorRegistryEntry> GetAll();

    /// <summary>
    /// Get connectors available for a specific region (user filtering).
    /// </summary>
    IReadOnlyList<ConnectorRegistryEntry> GetByRegion(string region);

    /// <summary>
    /// Resolve the IBankConnector instance for a specific connector type.
    /// </summary>
    IBankConnector? Resolve(string connectorType);
}
```

**How it works:**
1. During `BudgetExperiment.Api` startup, the application registers available connectors (e.g., Plaid, Nordigen).
2. When the user initiates bank account linking (UI → `POST /bank-connections/link-session`), the API queries the registry to determine which connectors are available for the user's region.
3. The user selects their preferred connector (e.g., "Plaid for US" or "Nordigen for EU").
4. The application stores the selected `ConnectorType` in the `BankConnection` entity (new field).
5. During sync, the system resolves the correct `IBankConnector` implementation via the registry and invokes it.

This design allows:
- Multiple connectors active simultaneously
- Region-aware filtering in the UI ("Choose your region" → see available providers)
- Easy addition of new adapters without touching the linking or sync logic
- Simple enable/disable of vendors via configuration

### New Abstraction: `IBankConnector`

The core abstraction lives in the Domain layer. Infrastructure provides vendor-specific implementations.

```csharp
// src/BudgetExperiment.Domain/BankConnectivity/IBankConnector.cs
public interface IBankConnector
{
    /// <summary>
    /// Creates a link token/session for the client-side bank linking widget.
    /// </summary>
    Task<LinkSessionResult> CreateLinkSessionAsync(
        Guid userId, string? institutionId, CancellationToken ct);

    /// <summary>
    /// Exchanges a public token (from the link widget callback) for a persistent access token.
    /// </summary>
    Task<LinkCompleteResult> CompleteLinkAsync(
        string publicToken, CancellationToken ct);

    /// <summary>
    /// Fetches accounts associated with a linked connection.
    /// </summary>
    Task<IReadOnlyList<BankAccountInfo>> GetAccountsAsync(
        string accessToken, CancellationToken ct);

    /// <summary>
    /// Fetches transaction changes since the last sync cursor.
    /// Returns added, modified, and removed transactions.
    /// </summary>
    Task<TransactionSyncResult> SyncTransactionsAsync(
        string accessToken, string? cursor, CancellationToken ct);

    /// <summary>
    /// Removes a linked connection (revokes access).
    /// </summary>
    Task RemoveLinkAsync(string accessToken, CancellationToken ct);

    /// <summary>
    /// Checks the health/status of a linked connection.
    /// </summary>
    Task<LinkHealthResult> CheckLinkHealthAsync(
        string accessToken, CancellationToken ct);
}
```

### Domain Value Objects

```csharp
// src/BudgetExperiment.Domain/BankConnectivity/LinkSessionResult.cs
public sealed record LinkSessionResult(
    string LinkToken,
    string LinkUrl,
    DateTime ExpiresAtUtc);

// src/BudgetExperiment.Domain/BankConnectivity/LinkCompleteResult.cs
public sealed record LinkCompleteResult(
    string AccessToken,
    string ItemId,
    IReadOnlyList<BankAccountInfo> Accounts);

// src/BudgetExperiment.Domain/BankConnectivity/BankAccountInfo.cs
public sealed record BankAccountInfo(
    string ExternalAccountId,
    string Name,
    string OfficialName,
    BankAccountType AccountType,
    string? Mask,
    string InstitutionId,
    string InstitutionName);

// src/BudgetExperiment.Domain/BankConnectivity/BankAccountType.cs
public enum BankAccountType
{
    Checking = 0,
    Savings = 1,
    CreditCard = 2,
    Other = 3,
}

// src/BudgetExperiment.Domain/BankConnectivity/TransactionSyncResult.cs
public sealed record TransactionSyncResult(
    IReadOnlyList<BankTransaction> Added,
    IReadOnlyList<BankTransaction> Modified,
    IReadOnlyList<string> RemovedTransactionIds,
    string NextCursor,
    bool HasMore);

// src/BudgetExperiment.Domain/BankConnectivity/BankTransaction.cs
public sealed record BankTransaction(
    string TransactionId,
    string AccountId,
    DateOnly Date,
    DateOnly? AuthorizedDate,
    decimal Amount,
    string? IsoCurrencyCode,
    string? MerchantName,
    string Name,
    string? Category,
    bool IsPending);

// src/BudgetExperiment.Domain/BankConnectivity/LinkHealthResult.cs
public sealed record LinkHealthResult(
    bool IsHealthy,
    string? ErrorCode,
    string? ErrorMessage,
    DateTime? LastSuccessfulSyncUtc);
```

### Domain Entities

```csharp
// src/BudgetExperiment.Domain/BankConnectivity/BankConnection.cs
public sealed class BankConnection
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string ConnectorType { get; private set; }      // "Plaid", "Nordigen", etc.
    public string ItemId { get; private set; }          // Vendor's connection identifier
    public string InstitutionId { get; private set; }
    public string InstitutionName { get; private set; }
    public string EncryptedAccessToken { get; private set; }
    public BankConnectionStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastSyncAtUtc { get; private set; }
    public string? LastSyncCursor { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public BudgetScope Scope { get; private set; }
    public Guid? OwnerUserId { get; private set; }

    public static BankConnection Create(
        Guid userId,
        string connectorType,
        string itemId,
        string institutionId,
        string institutionName,
        string encryptedAccessToken,
        BudgetScope scope,
        Guid? ownerUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectorType);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(institutionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(institutionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedAccessToken);

        return new BankConnection
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            ConnectorType = connectorType,
            ItemId = itemId,
            InstitutionId = institutionId,
            InstitutionName = institutionName,
            EncryptedAccessToken = encryptedAccessToken,
            Status = BankConnectionStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
            Scope = scope,
            OwnerUserId = ownerUserId,
        };
    }

    public void UpdateSyncState(string cursor, DateTime syncedAtUtc)
    {
        LastSyncCursor = cursor;
        LastSyncAtUtc = syncedAtUtc;
        LastErrorMessage = null;
        Status = BankConnectionStatus.Active;
    }

    public void MarkError(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        LastErrorMessage = errorMessage;
        Status = BankConnectionStatus.Error;
    }

    public void MarkRequiresReauth()
    {
        Status = BankConnectionStatus.RequiresReauth;
    }

    public void Deactivate()
    {
        Status = BankConnectionStatus.Inactive;
    }

    public void UpdateAccessToken(string encryptedAccessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedAccessToken);
        EncryptedAccessToken = encryptedAccessToken;
        Status = BankConnectionStatus.Active;
        LastErrorMessage = null;
    }
}

// src/BudgetExperiment.Domain/BankConnectivity/BankConnectionStatus.cs
public enum BankConnectionStatus
{
    Active = 0,
    Error = 1,
    RequiresReauth = 2,
    Inactive = 3,
}
```

```csharp
// src/BudgetExperiment.Domain/BankConnectivity/LinkedAccount.cs
public sealed class LinkedAccount
{
    public Guid Id { get; private set; }
    public Guid BankConnectionId { get; private set; }
    public Guid? AppAccountId { get; private set; }     // FK to the app's Account entity
    public string ExternalAccountId { get; private set; }
    public string Name { get; private set; }
    public string? OfficialName { get; private set; }
    public BankAccountType AccountType { get; private set; }
    public string? Mask { get; private set; }           // Last 4 digits
    public bool IsSyncEnabled { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static LinkedAccount Create(
        Guid bankConnectionId,
        string externalAccountId,
        string name,
        string? officialName,
        BankAccountType accountType,
        string? mask)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new LinkedAccount
        {
            Id = Guid.CreateVersion7(),
            BankConnectionId = bankConnectionId,
            ExternalAccountId = externalAccountId,
            Name = name,
            OfficialName = officialName,
            AccountType = accountType,
            Mask = mask,
            IsSyncEnabled = false,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void MapToAppAccount(Guid appAccountId)
    {
        AppAccountId = appAccountId;
        IsSyncEnabled = true;
    }

    public void Unmap()
    {
        AppAccountId = null;
        IsSyncEnabled = false;
    }

    public void EnableSync()
    {
        if (AppAccountId is null)
        {
            throw new DomainException(
                "Cannot enable sync without mapping to an app account.",
                DomainExceptionType.ValidationError);
        }

        IsSyncEnabled = true;
    }

    public void DisableSync()
    {
        IsSyncEnabled = false;
    }
}
```

### Repository Interfaces

```csharp
// src/BudgetExperiment.Domain/BankConnectivity/IBankConnectionRepository.cs
public interface IBankConnectionRepository
    : IReadRepository<BankConnection>, IWriteRepository<BankConnection>
{
    Task<IReadOnlyList<BankConnection>> GetByUserAsync(
        Guid userId, CancellationToken ct);

    Task<BankConnection?> GetByItemIdAsync(
        string itemId, CancellationToken ct);

    Task<IReadOnlyList<BankConnection>> GetActiveSyncableAsync(
        CancellationToken ct);
}

// src/BudgetExperiment.Domain/BankConnectivity/ILinkedAccountRepository.cs
public interface ILinkedAccountRepository
    : IReadRepository<LinkedAccount>, IWriteRepository<LinkedAccount>
{
    Task<IReadOnlyList<LinkedAccount>> GetByConnectionAsync(
        Guid bankConnectionId, CancellationToken ct);

    Task<IReadOnlyList<LinkedAccount>> GetSyncEnabledAsync(
        CancellationToken ct);

    Task<LinkedAccount?> GetByExternalIdAsync(
        string externalAccountId, CancellationToken ct);
}
```

### Sync Conflict Resolution

When `SyncTransactionsAsync` returns transactions, the sync service handles conflicts:

```
Bank says "added":
  1. Look up by ExternalReference (bank transaction ID) in app's Transaction table
  2. If NOT found → create new Transaction, set ImportBatchId to the sync batch, 
     set ExternalReference to bank transaction ID
  3. If found → skip (already synced — idempotent)

Bank says "modified":
  1. Look up by ExternalReference
  2. If found → update Amount, Description, Date, IsPending status
     (preserve user-applied Category — bank data is suggestion only)
  3. If NOT found → create as new (edge case: modification before initial sync)

Bank says "removed":
  1. Look up by ExternalReference
  2. If found → soft-delete or mark as bank-removed 
     (do NOT hard-delete — user may have categorized/reconciled it)
  3. If NOT found → no-op
```

**Key rule:** User-applied data (category, notes, cleared status) is NEVER overwritten by bank data. Bank sync updates only bank-sourced fields (amount, date, description, pending status).

### Token Storage Security

OAuth access tokens are sensitive. Storage approach:

- **Encryption at rest:** Access tokens are encrypted using ASP.NET Core Data Protection API before storage in PostgreSQL. The `EncryptedAccessToken` column stores the ciphertext.
- **Key management:** Data Protection keys stored in the file system (for Raspberry Pi deployment) or a configured key vault. Keys are rotated automatically by the Data Protection framework.
- **No bank credentials:** The app never receives or stores bank usernames/passwords. Only the OAuth access token (which can be revoked by the user or the bank).
- **Token refresh:** Plaid access tokens do not expire (they persist until revoked). If using a vendor with expiring tokens, the `BankConnection` entity would need `RefreshToken` and `ExpiresAtUtc` fields.

### Polling Strategy (Background Sync)

```csharp
// src/BudgetExperiment.Infrastructure/BankConnectivity/BankSyncBackgroundService.cs
// Hosted service that runs on a configurable schedule

// Default: every 6 hours
// User-configurable via AppSettings
// Each sync cycle:
//   1. Load all active BankConnections
//   2. For each connection with sync-enabled LinkedAccounts:
//      a. Decrypt access token
//      b. Call IBankConnector.SyncTransactionsAsync(token, lastCursor)
//      c. Process added/modified/removed transactions
//      d. Update BankConnection.LastSyncCursor and LastSyncAtUtc
//      e. On error: mark BankConnection.Status = Error, log, continue to next
//   3. Page through HasMore if the vendor returns paginated results
```

### Infrastructure: Plaid Adapter

```csharp
// src/BudgetExperiment.Infrastructure/BankConnectivity/Plaid/PlaidBankConnector.cs
// Implements IBankConnector using Plaid's REST API

// src/BudgetExperiment.Infrastructure/BankConnectivity/Plaid/PlaidOptions.cs
public sealed class PlaidOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string Environment { get; set; } = "sandbox";   // sandbox | development | production
    public string[] Products { get; set; } = ["transactions"];
    public string[] CountryCodes { get; set; } = ["US"];
    public string WebhookUrl { get; set; } = string.Empty;  // Optional, empty = polling only
}
```

**Configuration:**
- `ClientId` and `Secret` stored in user secrets (never committed)
- `Environment` in `appsettings.json` (sandbox for dev, production for deploy)

---

## Connector Configuration

Multiple connectors can be enabled simultaneously via `appsettings.json` configuration. The **ConnectorRegistry** reads this configuration at startup and determines which connectors are available for user selection.

**Example configuration (appsettings.json):**

```json
{
  "BankConnectivity": {
    "Connectors": {
      "Plaid": {
        "Enabled": true,
        "DisplayName": "Plaid (US, Canada, UK)",
        "SupportedRegions": ["US", "CA", "UK"],
        "ClientId": "set-via-user-secrets",
        "Secret": "set-via-user-secrets",
        "Environment": "production"
      },
      "Nordigen": {
        "Enabled": false,
        "DisplayName": "Nordigen / GoCardless (EU)",
        "SupportedRegions": ["DE", "FR", "IT", "ES", "NL", "BE", "AT", "SE", "NO", "DK"],
        "SecretId": "set-via-user-secrets",
        "SecretKey": "set-via-user-secrets"
      }
    },
    "DefaultSyncIntervalMinutes": 360,
    "EnableManualSync": true
  }
}
```

**How it works:**

1. **Startup:** The API's `Program.cs` instantiates the **ConnectorRegistry** by:
   - Reading the `BankConnectivity:Connectors` configuration
   - Creating a **ConnectorRegistryEntry** for each configured connector
   - Instantiating and registering the corresponding `IBankConnector` implementations (Plaid, Nordigen, etc.)
   - Marking entries as "Enabled" or "Disabled" based on configuration

2. **User Linking Flow:** When the user clicks "Link Bank Account":
   - The API queries the registry for enabled connectors
   - The UI filters by user's region (e.g., "US" → shows Plaid; "DE" → shows Nordigen)
   - User selects their connector
   - The `BankConnection` entity stores the selected `ConnectorType`

3. **Sync Execution:** During background sync:
   - The system queries all `BankConnection` records
   - For each connection, it resolves the correct `IBankConnector` via the registry using `ConnectorType`
   - The appropriate adapter's `SyncTransactionsAsync` is invoked
   - Results are merged and stored

4. **Adding a New Connector:**
   - Implement `IBankConnector` in Infrastructure (e.g., `NordigenBankConnector`)
   - Add configuration to `appsettings.json`
   - Register in `Program.cs`
   - No domain or application layer changes required

---

## Domain Model Changes Summary

| Entity / Type | Layer | Change |
|---------------|-------|--------|
| `BankConnection` | Domain | **New** — represents a linked institution connection; includes `ConnectorType` field to track which adapter is used |
| `LinkedAccount` | Domain | **New** — represents a bank account within a connection |
| `BankConnectionStatus` | Domain | **New** enum |
| `BankAccountType` | Domain | **New** enum |
| `IBankConnector` | Domain | **New** interface — vendor abstraction |
| `IBankConnectorRegistry` | Domain | **New** interface — manages registered connectors and enables routing |
| `ConnectorRegistryEntry` | Domain | **New** record — metadata for a registered connector (name, regions, enabled state) |
| `IBankConnectionRepository` | Domain | **New** interface |
| `ILinkedAccountRepository` | Domain | **New** interface |
| `BankTransaction` | Domain | **New** record — vendor-normalized transaction data |
| `TransactionSyncResult` | Domain | **New** record — sync response |
| `LinkSessionResult` | Domain | **New** record |
| `LinkCompleteResult` | Domain | **New** record |
| `LinkHealthResult` | Domain | **New** record |
| `BankAccountInfo` | Domain | **New** record |
| `Transaction` (existing) | Domain | **Modified** — `ExternalReference` already exists; no schema change needed for sync ID |
| `ImportBatch` (existing) | Domain | **Modified** — new `ImportSource` property (CSV vs BankSync) to distinguish sync batches |
| `ImportBatchSource` | Shared | **New** enum: `Csv = 0, BankSync = 1` |

---

## API Changes

### New Endpoints: Bank Connectivity

| Method | Endpoint | Description | Request | Response |
|--------|----------|-------------|---------|----------|
| POST | `/api/v1/bank-connections/link-session` | Create a link token for the bank widget | `CreateLinkSessionRequest` | `LinkSessionDto` |
| POST | `/api/v1/bank-connections/complete-link` | Exchange public token after bank OAuth | `CompleteLinkRequest` | `BankConnectionDto` |
| GET | `/api/v1/bank-connections` | List user's bank connections | — | `BankConnectionDto[]` |
| GET | `/api/v1/bank-connections/{id:guid}` | Get specific connection | — | `BankConnectionDto` |
| DELETE | `/api/v1/bank-connections/{id:guid}` | Remove a bank connection (revokes access) | — | 204 |
| POST | `/api/v1/bank-connections/{id:guid}/refresh-link` | Re-authenticate a connection requiring reauth | — | `LinkSessionDto` |
| GET | `/api/v1/bank-connections/{id:guid}/accounts` | List linked accounts for a connection | — | `LinkedAccountDto[]` |
| PUT | `/api/v1/bank-connections/{id:guid}/accounts/{accountId:guid}/map` | Map linked account to app account | `MapAccountRequest` | `LinkedAccountDto` |
| DELETE | `/api/v1/bank-connections/{id:guid}/accounts/{accountId:guid}/map` | Unmap linked account | — | 204 |
| POST | `/api/v1/bank-connections/sync` | Trigger immediate sync for all connections | — | `SyncResultDto` |
| POST | `/api/v1/bank-connections/{id:guid}/sync` | Trigger sync for specific connection | — | `SyncResultDto` |
| GET | `/api/v1/bank-connections/sync-status` | Get last sync status for all connections | — | `SyncStatusDto[]` |

### Contract DTOs

```csharp
// src/BudgetExperiment.Contracts/BankConnectivity/CreateLinkSessionRequest.cs
public sealed record CreateLinkSessionRequest(string? InstitutionId);

// src/BudgetExperiment.Contracts/BankConnectivity/CompleteLinkRequest.cs
public sealed record CompleteLinkRequest(string PublicToken);

// src/BudgetExperiment.Contracts/BankConnectivity/MapAccountRequest.cs
public sealed record MapAccountRequest(Guid AppAccountId);

// src/BudgetExperiment.Contracts/BankConnectivity/BankConnectionDto.cs
public sealed record BankConnectionDto(
    Guid Id,
    string InstitutionId,
    string InstitutionName,
    BankConnectionStatus Status,
    DateTime CreatedAtUtc,
    DateTime? LastSyncAtUtc,
    string? LastErrorMessage,
    int LinkedAccountCount,
    int SyncEnabledAccountCount);

// src/BudgetExperiment.Contracts/BankConnectivity/LinkedAccountDto.cs
public sealed record LinkedAccountDto(
    Guid Id,
    string ExternalAccountId,
    string Name,
    string? OfficialName,
    BankAccountType AccountType,
    string? Mask,
    Guid? AppAccountId,
    string? AppAccountName,
    bool IsSyncEnabled);

// src/BudgetExperiment.Contracts/BankConnectivity/LinkSessionDto.cs
public sealed record LinkSessionDto(
    string LinkToken,
    DateTime ExpiresAtUtc);

// src/BudgetExperiment.Contracts/BankConnectivity/SyncResultDto.cs
public sealed record SyncResultDto(
    int TransactionsAdded,
    int TransactionsModified,
    int TransactionsRemoved,
    int Errors,
    DateTime SyncedAtUtc);

// src/BudgetExperiment.Contracts/BankConnectivity/SyncStatusDto.cs
public sealed record SyncStatusDto(
    Guid ConnectionId,
    string InstitutionName,
    BankConnectionStatus Status,
    DateTime? LastSyncAtUtc,
    string? LastErrorMessage);
```

---

## Application Service

```csharp
// src/BudgetExperiment.Application/BankConnectivity/IBankConnectionService.cs
public interface IBankConnectionService
{
    Task<LinkSessionDto> CreateLinkSessionAsync(
        CreateLinkSessionRequest request, CancellationToken ct);

    Task<BankConnectionDto> CompleteLinkAsync(
        CompleteLinkRequest request, CancellationToken ct);

    Task<IReadOnlyList<BankConnectionDto>> GetConnectionsAsync(
        CancellationToken ct);

    Task<BankConnectionDto?> GetConnectionAsync(
        Guid connectionId, CancellationToken ct);

    Task RemoveConnectionAsync(
        Guid connectionId, CancellationToken ct);

    Task<LinkSessionDto> RefreshLinkAsync(
        Guid connectionId, CancellationToken ct);

    Task<IReadOnlyList<LinkedAccountDto>> GetLinkedAccountsAsync(
        Guid connectionId, CancellationToken ct);

    Task<LinkedAccountDto> MapAccountAsync(
        Guid connectionId, Guid linkedAccountId, Guid appAccountId, CancellationToken ct);

    Task UnmapAccountAsync(
        Guid connectionId, Guid linkedAccountId, CancellationToken ct);
}

// src/BudgetExperiment.Application/BankConnectivity/IBankSyncService.cs
public interface IBankSyncService
{
    Task<SyncResultDto> SyncAllAsync(CancellationToken ct);

    Task<SyncResultDto> SyncConnectionAsync(
        Guid connectionId, CancellationToken ct);

    Task<IReadOnlyList<SyncStatusDto>> GetSyncStatusAsync(
        CancellationToken ct);
}
```

---

## UI/UX

### Account Linking Flow

```
1. User navigates to Settings → Bank Connections
2. Clicks "Link Bank Account"
3. App calls POST /bank-connections/link-session → receives link token
4. Plaid Link widget opens (JavaScript interop from Blazor)
5. User authenticates with their bank in the widget
6. Widget returns a public token to the app
7. App calls POST /bank-connections/complete-link with public token
8. App displays discovered accounts
9. User maps each bank account to an existing app account (or creates new)
10. User enables sync for mapped accounts
```

### Blazor Components

| Component | Route | Purpose |
|-----------|-------|---------|
| `BankConnections.razor` | `/settings/bank-connections` | Manage linked institutions |
| `BankConnectionsViewModel.cs` | — | ViewModel for connection CRUD and linking flow |
| `BankConnectionCard.razor` | — | Single connection: institution name, status, last sync, account count |
| `LinkedAccountList.razor` | — | Accounts within a connection with mapping controls |
| `AccountMapper.razor` | — | Dropdown to map bank account → app account |
| `SyncStatusBar.razor` | — | Global sync status indicator (last sync time, errors) |
| `PlaidLinkInterop.razor` | — | JS interop wrapper for Plaid Link widget |
| `SyncStatusBadge.razor` | — | Sidebar badge showing sync health |

### Page Layout: Bank Connections

```
┌──────────────────────────────────────────────────────┐
│  Bank Connections                  [+ Link Account]  │
├──────────────────────────────────────────────────────┤
│  Last sync: 2 hours ago            [Sync Now]        │
├──────────────────────────────────────────────────────┤
│  ┌─ Chase ──────────────────────────────────────┐    │
│  │ Status: ● Active    Last sync: 2h ago        │    │
│  │                                               │    │
│  │ Checking ····1234  → [Checking Account ▼] ✅  │    │
│  │ Savings ·····5678  → [Savings Account ▼]  ✅  │    │
│  │ Credit ······9012  → [Not mapped      ▼]  ☐  │    │
│  │                                               │    │
│  │              [Refresh Auth]  [Remove]          │    │
│  └───────────────────────────────────────────────┘    │
│                                                       │
│  ┌─ Capital One ────────────────────────────────┐    │
│  │ Status: ⚠ Requires Reauth   Last: 3 days ago│    │
│  │                                               │    │
│  │ Credit ······4567  → [Credit Card ▼]     ✅  │    │
│  │                                               │    │
│  │              [Re-authenticate]  [Remove]       │    │
│  └───────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────┘
```

### Sync Status Indicators

- **Sidebar badge:** Small indicator next to "Bank Connections" nav item showing health (green dot = all active, yellow = stale > 24h, red = error/reauth needed)
- **Account detail pages:** "Last synced: X hours ago" on accounts linked to a bank
- **Transaction list:** Bank-synced transactions show a bank icon (🏦) vs. the existing import icon for CSV imports

---

## Data Enrichment Opportunities

Bank-provided data can enhance the existing categorization pipeline:

1. **Merchant normalization:** Plaid provides cleaned merchant names (e.g., `AMZN MKTP US*AB1CD2EFG` → `Amazon`). Feed into `ImportTransactionCreator` as a suggestion — not auto-applied over user rules.
2. **Category suggestions:** Plaid returns category arrays (e.g., `["Food and Drink", "Restaurants"]`). Map to the app's `BudgetCategory` taxonomy as suggestions shown in the UI.
3. **Pending → settled transitions:** When a pending transaction settles, amount and description often change. Track the transition and alert the user if the amount changed significantly (> $5 or > 10%).
4. **Location data:** Plaid provides location data for some transactions (city, region, lat/lng). Feed into the existing `TransactionLocationValue` enrichment path.

---

## Acceptance Criteria (Testable)

### Domain Layer (Vendor-Agnostic)

| ID | Criterion | Test Type |
|----|-----------|-----------|
| AC-126-01 | `BankConnection.Create()` sets Status to Active and generates a Version7 GUID | Unit |
| AC-126-02 | `BankConnection.Create()` throws `ArgumentException` when `itemId` is null/empty | Unit |
| AC-126-03 | `BankConnection.MarkError()` sets Status to Error and stores error message | Unit |
| AC-126-04 | `BankConnection.MarkRequiresReauth()` sets Status to RequiresReauth | Unit |
| AC-126-05 | `BankConnection.UpdateSyncState()` updates cursor, timestamp, clears error, sets Active | Unit |
| AC-126-06 | `LinkedAccount.Create()` sets IsSyncEnabled to false by default | Unit |
| AC-126-07 | `LinkedAccount.MapToAppAccount()` sets AppAccountId and enables sync | Unit |
| AC-126-08 | `LinkedAccount.EnableSync()` throws when AppAccountId is null | Unit |
| AC-126-09 | `LinkedAccount.Unmap()` clears AppAccountId and disables sync | Unit |

### Application Layer (Vendor-Agnostic)

| ID | Criterion | Test Type |
|----|-----------|-----------|
| AC-126-10 | `BankSyncService.SyncConnectionAsync` creates new transactions for "added" bank transactions not in the system | Unit |
| AC-126-11 | `BankSyncService.SyncConnectionAsync` updates amount/description for "modified" bank transactions matched by ExternalReference | Unit |
| AC-126-12 | `BankSyncService.SyncConnectionAsync` soft-marks "removed" bank transactions without deleting user data | Unit |
| AC-126-13 | `BankSyncService.SyncConnectionAsync` does NOT overwrite user-applied Category on modified transactions | Unit |
| AC-126-14 | `BankSyncService.SyncConnectionAsync` updates BankConnection.LastSyncCursor after successful sync | Unit |
| AC-126-15 | `BankSyncService.SyncConnectionAsync` marks BankConnection as Error on IBankConnector failure | Unit |
| AC-126-16 | `BankSyncService.SyncConnectionAsync` pages through HasMore=true responses until complete | Unit |
| AC-126-17 | `BankSyncService.SyncAllAsync` processes all active connections and aggregates results | Unit |
| AC-126-18 | `BankConnectionService.CompleteLinkAsync` creates BankConnection and LinkedAccounts from LinkCompleteResult | Unit |
| AC-126-19 | `BankConnectionService.RemoveConnectionAsync` calls IBankConnector.RemoveLinkAsync and deactivates connection | Unit |
| AC-126-20 | `BankConnectionService.MapAccountAsync` maps a LinkedAccount to an app Account and enables sync | Unit |

### API Layer

| ID | Criterion | Test Type |
|----|-----------|-----------|
| AC-126-21 | `POST /bank-connections/link-session` returns 200 with link token | Integration |
| AC-126-22 | `POST /bank-connections/complete-link` with invalid public token returns 400 | Integration |
| AC-126-23 | `GET /bank-connections` returns only the authenticated user's connections | Integration |
| AC-126-24 | `DELETE /bank-connections/{id}` returns 204 and deactivates the connection | Integration |
| AC-126-25 | `PUT /bank-connections/{id}/accounts/{id}/map` with non-existent app account returns 404 | Integration |
| AC-126-26 | `POST /bank-connections/sync` returns SyncResultDto with transaction counts | Integration |
| AC-126-27 | `GET /bank-connections/sync-status` returns status for all user connections | Integration |

### Infrastructure Layer (Vendor-Specific — Plaid)

| ID | Criterion | Test Type |
|----|-----------|-----------|
| AC-126-28 | `PlaidBankConnector.CreateLinkSessionAsync` calls Plaid `/link/token/create` and returns valid LinkSessionResult | Integration (sandbox) |
| AC-126-29 | `PlaidBankConnector.SyncTransactionsAsync` maps Plaid transaction response to BankTransaction records correctly | Unit |
| AC-126-30 | `PlaidBankConnector` handles Plaid error responses (ITEM_LOGIN_REQUIRED, etc.) and maps to appropriate LinkHealthResult | Unit |

### UI Layer

| ID | Criterion | Test Type |
|----|-----------|-----------|
| AC-126-31 | Bank connections page renders connection cards with institution name, status, and last sync time | bUnit |
| AC-126-32 | Account mapper dropdown shows only unmapped app accounts | bUnit |
| AC-126-33 | Sync status badge shows red indicator when any connection has Error status | bUnit |
| AC-126-34 | "Sync Now" button triggers sync and updates UI with results | bUnit |
| AC-126-35 | Connection card shows "Requires Re-authentication" with action button when status is RequiresReauth | bUnit |

### Multi-Vendor Support (Connector Registry & Selection)

| ID | Criterion | Test Type |
|----|-----------|-----------|
| AC-126-36 | `IBankConnectorRegistry.GetByRegion("US")` returns only US-enabled connectors (e.g., Plaid) | Unit |
| AC-126-37 | `IBankConnectorRegistry.GetByRegion("DE")` returns only EU-enabled connectors (e.g., Nordigen) | Unit |
| AC-126-38 | `IBankConnectorRegistry.Resolve("Plaid")` returns the registered `PlaidBankConnector` instance | Unit |
| AC-126-39 | `IBankConnectorRegistry.Resolve("InvalidConnector")` returns null gracefully | Unit |
| AC-126-40 | `BankConnection.Create()` stores the provided `connectorType` and retrieves it correctly | Unit |
| AC-126-41 | `BankConnectionService.CreateLinkSessionAsync` queries the registry for enabled connectors and filters by region | Integration |
| AC-126-42 | When user selects a connector during linking, the `BankConnection` stores the correct `ConnectorType` | Integration |
| AC-126-43 | `BankSyncService.SyncConnectionAsync` resolves the correct `IBankConnector` via registry using `BankConnection.ConnectorType` and invokes its methods | Unit |
| AC-126-44 | Multiple `BankConnection` records with different `ConnectorType` values sync independently without cross-contamination | Integration |
| AC-126-45 | UI "Link Bank Account" flow presents region/provider selection allowing user to choose Plaid (US) or Nordigen (EU) based on availability | bUnit |

---

## Implementation Plan

Each slice is a vertical cut delivering testable, deployable value.

### Slice 1: Domain Entities & Value Objects (Vendor-Agnostic)

**Objective:** Create the `BankConnection`, `LinkedAccount` entities and all value objects/records in the Domain layer.

**Tasks:**
- [ ] Create `BankConnection` entity with factory method and state transitions
- [ ] Create `LinkedAccount` entity with mapping lifecycle
- [ ] Create all value object records: `BankTransaction`, `TransactionSyncResult`, `LinkSessionResult`, `LinkCompleteResult`, `BankAccountInfo`, `LinkHealthResult`
- [ ] Create enums: `BankConnectionStatus`, `BankAccountType`
- [ ] Create `IBankConnector` interface
- [ ] Write unit tests: AC-126-01 through AC-126-09
- [ ] Create `IBankConnectionRepository` and `ILinkedAccountRepository` interfaces

**Commit:**
```bash
git commit -m "feat(domain): bank connectivity entities and IBankConnector abstraction

- BankConnection entity with Active/Error/RequiresReauth/Inactive lifecycle
- LinkedAccount entity with app account mapping and sync toggle
- IBankConnector interface (vendor-agnostic)
- Repository interfaces for persistence
- All value objects and enums

Refs: #126"
```

---

### Slice 2: Persistence Layer (EF Configuration + Migration)

**Objective:** EF Core configuration for new entities and database migration.

**Tasks:**
- [ ] Create `BankConnectionConfiguration` (table, indexes, encrypted token column)
- [ ] Create `LinkedAccountConfiguration` (table, FK to BankConnection, unique external ID index)
- [ ] Add `ImportBatchSource` enum and column to `ImportBatch` (migration-safe default: `Csv`)
- [ ] Create EF migration
- [ ] Implement `BankConnectionRepository` and `LinkedAccountRepository`

**Commit:**
```bash
git commit -m "feat(infra): bank connectivity persistence layer

- BankConnections and LinkedAccounts tables with indexes
- ImportBatch gains ImportSource column (Csv default)
- Repository implementations
- EF migration

Refs: #126"
```

---

### Slice 3: Application Services (Link & Manage)

**Objective:** `BankConnectionService` for link creation, completion, account management.

**Tasks:**
- [ ] Create `IBankConnectionService` interface
- [ ] Implement `CreateLinkSessionAsync`, `CompleteLinkAsync`, `GetConnectionsAsync`, `RemoveConnectionAsync`
- [ ] Implement `GetLinkedAccountsAsync`, `MapAccountAsync`, `UnmapAccountAsync`, `RefreshLinkAsync`
- [ ] Create contract DTOs: `BankConnectionDto`, `LinkedAccountDto`, `LinkSessionDto`, `CreateLinkSessionRequest`, `CompleteLinkRequest`, `MapAccountRequest`
- [ ] Write unit tests: AC-126-18, AC-126-19, AC-126-20
- [ ] DI registration in Application layer

**Commit:**
```bash
git commit -m "feat(app): bank connection management service

- IBankConnectionService with link/manage/map operations
- Contract DTOs for all bank connectivity requests/responses
- Unit tests for link completion, removal, and account mapping

Refs: #126"
```

---

### Slice 4: Sync Service (Core Transaction Sync Logic)

**Objective:** `BankSyncService` — the core logic for fetching and processing bank transactions.

**Tasks:**
- [ ] Create `IBankSyncService` interface
- [ ] Implement `SyncConnectionAsync` with full conflict resolution (add/modify/remove)
- [ ] Implement cursor pagination (HasMore loop)
- [ ] Implement `SyncAllAsync` (iterate all active connections)
- [ ] Implement `GetSyncStatusAsync`
- [ ] Write unit tests: AC-126-10 through AC-126-17
- [ ] Create `SyncResultDto`, `SyncStatusDto` contracts

**Commit:**
```bash
git commit -m "feat(app): bank transaction sync service

- IBankSyncService with per-connection and all-connections sync
- Conflict resolution: add new, update modified (preserve categories), soft-remove
- Cursor-based pagination through vendor responses
- Error handling with BankConnection status updates

Refs: #126"
```

---

### Slice 5: Plaid Infrastructure Adapter

**Objective:** `PlaidBankConnector` implementing `IBankConnector` against Plaid's REST API.

**Tasks:**
- [ ] Create `PlaidOptions` configuration class
- [ ] Create `PlaidBankConnector` implementing `IBankConnector`
- [ ] Implement HTTP client calls to Plaid API (link/token/create, item/public_token/exchange, accounts/get, transactions/sync, item/remove)
- [ ] Map Plaid error codes to `LinkHealthResult` and appropriate status transitions
- [ ] Create `PlaidTransactionMapper` for Plaid response → `BankTransaction` mapping
- [ ] Write unit tests: AC-126-29, AC-126-30
- [ ] Write integration test with Plaid sandbox: AC-126-28
- [ ] Add Plaid configuration to `appsettings.json` (environment only, secrets via user-secrets)
- [ ] DI registration with `PlaidOptions` binding

**Commit:**
```bash
git commit -m "feat(infra): Plaid bank connector adapter

- PlaidBankConnector implementing IBankConnector
- HTTP client for Plaid REST API endpoints
- Plaid error code mapping to domain status
- PlaidTransactionMapper for response normalization
- Configuration via PlaidOptions (secrets in user-secrets)

Refs: #126"
```

---

### Slice 6: Token Encryption Service

**Objective:** Secure storage of OAuth access tokens using Data Protection API.

**Tasks:**
- [ ] Create `ITokenEncryptionService` interface in Application layer
- [ ] Implement `DataProtectionTokenEncryptionService` in Infrastructure
- [ ] Integrate with `BankConnectionService` (encrypt on store, decrypt on use)
- [ ] Configure Data Protection key storage for Raspberry Pi deployment
- [ ] Write unit tests for encrypt/decrypt round-trip

**Commit:**
```bash
git commit -m "feat(infra): token encryption for bank access tokens

- ITokenEncryptionService abstraction
- Data Protection API implementation
- Key storage configuration for self-hosted deployment
- Encrypt on store, decrypt before API calls

Refs: #126"
```

---

### Slice 7: API Controller

**Objective:** REST endpoints for bank connection management and sync triggering.

**Tasks:**
- [ ] Create `BankConnectionsController` with all endpoints from the API table
- [ ] Wire DI registration
- [ ] Write integration tests: AC-126-21 through AC-126-27
- [ ] Update `IBudgetApiService` client with all new API calls
- [ ] OpenAPI documentation with tags and examples

**Commit:**
```bash
git commit -m "feat(api): bank connections REST endpoints

- BankConnectionsController with link/manage/sync operations
- 12 endpoints for full bank connectivity workflow
- Integration tests for auth, validation, and happy paths
- OpenAPI documentation

Refs: #126"
```

---

### Slice 8: Background Sync Service

**Objective:** Hosted background service for automatic periodic transaction sync.

**Tasks:**
- [ ] Create `BankSyncBackgroundService` (implements `BackgroundService`)
- [ ] Configurable interval via `BankSyncOptions` (default: 6 hours)
- [ ] Error isolation per connection (one failure doesn't stop others)
- [ ] Logging with structured fields (connection ID, institution, result counts)
- [ ] Health check integration (report sync age)
- [ ] Write unit tests for scheduling logic

**Commit:**
```bash
git commit -m "feat(infra): background bank sync service

- BankSyncBackgroundService with configurable interval
- Error isolation per connection
- Structured logging for sync operations
- Health check integration for sync staleness

Refs: #126"
```

---

### Slice 9: Blazor UI — Bank Connections Page

**Objective:** Settings page for managing bank connections and account mapping.

**Tasks:**
- [ ] Create `BankConnectionsViewModel`
- [ ] Create `BankConnections.razor` page at `/settings/bank-connections`
- [ ] Create `BankConnectionCard.razor`, `LinkedAccountList.razor`, `AccountMapper.razor`
- [ ] Create `PlaidLinkInterop.razor` — JavaScript interop for Plaid Link widget
- [ ] Write ViewModel tests and bUnit tests: AC-126-31, AC-126-32, AC-126-35
- [ ] Add navigation link in settings sidebar

**Commit:**
```bash
git commit -m "feat(client): bank connections management page

- Bank connections page with connection cards
- Plaid Link widget JS interop for bank authentication
- Account mapping UI with app account dropdown
- Connection status indicators and actions

Refs: #126"
```

---

### Slice 10: Blazor UI — Sync Status & Indicators

**Objective:** Sync status visibility throughout the app.

**Tasks:**
- [ ] Create `SyncStatusBar.razor` component (global, shown on connections page)
- [ ] Create `SyncStatusBadge.razor` for sidebar navigation
- [ ] Add "Sync Now" button with loading state
- [ ] Add bank-synced transaction icon (🏦) to transaction list rows
- [ ] Show "Last synced: X ago" on account detail pages with linked bank accounts
- [ ] Write bUnit tests: AC-126-33, AC-126-34

**Commit:**
```bash
git commit -m "feat(client): sync status indicators and badges

- Sidebar badge with connection health color
- Sync Now button with loading state
- Bank-synced transaction icon in transaction list
- Last synced timestamp on account details

Refs: #126"
```

---

### Slice 11: Polish & Integration

**Objective:** Cross-feature integration and final polish.

**Tasks:**
- [ ] Integration with existing import pipeline: bank-synced `ImportBatch` entries show in import history with "Bank Sync" source label
- [ ] Integration with Feature 125 reconciliation: bank-synced transactions participate in cleared balance
- [ ] Integration with auto-categorization: bank merchant names fed to rule suggestion pipeline
- [ ] OpenAPI spec review and XML doc completions
- [ ] Update README feature list
- [ ] Configuration documentation (user-secrets setup for Plaid credentials)

**Commit:**
```bash
git commit -m "feat: bank connectivity polish and cross-feature integration

- Import history shows bank sync batches
- Bank transactions participate in reconciliation
- Merchant names feed auto-categorization suggestions
- Configuration documentation

Refs: #126"
```

---

## Out of Scope

- **Bill payment or money transfers** — This is read-only bank data access. No payment initiation, transfers, or write operations against bank accounts.
- **Webhook-based real-time sync** — Requires a public URL. Polling-only for MVP. Webhook support can be added later when/if a reverse proxy or tunnel is configured.
- **Multi-vendor simultaneous use** — One active `IBankConnector` adapter per deployment. The abstraction supports swapping, not running two vendors in parallel.
- **Investment/brokerage accounts** — Transaction accounts only (checking, savings, credit cards). Investment positions, holdings, and portfolio data are deferred.
- **Bank credential storage** — The app never handles or stores bank login credentials. OAuth access tokens only.
- **Automatic account creation** — When discovering bank accounts, the user must map them to existing app accounts or manually create new ones. No auto-creation from bank data.
- **Historical backfill beyond vendor limits** — If the vendor provides 24 months of history, that's the limit. No supplemental CSV import to fill gaps (though CSV import remains available for the user to do this manually).
- **Balance sync as authoritative** — Bank-reported account balances are shown informationally. The app's transaction-computed balance remains the source of truth for budgeting.

---

## Open Questions

1. **Plaid Link in Blazor WebAssembly.** Plaid's Link widget is a JavaScript library. Blazor WASM requires JS interop to host it. Should we (a) use `IJSRuntime` to load and invoke Plaid Link directly, or (b) host a small HTML page in an iframe that handles the Plaid flow and messages back to Blazor? Option (a) is simpler; option (b) isolates the third-party JS. Recommend (a) with a dedicated `plaid-link.js` interop file.

2. **Token encryption key backup.** If the Raspberry Pi's SD card fails, the Data Protection keys are lost and all stored access tokens become undecipherable. Users would need to re-link all bank accounts. Should we (a) accept this as an acceptable rare-event cost, (b) document a key backup procedure, or (c) use a key vault service? Recommend (b) — document key backup to external storage as part of the deployment guide.

3. **Sync interval configurability.** Should the sync interval be (a) a global app setting (same interval for all connections), (b) per-connection configurable, or (c) global with per-connection override? Recommend (a) for simplicity — a single `BankSync:IntervalHours` setting in `appsettings.json`.

4. **Pending transaction handling.** When a pending transaction settles with a different amount, should the app (a) silently update the amount, (b) show a notification that the amount changed, or (c) create a separate "adjustment" transaction? Recommend (b) — update in place but log the change and surface it in the UI if the delta exceeds a threshold.

5. **Rate limiting and Plaid API costs.** Plaid's Launch tier is free for 100 Items, but API calls are still rate-limited. How aggressively should the background sync retry on rate limit errors? Recommend exponential backoff (1 min → 5 min → 30 min → next scheduled cycle).

6. **Existing CSV-imported transactions and bank sync overlap.** When a user links a bank account that already has CSV-imported transactions, the initial bank sync will return transactions that already exist in the app (matched by date/amount/description but with different IDs). Should the sync (a) import them as duplicates and let Feature 125a's duplicate detection handle it, (b) run the existing `ImportDuplicateDetector` during sync to skip likely matches, or (c) offer a one-time "reconcile existing with bank" workflow? Recommend (b) for MVP — leverage existing duplicate detection during the initial sync, with (c) as a future enhancement.

7. **Sandbox vs. Production testing.** Plaid's sandbox uses fake institutions and data. Integration tests can use sandbox mode, but there's no way to test against a real bank without a production key. Should we (a) test only against sandbox in CI, (b) add an optional manual integration test suite that uses production credentials (excluded from CI), or (c) accept sandbox-only automated testing? Recommend (c) — sandbox tests in CI, real-bank testing is manual during initial development only.
