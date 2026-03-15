using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyClaw.Core.Analytics;
using MyClaw.Core.Briefing;
using MyClaw.Core.Configuration;
using MyClaw.Core.Entities;
using MyClaw.Core.Evolution;
using MyClaw.Core.Execution;
using MyClaw.Core.Logging;
using MyClaw.Core.Memory;
using MyClaw.Core.Ribosome;
using MyClaw.Skills;

namespace MyClaw.MCP;

/// <summary>
/// MCP Server - Model Context Protocol over stdio
/// </summary>
public class McpServer : IDisposable
{
    private readonly string? _workspacePath;
    private CancellationTokenSource? _cts;
    private Task? _readLoopTask;

    private MemoryStore _memoryStore = null!;
    private EntityStore _entityStore = null!;
    private SkillManager _skillManager = null!;
    private CommandExecutor _commandExecutor = null!;
    private SignalDetector _signalDetector = null!;
    private RibosomeLoader _ribosomeLoader = null!;
    private AnalyticsService _analyticsService = null!;
    private ToolUsageTracker _toolUsageTracker = null!;
    private DailyBriefingService _dailyBriefingService = null!;
    private string _workspace = null!;

    // Protocol state
    private bool _initialized = false;
    private string _clientProtocolVersion = "2024-11-05";
    private ClientCapabilities? _clientCapabilities;

    public McpServer(string? workspacePath = null)
    {
        _workspacePath = workspacePath;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

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

        var templatesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");
        if (!Directory.Exists(templatesDir))
        {
            var cwd = Directory.GetCurrentDirectory();
            templatesDir = Path.Combine(cwd, "templates");
        }
        _ribosomeLoader = new RibosomeLoader(_workspace, templatesDir);

        _analyticsService = new AnalyticsService(_workspace);
        _toolUsageTracker = new ToolUsageTracker(_workspace);
        var statisticsReporter = new StatisticsReporter(_analyticsService, _toolUsageTracker);
        _dailyBriefingService = new DailyBriefingService(_memoryStore, _analyticsService, _entityStore, statisticsReporter, _toolUsageTracker);

        await _entityStore.LoadAsync();

        // Start reading from stdin
        _readLoopTask = Task.Run(() => ReadLoopAsync(_cts.Token), _cts.Token);

        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        if (_readLoopTask != null)
        {
            try { await _readLoopTask.WaitAsync(TimeSpan.FromSeconds(5)); } catch { }
        }
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await Console.In.ReadLineAsync(ct);
                if (line == null) break;

                if (string.IsNullOrWhiteSpace(line)) continue;

                _ = Task.Run(() => HandleMessageAsync(line, ct), ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            Log.Error($"[MCP] Read loop error: {ex.Message}");
        }
    }

    private async Task HandleMessageAsync(string line, CancellationToken ct)
    {
        try
        {
            var message = JsonSerializer.Deserialize<JsonRpcRequest>(line, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (message == null)
            {
                await SendErrorAsync(null, -32700, "Parse error");
                return;
            }

            if (message.JsonRpc != "2.0")
            {
                await SendErrorAsync(message.Id, -32600, "Invalid Request");
                return;
            }

            await HandleRequestAsync(message, ct);
        }
        catch (JsonException ex)
        {
            await SendErrorAsync(null, -32700, $"Parse error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Log.Error($"[MCP] Handle message error: {ex.Message}");
        }
    }

    private async Task HandleRequestAsync(JsonRpcRequest request, CancellationToken ct)
    {
        object? result = null;
        JsonRpcError? error = null;

        try
        {
            result = request.Method switch
            {
                "initialize" => HandleInitialize(request.Params),
                "notifications/initialized" => HandleInitialized(),
                "tools/list" => HandleListTools(),
                "tools/call" => await HandleCallToolAsync(request.Params, ct),
                "resources/list" => HandleListResources(),
                "resources/read" => HandleReadResource(request.Params),
                "resources/templates/list" => HandleListResourceTemplates(),
                "prompts/list" => HandleListPrompts(),
                "prompts/get" => HandleGetPrompt(request.Params),
                "ping" => new { },
                _ => null
            };

            if (result == null && request.Method != "notifications/initialized" && request.Method != "ping")
            {
                error = new JsonRpcError { Code = -32601, Message = $"Method not found: {request.Method}" };
            }
        }
        catch (Exception ex)
        {
            error = new JsonRpcError { Code = -32603, Message = ex.Message };
        }

        // Notifications (no id) don't send responses
        if (request.Id == null && request.Method.StartsWith("notifications/"))
        {
            return;
        }

        if (error != null)
        {
            await SendErrorAsync(request.Id, error.Code, error.Message);
        }
        else
        {
            await SendResultAsync(request.Id, result);
        }
    }

    private object HandleInitialize(JsonElement? Params)
    {
        _initialized = true;

        if (Params.HasValue)
        {
            var params_obj = Params.Value;
            if (params_obj.TryGetProperty("protocolVersion", out var versionProp))
            {
                _clientProtocolVersion = versionProp.GetString() ?? "2024-11-05";
            }
            if (params_obj.TryGetProperty("capabilities", out var capsProp))
            {
                _clientCapabilities = JsonSerializer.Deserialize<ClientCapabilities>(capsProp.GetRawText());
            }
        }

        return new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                tools = new { listChanged = false },
                resources = new { listChanged = false, subscribe = false },
                prompts = new { listChanged = false },
                logging = new { }
            },
            serverInfo = new { name = "myclaw", version = "1.0.0" }
        };
    }

