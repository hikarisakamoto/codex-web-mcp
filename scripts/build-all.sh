#!/usr/bin/env bash
# Builds both the C# and Go implementations of codex-web-mcp.
# Flags:
#   --skip-cs   Skip the C# build.
#   --skip-go   Skip the Go build.

set -euo pipefail

skip_cs=0
skip_go=0

for arg in "$@"; do
  case "$arg" in
    --skip-cs) skip_cs=1 ;;
    --skip-go) skip_go=1 ;;
    -h|--help)
      cat <<EOF
Usage: $(basename "$0") [--skip-cs] [--skip-go]

Builds both implementations into their respective output directories.
EOF
      exit 0
      ;;
    *)
      echo "Unknown argument: $arg" >&2
      exit 2
      ;;
  esac
done

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
echo "Repository root: $repo_root"

if [ "$skip_cs" -eq 0 ]; then
  echo
  echo "==> Building C# implementation (Release / linux-x64)..."
  dotnet publish "$repo_root/csharp/src/CodexWebMcp/CodexWebMcp.csproj" \
    -c Release -r linux-x64 \
    -o "$repo_root/csharp/src/CodexWebMcp/publish"
  echo "    OK: $repo_root/csharp/src/CodexWebMcp/publish/CodexWebMcp"
else
  echo "==> Skipping C# build (--skip-cs)"
fi

if [ "$skip_go" -eq 0 ]; then
  echo
  echo "==> Building Go implementation..."
  (cd "$repo_root/go" && go build -ldflags='-s -w' -o bin/codex-web-mcp ./cmd/codex-web-mcp)
  echo "    OK: $repo_root/go/bin/codex-web-mcp"
else
  echo "==> Skipping Go build (--skip-go)"
fi

echo
echo "All requested builds completed successfully."
