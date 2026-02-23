using System.Net;
using System.Text;
using System.Text.Json;
using MyClaw.Core.Configuration;
using MyClaw.Core.Entities;
using MyClaw.Core.Evolution;
using MyClaw.Core.Execution;
using MyClaw.Core.Memory;
using MyClaw.Skills;

namespace MyClaw.MCP;

/// <summary>
/// MCP Server - Model Context Protocol over HTTP (Streamable HTTP)
/// </summary>
public class McpServer
{
    private readonly int _port;
    private readonly string? _workspacePath;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;

    private MemoryStore _memoryStore = null!;
    private EntityStore _entityStore = null!;
    private SkillManager _skillManager = null!;
    private CommandExecutor _commandExecutor = null!;
    private SignalDetector _signalDetector = null!;
    private string _workspace = null!;

    public McpServer(int port, string? workspacePath = null)
    {
        _port = port;
        _workspacePath = workspacePath;
    }

    public async Task StartAsync()
    {
        _cts = new CancellationTokenSource();

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _workspace = !string.IsNullOrEmpty(_workspacePath) 
            ? _workspacePath 
            : Path.Combine(home, ".myclaw.net");
        Directory.CreateDirectory(_workspace);

        _memoryStore = new MemoryStore(_workspace);
        _entityStore = new EntityStore(_workspace);
        _skillManager = new SkillManager(_workspace);
        _skillManager.LoadSkills();
        _commandExecutor = new CommandExecutor();
        _signalDetector = new SignalDetector();

        await _entityStore.LoadAsync();

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _listener.Start();

        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));

        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        _listener?.Stop();
        await Task.CompletedTask;
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var context = await _listener!.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context), ct);
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCP] 接受连接错误: {ex.Message}");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Mcp-Session-Id");

        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 200;
            response.Close();
            return;
        }

        var path = request.Url?.AbsolutePath ?? "/";

        try
        {
            if (path == "/mcp" && request.HttpMethod == "POST")
            {
                await HandleJsonRpcAsync(request, response);
            }
            else if (path == "/health")
            {
                await WriteJsonAsync(response, new { status = "ok" });
            }
            else
            {
                response.StatusCode = 404;
                await WriteJsonAsync(response, new { error = "Not found" });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MCP] 错误: {ex.Message}");
            response.StatusCode = 500;
            await WriteJsonAsync(response, new { error = ex.Message });
        }
    }

    private async Task HandleJsonRpcAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        var body = await ReadBodyAsync(request);
        var jsonRpcRequest = JsonSerializer.Deserialize<JsonRpcRequest>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (jsonRpcRequest == null || jsonRpcRequest.JsonRpc != "2.0")
        {
            await WriteJsonRpcErrorAsync(response, null, -32600, "Invalid Request");
            return;
        }

        object? result = null;
        JsonRpcError? error = null;

        try
        {
            result = jsonRpcRequest.Method switch
            {
                "initialize" => HandleInitialize(jsonRpcRequest.Params),
                "notifications/initialized" => null,
                "tools/list" => HandleListTools(),
                "tools/call" => await HandleCallToolAsync(jsonRpcRequest.Params),
                "resources/list" => HandleListResources(),
                "resources/read" => HandleReadResource(jsonRpcRequest.Params),
                "resources/templates/list" => HandleListResourceTemplates(),
                "prompts/list" => HandleListPrompts(),
                "prompts/get" => HandleGetPrompt(jsonRpcRequest.Params),
                "ping" => new { },
                _ => null
            };

            if (result == null && jsonRpcRequest.Method != "notifications/initialized" && jsonRpcRequest.Method != "ping")
            {
                error = new JsonRpcError { Code = -32601, Message = $"Method not found: {jsonRpcRequest.Method}" };
            }
        }
        catch (Exception ex)
        {
            error = new JsonRpcError { Code = -32603, Message = ex.Message };
        }

        if (jsonRpcRequest.Id == null && jsonRpcRequest.Method == "notifications/initialized")
        {
            response.StatusCode = 204;
            response.Close();
            return;
        }

        if (error != null)
        {
            await WriteJsonRpcErrorAsync(response, jsonRpcRequest.Id, error.Code, error.Message);
        }
        else
        {
            await WriteJsonRpcResultAsync(response, jsonRpcRequest.Id, result);
        }
    }

    private object HandleInitialize(JsonElement? Params)
    {
        return new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                tools = new { listChanged = false },
                resources = new { listChanged = false, subscribe = false },
                prompts = new { listChanged = false }
            },
            serverInfo = new { name = "myclaw", version = "1.0.0" }
        };
    }

    private object HandleListTools()
    {
        var tools = new List<object>
        {
            new
            {
                name = "myclaw_update",
                description = "【本能：神经重塑】修改核心认知文件",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        filename = new { type = "string", description = "文件名", @enum = new[] { "AGENTS.md", "SOUL.md", "USER.md", "TOOLS.md", "IDENTITY.md", "MEMORY.md", "HEARTBEAT.md" } },
                        content = new { type = "string", description = "文件内容" }
                    },
                    required = new[] { "filename", "content" }
                }
            },
            new
            {
                name = "myclaw_note",
                description = "【本能：海马体写入】追加今日日志",
                inputSchema = new
                {
                    type = "object",
                    properties = new { text = new { type = "string", description = "日志内容" } },
                    required = new[] { "text" }
                }
            },
            new
            {
                name = "myclaw_read",
                description = "【本能：全脑唤醒】读取上下文和记忆",
                inputSchema = new
                {
                    type = "object",
                    properties = new { mode = new { type = "string", description = "读取模式", @enum = new[] { "full", "minimal" } } }
                }
            },
            new
            {
                name = "myclaw_archive",
                description = "【日志归档】归档今日日志",
                inputSchema = new { type = "object", description = "无需参数" }
            },
            new
            {
                name = "myclaw_entity",
                description = "【本能：概念连接】管理实体知识图谱",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        action = new { type = "string", description = "操作类型", @enum = new[] { "add", "remove", "link", "query", "list" } },
                        name = new { type = "string", description = "实体名称" },
                        type = new { type = "string", description = "实体类型", @enum = new[] { "person", "project", "tool", "concept", "place", "other" } },
                        attributes = new { type = "object", description = "属性" },
                        relation = new { type = "string", description = "关系" }
                    },
                    required = new[] { "action" }
                }
            },
            new
            {
                name = "myclaw_exec",
                description = "【本能：感官与手】安全执行终端命令",
                inputSchema = new
                {
                    type = "object",
                    properties = new { command = new { type = "string", description = "要执行的命令" } },
                    required = new[] { "command" }
                }
            },
            new
            {
                name = "myclaw_status",
                description = "【系统诊断】返回完整状态",
                inputSchema = new { type = "object", description = "无需参数" }
            }
        };

        foreach (var skill in _skillManager.LoadedSkills)
        {
            tools.Add(new
            {
                name = $"skill_{skill.Name}",
                description = $"【Skill: {skill.Name}】{skill.Description}",
                inputSchema = new { type = "object", description = "技能输入" }
            });
        }

        return new { tools };
    }

    private async Task<object> HandleCallToolAsync(JsonElement? Params)
    {
        if (Params == null || !Params.Value.TryGetProperty("name", out var nameEl))
        {
            return new { isError = true, content = new[] { new { type = "text", text = "Missing tool name" } } };
        }

        var name = nameEl.GetString() ?? "";
        Dictionary<string, object>? args = null;

        if (Params.Value.TryGetProperty("arguments", out var argsEl))
        {
            args = JsonSerializer.Deserialize<Dictionary<string, object>>(argsEl.GetRawText());
        }

        var result = await ExecuteToolAsync(name, args);
        return new { content = new[] { new { type = "text", text = result } } };
    }

    private async Task<string> ExecuteToolAsync(string name, Dictionary<string, object>? args)
    {
        args ??= new Dictionary<string, object>();

        try
        {
            return name switch
            {
                "myclaw_update" => await ToolUpdateAsync(args),
                "myclaw_note" => ToolNote(args),
                "myclaw_read" => ToolRead(args),
                "myclaw_archive" => ToolArchive(),
                "myclaw_entity" => await ToolEntityAsync(args),
                "myclaw_exec" => await ToolExecAsync(args),
                "myclaw_status" => ToolStatus(),
                _ => name.StartsWith("skill_") ? await ToolSkillAsync(name, args) : $"未知工具: {name}"
            };
        }
        catch (Exception ex)
        {
            return $"错误: {ex.Message}";
        }
    }

    private async Task<string> ToolUpdateAsync(Dictionary<string, object> args)
    {
        var filename = args["filename"].ToString()!;
        var content = args["content"].ToString()!;

        var path = Path.Combine(_workspace, filename);

        if (File.Exists(path))
        {
            File.Copy(path, path + ".bak", overwrite: true);
        }

        await File.WriteAllTextAsync(path, content);
        return $"已更新 {filename}。";
    }

    private string ToolNote(Dictionary<string, object> args)
    {
        var text = args["text"].ToString()!;
        _memoryStore.AppendToday(text);
        return "已记录到今日日志。";
    }

    private string ToolRead(Dictionary<string, object> args)
    {
        var mode = args.TryGetValue("mode", out var m) ? m.ToString() : "full";

        var parts = new List<string>();

        foreach (var file in new[] { "AGENTS.md", "SOUL.md", "IDENTITY.md", "USER.md", "TOOLS.md" })
        {
            var path = Path.Combine(_workspace, file);
            if (File.Exists(path))
            {
                parts.Add($"## {file}\n{File.ReadAllText(path)}");
            }
        }

        var memory = _memoryStore.ReadLongTerm();
        if (!string.IsNullOrEmpty(memory))
        {
            parts.Add($"## MEMORY.md\n{memory}");
        }

        var today = _memoryStore.ReadToday();
        if (!string.IsNullOrEmpty(today))
        {
            parts.Add($"## Today\n{today}");
        }

        return string.Join("\n\n", parts);
    }

    private string ToolArchive()
    {
        return _memoryStore.ArchiveToday() ? "已归档今日日志。" : "没有可归档的日志。";
    }

    private async Task<string> ToolEntityAsync(Dictionary<string, object> args)
    {
        var action = args["action"].ToString()!;

        return action switch
        {
            "add" => await EntityAddAsync(args),
            "remove" => await EntityRemoveAsync(args),
            "link" => await EntityLinkAsync(args),
            "query" => await EntityQueryAsync(args),
            "list" => await EntityListAsync(args),
            _ => "未知操作"
        };
    }

    private async Task<string> EntityAddAsync(Dictionary<string, object> args)
    {
        var name = args["name"].ToString()!;
        var type = Enum.Parse<EntityType>(args["type"].ToString()!, true);
        var attributes = args.TryGetValue("attributes", out var a) && a is Dictionary<string, object> dict
            ? dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "")
            : new Dictionary<string, string>();
        var relations = args.TryGetValue("relation", out var r) && r != null
            ? new List<string> { r.ToString()! }
            : new List<string>();

        var entity = new Entity
        {
            Name = name,
            Type = type,
            Attributes = attributes,
            Relations = relations
        };

        var result = await _entityStore.AddAsync(entity);
        return $"Entity '{result.Name}' ({result.Type}) - {result.MentionCount} mentions.";
    }

    private async Task<string> EntityRemoveAsync(Dictionary<string, object> args)
    {
        var name = args["name"].ToString()!;
        var removed = await _entityStore.RemoveAsync(name);
        return removed ? $"已删除 '{name}'。" : $"实体 '{name}' 不存在。";
    }

    private async Task<string> EntityLinkAsync(Dictionary<string, object> args)
    {
        var name = args["name"].ToString()!;
        var relation = args["relation"].ToString()!;
        var linked = await _entityStore.LinkAsync(name, relation);
        return linked ? $"已关联 '{name}' → '{relation}'。" : $"实体 '{name}' 不存在。";
    }

    private async Task<string> EntityQueryAsync(Dictionary<string, object> args)
    {
        var name = args["name"].ToString()!;
        var entity = await _entityStore.QueryAsync(name);
        if (entity == null) return $"实体 '{name}' 不存在。";

        var attrs = string.Join(", ", entity.Attributes.Select(a => $"{a.Key}: {a.Value}"));
        return $"**{entity.Name}** ({entity.Type})\nMentions: {entity.MentionCount}\nAttributes: {attrs}\nRelations: {string.Join("; ", entity.Relations)}";
    }

    private async Task<string> EntityListAsync(Dictionary<string, object> args)
    {
        EntityType? filter = null;
        if (args.TryGetValue("filterType", out var ft) && ft != null)
        {
            filter = Enum.Parse<EntityType>(ft.ToString()!, true);
        }

        var entities = await _entityStore.ListAsync(filter);
        if (entities.Count == 0) return "没有找到实体。";

        var lines = entities.Select(e => $"- **{e.Name}** ({e.Type}, {e.MentionCount}x) - last: {e.LastMentioned}");
        return $"## Entities ({entities.Count})\n{string.Join("\n", lines)}";
    }

    private async Task<string> ToolExecAsync(Dictionary<string, object> args)
    {
        var command = args["command"].ToString()!;
        var result = await _commandExecutor.ExecuteAsync(command);
        return result.IsSuccess ? result.Output : $"错误 (退出码 {result.ExitCode}): {result.Output}";
    }

    private string ToolStatus()
    {
        try
        {
            var evaluation = _memoryStore.EvaluateDistillation();
            var entityCount = _entityStore.GetCountAsync().GetAwaiter().GetResult();
            var archivedCount = _memoryStore.GetArchivedCount();

            return $"""
                === MyClaw Status ===

                Distillation: {(evaluation.ShouldDistill ? $"⚠️ {evaluation.Urgency}: {evaluation.Reason}" : "✅ OK")}
                Entities: {entityCount}
                Archived: {archivedCount}
                Skills: {_skillManager.LoadedSkills.Count}
                """;
        }
        catch (Exception ex)
        {
            return $"""
                === MyClaw Status ===
                
                Error: {ex.Message}
                Skills: {_skillManager.LoadedSkills.Count}
                """;
        }
    }

    private async Task<string> ToolSkillAsync(string name, Dictionary<string, object> args)
    {
        var skillName = name.Replace("skill_", "");
        var skill = _skillManager.GetSkill(skillName);
        if (skill == null) return $"技能 '{skillName}' 不存在。";

        var content = skill.GetSystemPrompt();
        return $"## Skill: {skill.Name}\n\n{content}\n\nInput: {JsonSerializer.Serialize(args)}";
    }

    private object HandleListResources()
    {
        var resources = new List<object>
        {
            new { uri = "myclaw://context", name = "MyClaw Context", mimeType = "text/markdown", description = "完整的上下文和记忆" },
            new { uri = "myclaw://skills", name = "Skills Index", mimeType = "text/markdown", description = "技能列表" },
            new { uri = "myclaw://status", name = "MyClaw Status", mimeType = "text/markdown", description = "系统状态" }
        };

        return new { resources };
    }

    private object HandleReadResource(JsonElement? Params)
    {
        if (Params == null || !Params.Value.TryGetProperty("uri", out var uriEl))
        {
            return new { contents = Array.Empty<object>() };
        }

        var uri = uriEl.GetString() ?? "";
        var content = uri switch
        {
            "myclaw://context" => ToolRead(new Dictionary<string, object>()),
            "myclaw://skills" => string.Join("\n", _skillManager.LoadedSkills.Select(s => $"- {s.Name}: {s.Description}")),
            "myclaw://status" => ToolStatus(),
            _ => "未知资源"
        };

        return new
        {
            contents = new[]
            {
                new { uri, mimeType = "text/markdown", text = content }
            }
        };
    }

    private object HandleListResourceTemplates()
    {
        return new { resourceTemplates = Array.Empty<object>() };
    }

    private object HandleListPrompts()
    {
        var prompts = new List<object>
        {
            new { name = "myclaw_wakeup", description = "唤醒并加载上下文" },
            new { name = "myclaw_growup", description = "记忆蒸馏" },
            new { name = "myclaw_briefing", description = "每日简报" }
        };

        return new { prompts };
    }

    private object HandleGetPrompt(JsonElement? Params)
    {
        if (Params == null || !Params.Value.TryGetProperty("name", out var nameEl))
        {
            return new { messages = Array.Empty<object>() };
        }

        var name = nameEl.GetString() ?? "";
        var messages = name switch
        {
            "myclaw_wakeup" => new[]
            {
                new { role = "user", content = new { type = "text", text = "系统: 正在唤醒... 调用工具 `myclaw_read` 加载上下文。" } }
            },
            "myclaw_growup" => new[]
            {
                new { role = "user", content = new { type = "text", text = "系统: 正在进行记忆蒸馏。检查今日日志并更新 MEMORY.md。" } }
            },
            "myclaw_briefing" => new[]
            {
                new { role = "user", content = new { type = "text", text = $"每日简报:\n{ToolStatus()}" } }
            },
            _ => Array.Empty<object>()
        };

        return new { messages };
    }

    private async Task WriteJsonRpcResultAsync(HttpListenerResponse response, object? id, object? result)
    {
        var jsonRpcResponse = new
        {
            jsonrpc = "2.0",
            id,
            result
        };
        await WriteJsonAsync(response, jsonRpcResponse);
    }

    private async Task WriteJsonRpcErrorAsync(HttpListenerResponse response, object? id, int code, string message)
    {
        var jsonRpcResponse = new
        {
            jsonrpc = "2.0",
            id,
            error = new { code, message }
        };
        response.StatusCode = 400;
        await WriteJsonAsync(response, jsonRpcResponse);
    }

    private async Task WriteJsonAsync(HttpListenerResponse response, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var bytes = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }

    private async Task<string> ReadBodyAsync(HttpListenerRequest request)
    {
        using var reader = new StreamReader(request.InputStream);
        return await reader.ReadToEndAsync();
    }

    private class JsonRpcRequest
    {
        public string JsonRpc { get; set; } = string.Empty;
        public object? Id { get; set; }
        public string Method { get; set; } = string.Empty;
        public JsonElement? Params { get; set; }
    }

    private class JsonRpcError
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
