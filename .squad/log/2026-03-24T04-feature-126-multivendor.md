# 2026-03-24: Feature 126 Multi-Vendor Bank Connector Update

**Scribe:** Copilot (via Alfred)

## Summary

Feature 126 (Bank Connectivity & Automatic Transaction Sync) updated to promote multi-vendor connector support from future consideration to first-class feature. Added ConnectorRegistry pattern, BankConnection.ConnectorType discriminator field, 10 new acceptance criteria, and architectural specifications for user-selectable connectors per region.

## Changes Made

1. **Feature Doc 126 (`docs/126-bank-connectivity-and-transaction-sync.md`)**
   - Removed Non-Goal NG3 (one active connector per deployment)
   - Added Goal G8 (user-selectable connector per account)
   - Introduced ConnectorRegistry pattern (IBankConnectorRegistry + ConnectorRegistryEntry)
   - Added BankConnection.ConnectorType field for vendor discrimination
   - Added 10 new acceptance criteria (AC-126-36 through AC-126-45)
   - New Connector Configuration section showing appsettings.json structure
   - Revised Vendor Recommendation: Plaid (US), Nordigen (EU), both active simultaneously

2. **Decision Record** (merged into `.squad/decisions/decisions.md`)
   - Documented multi-vendor as first-class feature
   - Outlined ConnectorRegistry architecture
   - Specified user selection at link time
   - Recorded implementation phases (Plaid, Nordigen, future vendors)

## Implementation Status

No code changes in this update—scope clarification and architectural specification only. TDD-based implementation follows in next phases (Phase 1: Plaid adapter; Phase 2: Nordigen adapter).

## Downstream

- Infrastructure: New adapter projects per vendor in `Infrastructure/BankConnectivity/{Vendor}/`
- API: Region filtering at endpoint level for user-facing connector list
- Configuration: appsettings.json per deployment environment
