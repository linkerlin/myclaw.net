namespace MyClaw.Core.Dna;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 端粒守卫 - DNA 写入前的结构校验机制
/// 防止 AI 自我修改时产生基因癌变（结构破坏）
/// 
/// 设计哲学：
/// - 在写入磁盘的最后一刻拦截
/// - 检查必需的 Markdown 标题结构
/// - 拒绝缺少结构的写入请求
/// - 返回清晰的错误信息引导自我修复
/// </summary>
public class TelomereGuard
{
    /// <summary>
    /// 端粒图谱 - 每个核心 DNA 文件的不可丢失结构标记
    /// 这些标记是文件结构的"端粒"，丢失意味着基因链断裂
    /// </summary>
    private static readonly Dictionary<string, string[]> TelomereMap = new()
    {
        ["SOUL.md"] = new[] { "##" },
        ["IDENTITY.md"] = new[] { "# ", "##" },
        ["USER.md"] = new[] { "##" },
        ["MEMORY.md"] = new[] { "##" },
        ["TOOLS.md"] = new[] { "##" },
        ["AGENTS.md"] = new[] { "#", "##" },
        ["NOCICEPTION.md"] = new[] { "###" },
        ["CONCEPTS.md"] = new[] { "##" },
        ["REFLECTION.md"] = new[] { "##" },
        ["HORIZONS.md"] = new[] { "##" },
        ["HEARTBEAT.md"] = new[] { "##" },
        ["BOOTSTRAP.md"] = new[] { "#", "##" },
        ["SUBAGENT.md"] = new[] { "##" },
    };

    /// <summary>
    /// 校验 DNA 文件内容结构完整性
    /// </summary>
    /// <param name="filename">DNA 文件名（不含路径）</param>
    /// <param name="content">文件内容</param>
    /// <exception cref="DnaMutationException">结构校验失败时抛出</exception>
    public static void Check(string filename, string content)
    {
        // 非核心文件不校验
        if (!TelomereMap.ContainsKey(filename))
            return;

        var required = TelomereMap[filename];
        var missing = required.Where(h => !content.Contains(h)).ToList();

        if (missing.Any())
        {
            var missingMarkers = string.Join(", ", missing.Select(m => $"\"{m}\""));
            throw new DnaMutationException(
                $"🧬 [基因链断裂] Telomere Guard rejected mutation of `{filename}`.\n" +
                $"Missing required structural markers: {missingMarkers}.\n" +
                $"请确保文件包含必要的 Markdown 标题结构。");
        }
    }

    /// <summary>
    /// 获取指定 DNA 文件的必需结构标记
    /// </summary>
    /// <param name="filename">DNA 文件名</param>
    /// <returns>结构标记列表，如果文件未知则返回空列表</returns>
    public static IEnumerable<string> GetRequiredMarkers(string filename)
    {
        return TelomereMap.TryGetValue(filename, out var markers) 
            ? markers 
            : Enumerable.Empty<string>();
    }

    /// <summary>
    /// 检查文件是否需要端粒守卫校验
    /// </summary>
    /// <param name="filename">文件名</param>
    /// <returns>是否需要校验</returns>
    public static bool ShouldCheck(string filename)
    {
        return TelomereMap.ContainsKey(filename);
    }
}

/// <summary>
/// DNA 突变异常
/// 当端粒守卫校验失败或 DNA 写入被拒绝时抛出
/// </summary>
public class DnaMutationException : Exception
{
    /// <summary>
    /// 创建 DNA 突变异常
    /// </summary>
    /// <param name="message">异常消息</param>
    public DnaMutationException(string message) : base(message) { }

    /// <summary>
    /// 创建 DNA 突变异常
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public DnaMutationException(string message, Exception? innerException) 
        : base(message, innerException) { }
}
