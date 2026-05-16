package codexwebmcp;

import java.io.File;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.TimeUnit;

public final class Codex {
    private Codex() {}

    public static String run(String prompt) {
        Path tmpPath = Path.of(System.getProperty("java.io.tmpdir"),
            "codex-out-" + UUID.randomUUID().toString().replace("-", "") + ".txt");

        List<String> args = new ArrayList<>();
        args.add(Config.codexBin());
        args.add("exec");
        args.add("--skip-git-repo-check");
        args.add("--ephemeral");
        args.add("--color"); args.add("never");
        args.add("--sandbox"); args.add("read-only");
        args.add("--output-last-message"); args.add(tmpPath.toString());
        args.add(prompt);

        ProcessBuilder pb = new ProcessBuilder(args);
        pb.redirectInput(ProcessBuilder.Redirect.from(devNull()));
        pb.redirectOutput(ProcessBuilder.Redirect.DISCARD);
        pb.redirectError(ProcessBuilder.Redirect.PIPE);

        Process process;
        try {
            process = pb.start();
        } catch (IOException e) {
            cleanup(tmpPath);
            return "ERROR: codex invocation failed: " + e.getMessage();
        }

        StringBuilder stderr = new StringBuilder();
        Thread stderrThread = new Thread(() -> {
            try (var is = process.getErrorStream()) {
                byte[] buf = new byte[8192];
                int n;
                while ((n = is.read(buf)) > 0) {
                    stderr.append(new String(buf, 0, n, StandardCharsets.UTF_8));
                }
            } catch (IOException ignored) {
                // best effort
            }
        }, "codex-stderr-drain");
        stderrThread.setDaemon(true);
        stderrThread.start();

        try {
            boolean exited = process.waitFor(Config.CODEX_TIMEOUT_SECONDS, TimeUnit.SECONDS);
            if (!exited) {
                process.destroyForcibly();
                return "ERROR: codex timed out after 180s";
            }
            stderrThread.join(2000);

            int code = process.exitValue();
            if (code != 0) {
                return "ERROR: codex exited with code " + code + ": " + stderr.toString().trim();
            }
            if (!Files.exists(tmpPath)) {
                return "ERROR: codex produced no output file";
            }
            if (Files.size(tmpPath) == 0L) {
                return "ERROR: codex output file empty";
            }
            String data = Files.readString(tmpPath);
            String trimmed = data.replaceAll("[ \\t\\r\\n]+$", "");
            if (trimmed.isEmpty()) return "ERROR: codex output file empty";
            return trimmed;
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            process.destroyForcibly();
            return "ERROR: codex interrupted";
        } catch (IOException e) {
            return "ERROR: failed to read codex output: " + e.getMessage();
        } finally {
            cleanup(tmpPath);
        }
    }

    private static File devNull() {
        String os = System.getProperty("os.name", "").toLowerCase();
        return new File(os.contains("win") ? "NUL" : "/dev/null");
    }

    private static void cleanup(Path p) {
        try {
            Files.deleteIfExists(p);
        } catch (IOException ignored) {
            // best effort
        }
    }
}
