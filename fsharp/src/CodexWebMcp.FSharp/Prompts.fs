module CodexWebMcp.FSharp.Prompts

open System
open System.Collections.Concurrent
open System.IO
open System.Reflection

let private lockObj = obj ()
let mutable private dirValue : string option = None
let private tplCache = ConcurrentDictionary<string, string>()

let private isPromptsDir (p: string) =
    try Directory.Exists(p) with _ -> false

let private walkUp (start: string) (attempted: ResizeArray<string>) : string option =
    let mutable cur = start
    let mutable result = None
    let mutable stop = false
    let mutable i = 0
    while not stop && i < 10 && result.IsNone do
        let cand = Path.Combine(cur, "prompts")
        attempted.Add(cand)
        if isPromptsDir cand then
            result <- Some cand
        else
            let parent = Path.GetDirectoryName(cur)
            if String.IsNullOrEmpty(parent) || parent = cur then
                stop <- true
            else
                cur <- parent
        i <- i + 1
    result

let resolveDir () : string =
    lock lockObj (fun () ->
        match dirValue with
        | Some v -> v
        | None ->
            let attempted = ResizeArray<string>()
            let envVal = Environment.GetEnvironmentVariable("CODEX_WEB_PROMPTS")
            let envFound =
                if not (String.IsNullOrEmpty envVal) then
                    attempted.Add(envVal)
                    if isPromptsDir envVal then Some envVal else None
                else None
            let resolved =
                match envFound with
                | Some v -> Some v
                | None ->
                    let exeDir =
                        try Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                        with _ -> ""
                    let fromExe =
                        if not (String.IsNullOrEmpty exeDir) then walkUp exeDir attempted
                        else None
                    match fromExe with
                    | Some _ -> fromExe
                    | None ->
                        try walkUp (Directory.GetCurrentDirectory()) attempted
                        with _ -> None
            match resolved with
            | Some v ->
                dirValue <- Some v
                v
            | None ->
                let joined = String.Join(", ", attempted)
                raise (FileNotFoundException(sprintf "prompts directory not found; tried: %s" joined))
    )

let render (toolName: string) (vars: Map<string, string>) : string =
    let tpl =
        match tplCache.TryGetValue(toolName) with
        | true, v -> v
        | false, _ ->
            let dir = resolveDir ()
            let path = Path.Combine(dir, toolName + ".md")
            let content = File.ReadAllText(path)
            tplCache.TryAdd(toolName, content) |> ignore
            content
    let mutable out = tpl
    for KeyValue(k, v) in vars do
        out <- out.Replace("{" + k + "}", v)
    out
