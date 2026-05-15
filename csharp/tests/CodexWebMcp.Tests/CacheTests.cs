using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace CodexWebMcp.Tests;

public class CacheTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string? _previousEnv;

    public CacheTests()
    {
        _previousEnv = Environment.GetEnvironmentVariable("CODEX_WEB_CACHE");
        _tempDir = Path.Combine(Path.GetTempPath(), "codex-web-mcp-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        Environment.SetEnvironmentVariable("CODEX_WEB_CACHE", _tempDir);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("CODEX_WEB_CACHE", _previousEnv);
        try
        {
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public void KeyHash_IsDeterministic()
    {
        var a = Cache.KeyHash("web_search", "hello world");
        var b = Cache.KeyHash("web_search", "hello world");
        Assert.Equal(a, b);
        Assert.Equal(32, a.Length);
    }

    [Fact]
    public void KeyHash_DiffersByTool()
    {
        var a = Cache.KeyHash("a", "x");
        var b = Cache.KeyHash("b", "x");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void KeyHash_DiffersByPayload()
    {
        var a = Cache.KeyHash("web_search", "alpha");
        var b = Cache.KeyHash("web_search", "beta");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public async Task RoundTrip_Hit()
    {
        const string tool = "web_search";
        const string payload = "round-trip-query";
        const string content = "<untrusted source='codex:web_search'>\nbody\n</untrusted>";

        await Cache.PutAsync(tool, payload, content);
        var got = await Cache.GetAsync(tool, payload, ttlSeconds: 3600);

        Assert.Equal(content, got);
    }

    [Fact]
    public async Task Get_Miss_ReturnsNull()
    {
        var got = await Cache.GetAsync("web_search", "never-written-" + Guid.NewGuid(), ttlSeconds: 3600);
        Assert.Null(got);
    }

    [Fact]
    public async Task Get_Expired_ReturnsNull()
    {
        const string tool = "web_search";
        const string payload = "expired-query";
        const string content = "expired body";

        await Cache.PutAsync(tool, payload, content);

        // Force the file's mtime far into the past.
        var hash = Cache.KeyHash(tool, payload);
        var path = Path.Combine(Config.CacheSubdir, $"{tool}-{hash}.json");
        Assert.True(File.Exists(path));
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddHours(-2));

        var got = await Cache.GetAsync(tool, payload, ttlSeconds: 60);
        Assert.Null(got);
    }

    [Fact]
    public async Task Get_WithinTTL_ReturnsContent()
    {
        const string tool = "web_search";
        const string payload = "fresh-query";
        const string content = "fresh body";

        await Cache.PutAsync(tool, payload, content);

        // Set mtime to 30 seconds ago — well within a 3600s TTL.
        var hash = Cache.KeyHash(tool, payload);
        var path = Path.Combine(Config.CacheSubdir, $"{tool}-{hash}.json");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(-30));

        var got = await Cache.GetAsync(tool, payload, ttlSeconds: 3600);
        Assert.Equal(content, got);
    }
}
