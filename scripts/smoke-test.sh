#!/usr/bin/env bash
# Smoke-tests an MCP server binary by sending initialize + tools/list over
# stdio and checking that all three expected tool names are advertised.
#
# Usage: smoke-test.sh <path-to-binary>

set -uo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: $0 <path-to-binary>" >&2
  exit 2
fi

binary="$1"

if [ ! -x "$binary" ]; then
  if [ ! -e "$binary" ]; then
    echo "FAIL: binary not found: $binary" >&2
    exit 1
  fi
fi

frame() {
  local body="$1"
  local len
  len=$(printf '%s' "$body" | wc -c | tr -d ' ')
  printf 'Content-Length: %s\r\n\r\n%s' "$len" "$body"
}

init='{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"smoke","version":"0.0.0"}}}'
initialized='{"jsonrpc":"2.0","method":"notifications/initialized"}'
tools_list='{"jsonrpc":"2.0","id":2,"method":"tools/list"}'

payload="$(frame "$init")$(frame "$initialized")$(frame "$tools_list")"

# Run the server with a 10-second wall-clock budget.
output_file="$(mktemp)"
err_file="$(mktemp)"
trap 'rm -f "$output_file" "$err_file"' EXIT

if command -v timeout >/dev/null 2>&1; then
  printf '%s' "$payload" | timeout 10 "$binary" >"$output_file" 2>"$err_file" || true
else
  # Fallback: launch in background, kill after 10s.
  printf '%s' "$payload" | "$binary" >"$output_file" 2>"$err_file" &
  pid=$!
  ( sleep 10 && kill "$pid" 2>/dev/null ) &
  watcher=$!
  wait "$pid" 2>/dev/null || true
  kill "$watcher" 2>/dev/null || true
fi

missing=()
for needle in web_search web_search_raw web_fetch_raw; do
  if ! grep -q "$needle" "$output_file"; then
    missing+=("$needle")
  fi
done

if [ ${#missing[@]} -eq 0 ]; then
  echo "PASS: all three tools advertised (web_search, web_search_raw, web_fetch_raw)"
  exit 0
else
  echo "FAIL: missing tool names: ${missing[*]}"
  echo "--- captured stdout ---"
  cat "$output_file"
  echo "--- captured stderr ---"
  cat "$err_file"
  exit 1
fi
