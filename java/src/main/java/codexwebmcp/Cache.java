package codexwebmcp;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ObjectNode;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.time.Instant;

public final class Cache {
    private static final ObjectMapper MAPPER = new ObjectMapper();

    private Cache() {}

    public static String keyHash(String tool, String payload) {
        try {
            MessageDigest md = MessageDigest.getInstance("SHA-256");
            byte[] hash = md.digest((tool + "::" + payload).getBytes(StandardCharsets.UTF_8));
            StringBuilder sb = new StringBuilder(64);
            for (byte b : hash) sb.append(String.format("%02x", b));
            return sb.substring(0, 32);
        } catch (NoSuchAlgorithmException e) {
            throw new IllegalStateException("SHA-256 unavailable", e);
        }
    }

    private static Path filePath(String tool, String payload) throws IOException {
        return Config.cacheSubdir().resolve(tool + "-" + keyHash(tool, payload) + ".json");
    }

    public static String get(String tool, String payload, int ttlSeconds) {
        try {
            Path p = filePath(tool, payload);
            if (!Files.exists(p)) return null;
            long mtime = Files.getLastModifiedTime(p).toMillis();
            long ageMs = System.currentTimeMillis() - mtime;
            if (ageMs > ttlSeconds * 1000L) return null;
            byte[] data = Files.readAllBytes(p);
            ObjectNode node = (ObjectNode) MAPPER.readTree(data);
            if (node == null) return null;
            return node.has("content") ? node.get("content").asText() : null;
        } catch (IOException | ClassCastException e) {
            System.err.println("[codex-web-mcp] cache.get error: " + e.getMessage());
            return null;
        }
    }

    public static void put(String tool, String payload, String content) {
        try {
            Path p = filePath(tool, payload);
            ObjectNode entry = MAPPER.createObjectNode();
            entry.put("content", content);
            entry.put("timestamp", Instant.now().toString());
            byte[] bytes = MAPPER.writeValueAsBytes(entry);
            Path tmp = p.resolveSibling(p.getFileName().toString() + ".tmp");
            Files.write(tmp, bytes);
            try {
                Files.move(tmp, p, java.nio.file.StandardCopyOption.REPLACE_EXISTING,
                    java.nio.file.StandardCopyOption.ATOMIC_MOVE);
            } catch (java.nio.file.AtomicMoveNotSupportedException ex) {
                Files.move(tmp, p, java.nio.file.StandardCopyOption.REPLACE_EXISTING);
            }
        } catch (IOException e) {
            System.err.println("[codex-web-mcp] cache.put error: " + e.getMessage());
        }
    }
}
