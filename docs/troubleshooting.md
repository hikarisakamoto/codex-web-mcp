# Troubleshooting

## "codex not found" / "executable file not found in $PATH"

The server can't locate the Codex CLI. Either:

- Add the directory containing `codex`/`codex.exe` to your `PATH`, or
- Set `CODEX_BIN` in the MCP server's env to an absolute path:
  ```json
  "env": { "CODEX_BIN": "C:\\Users\\you\\bin\\codex.exe" }
  ```

Verify by running `codex --version` in the same shell that launches Claude
Code.

## "prompts directory not found"

The prompt template loader exhausted all three resolution strategies. Fix
by setting `CODEX_WEB_PROMPTS` to the directory containing
`web_search.md`, `web_search_raw.md`, and `web_fetch_raw.md`:

```json
"env": { "CODEX_WEB_PROMPTS": "S:\\repos\\codex-web-mcp\\prompts" }
```

Or, easier: ensure the `prompts/` directory is somewhere up the tree from
your built binary. The build scripts don't copy it for you on purpose —
keep one canonical copy in the repo and point at it.

## "ERROR: codex exited with code N"

The Codex subprocess returned non-zero. The server includes the tail of
stderr in the error message. Common causes:

- **Auth**: Codex needs to be logged in. Run `codex auth status` in a
  shell.
- **Sandbox**: the read-only sandbox blocked something Codex tried to do.
  This shouldn't happen for `web_search` / `web_fetch` workloads, but a
  custom prompt template might trigger it. Check the stderr tail.
- **Network**: corporate proxies and TLS interception sometimes break
  Codex's HTTP client. Try `codex exec --sandbox read-only "ping
  example.com"` directly.

## Stdout JSON-RPC corruption / Claude Code disconnects immediately

Anything written to the server's stdout that isn't a framed JSON-RPC
response will desync the MCP channel. If you've forked the code and added
logging, route it to stderr — never stdout. The smoke-test scripts in
`scripts/` are a quick check: if `smoke-test` passes against your binary
but Claude still fails, the issue is somewhere else (auth, env, paths).

## Cache returns stale data

The on-disk cache TTL is 24 hours. To bust:

- Delete the cache directory:
  - Windows: `%LOCALAPPDATA%\codex-web-mcp\cache\`
  - Linux: `~/.cache/codex-web-mcp/cache/`
  - macOS: `~/Library/Caches/codex-web-mcp/cache/`
- Or override with `CODEX_WEB_CACHE` pointing at a fresh empty directory.
- Or edit the prompt template — the cache key includes the rendered
  prompt, so changing the template invalidates its entries.

## Smoke test fails with no output

The binary likely crashed before responding. Diagnose by running it
directly:

```powershell
./csharp/src/CodexWebMcp/publish/CodexWebMcp.exe --help
```

```bash
./go/bin/codex-web-mcp --help
```

If those succeed, the next suspect is environment: try launching with
`CODEX_BIN`, `CODEX_WEB_PROMPTS`, and `CODEX_WEB_CACHE` set explicitly to
known-good absolute paths. Watch stderr — the server logs every startup
step there.

## Tool returns `<untrusted>...</untrusted>`-wrapped content

That's by design. The wrapping signals to Claude that the content
originates from the open web and should not be treated as instructions.
Don't strip it client-side; let Claude handle the trust boundary.
