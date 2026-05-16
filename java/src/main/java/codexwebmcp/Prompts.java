package codexwebmcp;

import java.io.IOException;
import java.net.URISyntaxException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

public final class Prompts {
    private static final Object DIR_LOCK = new Object();
    private static volatile Path dirValue;
    private static final ConcurrentHashMap<String, String> TPL_CACHE = new ConcurrentHashMap<>();

    private Prompts() {}

    private static boolean isPromptsDir(Path p) {
        return Files.isDirectory(p);
    }

    private static Path walkUp(Path start, List<String> attempted) {
        Path cur = start;
        for (int i = 0; i < 10; i++) {
            Path cand = cur.resolve("prompts");
            attempted.add(cand.toString());
            if (isPromptsDir(cand)) return cand;
            Path parent = cur.getParent();
            if (parent == null || parent.equals(cur)) break;
            cur = parent;
        }
        return null;
    }

    public static Path resolveDir() {
        if (dirValue != null) return dirValue;
        synchronized (DIR_LOCK) {
            if (dirValue != null) return dirValue;
            List<String> attempted = new ArrayList<>();
            String env = System.getenv("CODEX_WEB_PROMPTS");
            if (env != null && !env.isEmpty()) {
                attempted.add(env);
                Path ep = Paths.get(env);
                if (isPromptsDir(ep)) {
                    dirValue = ep;
                    return dirValue;
                }
            }
            // From jar/class location.
            try {
                Path src = Paths.get(Prompts.class.getProtectionDomain()
                    .getCodeSource().getLocation().toURI());
                Path start = Files.isDirectory(src) ? src : src.getParent();
                if (start != null) {
                    Path found = walkUp(start, attempted);
                    if (found != null) {
                        dirValue = found;
                        return dirValue;
                    }
                }
            } catch (URISyntaxException | NullPointerException ignored) {
                // fall through
            }
            // From cwd.
            Path cwd = Paths.get("").toAbsolutePath();
            Path found = walkUp(cwd, attempted);
            if (found != null) {
                dirValue = found;
                return dirValue;
            }
            throw new IllegalStateException("prompts directory not found; tried: "
                + String.join(", ", attempted));
        }
    }

    public static String render(String toolName, Map<String, String> vars) {
        String tpl = TPL_CACHE.computeIfAbsent(toolName, name -> {
            Path dir = resolveDir();
            try {
                return Files.readString(dir.resolve(name + ".md"));
            } catch (IOException e) {
                throw new IllegalStateException("read prompt " + name + ": " + e.getMessage(), e);
            }
        });
        String out = tpl;
        for (Map.Entry<String, String> e : vars.entrySet()) {
            out = out.replace("{" + e.getKey() + "}", e.getValue());
        }
        return out;
    }
}
