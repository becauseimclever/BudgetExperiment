# Backup and Restore Drill Evidence Checklist

**Purpose:** This checklist is the operational evidence gate required for Feature 163 (data encryption) release readiness. An operator must complete and commit a filled-in copy of this checklist before any encrypted production release can be declared Done.

**Scope:** Covers scheduled backup execution, backup integrity verification, and the quarterly restore drill.

**Gate status required for release:** All items in sections 1–3 must show ✅ before tagging a production release.

---

## Section 1 — Backup Schedule Confirmation

| Item | Status | Evidence / Notes |
|------|--------|-----------------|
| Backup automation is configured (cron, CI schedule, or external scheduler) | ☐ | |
| Backup schedule runs at minimum daily on the production database | ☐ | |
| `backup-encrypted.sh` is the script used for all automated backups | ☐ | |
| Backup output directory is on durable, off-host storage | ☐ | |
| Backup retention policy is documented and enforced (minimum 30 days) | ☐ | |
| GPG recipient key is verified valid and accessible for the backup process | ☐ | |

**Completed by:** ___________________________  
**Date:** ___________________________

---

## Section 2 — Backup Integrity Spot-Check

Perform this check for the most recent production backup before each release.

| Step | Command / Action | Expected Result | Actual Result |
|------|-----------------|-----------------|---------------|
| Locate latest backup | `ls -lt <backup_dir>/*.sql.gz.gpg \| head -5` | Backup file exists within last 24 hours | |
| Verify co-located `.sha256` file exists | `ls <backup_file>.sha256` | File present | |
| Run checksum verification | `sha256sum --check <backup_file>.sha256` | `OK` | |
| Confirm backup file is non-zero size | `du -sh <backup_file>` | Size > 0 | |

**Backup file verified:** ___________________________  
**Checksum result:** ___________________________  
**Completed by:** ___________________________  
**Date:** ___________________________

---

## Section 3 — Quarterly Restore Drill

A restore drill must be performed at least once per quarter to a non-production (staging or dedicated DR) database. Document each drill below.

### Most Recent Drill

| Item | Value |
|------|-------|
| Drill date | |
| Backup file used (timestamp only, not path) | |
| Target environment | staging / DR (circle one) |
| Restore script version / commit SHA | |
| Checksum verification passed? | Yes / No |
| Restore completed without errors? | Yes / No |
| Row count / sanity check performed? | Yes / No — describe: |
| Any manual intervention required? | Yes / No — if yes, describe: |
| Drill duration (approx.) | |

**Drill conducted by:** ___________________________  
**Reviewed by:** ___________________________  
**Date:** ___________________________

### Drill History (rolling 12 months)

| Quarter | Date | Outcome | Operator |
|---------|------|---------|----------|
| Q1 20__ | | | |
| Q2 20__ | | | |
| Q3 20__ | | | |
| Q4 20__ | | | |

---

## Section 4 — Operational Owner

The following person is accountable for backup cadence, restore drill execution, and maintaining this artifact:

**Operational Owner:** ___________________________  
**Backup cadence commitment:** Daily automated + quarterly restore drill  
**Review cycle:** Quarterly; update before each production release

---

## Process Gate Instructions

1. Copy this template to `docs/operations/backup-restore-evidence-YYYYQN.md` at the start of each quarter (e.g., `backup-restore-evidence-2026Q2.md`).
2. Fill in Sections 1–4 before any production release tagged after the quarter starts.
3. Link the completed artifact from the release pull request description under "Operational Readiness".
4. The release review approver must verify all Section 1–3 items are ✅ before approving the release tag.

**CI Automated Coverage:** `BackupRestoreIntegrityTests` (in `tests/BudgetExperiment.Infrastructure.Tests/Encryption/`) validates the checksum format and verification logic used by the restore script on every CI run. This provides continuous evidence that the script's integrity gate cannot be bypassed. The checklist above covers the operational cadence and drill evidence that CI cannot automate.

---

*Last updated: 2026-04-28. Template owner: see Section 4.*
