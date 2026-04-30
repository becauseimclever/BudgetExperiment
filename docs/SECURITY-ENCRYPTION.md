# Security: Data Encryption at Rest

This guide explains how Budget Experiment encrypts sensitive data at rest, how to configure the master key, and what is still pending.

## What Is Implemented

Budget Experiment currently encrypts selected PostgreSQL columns using AES-256-GCM in the Infrastructure layer.

- Encryption service: `src/BudgetExperiment.Infrastructure/Encryption/EncryptionService.cs`
- Encryption interface: `src/BudgetExperiment.Domain/Services/IEncryptionService.cs`
- EF converters:
  - `src/BudgetExperiment.Infrastructure/Persistence/Converters/EncryptedStringConverter.cs`
  - `src/BudgetExperiment.Infrastructure/Persistence/Converters/EncryptedNullableStringConverter.cs`
  - `src/BudgetExperiment.Infrastructure/Persistence/Converters/EncryptedDecimalConverter.cs`

Ciphertext format is versioned (`enc::v1:`) and authenticated (tamper detection included).

## Encrypted Fields

- `Accounts.Name`
- `Transactions.Description`
- `Transactions.Amount.Amount`
- `ChatMessages.Content`
- `MonthlyReflections.IntentionText`
- `MonthlyReflections.GratitudeText`
- `MonthlyReflections.ImprovementText`
- `KaizenGoals.Description`
- `CategorizationRules.Pattern`

## Key Configuration

The encryption master key must be a Base64-encoded 32-byte value.

The app resolves keys in this order:

1. `ENCRYPTION_MASTER_KEY` environment variable
2. `Encryption:MasterKey` configuration key (for example, user secrets)

### Local development (recommended)

Generate a key in PowerShell:

```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
$key = [Convert]::ToBase64String($bytes)
$key
```

Set the key in user secrets:

```powershell
dotnet user-secrets set "Encryption:MasterKey" "<PASTE_BASE64_KEY>" --project src/BudgetExperiment.Api/BudgetExperiment.Api.csproj
```

### Container and deployment environments

Set `ENCRYPTION_MASTER_KEY` as an environment variable in your deployment environment.

Important: never commit this key to Git.

## Rotation Guidance (Current Manual Process)

Automated key rotation is not implemented yet. Current manual process:

1. Back up database and verify restore path.
2. Export data with the current key active.
3. Decrypt and re-encrypt data with a newly generated key in a controlled maintenance window.
4. Update `ENCRYPTION_MASTER_KEY` or `Encryption:MasterKey`.
5. Restart the API and verify read/write behavior.

Because this is manual, treat rotation as an operational task with a rollback plan.

## Encrypted Backup and Restore Operations

Feature 163 adds encrypted PostgreSQL backup scripts for deployment environments:

- `scripts/operations/backup-encrypted.sh`
- `scripts/operations/restore-encrypted.sh`

### Backup prerequisites

- `pg_dump`, `gzip`, `gpg`, and `sha256sum` installed
- `DB_CONNECTION_STRING` or `DB_CONNECTION_STRING_FILE` configured
- `BACKUP_GPG_RECIPIENT` or `BACKUP_GPG_RECIPIENT_FILE` configured to an imported GPG public key recipient

### Create an encrypted backup

```bash
DB_CONNECTION_STRING_FILE=/secure/db-conn.txt \
BACKUP_GPG_RECIPIENT_FILE=/secure/gpg-recipient.txt \
./scripts/operations/backup-encrypted.sh
```

Using `*_FILE` variables helps avoid placing secrets directly in shell history.

Outputs:

- `backups/<prefix>-<timestamp>.sql.gz.gpg`
- `backups/<prefix>-<timestamp>.sql.gz.gpg.sha256`

### Restore from an encrypted backup

```bash
TARGET_DB_CONNECTION_STRING_FILE=/secure/target-db-conn.txt \
RESTORE_ENVIRONMENT=staging \
RESTORE_ALLOW_DESTRUCTIVE=true \
RESTORE_CONFIRM_TARGET=db.internal/budgetexperiment \
./scripts/operations/restore-encrypted.sh backups/<backup-file>.sql.gz.gpg
```

For production restores, also set `ALLOW_PRODUCTION_RESTORE=true`.

Restore verifies the backup checksum before decrypting data and then streams decrypt + decompress into `psql` without writing plaintext SQL files to disk.

## Migration Notes

Feature 163 added migration `20260426235449_Feature163_EncryptedColumnStorageCompatibility` to make encrypted storage compatible with current converter output (for example, decimal and text columns stored as `text` where required).

The current rollout also supports legacy plaintext reads in the decryption service to reduce migration risk.

## Security Boundaries

Encryption at rest helps protect against database file or dump exposure.

Encryption at rest does not protect against:

- A fully compromised API process where decrypted values are already in memory
- SQL injection vulnerabilities
- Poor key handling practices

## Testing Coverage

Current tests include:

- Unit tests for key validation, round-trip encryption, tamper detection, and plaintext fallback
- Integration tests verifying ciphertext-at-rest and decrypted reads through EF Core

See:

- `tests/BudgetExperiment.Infrastructure.Tests/Encryption/EncryptionServiceTests.cs`
- `tests/BudgetExperiment.Infrastructure.Tests/Encryption/EncryptedStringConverterTests.cs`
- `tests/BudgetExperiment.Infrastructure.Tests/Encryption/EncryptedPersistenceTests.cs`

## Known Gaps

- No automated key rotation workflow
- No published encryption performance benchmark report yet

## Related Docs

- `docs/163-data-encryption-user-data.md`
- `docs/DEVELOPMENT.md`
