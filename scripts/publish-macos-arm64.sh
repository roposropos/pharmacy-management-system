#!/usr/bin/env bash
set -euo pipefail

export AVALONIA_TELEMETRY_OPTOUT="${AVALONIA_TELEMETRY_OPTOUT:-1}"
export DOTNET_ROOT="${DOTNET_ROOT:-/opt/homebrew/opt/dotnet/libexec}"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$(pwd)/.dotnet_home}"
SELF_CONTAINED="${SELF_CONTAINED:-true}"

dotnet publish Apteka.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained "${SELF_CONTAINED}" \
  -o dist/macos-arm64
