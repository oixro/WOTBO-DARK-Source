#!/usr/bin/env bash
# Собирает WOTBO.exe и публикует его в oixro/WOTBO в правильном порядке:
# exe -> ver.json -> lastchange.json. Именно оттуда 2.33+ берут обновления.
#
# Usage:
#   ./build/release-public.sh                 # build + publish
#   ./build/release-public.sh --no-build      # publish ready artifacts/x64/Release/WOTBO.exe
#   ./build/release-public.sh --dry-run       # checks + local commit, no push
#   ./build/release-public.sh --notes FILE    # свой текст для lastchange.json
#   ./build/release-public.sh --allow-dirty   # не требовать чистый git status
#
# Requires: git credentials for github.com, python3, and (for build/version
# check) WOTBO_VM_SSH or WOTBO_VM_HOST as in build/remote-build.sh.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

PUBLIC_REPO="${WOTBO_PUBLIC_REPO:-https://github.com/oixro/WOTBO.git}"
PUBLIC_BRANCH="${WOTBO_PUBLIC_BRANCH:-main}"
ARTIFACT="$ROOT/artifacts/x64/Release/WOTBO.exe"
ASSEMBLY_INFO="$ROOT/Properties/AssemblyInfo.cs"
CHANGELOG="$ROOT/CHANGELOG.md"

NO_BUILD=0
DRY_RUN=0
ALLOW_DIRTY=0
NOTES_FILE=""

die() { echo "release: $*" >&2; exit 1; }
info() { echo "==> $*"; }

usage() {
    sed -n '2,15p' "$0" | sed 's/^# \{0,1\}//'
    exit 0
}

while [ $# -gt 0 ]; do
    case "$1" in
        --no-build) NO_BUILD=1 ;;
        --dry-run) DRY_RUN=1 ;;
        --allow-dirty) ALLOW_DIRTY=1 ;;
        --notes) [ $# -ge 2 ] || die "--notes требует путь к файлу"; NOTES_FILE="$2"; shift ;;
        -h|--help) usage ;;
        *) die "неизвестный аргумент: $1 (см. --help)" ;;
    esac
    shift
done

# ---------------------------------------------------------------- version ---
[ -f "$ASSEMBLY_INFO" ] || die "нет $ASSEMBLY_INFO"

FULL_VERSION="$(grep -m1 '^\[assembly: AssemblyVersion("' "$ASSEMBLY_INFO" \
    | sed -E 's/.*AssemblyVersion\("([0-9.]+)"\).*/\1/')"
[ -n "$FULL_VERSION" ] || die "не удалось прочитать AssemblyVersion"

# Короткая подпись: 2.34.1, 2.34.1.5 и т.п. — тем же правилом, что в MainForm.
DISPLAY_VERSION="$(python3 - "$FULL_VERSION" <<'PY'
import sys
parts = [int(x) for x in sys.argv[1].split(".")]
while len(parts) < 4:
    parts.append(0)
out = parts if parts[3] > 0 else parts[:3]
print(".".join(str(x) for x in out))
PY
)"

info "версия: $DISPLAY_VERSION ($FULL_VERSION)"

# ------------------------------------------------------------------- git ---
if [ "$ALLOW_DIRTY" -eq 0 ] && [ -n "$(git status --porcelain --untracked-files=no)" ]; then
    die "рабочее дерево грязное. Закоммить изменения или передай --allow-dirty"
fi

# ----------------------------------------------------------------- build ---
if [ "$NO_BUILD" -eq 0 ]; then
    info "сборка Release|x64 на VM"
    "$ROOT/build/remote-build.sh"
else
    info "сборка пропущена (--no-build)"
fi

[ -f "$ARTIFACT" ] || die "нет $ARTIFACT — сборка не дала exe"

SIZE="$(wc -c < "$ARTIFACT" | tr -d ' ')"
[ "$SIZE" -gt 102400 ] || die "exe подозрительно маленький: $SIZE байт"

if command -v shasum >/dev/null 2>&1; then
    SHA="$(shasum -a 256 "$ARTIFACT" | awk '{print $1}')"
else
    SHA="$(sha256sum "$ARTIFACT" | awk '{print $1}')"
fi
info "exe: $SIZE байт, sha256 $SHA"

# Версия должна совпадать с AssemblyInfo, иначе публикуем не то.
TARGET="${WOTBO_VM_SSH:-${WOTBO_VM_HOST:-}}"
if [ -n "$TARGET" ]; then
    VM_PATH="${WOTBO_VM_PATH:-C:/src/WOTBO}"
    REMOTE_VERSION="$(ssh "$TARGET" \
        "powershell -NoProfile -Command \"[Reflection.AssemblyName]::GetAssemblyName('$VM_PATH/artifacts/x64/Release/WOTBO.exe').Version.ToString()\"" \
        2>/dev/null | tr -d '\r' | tail -n1 || true)"
    REMOTE_VERSION="${REMOTE_VERSION//$'\n'/}"

    if [ -n "$REMOTE_VERSION" ] && [ "$REMOTE_VERSION" != "$FULL_VERSION" ]; then
        die "exe собран с версией $REMOTE_VERSION, а в AssemblyInfo $FULL_VERSION"
    fi
