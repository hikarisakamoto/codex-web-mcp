"""Prompt template loader. Resolution: env var -> walk up from executable -> walk up from cwd."""

from __future__ import annotations

import os
import sys
import threading
from pathlib import Path

_dir_lock = threading.Lock()
_dir_value: Path | None = None
_tpl_lock = threading.Lock()
_tpl_cache: dict[str, str] = {}


def _is_prompts_dir(p: Path) -> bool:
    return p.is_dir()


def _walk_up(start: Path, attempted: list[str]) -> Path | None:
    cur = start
    for _ in range(10):
        cand = cur / "prompts"
        attempted.append(str(cand))
        if _is_prompts_dir(cand):
            return cand
        parent = cur.parent
        if parent == cur:
            break
        cur = parent
    return None


def resolve_dir() -> Path:
    global _dir_value
    with _dir_lock:
        if _dir_value is not None:
            return _dir_value
        attempted: list[str] = []
        env = os.environ.get("CODEX_WEB_PROMPTS")
        if env:
            attempted.append(env)
            ep = Path(env)
            if _is_prompts_dir(ep):
                _dir_value = ep
                return _dir_value
        try:
            exe_dir = Path(sys.argv[0]).resolve().parent
            found = _walk_up(exe_dir, attempted)
            if found:
                _dir_value = found
                return _dir_value
        except OSError:
            pass
        try:
            cwd = Path.cwd()
            found = _walk_up(cwd, attempted)
            if found:
                _dir_value = found
                return _dir_value
        except OSError:
            pass
        raise FileNotFoundError(
            "prompts directory not found; tried: " + ", ".join(attempted)
        )


def render(tool_name: str, vars: dict[str, str]) -> str:
    with _tpl_lock:
        tpl = _tpl_cache.get(tool_name)
    if tpl is None:
        d = resolve_dir()
        path = d / f"{tool_name}.md"
        tpl = path.read_text(encoding="utf-8")
        with _tpl_lock:
            _tpl_cache[tool_name] = tpl
    out = tpl
    for k, v in vars.items():
        out = out.replace("{" + k + "}", v)
    return out
