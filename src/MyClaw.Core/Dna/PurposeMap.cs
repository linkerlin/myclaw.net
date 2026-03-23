namespace MyClaw.Core.Dna;

/// <summary>
/// DNA 文件职责映射表 - 用于自检镜像机制
/// 每次 DNA 写入成功后返回文件职责声明，触发 AI 自我纠正
/// </summary>
public static class PurposeMap
{
    /// <summary>
    /// DNA 文件职责映射表
    /// 格式：文件名 -> 职责描述 + 绝不写入内容
    /// </summary>
    public static readonly Dictionary<string, string> Map = new()
    {
        ["SOUL.md"] = "[灵魂染色体] 性格三观、语言风格。绝不写入：用户习惯、项目配置。",
        ["IDENTITY.md"] = "[身份染色体] 核心身份、能力边界。绝不写入：临时状态、具体任务。",
        ["USER.md"] = "[共生染色体] 用户画像、偏好习惯。绝不写入：AI 自身性格。",
        ["MEMORY.md"] = "[海马体] 长期事实、项目信息。绝不写入：性格笔记、临时数据。",
        ["TOOLS.md"] = "[能力染色体] 工具使用经验。绝不写入：用户偏好、客观事实。",
        ["AGENTS.md"] = "[基因组控制中心] 工作流规范。绝不写入：具体任务内容。",
        ["NOCICEPTION.md"] = "[痛觉中枢] 执行失败的痛楚与教训。绝不写入：成功经验。",
        ["CONCEPTS.md"] = "[知识染色体] 概念定义。绝不写入：具体事件。",
        ["REFLECTION.md"] = "[反思维度] 错误反思。绝不写入：成功总结。",
        ["HORIZONS.md"] = "[欲望眼界] 里程碑、成就。绝不写入：日常琐事。",
        ["HEARTBEAT.md"] = "[脉搏系统] 心跳任务记录。绝不写入：用户指令。",
        ["BOOTSTRAP.md"] = "[胚胎发育] 初始化配置。绝不写入：运行时数据。",
        ["SUBAGENT.md"] = "[子代理] 子代理配置。绝不写入：主代理逻辑。",
    };

    /// <summary>
    /// 获取 DNA 文件的职责描述
    /// </summary>
    /// <param name="filename">DNA 文件名</param>
    /// <returns>职责描述，如果文件未知则返回默认描述</returns>
    public static string GetPurpose(string filename)
    {
        return Map.TryGetValue(filename, out var purpose) 
            ? purpose 
            : $"[未知 DNA 文件] 请确认文件用途。";
    }

    /// <summary>
    /// 检查文件是否属于核心 DNA 文件
    /// </summary>
    /// <param name="filename">文件名</param>
    /// <returns>是否为核心 DNA 文件</returns>
    public static bool IsCoreDnaFile(string filename)
    {
        return Map.ContainsKey(filename);
    }

    /// <summary>
    /// 获取所有核心 DNA 文件列表
    /// </summary>
    /// <returns>DNA 文件名列表</returns>
    public static IEnumerable<string> GetCoreDnaFiles()
    {
        return Map.Keys;
    }
}
