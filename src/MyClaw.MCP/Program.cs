using MyClaw.Core.Logging;
using MyClaw.MCP;

// 所有日志输出到 stderr，避免干扰 stdout（用于 stdio MCP 传输）
Log.Info("MyClaw MCP Server starting...");
Log.Info("Version: 1.0.0");
Log.Info("Protocol: MCP over HTTP/SSE");

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 2334;
var server = new McpServer(port);

await server.StartAsync();

Log.Info($"Server listening on http://localhost:{port}");
Log.Info("Press Ctrl+C to stop.");

await Task.Delay(-1);
