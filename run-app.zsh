#!/usr/bin/env zsh

set -euo pipefail

PROJECT_ROOT="${0:A:h}"
PROJECT_FILE="$PROJECT_ROOT/src/QrWifiConnect/QrWifiConnect.csproj"
TARGET_FRAMEWORK="net10.0-maccatalyst"
CONFIGURATION="${1:-Debug}"

log() {
  echo "[run-app] $1"
}

fail() {
  echo "[run-app] ERROR: $1" >&2
  exit 1
}

check_command() {
  local cmd="$1"
  local install_hint="$2"

  if ! command -v "$cmd" >/dev/null 2>&1; then
    fail "Missing dependency: '$cmd'. $install_hint"
  fi
}

if [[ ! -f "$PROJECT_FILE" ]]; then
  fail "Project file not found at '$PROJECT_FILE'."
fi

log "Checking dependencies..."
check_command "dotnet" "Install .NET SDK 10 (or newer)."
check_command "xcodebuild" "Install Xcode and command line tools (xcode-select --install)."

if ! xcode-select -p >/dev/null 2>&1; then
  fail "Xcode command line tools are not configured. Run: xcode-select --install"
fi

if ! dotnet workload list | grep -qiE '(^|[[:space:]])maui(-maccatalyst)?([[:space:]]|$)'; then
  fail "MAUI workload is missing. Run: dotnet workload install maui"
fi

log "Restoring NuGet packages..."
dotnet restore "$PROJECT_FILE"

log "Building application ($CONFIGURATION, $TARGET_FRAMEWORK)..."
dotnet build "$PROJECT_FILE" -c "$CONFIGURATION" -f "$TARGET_FRAMEWORK"

log "Running application..."
dotnet run --project "$PROJECT_FILE" -c "$CONFIGURATION" -f "$TARGET_FRAMEWORK"
