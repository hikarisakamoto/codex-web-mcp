module CodexWebMcp.FSharp.Tests.CacheTests

open System
open System.IO
open Xunit
open CodexWebMcp.FSharp

type CacheFixture() =
    let tempDir =
        Path.Combine(Path.GetTempPath(), "codex-web-mcp-fs-tests-" + Guid.NewGuid().ToString("N"))
    let previous = Environment.GetEnvironmentVariable("CODEX_WEB_CACHE")
    do
        Directory.CreateDirectory(tempDir) |> ignore
        Environment.SetEnvironmentVariable("CODEX_WEB_CACHE", tempDir)

    member _.Dir = tempDir

    interface IDisposable with
        member _.Dispose() =
            Environment.SetEnvironmentVariable("CODEX_WEB_CACHE", previous)
            try Directory.Delete(tempDir, true) with _ -> ()

[<Fact>]
let ``KeyHash is deterministic and 32 hex chars`` () =
    let a = Cache.keyHash "web_search" "hello world"
    let b = Cache.keyHash "web_search" "hello world"
    Assert.Equal(a, b)
    Assert.Equal(32, a.Length)

[<Fact>]
let ``KeyHash differs by tool`` () =
    let a = Cache.keyHash "web_search" "x"
    let b = Cache.keyHash "web_search_raw" "x"
    Assert.NotEqual<string>(a, b)

[<Fact>]
let ``KeyHash differs by payload`` () =
    let a = Cache.keyHash "web_search" "x"
    let b = Cache.keyHash "web_search" "y"
    Assert.NotEqual<string>(a, b)

[<Fact>]
let ``Round trip`` () =
    use fixture = new CacheFixture()
    Cache.put "web_search" "query1" "the answer"
    match Cache.get "web_search" "query1" 3600 with
    | Some v -> Assert.Equal("the answer", v)
    | None -> Assert.Fail("expected Some")

[<Fact>]
let ``Get miss returns None`` () =
    use fixture = new CacheFixture()
    Assert.Equal(None, Cache.get "web_search" "never-written" 3600)

[<Fact>]
let ``Get expired returns None`` () =
    use fixture = new CacheFixture()
    Cache.put "web_search" "stale" "old"
    let file =
        Path.Combine(fixture.Dir, "cache", sprintf "web_search-%s.json" (Cache.keyHash "web_search" "stale"))
    File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddHours(-2.0))
    Assert.Equal(None, Cache.get "web_search" "stale" 60)

[<Fact>]
let ``Get within TTL returns content`` () =
    use fixture = new CacheFixture()
    Cache.put "web_search" "fresh" "content"
    match Cache.get "web_search" "fresh" 3600 with
    | Some v -> Assert.Equal("content", v)
    | None -> Assert.Fail("expected Some")
