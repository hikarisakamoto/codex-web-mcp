package prompts

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"sync"
)

var (
	dirOnce sync.Once
	dirVal  string
	dirErr  error

	cacheMu  sync.Mutex
	tplCache = map[string]string{}
)

func Dir() (string, error) {
	dirOnce.Do(func() {
		var attempted []string
		if v := os.Getenv("CODEX_WEB_PROMPTS"); v != "" {
			attempted = append(attempted, v)
			if isPromptsDir(v) {
				dirVal = v
				return
			}
		}
		if exe, err := os.Executable(); err == nil {
			if p := walkUpForPrompts(filepath.Dir(exe), &attempted); p != "" {
				dirVal = p
				return
			}
		}
		if cwd, err := os.Getwd(); err == nil {
			if p := walkUpForPrompts(cwd, &attempted); p != "" {
				dirVal = p
				return
			}
		}
		dirErr = fmt.Errorf("prompts directory not found; tried: %s", strings.Join(attempted, ", "))
	})
	return dirVal, dirErr
}

func walkUpForPrompts(start string, attempted *[]string) string {
	cur := start
	for i := 0; i < 10; i++ {
		candidate := filepath.Join(cur, "prompts")
		*attempted = append(*attempted, candidate)
		if isPromptsDir(candidate) {
			return candidate
		}
		parent := filepath.Dir(cur)
		if parent == cur {
			break
		}
		cur = parent
	}
	return ""
}

func isPromptsDir(p string) bool {
	info, err := os.Stat(p)
	return err == nil && info.IsDir()
}

func Render(toolName string, vars map[string]string) (string, error) {
	cacheMu.Lock()
	tpl, ok := tplCache[toolName]
	cacheMu.Unlock()
	if !ok {
		d, err := Dir()
		if err != nil {
			return "", err
		}
		data, err := os.ReadFile(filepath.Join(d, toolName+".md"))
		if err != nil {
			return "", fmt.Errorf("read prompt %s: %w", toolName, err)
		}
		tpl = string(data)
		cacheMu.Lock()
		tplCache[toolName] = tpl
		cacheMu.Unlock()
	}
	out := tpl
	for k, v := range vars {
		out = strings.ReplaceAll(out, "{"+k+"}", v)
	}
	return out, nil
}
