using System.Text.Json;
using System.Text.Json.Nodes;

namespace MyClaw.Core.Ribosome;

/// <summary>
/// 核糖体加载器 - 从 RIBOSOME.json 加载本能定义
/// Ribosome Loader - Load instinct definitions from RIBOSOME.json
/// </summary>
public class RibosomeLoader
{
    private readonly string _myclawDir;
    private readonly string _templatesDir;
    private RibosomeConfig? _cachedConfig;
    private DateTime _lastCacheTime;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 创建 RibosomeLoader 实例
    /// </summary>
    /// <param name="myclawDir">MyClaw 数据目录 (~/.myclaw)</param>
    /// <param name="templatesDir">模板目录</param>
    public RibosomeLoader(string myclawDir, string templatesDir)
    {
        _myclawDir = myclawDir;
        _templatesDir = templatesDir;
    }

    /// <summary>
    /// 加载所有本能定义
    /// </summary>
    public async Task<Dictionary<string, InstinctDefinition>> LoadInstinctsAsync()
    {
        var config = await LoadConfigAsync();
        return config.Instincts;
    }

    /// <summary>
    /// 获取单个本能定义
    /// </summary>
    public async Task<InstinctDefinition?> GetInstinctAsync(string name)
    {
        var instincts = await LoadInstinctsAsync();
        return instincts.GetValueOrDefault(name);
    }

    /// <summary>
    /// 获取处理器名称
    /// </summary>
    public async Task<string?> GetHandlerAsync(string toolName)
    {
        var instinct = await GetInstinctAsync(toolName);
        return instinct?.Handler;
    }

    /// <summary>
    /// 获取所有工具名称
    /// </summary>
    public async Task<List<string>> GetToolNamesAsync()
    {
        var instincts = await LoadInstinctsAsync();
        return instincts.Keys.ToList();
    }

    /// <summary>
    /// 加载 RIBOSOME 配置
    /// </summary>
    public async Task<RibosomeConfig> LoadConfigAsync()
    {
        // 检查缓存
        if (_cachedConfig != null && DateTime.UtcNow - _lastCacheTime < _cacheTtl)
        {
            return _cachedConfig;
        }

        // 尝试从用户目录加载
        var userRibosomePath = Path.Combine(_myclawDir, "RIBOSOME.json");
        if (File.Exists(userRibosomePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(userRibosomePath);
                var config = ParseConfig(json);
                if (config != null)
                {
                    _cachedConfig = config;
                    _lastCacheTime = DateTime.UtcNow;
                    return config;
                }
            }
            catch
            {
                // 忽略错误，尝试从模板加载
            }
        }

        // 从模板目录加载
        var templatePath = Path.Combine(_templatesDir, "RIBOSOME.json");
        if (File.Exists(templatePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(templatePath);
                var config = ParseConfig(json);
                if (config != null)
                {
                    _cachedConfig = config;
                    _lastCacheTime = DateTime.UtcNow;
                    return config;
                }
            }
            catch
            {
                // 忽略错误
            }
        }

        // 返回默认配置
        _cachedConfig = GetDefaultConfig();
        _lastCacheTime = DateTime.UtcNow;
        return _cachedConfig;
    }

