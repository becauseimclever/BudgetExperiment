#!/usr/bin/env bash
set -euo pipefail

# Restores an encrypted PostgreSQL backup created by backup-encrypted.sh.
# Usage:
#   ./scripts/operations/restore-encrypted.sh /path/to/backup.sql.gz.gpg
# Required env vars:
#   TARGET_DB_CONNECTION_STRING (or TARGET_DB_CONNECTION_STRING_FILE)
#   RESTORE_ENVIRONMENT (development|staging|production)
#   RESTORE_CONFIRM_TARGET (<host>/<database>)
#   RESTORE_ALLOW_DESTRUCTIVE=true
# Optional env vars:
#   ALLOW_DB_CONNECTION_STRING_FALLBACK=true (allows DB_CONNECTION_STRING fallback)
#   ALLOW_PRODUCTION_RESTORE=true (required when RESTORE_ENVIRONMENT=production)
#   RESTORE_NO_PROMPT=true (only for non-interactive automation)

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "ERROR: required command not found: $1" >&2
    exit 1
  fi
}

require_command gpg
require_command gunzip
require_command psql
require_command sha256sum

fail() {
  echo "ERROR: $1" >&2
  exit 1
}

load_env_from_file_if_set() {
  local name="$1"
  local file_name="${name}_FILE"
  local value="${!name:-}"
  local file_path="${!file_name:-}"

  if [[ -n "${value}" && -n "${file_path}" ]]; then
    fail "set only one of ${name} or ${file_name}, not both."
  fi

  if [[ -n "${file_path}" ]]; then
    if [[ ! -f "${file_path}" ]]; then
      fail "secret file not found: ${file_path}"
    fi

    value="$(<"${file_path}")"
    if [[ -z "${value}" ]]; then
      fail "secret file is empty: ${file_path}"
    fi

    export "${name}=${value}"
  fi
}

extract_connection_value() {
  local connection_string="$1"
  local key="$2"

  awk -v RS=';' -v FS='=' -v search_key="${key}" '
    BEGIN { IGNORECASE = 1 }
    {
      current_key = $1
      gsub(/^[[:space:]]+|[[:space:]]+$/, "", current_key)
      if (tolower(current_key) == tolower(search_key)) {
        current_value = $2
        gsub(/^[[:space:]]+|[[:space:]]+$/, "", current_value)
        print current_value
        exit
      }
    }
  ' <<< "${connection_string}"
}

if [[ "$#" -ne 1 ]]; then
  echo "Usage: $0 /path/to/backup.sql.gz.gpg" >&2
  exit 1
fi

input_file="$1"
if [[ ! -f "${input_file}" ]]; then
  fail "backup file not found: ${input_file}"
fi

load_env_from_file_if_set TARGET_DB_CONNECTION_STRING
load_env_from_file_if_set DB_CONNECTION_STRING

checksum_file="${input_file}.sha256"
if [[ ! -f "${checksum_file}" ]]; then
  fail "checksum file not found: ${checksum_file}"
fi

expected_checksum="$(awk '{ print $1 }' "${checksum_file}")"
if [[ -z "${expected_checksum}" ]]; then
  fail "checksum file is invalid: ${checksum_file}"
fi

actual_checksum="$(sha256sum "${input_file}" | awk '{ print $1 }')"
if [[ "${actual_checksum}" != "${expected_checksum}" ]]; then
  fail "checksum verification failed for ${input_file}"
fi

echo "Checksum verified for: ${input_file}"

target_connection="${TARGET_DB_CONNECTION_STRING:-}"
if [[ -z "${target_connection}" ]]; then
  if [[ "${ALLOW_DB_CONNECTION_STRING_FALLBACK:-false}" == "true" ]]; then
    target_connection="${DB_CONNECTION_STRING:-}"
  fi
fi

if [[ -z "${target_connection}" ]]; then
  fail "set TARGET_DB_CONNECTION_STRING (or TARGET_DB_CONNECTION_STRING_FILE)."
fi

restore_environment="${RESTORE_ENVIRONMENT:-}"
if [[ -z "${restore_environment}" ]]; then
  fail "set RESTORE_ENVIRONMENT to development, staging, or production."
fi

case "${restore_environment}" in
  development|staging|production)
    ;;
  *)
    fail "RESTORE_ENVIRONMENT must be development, staging, or production."
    ;;
esac

if [[ "${restore_environment}" == "production" && "${ALLOW_PRODUCTION_RESTORE:-false}" != "true" ]]; then
  fail "production restores require ALLOW_PRODUCTION_RESTORE=true."
fi

if [[ "${RESTORE_ALLOW_DESTRUCTIVE:-false}" != "true" ]]; then
  fail "restore requires RESTORE_ALLOW_DESTRUCTIVE=true before execution."
fi

target_host="$(extract_connection_value "${target_connection}" "Host")"
target_database="$(extract_connection_value "${target_connection}" "Database")"

if [[ -z "${target_host}" || -z "${target_database}" ]]; then
  fail "target connection string must include Host and Database."
fi

target_fingerprint="${target_host}/${target_database}"
if [[ "${RESTORE_CONFIRM_TARGET:-}" != "${target_fingerprint}" ]]; then
  fail "set RESTORE_CONFIRM_TARGET=${target_fingerprint} to confirm target database."
fi

if [[ -t 0 && "${RESTORE_NO_PROMPT:-false}" != "true" ]]; then
  echo ""
  echo "Restore target: ${target_fingerprint}"
  echo "Environment: ${restore_environment}"
  read -r -p "Type '${target_fingerprint}' to continue: " typed_target
  if [[ "${typed_target}" != "${target_fingerprint}" ]]; then
    fail "interactive target confirmation failed."
  fi
fi

# This streams decrypt + decompress directly into psql to avoid plaintext files on disk.
gpg --batch --decrypt "${input_file}" \
  | gunzip \
  | psql --set=ON_ERROR_STOP=1 --single-transaction --no-psqlrc "${target_connection}"

echo "Restore completed from: ${input_file}"
