# codex-web-mcp — F#

F# (.NET) implementation of `codex-web-mcp`. Behavior identical to the Go,
C#, Node, Java, and Python implementations.

Reuses the same `ModelContextProtocol` NuGet package as the C# build.
Targets `net10.0` but unlike the C# project does not enable AOT — the
F# build is a straightforward framework-dependent or self-contained
publish.

## Requirements

- .NET SDK 10
- `codex` CLI on `PATH` (or `CODEX_BIN` set to its full path)

## Build

```powershell
dotnet publish .\src\CodexWebMcp.FSharp\CodexWebMcp.FSharp.fsproj -c Release -r win-x64 --self-contained -o .\src\CodexWebMcp.FSharp\publish
```

```bash
dotnet publish ./src/CodexWebMcp.FSharp/CodexWebMcp.FSharp.fsproj -c Release -r linux-x64 --self-contained -o ./src/CodexWebMcp.FSharp/publish
```

## Test

```bash
dotnet test
```

## Wire into Claude Code

Use `config/mcp.windows.fsharp.json` or `config/mcp.linux.fsharp.json` as a
starting template.
