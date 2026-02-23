using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace MyClaw.Core.Configuration;

/// <summary>
/// 配置加载器 - 从文件和环境变量加载配置
/// </summary>
public static class ConfigurationLoader
{
    public static string ConfigDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
        ".config", ".myclaw.net");
    
    public static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    /// <summary>
    /// 加载配置，优先级：环境变量 > 配置文件 > 默认值
    /// </summary>
    public static MyClawConfiguration Load(string? configPath = null)
    {
        var cfg = MyClawConfiguration.Default();
        var path = configPath ?? ConfigPath;

        // 1. 从 JSON 文件加载
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                var fileConfig = JsonSerializer.Deserialize<MyClawConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });
                
                if (fileConfig != null)
                {
                    MergeConfig(cfg, fileConfig);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[config] 警告: 加载配置文件失败: {ex.Message}");
            }
        }

        // 2. 环境变量覆盖
        ApplyEnvironmentVariables(cfg);

        // 3. 确保 workspace 有值
        if (string.IsNullOrEmpty(cfg.Agent.Workspace))
        {
            cfg.Agent.Workspace = MyClawConfiguration.Default().Agent.Workspace;
        }

        return cfg;
    }

    /// <summary>
    /// 保存配置到文件
    /// </summary>
    public static void Save(MyClawConfiguration config, string? path = null)
    {
        var configFile = path ?? ConfigPath;
        var dir = Path.GetDirectoryName(configFile);
        
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        File.WriteAllText(configFile, json);
    }

    /// <summary>
    /// 应用环境变量覆盖配置
    /// 优先级: OPENAI > DEEPSEEK > Anthropic > 其他
    /// </summary>
    private static void ApplyEnvironmentVariables(MyClawConfiguration cfg)
    {
        // ==================== OPENAI (最高优先级) ====================
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
        {
            cfg.Provider.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
            cfg.Provider.Type = "openai";
            
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_BASE_URL")))
                cfg.Provider.BaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL")!;
            
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_MODEL")))
                cfg.Agent.Model = Environment.GetEnvironmentVariable("OPENAI_MODEL")!;
        }
        // ==================== DEEPSEEK (第二优先级) ====================
        else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")))
        {
            cfg.Provider.ApiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")!;
            cfg.Provider.Type = "deepseek";
            cfg.Provider.BaseUrl = "https://api.deepseek.com/v1";
            
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEEPSEEK_MODEL")))
                cfg.Agent.Model = Environment.GetEnvironmentVariable("DEEPSEEK_MODEL")!;
            else
                cfg.Agent.Model = "deepseek-chat";
        }
        // ==================== Anthropic (第三优先级) ====================
        else
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
            {
                cfg.Provider.ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")!;
                cfg.Provider.Type = "anthropic";
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN")))
            {
                cfg.Provider.ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN")!;
                cfg.Provider.Type = "anthropic";
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYCLAW_API_KEY")))
            {
                cfg.Provider.ApiKey = Environment.GetEnvironmentVariable("MYCLAW_API_KEY")!;
            }

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL")))
                cfg.Provider.BaseUrl = Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL")!;
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYCLAW_BASE_URL")))
                cfg.Provider.BaseUrl = Environment.GetEnvironmentVariable("MYCLAW_BASE_URL")!;
        }

        // Channel settings
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYCLAW_TELEGRAM_TOKEN")))
            cfg.Channels.Telegram.Token = Environment.GetEnvironmentVariable("MYCLAW_TELEGRAM_TOKEN")!;
        
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYCLAW_FEISHU_APP_ID")))
            cfg.Channels.Feishu.AppId = Environment.GetEnvironmentVariable("MYCLAW_FEISHU_APP_ID")!;
        
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYCLAW_FEISHU_APP_SECRET")))
            cfg.Channels.Feishu.AppSecret = Environment.GetEnvironmentVariable("MYCLAW_FEISHU_APP_SECRET")!;
        
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYCLAW_WECOM_TOKEN")))
            cfg.Channels.WeCom.Token = Environment.GetEnvironmentVariable("MYCLAW_WECOM_TOKEN")!;
        
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYCLAW_WECOM_ENCODING_AES_KEY")))
            cfg.Channels.WeCom.EncodingAESKey = Environment.GetEnvironmentVariable("MYCLAW_WECOM_ENCODING_AES_KEY")!;
        
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYCLAW_WECOM_RECEIVE_ID")))
            cfg.Channels.WeCom.ReceiveId = Environment.GetEnvironmentVariable("MYCLAW_WECOM_RECEIVE_ID")!;
    }

    /// <summary>
    /// 合并配置文件到默认配置
    /// </summary>
    private static void MergeConfig(MyClawConfiguration target, MyClawConfiguration source)
    {
        // Agent
        if (!string.IsNullOrEmpty(source.Agent.Workspace)) target.Agent.Workspace = source.Agent.Workspace;
        if (!string.IsNullOrEmpty(source.Agent.Model)) target.Agent.Model = source.Agent.Model;
        if (source.Agent.MaxTokens > 0) target.Agent.MaxTokens = source.Agent.MaxTokens;
        if (source.Agent.Temperature >= 0) target.Agent.Temperature = source.Agent.Temperature;
        if (source.Agent.MaxToolIterations > 0) target.Agent.MaxToolIterations = source.Agent.MaxToolIterations;

        // Provider
        if (!string.IsNullOrEmpty(source.Provider.Type)) target.Provider.Type = source.Provider.Type;
        if (!string.IsNullOrEmpty(source.Provider.ApiKey)) target.Provider.ApiKey = source.Provider.ApiKey;
        if (!string.IsNullOrEmpty(source.Provider.BaseUrl)) target.Provider.BaseUrl = source.Provider.BaseUrl;

        // Channels
        target.Channels.Telegram = source.Channels.Telegram ?? target.Channels.Telegram;
        target.Channels.Feishu = source.Channels.Feishu ?? target.Channels.Feishu;
        target.Channels.WeCom = source.Channels.WeCom ?? target.Channels.WeCom;
        target.Channels.WhatsApp = source.Channels.WhatsApp ?? target.Channels.WhatsApp;
        target.Channels.WebUI = source.Channels.WebUI ?? target.Channels.WebUI;

        // Tools
        if (!string.IsNullOrEmpty(source.Tools.BraveApiKey)) target.Tools.BraveApiKey = source.Tools.BraveApiKey;
        if (source.Tools.ExecTimeout > 0) target.Tools.ExecTimeout = source.Tools.ExecTimeout;
        target.Tools.RestrictToWorkspace = source.Tools.RestrictToWorkspace;

        // Skills
        target.Skills.Enabled = source.Skills.Enabled;
        if (!string.IsNullOrEmpty(source.Skills.Dir)) target.Skills.Dir = source.Skills.Dir;

        // Gateway
        if (!string.IsNullOrEmpty(source.Gateway.Host)) target.Gateway.Host = source.Gateway.Host;
        if (source.Gateway.Port > 0) target.Gateway.Port = source.Gateway.Port;

        // AutoCompact
        target.AutoCompact.Enabled = source.AutoCompact.Enabled;
        if (source.AutoCompact.Threshold > 0) target.AutoCompact.Threshold = source.AutoCompact.Threshold;
        if (source.AutoCompact.PreserveCount > 0) target.AutoCompact.PreserveCount = source.AutoCompact.PreserveCount;

        // TokenTracking
        target.TokenTracking.Enabled = source.TokenTracking.Enabled;

        // MCP
        if (source.MCP.Servers?.Count > 0) target.MCP.Servers = source.MCP.Servers;
    }
}