fi

# ---------------------------------------------------------- public repo ----
TMP_DIR="$(mktemp -d "${TMPDIR:-/tmp}/wotbo-release.XXXXXX")"
trap 'rm -rf "$TMP_DIR"' EXIT

info "клонирую публичный канал $PUBLIC_REPO ($PUBLIC_BRANCH)"
git clone --quiet --depth 1 --branch "$PUBLIC_BRANCH" "$PUBLIC_REPO" "$TMP_DIR/public"

REMOTE_VERSION_FILE="$TMP_DIR/public/ver.json"
REMOTE_VERSION="$( [ -f "$REMOTE_VERSION_FILE" ] && tr -d ' \r\n\357\273\277' < "$REMOTE_VERSION_FILE" || true)"
[ -n "$REMOTE_VERSION" ] || die "в публичном repo нет ver.json"

if ! python3 - "$REMOTE_VERSION" "$FULL_VERSION" <<'PY'
import sys

def parse(text):
    text = text.strip().lstrip("vV")
    parts = []
    for chunk in text.split("."):
        digits = ""
        for ch in chunk:
            if ch.isdigit():
                digits += ch
            else:
                break
        if not digits:
            break
        parts.append(int(digits))
    while len(parts) < 4:
        parts.append(0)
    return tuple(parts[:4])

raise SystemExit(0 if parse(sys.argv[2]) > parse(sys.argv[1]) else 1)
PY
then
    die "публичная версия $REMOTE_VERSION >= $FULL_VERSION. Подними AssemblyVersion/AssemblyFileVersion"
fi

info "публичная версия: $REMOTE_VERSION -> $FULL_VERSION"

# ----------------------------------------------------------------- notes ---
if [ -n "$NOTES_FILE" ]; then
    [ -f "$NOTES_FILE" ] || die "нет файла заметок: $NOTES_FILE"
    NOTES="$(cat "$NOTES_FILE")"
else
    NOTES="$(python3 - "$CHANGELOG" "$DISPLAY_VERSION" <<'PY'
import re
import sys

path, version = sys.argv[1], sys.argv[2]
text = open(path, encoding="utf-8").read()
match = re.search(r"(?m)^##\s+(.+?)\s*$", text)
if not match:
    raise SystemExit("в CHANGELOG нет ни одного заголовка ## ")

heading = match.group(1)
if version not in heading:
    raise SystemExit(
        "первый раздел CHANGELOG — «%s», а версия %s. "
        "Добавь раздел для новой версии или передай --notes FILE" % (heading, version)
    )

start = match.end()
nxt = re.search(r"(?m)^##\s+", text[start:])
body = text[start:start + nxt.start()] if nxt else text[start:]
print(body.strip())
PY
)" || die "не удалось взять список изменений из CHANGELOG"
fi

[ -n "$NOTES" ] || die "пустой lastchange.json — нечего публиковать"
info "заметки: $(printf '%s' "$NOTES" | head -n1)"

# ------------------------------------------------------------- assemble ----
cp "$ARTIFACT" "$TMP_DIR/public/WOTBO.exe"
printf '%s\n' "$FULL_VERSION" > "$TMP_DIR/public/ver.json"
printf '%s\n' "$NOTES" > "$TMP_DIR/public/lastchange.json"

info "изменения публичного канала:"
git -C "$TMP_DIR/public" status --short

if [ "$DRY_RUN" -eq 1 ]; then
    info "dry-run: коммиты и push не делаются"
    exit 0
fi

# ---------------------------------------------------------------- publish ---
# Порядок важен: сначала бинарник, потом версия. Иначе пользователь увидит
# новую версию и скачает старый exe, после чего обновление зациклится.
push_step() {
    local file="$1" message="$2"
    git -C "$TMP_DIR/public" add "$file"
    git -C "$TMP_DIR/public" commit --quiet -m "$message"
    git -C "$TMP_DIR/public" push --quiet origin "$PUBLIC_BRANCH"
    info "опубликовано: $file ($message)"
}

push_step "WOTBO.exe" "WOTBO $DISPLAY_VERSION: exe"
push_step "ver.json" "WOTBO $DISPLAY_VERSION: version"
push_step "lastchange.json" "WOTBO $DISPLAY_VERSION: changes"

echo
info "готово: $DISPLAY_VERSION опубликована"
echo "  exe:        https://raw.githubusercontent.com/oixro/WOTBO/$PUBLIC_BRANCH/WOTBO.exe"
echo "  версия:     https://raw.githubusercontent.com/oixro/WOTBO/$PUBLIC_BRANCH/ver.json"
echo "  изменения:  https://raw.githubusercontent.com/oixro/WOTBO/$PUBLIC_BRANCH/lastchange.json"
echo "  sha256:     $SHA"