    /// <summary>
    /// 解析 RIBOSOME.json 配置
    /// </summary>
    private static RibosomeConfig? ParseConfig(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var config = JsonSerializer.Deserialize<RibosomeConfig>(json, options);
            return config;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 转换为 MCP 工具格式
    /// </summary>
    public async Task<List<McpToolDefinition>> GetMcpToolsAsync()
    {
        var instincts = await LoadInstinctsAsync();
        var tools = new List<McpToolDefinition>();

        foreach (var (name, instinct) in instincts)
        {
            tools.Add(new McpToolDefinition
            {
                Name = name,
                Description = instinct.Description,
                InputSchema = ConvertToJsonSchema(instinct.InputSchema)
            });
        }

        return tools;
    }

    /// <summary>
    /// 将内部 InputSchema 转换为 JSON 对象
    /// </summary>
    private static JsonObject? ConvertToJsonSchema(InstinctInputSchema? schema)
    {
        if (schema == null) return null;

        var jsonSchema = new JsonObject
        {
            ["type"] = schema.Type
        };

        if (schema.Properties != null && schema.Properties.Count > 0)
        {
            var properties = new JsonObject();
            foreach (var (propName, prop) in schema.Properties)
            {
                var propJson = new JsonObject { ["type"] = prop.Type };

                if (!string.IsNullOrEmpty(prop.Description))
                {
                    propJson["description"] = prop.Description;
                }

                if (prop.Enum != null && prop.Enum.Count > 0)
                {
                    var enumArray = new JsonArray();
                    foreach (var e in prop.Enum)
                    {
                        enumArray.Add(e);
                    }
                    propJson["enum"] = enumArray;
                }

                if (prop.Default != null)
                {
                    propJson["default"] = JsonValue.Create(prop.Default);
                }

                if (prop.Items != null)
                {
                    propJson["items"] = new JsonObject
                    {
                        ["type"] = prop.Items.Type
                    };
                }

                properties[propName] = propJson;
            }
            jsonSchema["properties"] = properties;
        }

        if (schema.Required != null && schema.Required.Count > 0)
        {
            var requiredArray = new JsonArray();
            foreach (var r in schema.Required)
            {
                requiredArray.Add(r);
            }
            jsonSchema["required"] = requiredArray;
        }

        return jsonSchema;
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    public void InvalidateCache()
    {
        _cachedConfig = null;
    }

    /// <summary>
    /// 获取默认配置（当文件不存在时使用）
    /// </summary>
    private static RibosomeConfig GetDefaultConfig()
    {
        return new RibosomeConfig
        {
            Type = "ribosome",
            Version = "1.0.0",
            Description = "核糖体 - 读取DNA并合成蛋白质（工具）的分子机器",
            Instincts = new Dictionary<string, InstinctDefinition>
            {
                ["myclaw_update"] = new InstinctDefinition
                {
                    Handler = "UpdateDNA",
                    Description = "【本能：神经重塑】修改DNA文件",
                    IsCore = true,
                    InputSchema = new InstinctInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, SchemaProperty>
                        {
                            ["action"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "操作类型",
                                Enum = new List<string> { "write", "list", "delete" }
                            },
                            ["filename"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "目标文件名"
                            },
                            ["content"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "新内容"
                            }
                        },
                        Required = new List<string>()
                    }
                },
                ["myclaw_mutate"] = new InstinctDefinition
                {
                    Handler = "MutateDNA",
                    Description = "【本能：基因重写手术】只允许重写 SOUL.md 或 IDENTITY.md；写入前执行 TelomereGuard 校验，并在覆写前自动创建备份。",
                    IsCore = true,
                    InputSchema = new InstinctInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, SchemaProperty>
                        {
                            ["target"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "目标 DNA 文件，仅允许 SOUL.md 或 IDENTITY.md",
                                Enum = new List<string> { "SOUL.md", "IDENTITY.md" }
                            },
                            ["content"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "完整的新文件内容"
                            }
                        },
                        Required = new List<string> { "target", "content" }
                    }
                },
                ["myclaw_reproduce"] = new InstinctDefinition
                {
                    Handler = "Reproduce",
                    Description = "【本能：孢子繁衍】打包当前核心 DNA、entities 和 skills，生成 zip 形式的孢子快照。",
                    IsCore = true,
                    InputSchema = new InstinctInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, SchemaProperty>
                        {
                            ["format"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "输出格式，当前仅支持 zip",
                                Enum = new List<string> { "zip" },
                                Default = "zip"
                            }
                        },
                        Required = new List<string>()
                    }
                },
                ["myclaw_note"] = new InstinctDefinition
                {
                    Handler = "Note",
                    Description = "【本能：海马体写入】将关键信息写入今日日记",
                    IsCore = true,
                    InputSchema = new InstinctInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, SchemaProperty>
                        {
                            ["text"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "记忆内容"
                            }
                        },
                        Required = new List<string> { "text" }
                    }
                },
                ["myclaw_read"] = new InstinctDefinition
                {
                    Handler = "Read",
                    Description = "【本能：全脑唤醒】加载所有DNA染色体到上下文",
                    IsCore = true,
                    InputSchema = new InstinctInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, SchemaProperty>(),
                        Required = new List<string>()
                    }
                },
                ["myclaw_exec"] = new InstinctDefinition
                {
                    Handler = "Exec",
                    Description = "【本能：感官与手】安全执行终端命令",
                    IsCore = true,
                    InputSchema = new InstinctInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, SchemaProperty>
                        {
                            ["command"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "要执行的命令"
                            }
                        },
                        Required = new List<string> { "command" }
                    }
                },
                ["myclaw_entity"] = new InstinctDefinition
                {
                    Handler = "Entity",
                    Description = "【本能：概念连接】构建知识图谱",
                    IsCore = true,
                    InputSchema = new InstinctInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, SchemaProperty>
                        {
                            ["action"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "动作",
                                Enum = new List<string> { "add", "remove", "link", "query", "list" }
                            },
                            ["name"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "实体名称"
                            },
                            ["type"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "实体类型",
                                Enum = new List<string> { "person", "project", "tool", "concept", "place", "other" }
                            }
                        },
                        Required = new List<string> { "action" }
                    }
                },
                ["myclaw_skill"] = new InstinctDefinition
                {
                    Handler = "Skill",
                    Description = "【技能创建器】创建、查看、删除可复用技能，并可通过 frontmatter 配置 hook",
                    IsCore = true,
                    InputSchema = new InstinctInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, SchemaProperty>
                        {
                            ["action"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "操作类型",
                                Enum = new List<string> { "create", "list", "delete", "harvest" }
                            },
                            ["name"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "技能名称"
                            },
                            ["description"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "技能描述"
                            },
                            ["content"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "技能内容"
                            },
                            ["keywords"] = new SchemaProperty
                            {
                                Type = "array",
                                Description = "可选关键词列表",
                                Items = new SchemaProperty { Type = "string" }
                            },
                            ["hooks"] = new SchemaProperty
                            {
                                Type = "array",
                                Description = "可选 hook 列表：onBoot, onHeartbeat, onMemoryWrite, onFileChanged",
                                Items = new SchemaProperty { Type = "string" }
                            },
                            ["filePatterns"] = new SchemaProperty
                            {
                                Type = "array",
                                Description = "onFileChanged 使用的可选文件匹配模式",
                                Items = new SchemaProperty { Type = "string" }
                            },
                            ["apply"] = new SchemaProperty
                            {
                                Type = "boolean",
                                Description = "harvest 时是否实际写入 skills 目录；默认 false 表示 dry-run"
                            },
                            ["overwrite"] = new SchemaProperty
                            {
                                Type = "boolean",
                                Description = "harvest 时是否允许覆盖已有技能"
                            },
                            ["sources"] = new SchemaProperty
                            {
                                Type = "array",
                                Description = "harvest 时要扫描的来源列表：copilot, cursor, claude, windsurf",
                                Items = new SchemaProperty { Type = "string" }
                            }
                        },
                        Required = new List<string> { "action" }
                    }
                },
                ["myclaw_introspect"] = new InstinctDefinition
                {
                    Handler = "Introspect",
                    Description = "【本能：自我观察】查看使用统计与行为模式",
                    IsCore = true,
                    InputSchema = new InstinctInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, SchemaProperty>
                        {
                            ["scope"] = new SchemaProperty
                            {
                                Type = "string",
                                Description = "观察范围",
                                Enum = new List<string> { "summary", "tools", "files" }
                            }
                        },
                        Required = new List<string>()
                    }
                },
                ["myclaw_dream"] = new InstinctDefinition
                {
                    Handler = "Dream",
                    Description = "【本能：做梦】从日志中提取洞察",
                    IsCore = true,
                    InputSchema = new InstinctInputSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, SchemaProperty>(),
                        Required = new List<string>()
                    }
                }
            }
        };
    }
}

/// <summary>
/// MCP 工具定义
/// </summary>
public class McpToolDefinition
{
    /// <summary>
    /// 工具名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工具描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 输入模式 (JSON Schema)
    /// </summary>
    public JsonObject? InputSchema { get; set; }
}
