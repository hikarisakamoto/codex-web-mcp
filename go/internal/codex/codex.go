package codex

import (
	"context"
	"fmt"
	"os"
	"os/exec"
	"strings"
	"time"

	"codex-web-mcp/internal/config"
)

func Run(ctx context.Context, prompt string) string {
	tmp, err := os.CreateTemp("", "codex-out-*.txt")
	if err != nil {
		return fmt.Sprintf("ERROR: failed to create temp file: %v", err)
	}
	tmpPath := tmp.Name()
	_ = tmp.Close()
	defer os.Remove(tmpPath)

	timeoutCtx, cancel := context.WithTimeout(ctx, time.Duration(config.CodexTimeoutSeconds)*time.Second)
	defer cancel()

	args := []string{
		"exec",
		"--skip-git-repo-check",
		"--ephemeral",
		"--color", "never",
		"--sandbox", "read-only",
		"--output-last-message", tmpPath,
		prompt,
	}
	cmd := exec.CommandContext(timeoutCtx, config.CodexBin(), args...)
	cmd.Stdin = strings.NewReader("")
	cmd.Stdout = nil // discard
	var stderr strings.Builder
	cmd.Stderr = &stderrCapture{w: &stderr}

	if err := cmd.Run(); err != nil {
		if timeoutCtx.Err() == context.DeadlineExceeded {
			return "ERROR: codex timed out after 180s"
		}
		return fmt.Sprintf("ERROR: codex exited with error: %v: %s", err, strings.TrimSpace(stderr.String()))
	}

	info, err := os.Stat(tmpPath)
	if err != nil {
		return "ERROR: codex produced no output file"
	}
	if info.Size() == 0 {
		return "ERROR: codex output file empty"
	}
	data, err := os.ReadFile(tmpPath)
	if err != nil {
		return fmt.Sprintf("ERROR: failed to read codex output: %v", err)
	}
	s := strings.TrimRight(string(data), " \t\r\n")
	if s == "" {
		return "ERROR: codex output file empty"
	}
	return s
}

type stderrCapture struct {
	w *strings.Builder
}

func (s *stderrCapture) Write(p []byte) (int, error) {
	s.w.Write(p)
	return len(p), nil
}
