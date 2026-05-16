# codex-web-mcp — Java

Java implementation of `codex-web-mcp`. Behavior identical to the Go, C#,
F#, Node, and Python implementations.

This implementation **does not** depend on a third-party MCP SDK — it
implements the JSON-RPC stdio surface (initialize / tools/list /
tools/call) directly. The only runtime dependency is Jackson for JSON.

## Requirements

- Java 17+
- Maven 3.9+
- `codex` CLI on `PATH` (or `CODEX_BIN` set to its full path)

## Build

```bash
cd java
mvn -B package
```

Produces `target/codex-web-mcp.jar` (shaded, single fat jar). Run with:

```bash
java -jar target/codex-web-mcp.jar
```

## Test

```bash
mvn -B test
```

The cache tests mutate `CODEX_WEB_CACHE` at runtime using a reflective
helper that talks to `ProcessEnvironment`. JDK 17+ requires
`--add-opens=java.base/java.lang=ALL-UNNAMED` for that path. Surefire
picks this up automatically via the `argLine` if you run into reflection
warnings, you can pass it explicitly:

```bash
mvn -B test -DargLine="--add-opens=java.base/java.lang=ALL-UNNAMED"
```

## Wire into Claude Code

Use `config/mcp.windows.java.json` or `config/mcp.linux.java.json` as a
starting template.