    private object? HandleInitialized()
    {
        Log.Info("[MCP] Client initialized");
        return null; // Notification, no response
    }

    private object HandleListTools()
    {
        var tools = new List<object>();

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

        foreach (var skill in _skillManager.LoadedSkills)
        {
            tools.Add(new
            {
                name = $"skill_{skill.Name}",
                description = $"[Skill: {skill.Name}] {skill.Description}",
                inputSchema = new { type = "object", description = "Skill input" }
            });
        }

        tools.Add(new
        {
            name = "myclaw_briefing",
            description = "Generate daily briefing: tool usage, memories, entities, todo reminders, and suggestions.",
            inputSchema = new { type = "object", properties = new { }, description = "No parameters required" }
        });

        return new { tools };
    }

    private async Task<object> HandleCallToolAsync(JsonElement? Params, CancellationToken ct)
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

        var sw = Stopwatch.StartNew();
        var result = await ExecuteToolAsync(name, args, ct);
        sw.Stop();
        var success = !result.StartsWith("Error") && !result.StartsWith("Error:");
        _analyticsService.TrackToolCall(name);
        _ = _toolUsageTracker.RecordToolCallAsync(name, success, (int)sw.ElapsedMilliseconds);

        return new { content = new[] { new { type = "text", text = result } } };
    }

    private async Task<string> ExecuteToolAsync(string name, Dictionary<string, object>? args, CancellationToken ct)
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
                "myclaw_briefing" => await ToolBriefingAsync(),
                _ => name.StartsWith("skill_") ? await ToolSkillAsync(name, args) : $"Unknown tool: {name}"
            };
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    // Tool implementations (same as before, with async overloads)
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
        return $"Updated {filename}.";
    }

    private string ToolNote(Dictionary<string, object> args)
    {
        var text = args["text"].ToString()!;
        _memoryStore.AppendToday(text);
        return "Recorded to today's log.";
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
        return _memoryStore.ArchiveToday() ? "Archived today's log." : "No log to archive.";
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
            _ => "Unknown action"
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
        return removed ? $"Deleted '{name}'." : $"Entity '{name}' does not exist.";
    }

    private async Task<string> EntityLinkAsync(Dictionary<string, object> args)
    {
        var name = args["name"].ToString()!;
        var relation = args["relation"].ToString()!;
        var linked = await _entityStore.LinkAsync(name, relation);
        return linked ? $"Linked '{name}' -> '{relation}'." : $"Entity '{name}' does not exist.";
    }

    private async Task<string> EntityQueryAsync(Dictionary<string, object> args)
    {
        var name = args["name"].ToString()!;
        var entity = await _entityStore.QueryAsync(name);
        if (entity == null) return $"Entity '{name}' does not exist.";

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
        if (entities.Count == 0) return "No entities found.";

        var lines = entities.Select(e => $"- **{e.Name}** ({e.Type}, {e.MentionCount}x) - last: {e.LastMentioned}");
        return $"## Entities ({entities.Count})\n{string.Join("\n", lines)}";
    }

    private async Task<string> ToolExecAsync(Dictionary<string, object> args)
    {
        var command = args["command"].ToString()!;
        var result = await _commandExecutor.ExecuteAsync(command);
        return result.IsSuccess ? result.Output : $"Error (exit code {result.ExitCode}): {result.Output}";
    }

    private async Task<string> ToolBriefingAsync()
    {
        return await _dailyBriefingService.GenerateBriefingAsync();
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

                Distillation: {(evaluation.ShouldDistill ? $"Warning {evaluation.Urgency}: {evaluation.Reason}" : "OK")}
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
        if (skill == null) return $"Skill '{skillName}' does not exist.";

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
            _ => "Unknown action"
        };
    }

    private string ToolSkillList()
    {
        if (_skillManager.LoadedSkills.Count == 0) return "No skills installed.";
        var lines = _skillManager.LoadedSkills.Select(s => $"- **{s.Name}**: {s.Description}");
        return $"## Skills ({_skillManager.LoadedSkills.Count})\n{string.Join("\n", lines)}";
    }

    private async Task<string> ToolSkillCreateAsync(Dictionary<string, object> args)
    {
        if (!args.TryGetValue("name", out var nameObj) || nameObj == null)
            return "Error: name parameter required.";
        if (!args.TryGetValue("description", out var descObj) || descObj == null)
            return "Error: description parameter required.";
        if (!args.TryGetValue("content", out var contentObj) || contentObj == null)
            return "Error: content parameter required.";

        var name = nameObj.ToString()!;
        var description = descObj.ToString()!;
        var content = contentObj.ToString()!;

        var skillPath = Path.Combine(_workspace, "skills", $"{name}.md");
        Directory.CreateDirectory(Path.GetDirectoryName(skillPath)!);

        var skillContent = $"---\ndescription: {description}\n---\n\n{content}";
        await File.WriteAllTextAsync(skillPath, skillContent);

        _skillManager.LoadSkills();
        return $"Skill '{name}' created.";
    }

    private string ToolSkillDelete(Dictionary<string, object> args)
    {
        if (!args.TryGetValue("name", out var nameObj) || nameObj == null)
            return "Error: name parameter required.";

        var name = nameObj.ToString()!;
        var skillPath = Path.Combine(_workspace, "skills", $"{name}.md");

        if (!File.Exists(skillPath)) return $"Skill '{name}' does not exist.";

        File.Delete(skillPath);
        _skillManager.LoadSkills();
        return $"Skill '{name}' deleted.";
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
            _ => "Unknown scope parameter"
        };
    }

    private string ToolDream()
    {
        var today = _memoryStore.ReadToday();
        if (string.IsNullOrEmpty(today)) return "No today's log available for analysis.";

        var evaluation = _memoryStore.EvaluateDistillation();
        return $"""
            ## Dream Analysis

            Today's activity log length: {today.Length} characters
            Distillation recommendation: {(evaluation.ShouldDistill ? $"Needed ({evaluation.Urgency})" : "Not yet needed")}
            Reason: {evaluation.Reason}

            Purpose: Review today's records, identify patterns and insights, prepare for long-term memory integration.
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

        return $"Immune upgrade complete. Backed up {backedUp.Count} core files: {string.Join(", ", backedUp)}";
    }

    private string ToolHeal()
    {
        var backupDir = Path.Combine(_workspace, ".backup");
        if (!Directory.Exists(backupDir)) return "Backup directory not found.";

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

        return $"Gene repair complete. Restored {restored.Count} core files: {string.Join(", ", restored)}";
    }

    private string ToolNociception(Dictionary<string, object> args)
    {
        var action = args.TryGetValue("action", out var a) ? a.ToString() : "list";
        var nociceptionPath = Path.Combine(_workspace, "NOCICEPTION.md");

        return action switch
        {
            "list" => File.Exists(nociceptionPath) ? File.ReadAllText(nociceptionPath) : "No pain memories.",
            "record" => ToolNociceptionRecord(args, nociceptionPath),
            "check" => ToolNociceptionCheck(args, nociceptionPath),
            "clear" => ToolNociceptionClear(nociceptionPath),
            _ => "Unknown action"
        };
    }

    private string ToolNociceptionRecord(Dictionary<string, object> args, string path)
    {
        if (!args.TryGetValue("stimulus", out var stimulus) || stimulus == null)
            return "Error: stimulus parameter required.";
        if (!args.TryGetValue("harm", out var harm) || harm == null)
            return "Error: harm parameter required.";
        if (!args.TryGetValue("strategy", out var strategy) || strategy == null)
            return "Error: strategy parameter required.";

        var entry = $"""

            ## Pain Record - {DateTime.Now:yyyy-MM-dd HH:mm}

            **Trigger**: {stimulus}
            **Harm Result**: {harm}
            **Avoidance Strategy**: {strategy}

            """;

        File.AppendAllText(path, entry);
        return "Pain memory recorded.";
    }

    private string ToolNociceptionCheck(Dictionary<string, object> args, string path)
    {
        if (!File.Exists(path)) return "No pain memories.";

        if (!args.TryGetValue("stimulus", out var stimulus) || stimulus == null)
            return "Error: stimulus parameter required.";

        var content = File.ReadAllText(path);
        return content.Contains(stimulus.ToString()!)
            ? $"Warning: '{stimulus}' found in pain memory!"
            : $"OK: '{stimulus}' not found in pain memory.";
    }

    private string ToolNociceptionClear(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            return "Pain memories cleared.";
        }
        return "No pain memories to clear.";
    }

    private object HandleListResources()
    {
        var resources = new List<object>
        {
            new { uri = "myclaw://context", name = "MyClaw Context", mimeType = "text/markdown", description = "Complete context and memories" },
            new { uri = "myclaw://skills", name = "Skills Index", mimeType = "text/markdown", description = "Skills list" },
            new { uri = "myclaw://status", name = "MyClaw Status", mimeType = "text/markdown", description = "System status" },
            new { uri = "myclaw://briefing", name = "Daily Briefing", mimeType = "text/markdown", description = "Daily briefing (tool usage, memories, todos, suggestions)" }
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
        string content;
        if (uri == "myclaw://briefing")
        {
            content = _dailyBriefingService.GenerateBriefingAsync().GetAwaiter().GetResult();
        }
        else
        {
            content = uri switch
            {
                "myclaw://context" => ToolRead(new Dictionary<string, object>()),
                "myclaw://skills" => string.Join("\n", _skillManager.LoadedSkills.Select(s => $"- {s.Name}: {s.Description}")),
                "myclaw://status" => ToolStatus(),
                _ => "Unknown resource"
            };
        }

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
            new { name = "myclaw_wakeup", description = "Wake up and load context" },
            new { name = "myclaw_growup", description = "Memory distillation" },
            new { name = "myclaw_briefing", description = "Daily briefing" }
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
                new { role = "user", content = new { type = "text", text = "System: Waking up... Call tool `myclaw_read` to load context." } }
            },
            "myclaw_growup" => new[]
            {
                new { role = "user", content = new { type = "text", text = "System: Performing memory distillation. Check today's log and update MEMORY.md." } }
            },
            "myclaw_briefing" => new[]
            {
                new { role = "user", content = new { type = "text", text = "Please call tool myclaw_briefing for complete daily briefing." } }
            },
            _ => Array.Empty<object>()
        };

        return new { messages };
    }

    // Output helpers
    private async Task SendResultAsync(object? id, object? result)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id,
            result
        };
        await WriteLineAsync(JsonSerializer.Serialize(response));
    }

    private async Task SendErrorAsync(object? id, int code, string message)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id,
            error = new { code, message }
        };
        await WriteLineAsync(JsonSerializer.Serialize(response));
    }

    private async Task WriteLineAsync(string line)
    {
        await Console.Out.WriteLineAsync(line);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    // JSON RPC types
    private class JsonRpcRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public object? Id { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("params")]
        public JsonElement? Params { get; set; }
    }

    private class JsonRpcError
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    private class ClientCapabilities
    {
        // Placeholder for client capabilities
    }
}
