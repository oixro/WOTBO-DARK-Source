#!/usr/bin/env bash
# Sync sources to the Windows VM, build there and pull build logs back.
#
# Usage:
#   export WOTBO_VM_SSH=wotbo-vm        # alias from ~/.ssh/config
#   # or: export WOTBO_VM_HOST=192.168.1.50 WOTBO_VM_USER=wotbo
#
# Optional:
#   export WOTBO_VM_PATH=C:/src/WOTBO
#   export WOTBO_CONFIG=Release

set -euo pipefail

VM_PATH="${WOTBO_VM_PATH:-C:/src/WOTBO}"
CONFIG="${WOTBO_CONFIG:-Release}"
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
SCP_PATH="/${VM_PATH#/}"

"$ROOT/build/sync.sh"

echo "Building $CONFIG|x64 in the VM..."
ssh "$VM_TARGET" \
    "powershell -NoProfile -ExecutionPolicy Bypass -File $VM_PATH/build/build.ps1 -Configuration $CONFIG"

mkdir -p "$ROOT/artifacts/logs"
if scp -q -r "$VM_TARGET:$SCP_PATH/artifacts/logs/." "$ROOT/artifacts/logs/"; then
    echo "Build logs copied to $ROOT/artifacts/logs"
else
    echo "Warning: could not copy build logs with scp. Use the VM shared folder if needed." >&2
fi

mkdir -p "$ROOT/artifacts/x64/$CONFIG"
if scp -q -r "$VM_TARGET:$SCP_PATH/artifacts/x64/$CONFIG/." "$ROOT/artifacts/x64/$CONFIG/"; then
    echo "Build output copied to $ROOT/artifacts/x64/$CONFIG"
else
    echo "Warning: could not copy build output with scp." >&2
fi

echo "Done."
