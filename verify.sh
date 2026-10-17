#!/usr/bin/env bash
#
# The one command that decides whether a change is acceptable: every gate the
# repository has, in order, failing on the first one that breaks.
#
#   restore -> build with warnings as errors -> format check -> boot the project
#   headlessly -> tests
#
# The Godot parts need the .NET build of Godot 4.7.2 (see docs/toolchain.md).
# Point GODOT_BIN at it, or have `godot-mono` on the PATH:
#
#   GODOT_BIN=/path/to/godot ./verify.sh
#
set -euo pipefail

cd "$(dirname "$0")"

RUNSETTINGS=tests/Oryndra.Game.Tests/.runsettings
SOLUTION=oryndra.sln

if [ -z "${GODOT_BIN:-}" ]; then
  GODOT_BIN=$(command -v godot-mono || true)
fi

if [ -z "${GODOT_BIN:-}" ] || [ ! -x "$GODOT_BIN" ]; then
  echo "GODOT_BIN must point at the .NET build of Godot 4.7.2 (see docs/toolchain.md)." >&2
  exit 1
fi

echo "==> restore"
dotnet restore "$SOLUTION"

echo "==> build (warnings are errors)"
dotnet build "$SOLUTION" --no-restore --configuration Debug -warnaserror

echo "==> format check"
dotnet format "$SOLUTION" --no-restore --verify-no-changes

echo "==> boot the project headlessly"
# project.godot, the main scene, the C# assembly and the simulation all have to
# come up before the tests below are worth running. Godot reports a failed boot in
# its output but still exits 0, so the ready line the game prints on its first
# tick is the signal that matters.
if ! boot_output=$("$GODOT_BIN" --headless --path game --quit-after 10 2>&1); then
  printf '%s\n' "$boot_output"
  echo "the project failed to run" >&2
  exit 1
fi

printf '%s\n' "$boot_output"
if ! printf '%s' "$boot_output" | grep -qF 'oryndra: simulation running'; then
  echo "the project booted but never advanced a simulation tick" >&2
  exit 1
fi

echo "==> tests"
export GODOT_BIN
dotnet test "$SOLUTION" --no-build --settings "$RUNSETTINGS"

echo "==> verify passed"
