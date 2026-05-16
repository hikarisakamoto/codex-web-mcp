"""File-per-entry cache with SHA-256 keys and mtime-based TTL."""

from __future__ import annotations

import hashlib
import json
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

from . import config


def key_hash(tool: str, payload: str) -> str:
    h = hashlib.sha256(f"{tool}::{payload}".encode("utf-8")).hexdigest()
    return h[:32]


def _file_path(tool: str, payload: str) -> Path:
    return config.cache_subdir() / f"{tool}-{key_hash(tool, payload)}.json"


def get(tool: str, payload: str, ttl_seconds: int) -> str | None:
    try:
        path = _file_path(tool, payload)
        if not path.exists():
            return None
        if (time.time() - path.stat().st_mtime) > ttl_seconds:
            return None
        with open(path, encoding="utf-8") as f:
            entry = json.load(f)
        c = entry.get("content")
        return c if isinstance(c, str) else None
    except (OSError, json.JSONDecodeError):
        return None


def put(tool: str, payload: str, content: str) -> None:
    try:
        path = _file_path(tool, payload)
        data = json.dumps(
            {"content": content, "timestamp": datetime.now(timezone.utc).isoformat()},
            ensure_ascii=False,
        )
        with open(path, "w", encoding="utf-8") as f:
            f.write(data)
    except OSError as e:
        print(f"[codex-web-mcp] cache write failed: {e}", file=sys.stderr)
