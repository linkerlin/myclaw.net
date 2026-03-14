namespace MyClaw.Core.Ribosome;

/// <summary>
/// 本能定义 - 从 RIBOSOME.json 加载的工具定义
/// Instinct Definition - Tool definition loaded from RIBOSOME.json
/// </summary>
public class InstinctDefinition
{
    /// <summary>
    /// 处理器名称 - 对应代码中的处理方法
    /// </summary>
    public string Handler { get; set; } = string.Empty;

    /// <summary>
    /// 工具描述 - 提供给 AI 的使用说明
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 输入模式 - JSON Schema 格式
    /// </summary>
    public InstinctInputSchema? InputSchema { get; set; }

    /// <summary>
    /// 是否为核心本能（不可删除）
    /// </summary>
    public bool IsCore { get; set; } = true;

    /// <summary>
    /// 信号检测规则 - 触发条件
    /// </summary>
    public List<SignalRule>? SignalRules { get; set; }

    /// <summary>
    /// 目标文件映射
    /// </summary>
    public string? DefaultTargetFile { get; set; }
}

/// <summary>
/// 输入模式定义
/// </summary>
public class InstinctInputSchema
{
    /// <summary>
    /// 类型，通常是 "object"
    /// </summary>
    public string Type { get; set; } = "object";

    /// <summary>
    /// 属性定义
    /// </summary>
    public Dictionary<string, SchemaProperty>? Properties { get; set; }

    /// <summary>
    /// 必填属性列表
    /// </summary>
    public List<string>? Required { get; set; }
}

/// <summary>
/// 模式属性定义
/// </summary>
public class SchemaProperty
{
    /// <summary>
    /// 属性类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 属性描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 枚举值列表（用于 enum 类型）
    /// </summary>
    public List<string>? Enum { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public object? Default { get; set; }
}

/// <summary>
/// 信号检测规则 - 定义何时应该调用此工具
/// </summary>
public class SignalRule
{
    /// <summary>
    /// 规则名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 触发关键词/模式
    /// </summary>
    public List<string> Patterns { get; set; } = new();

    /// <summary>
    /// 目标文件
    /// </summary>
    public string? TargetFile { get; set; }

    /// <summary>
    /// 推理逻辑描述
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// 置信度权重
    /// </summary>
    public double ConfidenceWeight { get; set; } = 1.0;
}

/// <summary>
/// RIBOSOME 配置文件结构
/// </summary>
public class RibosomeConfig
{
    /// <summary>
    /// 配置类型
    /// </summary>
    public string Type { get; set; } = "ribosome";

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 本能工具定义
    /// </summary>
    public Dictionary<string, InstinctDefinition> Instincts { get; set; } = new();
}
