# Prompt templates

The three tools are powered by markdown prompt files in `prompts/`. They
are loaded at runtime — **not baked into the binaries** — so you can tune
the wording without rebuilding.

## Files

| File                         | Tool             | Variables                  |
|------------------------------|------------------|----------------------------|
| `prompts/web_search.md`      | `web_search`     | `{query}`                  |
| `prompts/web_search_raw.md`  | `web_search_raw` | `{query}`, `{max_results}` |
| `prompts/web_fetch_raw.md`   | `web_fetch_raw`  | `{url}`, `{max_words}`     |

## Placeholder syntax

Substitution is literal `{key}` -> value. There is no escaping, no
conditionals, no loops. If you need a literal `{` in a template, you'll
need to extend the renderer — keep things simple by avoiding it.

Unknown placeholders left in the template are passed through unchanged.
Missing values for a known placeholder are an error at call time.

## Resolution order

When the server starts, it locates the `prompts/` directory by trying, in
order:

1. `$CODEX_WEB_PROMPTS` (if set, must be a directory).
2. Walk upward from the executable's directory, looking for a child named
   `prompts/`.
3. Walk upward from the current working directory, same check.
4. Fail with an error that names all three search strategies.

In practice this means: ship `prompts/` next to the binary (or anywhere up
the tree from it) and it just works. For development, run from the repo
root and the cwd-walk finds `prompts/` immediately.

## Editing prompts

Edits take effect on next server restart. Because the rendered prompt text
is part of the cache key, changing a template automatically invalidates
its slice of the cache — no manual purge needed.

Suggested workflow: tweak the prompt, restart Claude Code (which
respawns the MCP server), retry the tool, observe new output.

## Design notes

- Templates are intentionally short and directive. Codex's underlying
  model handles the heavy lifting; the template only constrains output
  shape.
- `web_search` is for natural-language consumption — it includes
  citations and prose.
- `web_search_raw` is for programmatic consumers (other tools, scripts) —
  pure JSON, verbatim snippets.
- `web_fetch_raw` deliberately resists summarization; if you want a
  summary, run a follow-up `web_search` with the URL in your query.
