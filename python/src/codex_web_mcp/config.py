"""Config knobs and paths. Env-var first, OS-default second."""

from __future__ import annotations

import os
import sys
from pathlib import Path

CODEX_TIMEOUT_SECONDS = 180


def codex_bin() -> str:
    v = os.environ.get("CODEX_BIN")
    if v:
        return v
    return "codex.exe" if sys.platform == "win32" else "codex"


def cache_dir() -> Path:
    v = os.environ.get("CODEX_WEB_CACHE")
    if v:
        p = Path(v)
        p.mkdir(parents=True, exist_ok=True)
        return p
    if sys.platform == "win32":
        base = Path(os.environ.get("LOCALAPPDATA") or (Path.home() / "AppData" / "Local"))
    elif sys.platform == "darwin":
        base = Path.home() / "Library" / "Caches"
    else:
        base = Path(os.environ.get("XDG_CACHE_HOME") or (Path.home() / ".cache"))
    p = base / "codex-web-mcp"
    p.mkdir(parents=True, exist_ok=True)
    return p


def cache_subdir() -> Path:
    p = cache_dir() / "cache"
    p.mkdir(parents=True, exist_ok=True)
    return p


def log_path() -> Path:
    return cache_dir() / "calls.log"


def append_call_log(line: str) -> None:
    try:
        with open(log_path(), "a", encoding="utf-8") as f:
            f.write(line + "\n")
    except OSError:
        pass
