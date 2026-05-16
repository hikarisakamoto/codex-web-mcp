#!/usr/bin/env bash
# Builds every available implementation of codex-web-mcp.
# Flags:
#   --skip-cs       Skip the C# build.
#   --skip-fs       Skip the F# build.
#   --skip-go       Skip the Go build.
#   --skip-java     Skip the Java build.
#   --skip-node     Skip the Node TypeScript build.
#   --skip-python   Skip the Python build.

set -euo pipefail

skip_cs=0; skip_fs=0; skip_go=0; skip_java=0; skip_node=0; skip_python=0

for arg in "$@"; do
  case "$arg" in
    --skip-cs) skip_cs=1 ;;
    --skip-fs) skip_fs=1 ;;
    --skip-go) skip_go=1 ;;
    --skip-java) skip_java=1 ;;
    --skip-node) skip_node=1 ;;
    --skip-python) skip_python=1 ;;
    -h|--help)
      cat <<EOF
Usage: $(basename "$0") [--skip-cs] [--skip-fs] [--skip-go] [--skip-java] [--skip-node] [--skip-python]

Builds every implementation whose toolchain is on PATH.
EOF
      exit 0
      ;;
    *) echo "Unknown argument: $arg" >&2; exit 2 ;;
  esac
done

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
echo "Repository root: $repo_root"

has_cmd() { command -v "$1" >/dev/null 2>&1; }

# --- C# ---
if [ "$skip_cs" -eq 0 ]; then
  if has_cmd dotnet; then
    echo
    echo "==> Building C# implementation (Release / linux-x64)..."
    dotnet publish "$repo_root/csharp/src/CodexWebMcp/CodexWebMcp.csproj" \
      -c Release -r linux-x64 \
      -o "$repo_root/csharp/src/CodexWebMcp/publish"
    echo "    OK: $repo_root/csharp/src/CodexWebMcp/publish/CodexWebMcp"
  else
    echo "==> Skipping C# build (dotnet not on PATH)"
  fi
else
  echo "==> Skipping C# build (--skip-cs)"
fi

# --- F# ---
if [ "$skip_fs" -eq 0 ]; then
  if has_cmd dotnet; then
    echo
    echo "==> Building F# implementation (Release / linux-x64)..."
    dotnet publish "$repo_root/fsharp/src/CodexWebMcp.FSharp/CodexWebMcp.FSharp.fsproj" \
      -c Release -r linux-x64 --self-contained \
      -o "$repo_root/fsharp/src/CodexWebMcp.FSharp/publish"
    echo "    OK: $repo_root/fsharp/src/CodexWebMcp.FSharp/publish/CodexWebMcp.FSharp"
  else
    echo "==> Skipping F# build (dotnet not on PATH)"
  fi
else
  echo "==> Skipping F# build (--skip-fs)"
fi

# --- Go ---
if [ "$skip_go" -eq 0 ]; then
  if has_cmd go; then
    echo
    echo "==> Building Go implementation..."
    (cd "$repo_root/go" && go build -ldflags='-s -w' -o bin/codex-web-mcp ./cmd/codex-web-mcp)
    echo "    OK: $repo_root/go/bin/codex-web-mcp"
  else
    echo "==> Skipping Go build (go not on PATH)"
  fi
else
  echo "==> Skipping Go build (--skip-go)"
fi

# --- Java ---
if [ "$skip_java" -eq 0 ]; then
  if has_cmd mvn; then
    echo
    echo "==> Building Java implementation (mvn -B package)..."
    (cd "$repo_root/java" && mvn -B package -DskipTests)
    echo "    OK: $repo_root/java/target/codex-web-mcp.jar"
  else
    echo "==> Skipping Java build (mvn not on PATH)"
  fi
else
  echo "==> Skipping Java build (--skip-java)"
fi

# --- Node ---
if [ "$skip_node" -eq 0 ]; then
  if has_cmd npm; then
    echo
    echo "==> Building Node TypeScript implementation..."
    (cd "$repo_root/node" && npm install --no-audit --no-fund && npm run build)
    echo "    OK: $repo_root/node/dist/index.js"
  else
    echo "==> Skipping Node build (npm not on PATH)"
  fi
else
  echo "==> Skipping Node build (--skip-node)"
fi

# --- Python ---
if [ "$skip_python" -eq 0 ]; then
  if has_cmd python3 || has_cmd python; then
    echo
    echo "==> Installing Python implementation (pip install -e)..."
    py=$(command -v python3 || command -v python)
    (cd "$repo_root/python" && "$py" -m pip install --quiet -e .)
    echo "    OK: Python package installed (run with: python -m codex_web_mcp)"
  else
    echo "==> Skipping Python build (python not on PATH)"
  fi
else
  echo "==> Skipping Python build (--skip-python)"
fi

echo
echo "All requested builds completed."
