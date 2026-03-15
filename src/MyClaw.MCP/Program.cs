using MyClaw.Core.Logging;
using MyClaw.MCP;

// All logs to stderr to avoid stdout pollution (stdio MCP transport)
Log.Info("MyClaw MCP Server starting...");
Log.Info("Version: 1.0.0");
Log.Info("Protocol: MCP over stdio");
Log.Info("Use Ctrl+C to stop.");
Log.Info("");

// Get workspace path from args or use default
string? workspacePath = null;
if (args.Length > 0 && !args[0].StartsWith("--"))
{
    workspacePath = args[0];
}

using var cts = new CancellationTokenSource();

// Handle Ctrl+C
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Log.Info("Shutting down...");
    cts.Cancel();
};

// Start server
var server = new McpServer(workspacePath);
await server.StartAsync(cts.Token);

// Wait for cancellation
try
{
    await Task.Delay(-1, cts.Token);
}
catch (OperationCanceledException)
{
    // Normal shutdown
}

await server.StopAsync();
Log.Info("Server stopped.");
