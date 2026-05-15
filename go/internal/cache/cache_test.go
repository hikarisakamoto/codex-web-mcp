package cache_test

import (
	"fmt"
	"os"
	"path/filepath"
	"testing"
	"time"

	"codex-web-mcp/internal/cache"
)

func cacheFilePath(t *testing.T, dir, tool, payload string) string {
	t.Helper()
	return filepath.Join(dir, "cache", fmt.Sprintf("%s-%s.json", tool, cache.KeyHash(tool, payload)))
}

func TestKeyHash_Deterministic(t *testing.T) {
	a := cache.KeyHash("web_search", "hello world")
	b := cache.KeyHash("web_search", "hello world")
	if a != b {
		t.Fatalf("hash not deterministic: %s vs %s", a, b)
	}
	if len(a) != 32 {
		t.Fatalf("hash length should be 32, got %d", len(a))
	}
}

func TestKeyHash_DiffersByTool(t *testing.T) {
	a := cache.KeyHash("web_search", "x")
	b := cache.KeyHash("web_search_raw", "x")
	if a == b {
		t.Fatalf("hashes should differ across tools, got %s", a)
	}
}

func TestKeyHash_DiffersByPayload(t *testing.T) {
	a := cache.KeyHash("web_search", "x")
	b := cache.KeyHash("web_search", "y")
	if a == b {
		t.Fatalf("hashes should differ across payloads, got %s", a)
	}
}

func TestRoundTrip(t *testing.T) {
	dir := t.TempDir()
	t.Setenv("CODEX_WEB_CACHE", dir)

	cache.Put("web_search", "query1", "the answer")
	got, ok := cache.Get("web_search", "query1", 3600)
	if !ok {
		t.Fatalf("expected ok=true, got false")
	}
	if got != "the answer" {
		t.Fatalf("expected 'the answer', got %q", got)
	}
}

func TestGet_Miss(t *testing.T) {
	dir := t.TempDir()
	t.Setenv("CODEX_WEB_CACHE", dir)

	got, ok := cache.Get("web_search", "never-written", 3600)
	if ok {
		t.Fatalf("expected ok=false, got true with %q", got)
	}
	if got != "" {
		t.Fatalf("expected empty string, got %q", got)
	}
}

func TestGet_Expired(t *testing.T) {
	dir := t.TempDir()
	t.Setenv("CODEX_WEB_CACHE", dir)

	cache.Put("web_search", "stale", "old")
	p := cacheFilePath(t, dir, "web_search", "stale")
	past := time.Now().Add(-2 * time.Hour)
	if err := os.Chtimes(p, past, past); err != nil {
		t.Fatalf("chtimes: %v", err)
	}

	got, ok := cache.Get("web_search", "stale", 60)
	if ok {
		t.Fatalf("expected ok=false (expired), got true with %q", got)
	}
	if got != "" {
		t.Fatalf("expected empty string, got %q", got)
	}
}

func TestGet_WithinTTL(t *testing.T) {
	dir := t.TempDir()
	t.Setenv("CODEX_WEB_CACHE", dir)

	cache.Put("web_search", "fresh", "content")
	got, ok := cache.Get("web_search", "fresh", 3600)
	if !ok {
		t.Fatalf("expected ok=true within TTL, got false")
	}
	if got != "content" {
		t.Fatalf("expected 'content', got %q", got)
	}
}
