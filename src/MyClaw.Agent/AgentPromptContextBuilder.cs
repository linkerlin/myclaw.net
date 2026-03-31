using System.Globalization;
using System.Text;
using MyClaw.Core.Ace;
using MyClaw.Core.Affect;
using MyClaw.Core.Analytics;
using MyClaw.Core.Configuration;
using MyClaw.Core.Epigenetics;
using MyClaw.Core.Nociception;
using MyClaw.Core.Perception;
using MyClaw.Core.Workspace;
using MyClaw.Memory;

namespace MyClaw.Agent;

/// <summary>
/// Agent 提示词上下文装配器
/// 统一组装 DNA、工作区、记忆与运行时状态，避免多处手工拼接分叉。
/// </summary>
public class AgentPromptContextBuilder
{
    private readonly MyClawConfiguration _config;
    private readonly MemoryStore _memoryStore;
    private readonly WorkspaceContextService _workspaceContextService;
    private readonly AffectManager _affectManager;
    private readonly NociceptionManager _nociceptionManager;
    private readonly MethylationManager _methylationManager;
    private readonly AnalyticsService _analyticsService;
    private readonly ContinuationDetector _continuationDetector;
    private readonly PerceptionContextService _perceptionContextService;
    private readonly Func<DateTime> _nowProvider;

    public AgentPromptContextBuilder(
        MyClawConfiguration config,
        MemoryStore memoryStore,
        WorkspaceContextService? workspaceContextService = null,
        AffectManager? affectManager = null,
        NociceptionManager? nociceptionManager = null,
        MethylationManager? methylationManager = null,
        AnalyticsService? analyticsService = null,
        ContinuationDetector? continuationDetector = null,
        PerceptionContextService? perceptionContextService = null,
        Func<DateTime>? nowProvider = null)
    {
        _config = config;
        _memoryStore = memoryStore;
        _workspaceContextService = workspaceContextService ?? new WorkspaceContextService(WorkspacePath);
        _affectManager = affectManager ?? new AffectManager();
        _nociceptionManager = nociceptionManager ?? new NociceptionManager(_affectManager);
        _methylationManager = methylationManager ?? new MethylationManager();
        _analyticsService = analyticsService ?? new AnalyticsService(WorkspacePath);
        _continuationDetector = continuationDetector ?? new ContinuationDetector();
        _perceptionContextService = perceptionContextService ?? new PerceptionContextService();
        _nowProvider = nowProvider ?? (() => DateTime.Now);
    }

    public string BuildSystemPrompt(string? providerName = null)
    {
        var sections = new List<ContextSection>
        {
            CreateInstructionSection(providerName),
            CreateAffectSection(),
            CreateTimeModeSection()
        };

        AddWorkspaceFileSection(sections, "AGENTS.md", "agents", 9);
        AddWorkspaceFileSection(sections, "IDENTITY.md", "identity", 9);
        AddWorkspaceFileSection(sections, "SOUL.md", "soul", 8);
        AddWorkspaceFileSection(sections, "USER.md", "user", 8);
        AddWorkspaceFileSection(sections, "TOOLS.md", "tools", 6);
        AddWorkspaceFileSection(sections, "HEARTBEAT.md", "heartbeat", 7, "## 心跳任务");

        var workspaceSection = TryCreateWorkspaceSection();
        if (workspaceSection != null)
        {
            sections.Add(workspaceSection);
        }

        var perceptionSection = TryCreatePerceptionSection();
        if (perceptionSection != null)
        {
            sections.Add(perceptionSection);
        }

        var continuationSection = CreateContinuationSection();
        if (continuationSection != null)
        {
            sections.Add(continuationSection);
        }

        var nociceptionSection = CreateNociceptionSection();
        if (nociceptionSection != null)
        {
            sections.Add(nociceptionSection);
        }

        var methylationSection = CreateMethylationSection();
        if (methylationSection != null)
        {
            sections.Add(methylationSection);
        }

        var memorySection = CreateMemorySection();
        if (memorySection != null)
        {
            sections.Add(memorySection);
        }

        var compiler = new ContextCompiler(GetTokenBudget());
        var compiled = compiler.Compile(sections, includeAffect: false);
        return compiled.Output.Trim();
    }

    private string WorkspacePath => string.IsNullOrWhiteSpace(_config.Agent.Workspace)
        ? Directory.GetCurrentDirectory()
        : _config.Agent.Workspace;

    private int GetTokenBudget()
    {
        return Math.Max(2048, _config.Agent.MaxTokens);
    }

    private void AddWorkspaceFileSection(List<ContextSection> sections, string fileName, string name, int priority, string? fallbackHeading = null)
    {
        var section = CreateWorkspaceFileSection(fileName, name, priority, fallbackHeading);
        if (section != null)
        {
            sections.Add(section);
        }
    }

    private ContextSection? CreateWorkspaceFileSection(string fileName, string name, int priority, string? fallbackHeading = null)
    {
        var path = Path.Combine(WorkspacePath, fileName);
        if (!File.Exists(path))
        {
            return null;
        }

        var content = File.ReadAllText(path).Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(fallbackHeading) && !content.StartsWith('#'))
        {
            content = $"{fallbackHeading}\n{content}";
        }

