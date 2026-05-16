module CodexWebMcp.FSharp.Codex

open System
open System.Diagnostics
open System.IO
open System.Text
open System.Threading
open System.Threading.Tasks

let runAsync (prompt: string) : Task<string> =
    task {
        let tmpPath =
            Path.Combine(Path.GetTempPath(), sprintf "codex-out-%s.txt" (Guid.NewGuid().ToString("N")))

        let psi = ProcessStartInfo()
        psi.FileName <- Config.codexBin ()
        psi.RedirectStandardInput <- true
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.UseShellExecute <- false
        psi.CreateNoWindow <- true
        for a in [
            "exec"
            "--skip-git-repo-check"
            "--ephemeral"
            "--color"; "never"
            "--sandbox"; "read-only"
            "--output-last-message"; tmpPath
            prompt
        ] do psi.ArgumentList.Add(a)

        let proc = new Process()
        proc.StartInfo <- psi

        try
            try
                if not (proc.Start()) then
                    return "ERROR: codex invocation failed: Start returned false"
                else
                    proc.StandardInput.Close()

                    let stdoutTask = proc.StandardOutput.ReadToEndAsync()
                    let stderrTask = proc.StandardError.ReadToEndAsync()

                    use cts = new CancellationTokenSource(TimeSpan.FromSeconds(float Config.CodexTimeoutSeconds))
                    let! exited =
                        task {
                            try
                                do! proc.WaitForExitAsync(cts.Token)
                                return true
                            with :? OperationCanceledException ->
                                return false
                        }

                    if not exited then
                        try proc.Kill(entireProcessTree = true) with _ -> ()
                        return "ERROR: codex timed out after 180s"
                    else
                        let! _ = stdoutTask
                        let! stderr = stderrTask
                        if proc.ExitCode <> 0 then
                            return sprintf "ERROR: codex exited with code %d: %s" proc.ExitCode (stderr.Trim())
                        elif not (File.Exists tmpPath) then
                            return "ERROR: codex produced no output file"
                        else
                            let fi = FileInfo(tmpPath)
                            if fi.Length = 0L then
                                return "ERROR: codex output file empty"
                            else
                                let data = (File.ReadAllText(tmpPath)).TrimEnd([| ' '; '\t'; '\r'; '\n' |])
                                if data = "" then return "ERROR: codex output file empty"
                                else return data
            with ex ->
                return sprintf "ERROR: codex invocation failed: %s" ex.Message
        finally
            try File.Delete(tmpPath) with _ -> ()
            proc.Dispose()
    }
