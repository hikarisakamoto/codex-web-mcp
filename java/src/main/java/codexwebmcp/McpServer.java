package codexwebmcp;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ArrayNode;
import com.fasterxml.jackson.databind.node.ObjectNode;

import java.io.BufferedInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;

/**
 * Minimal MCP stdio server.
 *
 * Accepts JSON-RPC 2.0 messages framed either as:
 *   - LSP-style:  Content-Length: N\r\n\r\n{...body of length N bytes...}
 *   - NDJSON:     one JSON object per line
 *
 * Writes responses using LSP-style Content-Length framing.
 */
public final class McpServer {
    private static final ObjectMapper MAPPER = new ObjectMapper();
    private static final String PROTOCOL_VERSION = "2024-11-05";

    private final InputStream in;
    private final OutputStream out;

    public McpServer(InputStream in, OutputStream out) {
        this.in = new BufferedInputStream(in);
        this.out = out;
    }

    public void serve() throws IOException {
        while (true) {
            String body = readMessage();
            if (body == null) return; // EOF
            handleMessage(body);
        }
    }

    private String readMessage() throws IOException {
        in.mark(1024);
        int first = in.read();
        if (first < 0) return null;

        if (first == 'C') {
            // Looks like a Content-Length header. Read the header.
            StringBuilder header = new StringBuilder();
            header.append((char) first);
            int prev = -1, prev2 = -1, prev3 = -1;
            while (true) {
                int c = in.read();
                if (c < 0) return null;
                header.append((char) c);
                if (prev3 == '\r' && prev2 == '\n' && prev == '\r' && c == '\n') break;
                prev3 = prev2; prev2 = prev; prev = c;
            }
            int len = parseContentLength(header.toString());
            if (len <= 0) return "";
            byte[] buf = new byte[len];
            int read = 0;
            while (read < len) {
                int n = in.read(buf, read, len - read);
                if (n < 0) break;
                read += n;
            }
            return new String(buf, 0, read, StandardCharsets.UTF_8);
        } else {
            // Assume NDJSON: rewind and read until newline.
            in.reset();
            StringBuilder sb = new StringBuilder();
            while (true) {
                int c = in.read();
                if (c < 0) return sb.length() == 0 ? null : sb.toString();
                if (c == '\n') break;
                if (c == '\r') continue;
                sb.append((char) c);
            }
            return sb.toString();
        }
    }

    private int parseContentLength(String header) {
        for (String line : header.split("\r\n")) {
            int colon = line.indexOf(':');
            if (colon > 0 && line.substring(0, colon).trim().equalsIgnoreCase("Content-Length")) {
                try {
                    return Integer.parseInt(line.substring(colon + 1).trim());
                } catch (NumberFormatException ignored) { return -1; }
            }
        }
        return -1;
    }

    private void handleMessage(String body) throws IOException {
        if (body.isEmpty()) return;
        JsonNode msg;
        try {
            msg = MAPPER.readTree(body);
        } catch (IOException e) {
            System.err.println("[codex-web-mcp] parse error: " + e.getMessage());
            return;
        }

        JsonNode idNode = msg.get("id");
        String method = msg.has("method") ? msg.get("method").asText() : "";
        JsonNode params = msg.get("params");

        switch (method) {
            case "initialize": {
                ObjectNode result = MAPPER.createObjectNode();
                result.put("protocolVersion", PROTOCOL_VERSION);
                ObjectNode caps = MAPPER.createObjectNode();
                caps.set("tools", MAPPER.createObjectNode());
                result.set("capabilities", caps);
                ObjectNode info = MAPPER.createObjectNode();
                info.put("name", "codex-web");
                info.put("version", "0.1.0");
                result.set("serverInfo", info);
                sendResult(idNode, result);
                return;
            }
            case "notifications/initialized":
            case "initialized":
                return; // notification, no response
            case "tools/list": {
                ObjectNode result = MAPPER.createObjectNode();
                result.set("tools", Tools.listToolsArray());
                sendResult(idNode, result);
                return;
            }
            case "tools/call": {
                String name = params != null && params.has("name") ? params.get("name").asText() : "";
                JsonNode args = params != null ? params.get("arguments") : null;
                String text;
                try {
                    text = Tools.dispatch(name, args);
                } catch (Exception e) {
                    sendError(idNode, -32603, "Internal error: " + e.getMessage());
                    return;
                }
                ObjectNode result = MAPPER.createObjectNode();
                ArrayNode content = MAPPER.createArrayNode();
                ObjectNode block = MAPPER.createObjectNode();
                block.put("type", "text");
                block.put("text", text);
                content.add(block);
                result.set("content", content);
                result.put("isError", false);
                sendResult(idNode, result);
                return;
            }
            default:
                if (idNode != null && !idNode.isNull()) {
                    sendError(idNode, -32601, "Method not found: " + method);
                }
        }
    }

    private void sendResult(JsonNode id, JsonNode result) throws IOException {
        ObjectNode resp = MAPPER.createObjectNode();
        resp.put("jsonrpc", "2.0");
        resp.set("id", id == null ? null : id);
        resp.set("result", result);
        sendFramed(resp);
    }

    private void sendError(JsonNode id, int code, String message) throws IOException {
        ObjectNode resp = MAPPER.createObjectNode();
        resp.put("jsonrpc", "2.0");
        resp.set("id", id == null ? null : id);
        ObjectNode err = MAPPER.createObjectNode();
        err.put("code", code);
        err.put("message", message);
        resp.set("error", err);
        sendFramed(resp);
    }

    private void sendFramed(ObjectNode msg) throws IOException {
        byte[] body = MAPPER.writeValueAsBytes(msg);
        String header = "Content-Length: " + body.length + "\r\n\r\n";
        synchronized (out) {
            out.write(header.getBytes(StandardCharsets.US_ASCII));
            out.write(body);
            out.flush();
        }
    }
}
