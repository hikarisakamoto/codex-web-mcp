using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CodexWebMcp;

public static class Prompts
{
    private static string? _resolvedDir;
    private static readonly object _dirLock = new();
    private static readonly ConcurrentDictionary<string, string> _templateCache = new();

    public static string ResolveDir()
    {
        if (_resolvedDir is not null) return _resolvedDir;
        lock (_dirLock)
        {
            if (_resolvedDir is not null) return _resolvedDir;

            var attempted = new List<string>();

            // 1. Env var override
            var env = Environment.GetEnvironmentVariable("CODEX_WEB_PROMPTS");
            if (!string.IsNullOrWhiteSpace(env))
            {
                attempted.Add($"env:CODEX_WEB_PROMPTS={env}");
                if (Directory.Exists(env))
                {
                    _resolvedDir = env;
                    return _resolvedDir;
                }
            }

            // 2. Walk up from AppContext.BaseDirectory
            if (TryWalkUp(AppContext.BaseDirectory, attempted, out var dir1))
            {
                _resolvedDir = dir1;
                return _resolvedDir!;
            }

            // 3. Walk up from current working directory
            if (TryWalkUp(Directory.GetCurrentDirectory(), attempted, out var dir2))
            {
                _resolvedDir = dir2;
                return _resolvedDir!;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Could not locate prompts/ directory. Attempted paths:");
            foreach (var a in attempted) sb.AppendLine($"  - {a}");
            throw new InvalidOperationException(sb.ToString());
        }
    }

    private static bool TryWalkUp(string start, List<string> attempted, out string? found)
    {
        found = null;
        if (string.IsNullOrWhiteSpace(start)) return false;

        var cur = Path.GetFullPath(start);
        for (int i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(cur, "prompts");
            attempted.Add(candidate);
            if (Directory.Exists(candidate))
            {
                found = candidate;
                return true;
            }
            var parent = Path.GetDirectoryName(cur);
            if (string.IsNullOrEmpty(parent) || parent == cur) break;
            cur = parent;
        }
        return false;
    }

    public static string LoadTemplate(string toolName)
    {
        return _templateCache.GetOrAdd(toolName, name =>
        {
            var dir = ResolveDir();
            var path = Path.Combine(dir, $"{name}.md");
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"Prompt template not found: {path}", path);
            return File.ReadAllText(path);
        });
    }

    public static string Render(string toolName, IReadOnlyDictionary<string, string> vars)
    {
        var template = LoadTemplate(toolName);
        var result = template;
        foreach (var kv in vars)
        {
            result = result.Replace("{" + kv.Key + "}", kv.Value);
        }
        return result;
    }
}
