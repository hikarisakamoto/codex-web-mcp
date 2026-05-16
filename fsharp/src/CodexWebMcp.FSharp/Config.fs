module CodexWebMcp.FSharp.Config

open System
open System.IO

let CodexTimeoutSeconds = 180

let codexBin () =
    match Environment.GetEnvironmentVariable("CODEX_BIN") with
    | null | "" ->
        if OperatingSystem.IsWindows() then "codex.exe" else "codex"
    | v -> v

let cacheDir () =
    let dir =
        match Environment.GetEnvironmentVariable("CODEX_WEB_CACHE") with
        | null | "" ->
            let baseDir =
                if OperatingSystem.IsWindows() then
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                elif OperatingSystem.IsMacOS() then
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Caches")
                else
                    match Environment.GetEnvironmentVariable("XDG_CACHE_HOME") with
                    | null | "" ->
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache")
                    | v -> v
            Path.Combine(baseDir, "codex-web-mcp")
        | v -> v
    Directory.CreateDirectory(dir) |> ignore
    dir

let cacheSubdir () =
    let sub = Path.Combine(cacheDir (), "cache")
    Directory.CreateDirectory(sub) |> ignore
    sub

let logPath () = Path.Combine(cacheDir (), "calls.log")

let appendCallLog (line: string) =
    try
        File.AppendAllText(logPath (), line + "\n")
    with _ ->
        ()
