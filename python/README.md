# codex-web-mcp — Python

Python implementation of `codex-web-mcp`. Behavior identical to the Go and
C# implementations.

## Requirements

- Python 3.10+
- `codex` CLI on `PATH` (or `CODEX_BIN` set to its full path)

## Install

```bash
cd python
pip install -e .
```

This installs a `codex-web-mcp` console script (also runnable as
`python -m codex_web_mcp`).

## Test

```bash
pip install pytest
pytest
```

## Wire into Claude Code

Use `config/mcp.windows.python.json` or `config/mcp.linux.python.json` as a
starting template.
