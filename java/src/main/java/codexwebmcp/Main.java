package codexwebmcp;

public final class Main {
    private Main() {}

    public static void main(String[] args) throws Exception {
        // stdout is reserved for JSON-RPC. Anything printed via System.err goes to stderr.
        McpServer server = new McpServer(System.in, System.out);
        server.serve();
    }
}
