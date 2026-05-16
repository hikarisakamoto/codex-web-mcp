import { test } from "node:test";
import * as assert from "node:assert/strict";
import * as fs from "node:fs";
import * as os from "node:os";
import * as path from "node:path";
import * as crypto from "node:crypto";

function withTempCache(fn: (dir: string) => void): void {
  const dir = path.join(os.tmpdir(), `codex-web-mcp-tests-${crypto.randomUUID().replace(/-/g, "")}`);
  fs.mkdirSync(dir, { recursive: true });
  const prev = process.env.CODEX_WEB_CACHE;
  process.env.CODEX_WEB_CACHE = dir;
  try {
    // Force re-import so the cache module reads the fresh env.
    fn(dir);
  } finally {
    if (prev === undefined) delete process.env.CODEX_WEB_CACHE;
    else process.env.CODEX_WEB_CACHE = prev;
    try {
      fs.rmSync(dir, { recursive: true, force: true });
    } catch {
      /* ignore */
    }
  }
}

test("keyHash is deterministic and 32 hex chars", async () => {
  const { keyHash } = await import("../src/cache.js");
  const a = keyHash("web_search", "hello world");
  const b = keyHash("web_search", "hello world");
  assert.equal(a, b);
  assert.equal(a.length, 32);
});

test("keyHash differs by tool", async () => {
  const { keyHash } = await import("../src/cache.js");
  assert.notEqual(keyHash("web_search", "x"), keyHash("web_search_raw", "x"));
});

test("keyHash differs by payload", async () => {
  const { keyHash } = await import("../src/cache.js");
  assert.notEqual(keyHash("web_search", "x"), keyHash("web_search", "y"));
});

test("round trip", async () => {
  const cache = await import("../src/cache.js");
  withTempCache(() => {
    cache.put("web_search", "query1", "the answer");
    assert.equal(cache.get("web_search", "query1", 3600), "the answer");
  });
});

test("miss returns null", async () => {
  const cache = await import("../src/cache.js");
  withTempCache(() => {
    assert.equal(cache.get("web_search", "never-written", 3600), null);
  });
});

test("expired returns null", async () => {
  const cache = await import("../src/cache.js");
  withTempCache((dir) => {
    cache.put("web_search", "stale", "old");
    const file = path.join(dir, "cache", `web_search-${cache.keyHash("web_search", "stale")}.json`);
    const past = (Date.now() - 2 * 3600 * 1000) / 1000;
    fs.utimesSync(file, past, past);
    assert.equal(cache.get("web_search", "stale", 60), null);
  });
});

test("within TTL returns content", async () => {
  const cache = await import("../src/cache.js");
  withTempCache(() => {
    cache.put("web_search", "fresh", "content");
    assert.equal(cache.get("web_search", "fresh", 3600), "content");
  });
});