        return new ContextSection
        {
            Name = name,
            Content = WrapSection(content),
            Priority = priority
        };
    }

    private ContextSection CreateInstructionSection(string? providerName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## SYSTEM");
        sb.AppendLine("你是 MyClaw，一个个人 AI 助手。");

        if (!string.IsNullOrWhiteSpace(providerName))
        {
            sb.AppendLine($"当前模型提供者: {providerName}");
        }

        sb.AppendLine("优先遵循高优先级 DNA 文档中的约束，并结合工作区事实、记忆、时间模式、延续状态和当前内在状态作答。");
        sb.AppendLine("如果不同上下文存在冲突，以更高优先级段落和工作区中的最新事实为准。");
        sb.AppendLine("你可以使用当前会话中已注册的工具和 Skills 完成任务。");
        sb.AppendLine("请用中文或用户使用的语言回复。");

        return new ContextSection
        {
            Name = "system",
            Content = WrapSection(sb.ToString()),
            Priority = 10
        };
    }

    private ContextSection CreateAffectSection()
    {
        return new ContextSection
        {
            Name = "affect",
            Content = WrapSection(_affectManager.FormatForContext()),
            Priority = 8
        };
    }

    private ContextSection CreateTimeModeSection()
    {
        var now = _nowProvider();
        var mode = TimeModeManager.GetCurrentMode(now);
        var config = TimeModeManager.GetConfig(mode);

        var guidance = config.MinimalMode
            ? "保持极简回复，优先直接执行。"
            : config.SuggestReflective
                ? "完成任务后可附带简短反思。"
                : config.ShowBriefing
                    ? "对宽泛请求可先给简短简报，再进入执行。"
                    : "优先直接进入任务。";

        var sb = new StringBuilder();
        sb.AppendLine("## TIME MODE");
        sb.AppendLine($"- Current: {config.Emoji} {config.Label}");
        sb.AppendLine($"- Local time: {now:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"- Show briefing: {(config.ShowBriefing ? "yes" : "no")}");
        sb.AppendLine($"- Suggest reflective: {(config.SuggestReflective ? "yes" : "no")}");
        sb.AppendLine($"- Minimal mode: {(config.MinimalMode ? "yes" : "no")}");
        sb.AppendLine($"- Guidance: {guidance}");

        return new ContextSection
        {
            Name = "time_mode",
            Content = WrapSection(sb.ToString()),
            Priority = 7
        };
    }

    private ContextSection? TryCreateWorkspaceSection()
    {
        try
        {
            return _workspaceContextService.GetContextSectionAsync().GetAwaiter().GetResult();
        }
        catch
        {
            try
            {
                return _workspaceContextService.GetQuickContextSection();
            }
            catch
            {
                return null;
            }
        }
    }

    private ContextSection? TryCreatePerceptionSection()
    {
        try
        {
            return _perceptionContextService.GetContextSectionAsync().GetAwaiter().GetResult();
        }
        catch
        {
            try
            {
                return _perceptionContextService.GetQuickContextSection();
            }
            catch
            {
                return null;
            }
        }
    }

    private ContextSection? CreateContinuationSection()
    {
        var lastActivity = TryGetLastActivity();
        var dailyLog = _memoryStore.ReadToday();
        var result = _continuationDetector.Detect(dailyLog, lastActivity);

        if (!result.IsReturn)
        {
            return null;
        }

        var sb = new StringBuilder();
        sb.AppendLine("## CONTINUATION");
        sb.AppendLine($"- Returning after {result.HoursSinceLastActivity:F1} hours");

        if (!string.IsNullOrWhiteSpace(result.LastTopic))
        {
            sb.AppendLine($"- Last topic: {result.LastTopic}");
        }

        foreach (var decision in result.RecentDecisions.Take(3))
        {
            sb.AppendLine($"- Recent decision: {decision}");
        }

        foreach (var question in result.OpenQuestions.Take(3))
        {
            sb.AppendLine($"- Open question: {question}");
        }

        return new ContextSection
        {
            Name = "continuation",
            Content = WrapSection(sb.ToString()),
            Priority = 8
        };
    }

    private ContextSection? CreateNociceptionSection()
    {
        if (_nociceptionManager.Count == 0)
        {
            return null;
        }

        return new ContextSection
        {
            Name = "nociception",
            Content = WrapSection(_nociceptionManager.FormatForContext()),
            Priority = 8
        };
    }

    private ContextSection? CreateMethylationSection()
    {
        if (_methylationManager.Count == 0)
        {
            return null;
        }

        return new ContextSection
        {
            Name = "methylation",
            Content = WrapSection(_methylationManager.FormatForContext()),
            Priority = 7
        };
    }

    private ContextSection? CreateMemorySection()
    {
        var memoryContext = MemoryContextProvider.GetMemoryContext(_memoryStore).Trim();
        if (string.IsNullOrWhiteSpace(memoryContext))
        {
            return null;
        }

        return new ContextSection
        {
            Name = "memory",
            Content = WrapSection($"## 记忆上下文\n{memoryContext}"),
            Priority = 8
        };
    }

    private DateTime? TryGetLastActivity()
    {
        var lastActivity = _analyticsService.GetAnalytics().LastActivity;
        if (string.IsNullOrWhiteSpace(lastActivity))
        {
            return null;
        }

        if (!DateTime.TryParse(lastActivity, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return null;
        }

        return parsed.Kind == DateTimeKind.Utc ? parsed.ToLocalTime() : parsed;
    }

    private static string WrapSection(string content)
    {
        return content.TrimEnd() + "\n\n";
    }
}