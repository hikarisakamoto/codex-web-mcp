module CodexWebMcp.FSharp.Cache

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.Json.Serialization

type CacheEntry = {
    [<JsonPropertyName("content")>] Content: string
    [<JsonPropertyName("timestamp")>] Timestamp: string
}

let private jsonOptions =
    let o = JsonSerializerOptions()
    o.WriteIndented <- false
    o

let keyHash (tool: string) (payload: string) : string =
    let combined = sprintf "%s::%s" tool payload
    let bytes = Encoding.UTF8.GetBytes(combined)
    let hash = SHA256.HashData(bytes)
    let sb = StringBuilder(64)
    for b in hash do sb.Append(b.ToString("x2")) |> ignore
    sb.ToString().Substring(0, 32)

let private filePath (tool: string) (payload: string) : string =
    Path.Combine(Config.cacheSubdir (), sprintf "%s-%s.json" tool (keyHash tool payload))

let get (tool: string) (payload: string) (ttlSeconds: int) : string option =
    try
        let path = filePath tool payload
        if not (File.Exists path) then
            None
        else
            let mtime = File.GetLastWriteTimeUtc(path)
            let age = DateTime.UtcNow - mtime
            if age.TotalSeconds > float ttlSeconds then
                None
            else
                let data = File.ReadAllText(path)
                let entry = JsonSerializer.Deserialize<CacheEntry>(data, jsonOptions)
                if isNull (box entry) then None
                else Some entry.Content
    with ex ->
        eprintfn "cache.get error: %s" ex.Message
        None

let put (tool: string) (payload: string) (content: string) : unit =
    try
        Directory.CreateDirectory(Config.cacheSubdir ()) |> ignore
        let path = filePath tool payload
        let entry = { Content = content; Timestamp = DateTime.UtcNow.ToString("o") }
        let data = JsonSerializer.Serialize(entry, jsonOptions)
        let tmp = path + ".tmp"
        File.WriteAllText(tmp, data)
        if File.Exists path then
            try File.Delete(path) with _ -> ()
        File.Move(tmp, path)
    with ex ->
        eprintfn "cache.put error: %s" ex.Message
