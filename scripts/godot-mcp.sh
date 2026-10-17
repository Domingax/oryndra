#!/usr/bin/env bash
#
# The MCP server OpenCode starts from opencode.json (docs/agents/godot-mcp.md).
#
# It is **observability only**. Nothing in `./verify.sh` and nothing in CI starts
# it, and no build, test or gate depends on it: it exists so that an agent can
# look at a running Godot — the live scene tree, the debug output, a screenshot —
# instead of reasoning about it from the source alone. A red or missing MCP
# server never makes a change wrong.
#
# The server is the engine-agnostic Godot MCP Runtime, pinned below because an
# unpinned `npx -y` would fetch whatever npm published today (docs/toolchain.md).
#
# stdio belongs to the MCP client: the server speaks JSON-RPC on stdout, so this
# script prints nothing and hands its own stdio over with `exec`.
set -euo pipefail

GODOT_PATH="${GODOT_BIN:-$(command -v godot-mono || true)}"

if [[ -z "$GODOT_PATH" || ! -x "$GODOT_PATH" ]]; then
  echo "godot-mono must be on the PATH, or GODOT_BIN must point at it" >&2
  echo "(the .NET build of Godot; see docs/toolchain.md)." >&2
  exit 1
fi

export GODOT_PATH

exec npx -y godot-mcp-runtime@3.8.1
