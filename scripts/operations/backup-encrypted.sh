#!/usr/bin/env bash
set -euo pipefail

# Creates an encrypted PostgreSQL backup using pg_dump + gzip + GPG public-key encryption.
# Required env vars:
#   DB_CONNECTION_STRING (or DB_CONNECTION_STRING_FILE)
#   BACKUP_GPG_RECIPIENT (or BACKUP_GPG_RECIPIENT_FILE)
# Optional env vars:
#   BACKUP_OUTPUT_DIR (default: ./backups)
#   BACKUP_FILE_PREFIX (default: budgetexperiment)

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "ERROR: required command not found: $1" >&2
    exit 1
  fi
}

require_env() {
  local name="$1"
  if [[ -z "${!name:-}" ]]; then
    echo "ERROR: required environment variable is not set: ${name}" >&2
    exit 1
  fi
}

load_env_from_file_if_set() {
  local name="$1"
  local file_name="${name}_FILE"
  local value="${!name:-}"
  local file_path="${!file_name:-}"

  if [[ -n "${value}" && -n "${file_path}" ]]; then
    echo "ERROR: set only one of ${name} or ${file_name}, not both." >&2
    exit 1
  fi

  if [[ -n "${file_path}" ]]; then
    if [[ ! -f "${file_path}" ]]; then
      echo "ERROR: secret file not found: ${file_path}" >&2
      exit 1
    fi

    value="$(<"${file_path}")"
    if [[ -z "${value}" ]]; then
      echo "ERROR: secret file is empty: ${file_path}" >&2
      exit 1
    fi

    export "${name}=${value}"
  fi
}

require_command pg_dump
require_command gpg
require_command gzip
require_command sha256sum

load_env_from_file_if_set DB_CONNECTION_STRING
load_env_from_file_if_set BACKUP_GPG_RECIPIENT

require_env DB_CONNECTION_STRING
require_env BACKUP_GPG_RECIPIENT

backup_dir="${BACKUP_OUTPUT_DIR:-./backups}"
backup_prefix="${BACKUP_FILE_PREFIX:-budgetexperiment}"
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
output_file="${backup_dir}/${backup_prefix}-${timestamp}.sql.gz.gpg"
checksum_file="${output_file}.sha256"

mkdir -p "${backup_dir}"

pg_dump --dbname="${DB_CONNECTION_STRING}" --format=plain --no-owner --no-privileges \
  | gzip -9 \
  | gpg --batch --yes --trust-model always --recipient "${BACKUP_GPG_RECIPIENT}" --encrypt --output "${output_file}"

sha256sum "${output_file}" > "${checksum_file}"

echo "Encrypted backup created: ${output_file}"
echo "Checksum file created: ${checksum_file}"
