using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CodexWebMcp;

internal sealed record CacheEntry(string Content, string Timestamp);

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(CacheEntry))]
internal partial class CacheJsonContext : JsonSerializerContext { }

public static class Cache
{
    /// <summary>
    /// SHA-256 hex of "{tool}::{payload}", truncated to 32 chars.
    /// </summary>
    public static string KeyHash(string tool, string payload)
    {
        var combined = $"{tool}::{payload}";
        Span<byte> hash = stackalloc byte[32];
        var bytes = Encoding.UTF8.GetBytes(combined);
        SHA256.HashData(bytes, hash);

        // Hex encode (lower-case), then truncate to 32 chars.
        var sb = new StringBuilder(64);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString(0, 32);
    }

    private static string FilePath(string tool, string payload)
    {
        var hash = KeyHash(tool, payload);
        return Path.Combine(Config.CacheSubdir, $"{tool}-{hash}.json");
    }

    public static async Task<string?> GetAsync(string tool, string payload, int ttlSeconds, CancellationToken ct = default)
    {
        try
        {
            var path = FilePath(tool, payload);
            if (!File.Exists(path)) return null;

            var mtime = File.GetLastWriteTimeUtc(path);
            var age = DateTime.UtcNow - mtime;
            if (age.TotalSeconds > ttlSeconds)
                return null;

            await using var fs = File.OpenRead(path);
            var entry = await JsonSerializer.DeserializeAsync(
                fs, CacheJsonContext.Default.CacheEntry, ct);
            return entry?.Content;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"cache.get error: {ex.Message}");
            return null;
        }
    }

    public static async Task PutAsync(string tool, string payload, string content, CancellationToken ct = default)
    {
        try
        {
            // Ensure directory exists (Config.CacheSubdir creates it, but be defensive)
            Directory.CreateDirectory(Config.CacheSubdir);

            var path = FilePath(tool, payload);
            var entry = new CacheEntry(content, DateTime.UtcNow.ToString("o"));

            // Atomic-ish write: write to temp then move
            var tmp = path + ".tmp";
            await using (var fs = File.Create(tmp))
            {
                await JsonSerializer.SerializeAsync(fs, entry, CacheJsonContext.Default.CacheEntry, ct);
            }

            if (File.Exists(path))
            {
                try { File.Delete(path); } catch { /* ignore */ }
            }
            File.Move(tmp, path);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"cache.put error: {ex.Message}");
        }
    }
}
