#!/usr/bin/env bash
# Sync the repository from macOS to the Windows VM over OpenSSH.
#
# Two ways to point the scripts at the VM:
#
#   # A. Alias from ~/.ssh/config (recommended, keeps the key settings there)
#   export WOTBO_VM_SSH=wotbo-vm
#
#   # B. Host and user explicitly
#   export WOTBO_VM_HOST=192.168.1.50
#   export WOTBO_VM_USER=wotbo
#
# Optional:
#   export WOTBO_VM_PATH=C:/src/WOTBO
#
# Notes:
#   * VM_PATH must not contain spaces. OpenSSH on Windows runs commands through
#     cmd.exe, and nested quoting there is unreliable.
#   * The VM must have OpenSSH Server enabled (see docs/VM-SETUP.md).

set -euo pipefail

VM_PATH="${WOTBO_VM_PATH:-C:/src/WOTBO}"
VM_TARGET="${WOTBO_VM_SSH:-}"

if [ -z "$VM_TARGET" ]; then
    if [ -z "${WOTBO_VM_HOST:-}" ]; then
        echo "Set WOTBO_VM_SSH=wotbo-vm (alias from ~/.ssh/config) or WOTBO_VM_HOST=<vm-ip>." >&2
        exit 1
    fi
    VM_TARGET="${WOTBO_VM_USER:-wotbo}@$WOTBO_VM_HOST"
fi

case "$VM_PATH" in
    *" "*) echo "WOTBO_VM_PATH must not contain spaces: $VM_PATH" >&2; exit 1 ;;
esac

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "Syncing $ROOT -> $VM_TARGET:$VM_PATH"

ssh "$VM_TARGET" "powershell -NoProfile -ExecutionPolicy Bypass -Command New-Item -ItemType Directory -Force -Path $VM_PATH"

# COPYFILE_DISABLE prevents macOS AppleDouble (._*) files from being copied.
COPYFILE_DISABLE=1 tar -C "$ROOT" \
    --exclude='.git' \
    --exclude='.vs' \
    --exclude='bin' \
    --exclude='obj' \
    --exclude='artifacts' \
    --exclude='.DS_Store' \
    --exclude='*.user' \
    --exclude='*.log' \
    -czf - . | ssh "$VM_TARGET" "tar -xzf - -C $VM_PATH"

echo "Done."
