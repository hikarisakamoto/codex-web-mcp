"""Tests mirror Go/C# cache tests: deterministic hashing, round-trip, TTL via mtime."""

from __future__ import annotations

import os
import time
from pathlib import Path

import pytest

from codex_web_mcp import cache


@pytest.fixture
def tmp_cache(tmp_path: Path, monkeypatch: pytest.MonkeyPatch) -> Path:
    monkeypatch.setenv("CODEX_WEB_CACHE", str(tmp_path))
    return tmp_path


def test_key_hash_deterministic() -> None:
    a = cache.key_hash("web_search", "hello world")
    b = cache.key_hash("web_search", "hello world")
    assert a == b
    assert len(a) == 32


def test_key_hash_differs_by_tool() -> None:
    a = cache.key_hash("web_search", "x")
    b = cache.key_hash("web_search_raw", "x")
    assert a != b


def test_key_hash_differs_by_payload() -> None:
    a = cache.key_hash("web_search", "x")
    b = cache.key_hash("web_search", "y")
    assert a != b


def test_round_trip(tmp_cache: Path) -> None:
    cache.put("web_search", "query1", "the answer")
    got = cache.get("web_search", "query1", 3600)
    assert got == "the answer"


def test_get_miss(tmp_cache: Path) -> None:
    got = cache.get("web_search", "never-written", 3600)
    assert got is None


def test_get_expired(tmp_cache: Path) -> None:
    cache.put("web_search", "stale", "old")
    path = tmp_cache / "cache" / f"web_search-{cache.key_hash('web_search', 'stale')}.json"
    past = time.time() - 2 * 3600
    os.utime(path, (past, past))
    assert cache.get("web_search", "stale", 60) is None


def test_get_within_ttl(tmp_cache: Path) -> None:
    cache.put("web_search", "fresh", "content")
    assert cache.get("web_search", "fresh", 3600) == "content"
