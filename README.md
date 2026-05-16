# codex-web-mcp

An MCP (Model Context Protocol) server that gives Claude Code web access by
proxying queries through the OpenAI Codex CLI (`codex exec`). Useful when
your Claude Code deployment lacks native web search but Codex CLI is
available locally — this bridge lets Claude reach the web via Codex's
sandboxed agent.

Six implementations live side-by-side in this repo. Pick whichever fits
your environment best:

- **C#** — `csharp/` — .NET 10, AOT-trimmed self-contained binary.
- **F#** — `fsharp/` — .NET 10, framework-dependent or self-contained.
- **Go** — `go/` — single static binary, ~10MB, zero runtime dependencies.
- **Java** — `java/` — Maven, shaded fat jar, runs on JDK 17+.
- **Node** — `node/` — TypeScript compiled to `dist/`, runs on Node 20+.
- **Python** — `python/` — package installable with `pip`, Python 3.10+.

All six expose the same three tools, read the same prompt templates from
`prompts/`, honor the same environment variables, and produce
byte-equivalent output.

## Tools

| Tool             | Purpose                                                          |
|------------------|------------------------------------------------------------------|
| `web_search`     | Synthesized natural-language answer with numbered citations.     |
| `web_search_raw` | JSON array of `{title, url, snippet}` hits from the search.      |
| `web_fetch_raw`  | Verbatim main text of a single URL, optionally truncated.        |

## Quick start

```powershell
# Windows — builds whatever toolchains are available
./scripts/build-all.ps1
./scripts/smoke-test.ps1 ./go/bin/codex-web-mcp.exe
./scripts/smoke-test.ps1 ./csharp/src/CodexWebMcp/publish/CodexWebMcp.exe
```

```bash
# Linux / macOS
./scripts/build-all.sh
./scripts/smoke-test.sh ./go/bin/codex-web-mcp
./scripts/smoke-test.sh ./csharp/src/CodexWebMcp/publish/CodexWebMcp
```

Skip individual stacks with `-SkipCsharp`, `-SkipFsharp`, `-SkipGo`,
`-SkipJava`, `-SkipNode`, `-SkipPython` (PS) or `--skip-cs`,
`--skip-fs`, `--skip-go`, `--skip-java`, `--skip-node`, `--skip-python`
(bash).

## Wiring into Claude Code

1. Copy a template from `config/` matching your OS and chosen
   implementation:

   ```
   config/mcp.windows.csharp.json   config/mcp.linux.csharp.json
   config/mcp.windows.fsharp.json   config/mcp.linux.fsharp.json
   config/mcp.windows.go.json       config/mcp.linux.go.json
   config/mcp.windows.java.json     config/mcp.linux.java.json
   config/mcp.windows.node.json     config/mcp.linux.node.json
   config/mcp.windows.python.json   config/mcp.linux.python.json
   ```

2. Edit the `command` path to point at your built binary.

3. Save the file as `.mcp.json` in your project root (or merge it into an
   existing `.mcp.json`).

4. Restart Claude Code. The three `web_*` tools should appear.

## Environment variables

| Variable             | Purpose                                                                                                  |
|----------------------|----------------------------------------------------------------------------------------------------------|
| `CODEX_BIN`          | Path to the `codex` binary. Defaults to `codex` (PATH lookup).                                           |
| `CODEX_WEB_PROMPTS`  | Override directory for prompt templates. Defaults to walking from the exe, then cwd, looking for `prompts/`. |
| `CODEX_WEB_CACHE`    | Override the on-disk cache directory. Defaults to `%LOCALAPPDATA%\codex-web-mcp\cache` on Windows, `~/.cache/codex-web-mcp/cache` on Linux. |

## Why six implementations?

Different shops standardize on different stacks. Rather than force a
choice, every implementation is a first-class citizen with identical
behavior. Build whichever you can install a toolchain for — the resulting
binary is interchangeable from Claude Code's perspective.

## Documentation

- [`docs/architecture.md`](docs/architecture.md) — how the proxy works
  end-to-end, including caching and prompt-injection defenses.
- [`docs/prompts.md`](docs/prompts.md) — prompt template format and
  resolution order.
- [`docs/troubleshooting.md`](docs/troubleshooting.md) — common failure
  modes and fixes.

## License

MIT — see [`LICENSE`](LICENSE).
