#!/usr/bin/env bash
set -euo pipefail

export AVALONIA_TELEMETRY_OPTOUT="${AVALONIA_TELEMETRY_OPTOUT:-1}"
export DOTNET_ROOT="${DOTNET_ROOT:-/opt/homebrew/opt/dotnet/libexec}"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$(pwd)/.dotnet_home}"

dotnet run --project Apteka.csproj
