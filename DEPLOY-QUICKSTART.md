# Quick Start: Deployment

This guide gives you two deployment paths:

1. Demo mode for fast evaluation.
2. Raspberry Pi production-like deployment using CI-built images.

For local development, use .NET tools (`dotnet run`) and do not use Docker.

## Demo Mode (Fastest)

Run a zero-setup demo with bundled PostgreSQL and authentication disabled.

```bash
git clone https://github.com/becauseimclever/BudgetExperiment.git
cd BudgetExperiment
docker compose -f docker-compose.demo.yml up -d
```

Open http://localhost:5099.

Useful demo commands:

```bash
# Stop (keeps data)
docker compose -f docker-compose.demo.yml down

# Stop and remove demo data
docker compose -f docker-compose.demo.yml down -v

# Update to latest image
docker compose -f docker-compose.demo.yml pull
docker compose -f docker-compose.demo.yml up -d
```

## Raspberry Pi Deployment (Production-Like)

This path pulls pre-built images from GitHub Container Registry.

### 1. Prepare the Raspberry Pi

Run on the Pi:

```bash
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker "$USER"
sudo apt-get update
sudo apt-get install -y docker-compose-plugin
```

Log out and back in.

### 2. Authenticate to GitHub Container Registry

Run on the Pi:

```bash
docker login ghcr.io
```

Use a GitHub token with `read:packages` scope when prompted.

### 3. Create deployment folder and environment file

Run on the Pi:

```bash
mkdir -p ~/BudgetExperiment
cd ~/BudgetExperiment
nano .env
chmod 600 .env
```

Add values like this (replace placeholders):

```env
DB_CONNECTION_STRING=Host=YOUR_DB_HOST;Port=5432;Database=budgetexperiment;Username=YOUR_USER;Password=YOUR_PASSWORD
ENCRYPTION_MASTER_KEY=BASE64_ENCODED_32_BYTE_KEY
AUTHENTIK_ENABLED=true
AUTHENTIK_AUTHORITY=https://auth.example.com/application/o/budget-experiment/
AUTHENTIK_AUDIENCE=budget-experiment
AUTHENTIK_REQUIRE_HTTPS=true
```

Important:

- `ENCRYPTION_MASTER_KEY` is required for production-like deployments.
- Keep `.env` private and never commit it.

### 4. Start the app

Run on the Pi in the repository root:

```bash
docker compose -f docker-compose.pi.yml up -d
```

For a second instance, run:

```bash
docker compose -f docker-compose.pi.yml -f docker-compose.second.yml up -d
```

### 5. Verify

Run on the Pi:

```bash
docker compose -f docker-compose.pi.yml ps
curl http://localhost:5099/health
```

## Encrypted Backup and Restore (Feature 163)

Use these scripts from the repository root on your deployment host:

- `scripts/operations/backup-encrypted.sh`
- `scripts/operations/restore-encrypted.sh`

### Backup (safer secret loading)

Store secrets in files with restricted permissions and reference them with `*_FILE` variables.

```bash
install -m 600 /dev/null /secure/db-conn.txt
install -m 600 /dev/null /secure/gpg-recipient.txt
# write values into those files using your secure process

DB_CONNECTION_STRING_FILE=/secure/db-conn.txt \
BACKUP_GPG_RECIPIENT_FILE=/secure/gpg-recipient.txt \
./scripts/operations/backup-encrypted.sh
```

The script outputs:

- `backups/<prefix>-<timestamp>.sql.gz.gpg`
- `backups/<prefix>-<timestamp>.sql.gz.gpg.sha256`

### Restore (checksum + guardrails)

Restore now enforces checksum verification and explicit target confirmations before execution.

```bash
TARGET_DB_CONNECTION_STRING_FILE=/secure/target-db-conn.txt \
RESTORE_ENVIRONMENT=staging \
RESTORE_ALLOW_DESTRUCTIVE=true \
RESTORE_CONFIRM_TARGET=db.internal/budgetexperiment \
./scripts/operations/restore-encrypted.sh backups/<backup-file>.sql.gz.gpg
```

For production restores, also set:

```bash
ALLOW_PRODUCTION_RESTORE=true
```

## Troubleshooting

1. Cannot pull images:
- Re-run `docker login ghcr.io`.
- Confirm token has `read:packages`.

2. App cannot reach database:
- Confirm `DB_CONNECTION_STRING` in `.env`.
- Test DB network access from the Pi.

3. Restore blocked by guardrails:
- Confirm `RESTORE_CONFIRM_TARGET` exactly matches `<Host>/<Database>` from your target connection string.
- Confirm `RESTORE_ALLOW_DESTRUCTIVE=true` is set.
- Confirm `ALLOW_PRODUCTION_RESTORE=true` when restoring to production.

## References

- `docs/ci-cd-deployment.md`
- `docs/SECURITY-ENCRYPTION.md`
- `docs/AUTH-PROVIDERS.md`
