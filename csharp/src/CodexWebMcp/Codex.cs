using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CodexWebMcp;

public static class Codex
{
    public static async Task<string> RunAsync(string prompt, CancellationToken ct = default)
    {
        string tmpFile = Path.Combine(Path.GetTempPath(), $"codex-out-{Guid.NewGuid():N}.txt");
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = Config.CodexBin,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            // Use ArgumentList — NOT Arguments — for correct quoting on all platforms
            psi.ArgumentList.Add("exec");
            psi.ArgumentList.Add("--skip-git-repo-check");
            psi.ArgumentList.Add("--ephemeral");
            psi.ArgumentList.Add("--color");
            psi.ArgumentList.Add("never");
            psi.ArgumentList.Add("--sandbox");
            psi.ArgumentList.Add("read-only");
            psi.ArgumentList.Add("--output-last-message");
            psi.ArgumentList.Add(tmpFile);
            psi.ArgumentList.Add(prompt);

            using var proc = Process.Start(psi)
                ?? throw new InvalidOperationException("Process.Start returned null");

            // Close stdin so codex doesn't wait for any input
            try { proc.StandardInput.Close(); } catch { /* ignore */ }

            // Drain stdout & stderr concurrently — pipe buffer (~64KB) would block the child otherwise
            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Config.CodexTimeoutSeconds));

            try
            {
                await proc.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return $"ERROR: codex timed out after {Config.CodexTimeoutSeconds}s";
            }

            // Discard stdout content but ensure the drain completes
            await stdoutTask;
            var stderr = await stderrTask;

            if (proc.ExitCode != 0)
                return $"ERROR: codex exited with code {proc.ExitCode}: {stderr.Trim()}";

            if (!File.Exists(tmpFile))
                return "ERROR: codex produced no output file";

            var content = await File.ReadAllTextAsync(tmpFile, ct);
            if (string.IsNullOrWhiteSpace(content))
                return "ERROR: codex output file empty";

            return content.TrimEnd();
        }
        catch (Exception ex)
        {
            return $"ERROR: codex invocation failed: {ex.Message}";
        }
        finally
        {
            try { if (File.Exists(tmpFile)) File.Delete(tmpFile); } catch { /* ignore */ }
        }
    }
}
