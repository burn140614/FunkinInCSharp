#!/usr/bin/env bash
# Builds the C# port, generates a minimal asset tree and plays one song with Botplay.
# Exits non-zero when the playthrough fails, so it can run in CI.
set -euo pipefail

cd "$(dirname "$0")/../.."

./scripts/csharp/make-test-assets.py build/test-assets

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"

dotnet build src/Funkin.Game/Funkin.Game.csproj -c Release --no-incremental

BINARY="src/Funkin.Game/bin/Release/net8.0/linux-x64/Funkin"

if command -v Xvfb >/dev/null 2>&1; then
  Xvfb :99 -screen 0 1280x720x24 >/dev/null 2>&1 &
  XVFB_PID=$!
  trap 'kill $XVFB_PID 2>/dev/null || true' EXIT
  sleep 1
  export DISPLAY=:99
fi

export SDL_AUDIODRIVER=dummy

"$BINARY" --root="$(pwd)/build/test-assets" --smoke-song=test --smoke-timeout=90
