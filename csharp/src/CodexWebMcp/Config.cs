using System;
using System.IO;

namespace CodexWebMcp;

public static class Config
{
    public static int CodexTimeoutSeconds { get; set; } = 180;

    public static string CodexBin =>
        Environment.GetEnvironmentVariable("CODEX_BIN")
            ?? (OperatingSystem.IsWindows() ? "codex.exe" : "codex");

    public static string CacheDir
    {
        get
        {
            var dir = ResolveCacheDir();
            try { Directory.CreateDirectory(dir); } catch { /* non-fatal */ }
            return dir;
        }
    }

    public static string CacheSubdir
    {
        get
        {
            var dir = Path.Combine(CacheDir, "cache");
            try { Directory.CreateDirectory(dir); } catch { /* non-fatal */ }
            return dir;
        }
    }

    public static string LogPath => Path.Combine(CacheDir, "calls.log");

    private static string ResolveCacheDir()
    {
        var env = Environment.GetEnvironmentVariable("CODEX_WEB_CACHE");
        if (!string.IsNullOrWhiteSpace(env))
            return env;

        if (OperatingSystem.IsWindows())
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(local, "codex-web-mcp");
        }

        var xdg = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        if (!string.IsNullOrWhiteSpace(xdg))
            return Path.Combine(xdg, "codex-web-mcp");

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(home))
            home = Environment.GetEnvironmentVariable("HOME") ?? ".";
        return Path.Combine(home, ".cache", "codex-web-mcp");
    }

    public static void AppendCallLog(string line)
    {
        try
        {
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
        catch
        {
            // non-fatal — never break a tool call because of logging
        }
    }
}
