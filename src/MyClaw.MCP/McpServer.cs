using System.Net;
using System.Text;
using System.Text.Json;
using MyClaw.Core.Configuration;
using MyClaw.Core.Entities;
using MyClaw.Core.Evolution;
using MyClaw.Core.Execution;
using MyClaw.Core.Memory;
using MyClaw.Core.Ribosome;
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
    private RibosomeLoader _ribosomeLoader = null!;
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

        // 初始化 RibosomeLoader - 从项目根目录的 templates 文件夹加载
        var templatesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");
        if (!Directory.Exists(templatesDir))
        {
            // 尝试从当前工作目录的上级查找
            var cwd = Directory.GetCurrentDirectory();
            templatesDir = Path.Combine(cwd, "templates");
        }
        _ribosomeLoader = new RibosomeLoader(_workspace, templatesDir);

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
        var tools = new List<object>();

        // 从 RibosomeLoader 加载本能工具
        var mcpTools = _ribosomeLoader.GetMcpToolsAsync().GetAwaiter().GetResult();
        foreach (var tool in mcpTools)
        {
            tools.Add(new
            {
                name = tool.Name,
                description = tool.Description,
                inputSchema = tool.InputSchema
            });
        }

        // 添加技能工具
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
                "myclaw_skill" => await ToolSkillManagerAsync(args),
                "myclaw_introspect" => ToolIntrospect(args),
                "myclaw_dream" => ToolDream(),
                "myclaw_immune" => ToolImmune(),
                "myclaw_heal" => ToolHeal(),
                "myclaw_nociception" => ToolNociception(args),
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

    private async Task<string> ToolSkillManagerAsync(Dictionary<string, object> args)
    {
        var action = args.TryGetValue("action", out var a) ? a.ToString() : "list";

        return action switch
        {
            "list" => ToolSkillList(),
            "create" => await ToolSkillCreateAsync(args),
            "delete" => ToolSkillDelete(args),
            _ => "未知操作"
        };
    }

    private string ToolSkillList()
    {
        if (_skillManager.LoadedSkills.Count == 0) return "没有已安装的技能。";
        var lines = _skillManager.LoadedSkills.Select(s => $"- **{s.Name}**: {s.Description}");
        return $"## Skills ({_skillManager.LoadedSkills.Count})\n{string.Join("\n", lines)}";
    }

    private async Task<string> ToolSkillCreateAsync(Dictionary<string, object> args)
    {
        if (!args.TryGetValue("name", out var nameObj) || nameObj == null)
            return "错误: 需要提供 name 参数。";
        if (!args.TryGetValue("description", out var descObj) || descObj == null)
            return "错误: 需要提供 description 参数。";
        if (!args.TryGetValue("content", out var contentObj) || contentObj == null)
            return "错误: 需要提供 content 参数。";

        var name = nameObj.ToString()!;
        var description = descObj.ToString()!;
        var content = contentObj.ToString()!;

        var skillPath = Path.Combine(_workspace, "skills", $"{name}.md");
        Directory.CreateDirectory(Path.GetDirectoryName(skillPath)!);

        var skillContent = $"---\ndescription: {description}\n---\n\n{content}";
        await File.WriteAllTextAsync(skillPath, skillContent);

        _skillManager.LoadSkills();
        return $"技能 '{name}' 已创建。";
    }

    private string ToolSkillDelete(Dictionary<string, object> args)
    {
        if (!args.TryGetValue("name", out var nameObj) || nameObj == null)
            return "错误: 需要提供 name 参数。";

        var name = nameObj.ToString()!;
        var skillPath = Path.Combine(_workspace, "skills", $"{name}.md");

        if (!File.Exists(skillPath)) return $"技能 '{name}' 不存在。";

        File.Delete(skillPath);
        _skillManager.LoadSkills();
        return $"技能 '{name}' 已删除。";
    }

    private string ToolIntrospect(Dictionary<string, object> args)
    {
        var scope = args.TryGetValue("scope", out var s) ? s.ToString() : "summary";

        var entityCount = _entityStore.GetCountAsync().GetAwaiter().GetResult();
        var archivedCount = _memoryStore.GetArchivedCount();
        var skillCount = _skillManager.LoadedSkills.Count;

        return scope switch
        {
            "summary" => $"""
                ## Introspection Summary

                - Entities: {entityCount}
                - Archived Memories: {archivedCount}
                - Skills: {skillCount}
                - Workspace: {_workspace}
                """,
            "tools" => $"""
                ## Tool Usage Analysis

                Available tools from RIBOSOME:
                {string.Join("\n", _ribosomeLoader.GetToolNamesAsync().GetAwaiter().GetResult().Select(t => $"- {t}"))}

                Skills: {skillCount}
                """,
            "files" => $"""
                ## File Analysis

                Workspace: {_workspace}
                - Memory files: {(_memoryStore != null ? "active" : "none")}
                - Entity store: {entityCount} entities
                - Skills directory: {skillCount} files
                """,
            _ => "未知 scope 参数"
        };
    }

    private string ToolDream()
    {
        var today = _memoryStore.ReadToday();
        if (string.IsNullOrEmpty(today)) return "没有今日日志可供分析。";

        var evaluation = _memoryStore.EvaluateDistillation();
        return $"""
            ## Dream Analysis

            今日活动记录长度: {today.Length} 字符
            蒸馏建议: {(evaluation.ShouldDistill ? $"需要 ({evaluation.Urgency})" : "暂不需要")}
            原因: {evaluation.Reason}

            意义: 回顾今日记录，识别模式和洞察，准备长期记忆整合。
            """;
    }

    private string ToolImmune()
    {
        var backupDir = Path.Combine(_workspace, ".backup");
        Directory.CreateDirectory(backupDir);

        var coreFiles = new[] { "IDENTITY.md", "SOUL.md", "AGENTS.md", "USER.md", "TOOLS.md", "MEMORY.md" };
        var backedUp = new List<string>();

        foreach (var file in coreFiles)
        {
            var path = Path.Combine(_workspace, file);
            if (File.Exists(path))
            {
                var backupPath = Path.Combine(backupDir, file);
                File.Copy(path, backupPath, overwrite: true);
                backedUp.Add(file);
            }
        }

        return $"免疫升级完成。已备份 {backedUp.Count} 个核心文件: {string.Join(", ", backedUp)}";
    }

    private string ToolHeal()
    {
        var backupDir = Path.Combine(_workspace, ".backup");
        if (!Directory.Exists(backupDir)) return "没有找到备份目录。";

        var coreFiles = new[] { "IDENTITY.md", "SOUL.md", "AGENTS.md", "USER.md", "TOOLS.md", "MEMORY.md" };
        var restored = new List<string>();

        foreach (var file in coreFiles)
        {
            var backupPath = Path.Combine(backupDir, file);
            if (File.Exists(backupPath))
            {
                var targetPath = Path.Combine(_workspace, file);
                File.Copy(backupPath, targetPath, overwrite: true);
                restored.Add(file);
            }
        }

        return $"基因修复完成。已恢复 {restored.Count} 个核心文件: {string.Join(", ", restored)}";
    }

    private string ToolNociception(Dictionary<string, object> args)
    {
        var action = args.TryGetValue("action", out var a) ? a.ToString() : "list";
        var nociceptionPath = Path.Combine(_workspace, "NOCICEPTION.md");

        return action switch
        {
            "list" => File.Exists(nociceptionPath) ? File.ReadAllText(nociceptionPath) : "没有痛觉记忆。",
            "record" => ToolNociceptionRecord(args, nociceptionPath),
            "check" => ToolNociceptionCheck(args, nociceptionPath),
            "clear" => ToolNociceptionClear(nociceptionPath),
            _ => "未知操作"
        };
    }

    private string ToolNociceptionRecord(Dictionary<string, object> args, string path)
    {
        if (!args.TryGetValue("stimulus", out var stimulus) || stimulus == null)
            return "错误: 需要 stimulus 参数。";
        if (!args.TryGetValue("harm", out var harm) || harm == null)
            return "错误: 需要 harm 参数。";
        if (!args.TryGetValue("strategy", out var strategy) || strategy == null)
            return "错误: 需要 strategy 参数。";

        var entry = $"""

            ## 痛觉记录 - {DateTime.Now:yyyy-MM-dd HH:mm}

            **触发点**: {stimulus}
            **伤害结果**: {harm}
            **规避方案**: {strategy}

            """;

        File.AppendAllText(path, entry);
        return "痛觉记忆已记录。";
    }

    private string ToolNociceptionCheck(Dictionary<string, object> args, string path)
    {
        if (!File.Exists(path)) return "没有痛觉记忆。";

        if (!args.TryGetValue("stimulus", out var stimulus) || stimulus == null)
            return "错误: 需要 stimulus 参数。";

        var content = File.ReadAllText(path);
        return content.Contains(stimulus.ToString()!)
            ? $"⚠️ 警告: '{stimulus}' 在痛觉记忆中找到匹配！"
            : $"✅ '{stimulus}' 未在痛觉记忆中找到。";
    }

    private string ToolNociceptionClear(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            return "痛觉记忆已清除。";
        }
        return "没有需要清除的痛觉记忆。";
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
