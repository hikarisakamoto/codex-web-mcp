# codex-web-mcp — Node / TypeScript

Node implementation of `codex-web-mcp`. Behavior identical to the Go, C#,
F#, Java, and Python implementations.

## Requirements

- Node.js 20+
- `codex` CLI on `PATH` (or `CODEX_BIN` set to its full path)

## Install

```bash
cd node
npm install
npm run build
```

The compiled entrypoint is `dist/index.js`. The `package.json` declares it
as a `bin`, so `npm link` (or `npm install -g .`) makes the
`codex-web-mcp` script available globally.

## Test

```bash
npm test
```

## Wire into Claude Code

Use `config/mcp.windows.node.json` or `config/mcp.linux.node.json` as a
starting template.
