using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyClaw.Core.Configuration;

/// <summary>
/// MyClaw 主配置类
/// </summary>
public class MyClawConfiguration
{
    public const string DefaultModel = "gpt-4o";
    public const int DefaultMaxTokens = 8192;
    public const double DefaultTemperature = 0.7;
    public const int DefaultMaxToolIterations = 20;
    public const int DefaultExecTimeout = 60;
    public const string DefaultHost = "0.0.0.0";
    public const int DefaultPort = 18790;
    public const int DefaultBufSize = 100;

    [JsonPropertyName("agent")]
    public AgentConfig Agent { get; set; } = new();

    [JsonPropertyName("channels")]
    public ChannelsConfig Channels { get; set; } = new();

    [JsonPropertyName("provider")]
    public ProviderConfig Provider { get; set; } = new();

    [JsonPropertyName("tools")]
    public ToolsConfig Tools { get; set; } = new();

    [JsonPropertyName("skills")]
    public SkillsConfig Skills { get; set; } = new();

    [JsonPropertyName("hooks")]
    public HooksConfig Hooks { get; set; } = new();

    [JsonPropertyName("mcp")]
    public MCPConfig MCP { get; set; } = new();

    [JsonPropertyName("autoCompact")]
    public AutoCompactConfig AutoCompact { get; set; } = new();

    [JsonPropertyName("tokenTracking")]
    public TokenTrackingConfig TokenTracking { get; set; } = new();

    [JsonPropertyName("gateway")]
    public GatewayConfig Gateway { get; set; } = new();

    public static MyClawConfiguration Default()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return new MyClawConfiguration
        {
            Agent = new AgentConfig
            {
                Workspace = Path.Combine(home, ".myclaw.net"),
                Model = DefaultModel,
                MaxTokens = DefaultMaxTokens,
                Temperature = DefaultTemperature,
                MaxToolIterations = DefaultMaxToolIterations,
                Verbose = false
            },
            Tools = new ToolsConfig
            {
                ExecTimeout = DefaultExecTimeout,
                RestrictToWorkspace = true
            },
            Skills = new SkillsConfig
            {
                Enabled = true
            },
            AutoCompact = new AutoCompactConfig
            {
                Enabled = true,
                Threshold = 0.8,
                PreserveCount = 5
            },
            Gateway = new GatewayConfig
            {
                Host = DefaultHost,
                Port = DefaultPort
            },
            Channels = new ChannelsConfig
            {
                Uno = new UnoUIConfig
                {
                    Enabled = true,
                    Mode = "desktop"
                }
            }
        };
    }
}

public class AgentConfig
{
    [JsonPropertyName("workspace")]
    public string Workspace { get; set; } = "";

    [JsonPropertyName("model")]
    public string Model { get; set; } = MyClawConfiguration.DefaultModel;

    [JsonPropertyName("maxTokens")]
    public int MaxTokens { get; set; } = MyClawConfiguration.DefaultMaxTokens;

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = MyClawConfiguration.DefaultTemperature;

    [JsonPropertyName("maxToolIterations")]
    public int MaxToolIterations { get; set; } = MyClawConfiguration.DefaultMaxToolIterations;

    [JsonPropertyName("verbose")]
    public bool Verbose { get; set; } = false;
}

public class ProviderConfig
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "openai";

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = "";

    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = "";
}

public class ChannelsConfig
{
    [JsonPropertyName("telegram")]
    public TelegramConfig Telegram { get; set; } = new();

    [JsonPropertyName("feishu")]
    public FeishuConfig Feishu { get; set; } = new();

    [JsonPropertyName("wecom")]
    public WeComConfig WeCom { get; set; } = new();

    [JsonPropertyName("whatsapp")]
    public WhatsAppConfig WhatsApp { get; set; } = new();

    [JsonPropertyName("webui")]
    public WebUIConfig WebUI { get; set; } = new();

    [JsonPropertyName("uno")]
    public UnoUIConfig Uno { get; set; } = new();
}

public class TelegramConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    [JsonPropertyName("allowFrom")]
    public List<string> AllowFrom { get; set; } = new();

    [JsonPropertyName("proxy")]
    public string Proxy { get; set; } = "";
}

public class FeishuConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("appId")]
    public string AppId { get; set; } = "";

    [JsonPropertyName("appSecret")]
    public string AppSecret { get; set; } = "";

    [JsonPropertyName("verificationToken")]
    public string VerificationToken { get; set; } = "";

    [JsonPropertyName("encryptKey")]
    public string EncryptKey { get; set; } = "";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 9876;

    [JsonPropertyName("allowFrom")]
    public List<string> AllowFrom { get; set; } = new();
}

public class WeComConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    [JsonPropertyName("encodingAESKey")]
    public string EncodingAESKey { get; set; } = "";

    [JsonPropertyName("receiveId")]
    public string ReceiveId { get; set; } = "";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 9886;

    [JsonPropertyName("allowFrom")]
    public List<string> AllowFrom { get; set; } = new();
}

public class WhatsAppConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("jid")]
    public string JID { get; set; } = "";

    [JsonPropertyName("storePath")]
    public string StorePath { get; set; } = "";

    [JsonPropertyName("allowFrom")]
    public List<string> AllowFrom { get; set; } = new();
}

public class WebUIConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("host")]
    public string Host { get; set; } = "127.0.0.1";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 8080;

    [JsonPropertyName("allowFrom")]
    public List<string> AllowFrom { get; set; } = new();
}

public class UnoUIConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "desktop";

    [JsonPropertyName("allowFrom")]
    public List<string> AllowFrom { get; set; } = new();
}

public class ToolsConfig
{
    [JsonPropertyName("braveApiKey")]
    public string BraveApiKey { get; set; } = "";

    [JsonPropertyName("execTimeout")]
    public int ExecTimeout { get; set; }

    [JsonPropertyName("restrictToWorkspace")]
    public bool RestrictToWorkspace { get; set; }
}

public class GatewayConfig
{
    [JsonPropertyName("host")]
    public string Host { get; set; } = MyClawConfiguration.DefaultHost;

    [JsonPropertyName("port")]
    public int Port { get; set; } = MyClawConfiguration.DefaultPort;
}

public class SkillsConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("dir")]
    public string Dir { get; set; } = "";
}

public class HooksConfig
{
    [JsonPropertyName("preToolUse")]
    public List<HookEntry> PreToolUse { get; set; } = new();

    [JsonPropertyName("postToolUse")]
    public List<HookEntry> PostToolUse { get; set; } = new();

    [JsonPropertyName("stop")]
    public List<HookEntry> Stop { get; set; } = new();
}

public class HookEntry
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = "";

    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = "";

    [JsonPropertyName("timeout")]
    public int Timeout { get; set; }
}

public class MCPConfig
{
    [JsonPropertyName("servers")]
    public List<string> Servers { get; set; } = new();
}

public class AutoCompactConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("threshold")]
    public double Threshold { get; set; } = 0.8;

    [JsonPropertyName("preserveCount")]
    public int PreserveCount { get; set; } = 5;
}

public class TokenTrackingConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}
