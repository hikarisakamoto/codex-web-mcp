package codexwebmcp;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.StandardOpenOption;

public final class Config {
    public static final int CODEX_TIMEOUT_SECONDS = 180;

    private Config() {}

    public static String codexBin() {
        String v = System.getenv("CODEX_BIN");
        if (v != null && !v.isEmpty()) return v;
        return isWindows() ? "codex.exe" : "codex";
    }

    public static Path cacheDir() throws IOException {
        String v = System.getenv("CODEX_WEB_CACHE");
        Path dir;
        if (v != null && !v.isEmpty()) {
            dir = Paths.get(v);
        } else {
            Path base;
            if (isWindows()) {
                String localAppData = System.getenv("LOCALAPPDATA");
                if (localAppData != null && !localAppData.isEmpty()) {
                    base = Paths.get(localAppData);
                } else {
                    base = Paths.get(System.getProperty("user.home"), "AppData", "Local");
                }
            } else if (isMac()) {
                base = Paths.get(System.getProperty("user.home"), "Library", "Caches");
            } else {
                String xdg = System.getenv("XDG_CACHE_HOME");
                if (xdg != null && !xdg.isEmpty()) {
                    base = Paths.get(xdg);
                } else {
                    base = Paths.get(System.getProperty("user.home"), ".cache");
                }
            }
            dir = base.resolve("codex-web-mcp");
        }
        Files.createDirectories(dir);
        return dir;
    }

    public static Path cacheSubdir() throws IOException {
        Path sub = cacheDir().resolve("cache");
        Files.createDirectories(sub);
        return sub;
    }

    public static Path logPath() throws IOException {
        return cacheDir().resolve("calls.log");
    }

    public static void appendCallLog(String line) {
        try {
            Path p = logPath();
            Files.writeString(p, line + "\n",
                StandardOpenOption.CREATE,
                StandardOpenOption.WRITE,
                StandardOpenOption.APPEND);
        } catch (IOException ignored) {
            // logging failures must not break tool calls
        }
    }

    private static boolean isWindows() {
        return System.getProperty("os.name", "").toLowerCase().contains("win");
    }

    private static boolean isMac() {
        return System.getProperty("os.name", "").toLowerCase().contains("mac");
    }
}
