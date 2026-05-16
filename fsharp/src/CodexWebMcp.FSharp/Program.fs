module CodexWebMcp.FSharp.Program

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open ModelContextProtocol.Server
open CodexWebMcp.FSharp.Tools

[<EntryPoint>]
let main argv =
    let builder = Host.CreateApplicationBuilder(argv)

    // CRITICAL: ALL logs to stderr — stdout is JSON-RPC.
    builder.Logging.ClearProviders() |> ignore
    builder.Logging.AddConsole(fun o ->
        o.LogToStandardErrorThreshold <- LogLevel.Trace) |> ignore

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<CodexWebTools>() |> ignore

    builder.Build().Run()
    0
