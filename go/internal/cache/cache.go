package cache

import (
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"time"

	"codex-web-mcp/internal/config"
)

type Entry struct {
	Content   string `json:"content"`
	Timestamp string `json:"timestamp"`
}

func KeyHash(tool, payload string) string {
	h := sha256.Sum256([]byte(tool + "::" + payload))
	return hex.EncodeToString(h[:])[:32]
}

func filePath(tool, payload string) (string, error) {
	sub, err := config.CacheSubdir()
	if err != nil {
		return "", err
	}
	return filepath.Join(sub, fmt.Sprintf("%s-%s.json", tool, KeyHash(tool, payload))), nil
}

func Get(tool, payload string, ttlSeconds int) (string, bool) {
	p, err := filePath(tool, payload)
	if err != nil {
		return "", false
	}
	info, err := os.Stat(p)
	if err != nil {
		return "", false
	}
	if time.Since(info.ModTime()) > time.Duration(ttlSeconds)*time.Second {
		return "", false
	}
	data, err := os.ReadFile(p)
	if err != nil {
		return "", false
	}
	var e Entry
	if err := json.Unmarshal(data, &e); err != nil {
		return "", false
	}
	return e.Content, true
}

func Put(tool, payload, content string) {
	p, err := filePath(tool, payload)
	if err != nil {
		log.Printf("cache put: %v", err)
		return
	}
	e := Entry{Content: content, Timestamp: time.Now().UTC().Format(time.RFC3339)}
	data, err := json.Marshal(&e)
	if err != nil {
		log.Printf("cache marshal: %v", err)
		return
	}
	if err := os.WriteFile(p, data, 0o644); err != nil {
		log.Printf("cache write: %v", err)
		return
	}
}
