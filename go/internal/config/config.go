package config

import (
	"os"
	"path/filepath"
	"runtime"
)

const CodexTimeoutSeconds = 180

func CodexBin() string {
	if v := os.Getenv("CODEX_BIN"); v != "" {
		return v
	}
	if runtime.GOOS == "windows" {
		return "codex.exe"
	}
	return "codex"
}

func CacheDir() (string, error) {
	if v := os.Getenv("CODEX_WEB_CACHE"); v != "" {
		if err := os.MkdirAll(v, 0o755); err != nil {
			return "", err
		}
		return v, nil
	}
	base, err := os.UserCacheDir()
	if err != nil {
		return "", err
	}
	dir := filepath.Join(base, "codex-web-mcp")
	if err := os.MkdirAll(dir, 0o755); err != nil {
		return "", err
	}
	return dir, nil
}

func CacheSubdir() (string, error) {
	base, err := CacheDir()
	if err != nil {
		return "", err
	}
	sub := filepath.Join(base, "cache")
	if err := os.MkdirAll(sub, 0o755); err != nil {
		return "", err
	}
	return sub, nil
}

func LogPath() (string, error) {
	base, err := CacheDir()
	if err != nil {
		return "", err
	}
	return filepath.Join(base, "calls.log"), nil
}

func AppendCallLog(line string) {
	p, err := LogPath()
	if err != nil {
		return
	}
	f, err := os.OpenFile(p, os.O_APPEND|os.O_CREATE|os.O_WRONLY, 0o644)
	if err != nil {
		return
	}
	defer f.Close()
	_, _ = f.WriteString(line + "\n")
}
