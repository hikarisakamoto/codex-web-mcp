package codexwebmcp;

import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.io.IOException;
import java.lang.reflect.Field;
import java.nio.file.FileTime;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.Instant;
import java.time.temporal.ChronoUnit;
import java.util.Collections;
import java.util.HashMap;
import java.util.Map;
import java.util.UUID;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotEquals;
import static org.junit.jupiter.api.Assertions.assertNull;

class CacheTest {
    private Path tempDir;

    @BeforeEach
    void setUp() throws IOException {
        tempDir = Files.createTempDirectory("codex-web-mcp-tests-" + UUID.randomUUID());
        setEnv(Map.of("CODEX_WEB_CACHE", tempDir.toString()));
    }

    @AfterEach
    void tearDown() throws IOException {
        unsetEnv("CODEX_WEB_CACHE");
        if (Files.exists(tempDir)) {
            try (var stream = Files.walk(tempDir)) {
                stream.sorted((a, b) -> b.compareTo(a))
                    .forEach(p -> { try { Files.deleteIfExists(p); } catch (IOException ignored) {} });
            }
        }
    }

    @Test
    void keyHashIsDeterministic() {
        String a = Cache.keyHash("web_search", "hello world");
        String b = Cache.keyHash("web_search", "hello world");
        assertEquals(a, b);
        assertEquals(32, a.length());
    }

    @Test
    void keyHashDiffersByTool() {
        assertNotEquals(Cache.keyHash("web_search", "x"), Cache.keyHash("web_search_raw", "x"));
    }

    @Test
    void keyHashDiffersByPayload() {
        assertNotEquals(Cache.keyHash("web_search", "x"), Cache.keyHash("web_search", "y"));
    }

    @Test
    void roundTrip() {
        Cache.put("web_search", "query1", "the answer");
        assertEquals("the answer", Cache.get("web_search", "query1", 3600));
    }

    @Test
    void getMissReturnsNull() {
        assertNull(Cache.get("web_search", "never-written", 3600));
    }

    @Test
    void getExpiredReturnsNull() throws IOException {
        Cache.put("web_search", "stale", "old");
        Path file = tempDir.resolve("cache")
            .resolve("web_search-" + Cache.keyHash("web_search", "stale") + ".json");
        Instant past = Instant.now().minus(2, ChronoUnit.HOURS);
        Files.setLastModifiedTime(file, FileTime.from(past));
        assertNull(Cache.get("web_search", "stale", 60));
    }

    @Test
    void getWithinTtlReturnsContent() {
        Cache.put("web_search", "fresh", "content");
        assertEquals("content", Cache.get("web_search", "fresh", 3600));
    }

    // --- Env-var helpers (reflection — JDK has no public setenv) ---

    @SuppressWarnings("unchecked")
    private static void setEnv(Map<String, String> additions) {
        try {
            Class<?> envCls = Class.forName("java.lang.ProcessEnvironment");
            Field theEnvField = envCls.getDeclaredField("theEnvironment");
            theEnvField.setAccessible(true);
            Map<String, String> env = (Map<String, String>) theEnvField.get(null);
            env.putAll(additions);
            Field theCIEnvField = envCls.getDeclaredField("theCaseInsensitiveEnvironment");
            theCIEnvField.setAccessible(true);
            Map<String, String> cienv = (Map<String, String>) theCIEnvField.get(null);
            cienv.putAll(additions);
        } catch (ReflectiveOperationException e) {
            // Fallback: use a property override picked up via System.getProperty in tests, or no-op
            for (Map.Entry<String, String> e2 : additions.entrySet()) {
                System.setProperty(e2.getKey(), e2.getValue());
            }
        }
    }

    @SuppressWarnings("unchecked")
    private static void unsetEnv(String key) {
        try {
            Class<?> envCls = Class.forName("java.lang.ProcessEnvironment");
            Field theEnvField = envCls.getDeclaredField("theEnvironment");
            theEnvField.setAccessible(true);
            ((Map<String, String>) theEnvField.get(null)).remove(key);
            Field theCIEnvField = envCls.getDeclaredField("theCaseInsensitiveEnvironment");
            theCIEnvField.setAccessible(true);
            ((Map<String, String>) theCIEnvField.get(null)).remove(key);
        } catch (ReflectiveOperationException ignored) {
            System.clearProperty(key);
        }
    }
}
