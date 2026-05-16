"""Spawn `codex exec`, read --output-last-message tempfile, return its contents."""

from __future__ import annotations

import os
import subprocess
import sys
import tempfile
import uuid

from . import config


def run(prompt: str) -> str:
    tmp_dir = tempfile.gettempdir()
    tmp_path = os.path.join(tmp_dir, f"codex-out-{uuid.uuid4().hex}.txt")

    args = [
        config.codex_bin(),
        "exec",
        "--skip-git-repo-check",
        "--ephemeral",
        "--color", "never",
        "--sandbox", "read-only",
        "--output-last-message", tmp_path,
        prompt,
    ]

    try:
        proc = subprocess.run(
            args,
            stdin=subprocess.DEVNULL,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.PIPE,
            timeout=config.CODEX_TIMEOUT_SECONDS,
            check=False,
        )
    except subprocess.TimeoutExpired:
        return "ERROR: codex timed out after 180s"
    except FileNotFoundError as e:
        _cleanup(tmp_path)
        return f"ERROR: codex invocation failed: {e}"
    except OSError as e:
        _cleanup(tmp_path)
        return f"ERROR: codex invocation failed: {e}"

    try:
        if proc.returncode != 0:
            stderr_tail = (proc.stderr or b"").decode("utf-8", errors="replace").strip()
            return f"ERROR: codex exited with code {proc.returncode}: {stderr_tail}"

        if not os.path.exists(tmp_path):
            return "ERROR: codex produced no output file"
        if os.path.getsize(tmp_path) == 0:
            return "ERROR: codex output file empty"
        with open(tmp_path, encoding="utf-8") as f:
            data = f.read().rstrip(" \t\r\n")
        if not data:
            return "ERROR: codex output file empty"
        return data
    finally:
        _cleanup(tmp_path)


def _cleanup(path: str) -> None:
    try:
        os.remove(path)
    except OSError:
        pass
