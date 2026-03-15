using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MyClaw.MCP;

namespace MyClaw.Integration.Tests.Mcp;

/// <summary>
/// MCP Test Fixture using stdio transport
/// </summary>
public class McpTestFixture : IAsyncLifetime
{
    public string WorkspacePath { get; }
    private Process? _serverProcess;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;
    private int _requestId = 0;
    private readonly Dictionary<string, TaskCompletionSource<JsonDocument>> _pendingResponses = new();
    private readonly object _lock = new();
    private Task? _readLoop;

    public McpTestFixture()
    {
        WorkspacePath = Path.Combine(Path.GetTempPath(), $"myclaw_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(WorkspacePath);
        Directory.CreateDirectory(Path.Combine(WorkspacePath, "memory"));
        Directory.CreateDirectory(Path.Combine(WorkspacePath, "skills"));
    }

    public async Task InitializeAsync()
    {
        // Start MCP server process
        var assemblyPath = typeof(McpServer).Assembly.Location;
        var projectDir = Path.GetDirectoryName(assemblyPath)!;
        var exePath = Path.Combine(projectDir, "..", "..", "..", "MyClaw.MCP.exe");
        
        // Try different paths
        if (!File.Exists(exePath))
        {
            exePath = Path.Combine(projectDir, "..", "..", "..", "MyClaw.MCP");
        }
        if (!File.Exists(exePath))
        {
            // Use dotnet run
            exePath = "dotnet";
        }

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = exePath == "dotnet" ? $"run --project ..\\..\\..\\..\\..\\src\\MyClaw.MCP -- {WorkspacePath}" : WorkspacePath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _serverProcess = Process.Start(psi);
        if (_serverProcess == null)
        {
            throw new InvalidOperationException("Failed to start MCP server");
        }

        _stdin = _serverProcess.StandardInput;
        _stdout = _serverProcess.StandardOutput;

        // Start reading responses
        _readLoop = Task.Run(ReadLoopAsync);

        // Wait for server to be ready
        await Task.Delay(500);

        // Send initialize
        var response = await SendRequestAsync("initialize", new { });
        if (response.RootElement.TryGetProperty("result", out var result))
        {
            // Server is ready
        }
    }

    public async Task DisposeAsync()
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            try
            {
                _serverProcess.Kill();
                _serverProcess.WaitForExit(TimeSpan.FromSeconds(5));
            }
            catch { }
        }

        _stdin?.Dispose();
        _stdout?.Dispose();
        _serverProcess?.Dispose();

        try
        {
            if (Directory.Exists(WorkspacePath))
            {
                Directory.Delete(WorkspacePath, true);
            }
        }
        catch { }
    }

    /// <summary>
    /// Send a JSON-RPC request and wait for response
    /// </summary>
    public async Task<JsonDocument> SendRequestAsync(string method, object? Params)
    {
        var id = Interlocked.Increment(ref _requestId).ToString();
        var request = new
        {
            jsonrpc = "2.0",
            id,
            method,
            @params = Params
        };

        var tcs = new TaskCompletionSource<JsonDocument>();
        lock (_lock)
        {
            _pendingResponses[id] = tcs;
        }

        var json = JsonSerializer.Serialize(request);
        await _stdin!.WriteLineAsync(json);
        await _stdin.FlushAsync();

        return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Send a notification (no response expected)
    /// </summary>
    public async Task SendNotificationAsync(string method, object? Params)
    {
        var request = new
        {
            jsonrpc = "2.0",
            method,
            @params = Params
        };

        var json = JsonSerializer.Serialize(request);
        await _stdin!.WriteLineAsync(json);
        await _stdin.FlushAsync();
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            while (_serverProcess != null && !_serverProcess.HasExited)
            {
                var line = await _stdout!.ReadLineAsync();
                if (line == null) break;

                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var doc = JsonDocument.Parse(line);
                    if (doc.RootElement.TryGetProperty("id", out var idProp))
                    {
                        var id = idProp.GetString();
                        if (id != null)
                        {
                            lock (_lock)
                            {
                                if (_pendingResponses.TryGetValue(id, out var tcs))
                                {
                                    _pendingResponses.Remove(id);
                                    tcs.TrySetResult(doc);
                                }
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Ignore invalid JSON
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MCP Test] Read loop error: {ex.Message}");
        }
    }
}
