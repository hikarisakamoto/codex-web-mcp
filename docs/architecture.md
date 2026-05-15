# Architecture

`codex-web-mcp` is a thin proxy: Claude Code talks to it over MCP stdio, and
it talks to the OpenAI Codex CLI over a child-process boundary. There is no
direct network I/O in this server — Codex does all the searching and
fetching.

## End-to-end sequence

```
Claude Code
    | (MCP stdio: tools/call web_search)
    v
codex-web-mcp server
    |
    +--> cache lookup (key = sha256 of tool+args+prompt)
    |       hit  --> return cached result
    |       miss --> continue
    |
    +--> render prompt template (substitute {query}, {max_results}, {url}, {max_words})
    |
    +--> create temp file <tempdir>/codex-out-XXXX
    |
    +--> spawn:
    |       $CODEX_BIN exec --skip-git-repo-check --ephemeral --color never \
    |                        --sandbox read-only \
    |                        --output-last-message <tempfile> \
    |                        "<rendered prompt>"
    |
    +--> drain stdout/stderr (DISCARD stdout — it is human-formatted log
    |                        noise; preserve stderr for diagnostics)
    |
    +--> wait for exit
    |       non-zero --> return MCP error containing exit code + stderr tail
    |
    +--> read tempfile (this is the clean answer Codex produced)
    |
    +--> wrap content in <untrusted>...</untrusted> (prompt-injection defense)
    |
    +--> write to cache with 24h TTL
    |
    +--> return as MCP tool result text
```

## Why `--output-last-message` and not stdout

`codex exec` writes a structured human-readable transcript to stdout that
includes the version banner, the workdir, model identifier, sandbox mode,
timestamps, "thinking" blocks, the actual answer, and a final tokens-used
line. Parsing this is brittle — the format has changed across Codex
versions and is explicitly not a stable contract.

`--output-last-message <path>` writes only the final assistant message
verbatim to the given file. We use a unique tempfile per invocation, read
it after the process exits, and delete it. This is the contract Codex
actually supports for programmatic use.

A sample of the discarded stdout lives at
`testdata/fixtures/codex-sample-output.txt` for reference.

## Caching

- Backend: file-per-entry under `$CODEX_WEB_CACHE` (default
  `%LOCALAPPDATA%\codex-web-mcp\cache` on Windows,
  `~/.cache/codex-web-mcp/cache` on Linux/macOS).
- Key: SHA-256 of the concatenated tool name, JSON-canonical arguments,
  and the rendered prompt. This means changing a prompt template
  invalidates its cache slice automatically.
- TTL: 24 hours. Stale entries are ignored on read and overwritten on the
  next write.
- Format: small JSON envelope `{ "created_at": <unix>, "ttl_seconds": 86400,
  "value": "<text>" }`.

To force a fresh fetch: delete the cache directory, or set the entry's
`ttl_seconds` to `0`.

## Prompt-injection defense

Web content is hostile by default. Any text that originates from the open
internet (search snippets, fetched page bodies, or Codex's synthesized
answers built on top of them) is wrapped in `<untrusted>...</untrusted>`
before being returned over MCP. Claude Code is instructed by convention to
treat content inside `<untrusted>` blocks as data, never as instructions.

## Cross-platform paths

Both implementations use platform-aware path resolution:

- `CODEX_BIN`: respects an absolute path; otherwise resolved against
  `PATH`. On Windows the `.exe` suffix is appended if missing.
- Cache and prompt directories: standard per-OS user dirs (LOCALAPPDATA on
  Windows, XDG cache on Linux, `~/Library/Caches` on macOS).
- Tempfiles: created via the runtime's standard tempdir API and cleaned up
  in a `finally` block even on error paths.

## Prompt template loader

The loader's resolution order:

1. `$CODEX_WEB_PROMPTS` env var, if set — must point to a directory
   containing the three `.md` files.
2. Walk upward from the executable's directory, checking each parent for a
   `prompts/` subfolder.
3. Walk upward from the current working directory, same check.
4. If none found, the server fails on startup with a clear error pointing
   at the env var.

Templates are read once at startup, then re-rendered per call by simple
`{key}` substitution. Editing a template requires a restart (and
invalidates the relevant cache entries automatically because the rendered
prompt feeds into the cache key).

## Process-lifetime stdio discipline

MCP stdio is unforgiving: any byte written to stdout that isn't a valid
JSON-RPC frame corrupts the channel and Claude Code disconnects. Both
implementations route every log, warning, and diagnostic to stderr. The
only thing that ever touches stdout is the framed JSON-RPC response.
