namespace MyClaw.Core.Entities;

/// <summary>
/// 实体类型枚举
/// </summary>
public enum EntityType
{
    Person,     // 人物
    Project,    // 项目
    Tool,       // 工具
    Concept,    // 概念
    Place,      // 地点
    Other       // 其他
}

/// <summary>
/// 实体定义 - 知识图谱中的节点
/// </summary>
public class Entity
{
    /// <summary>
    /// 实体名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 实体类型
    /// </summary>
    public EntityType Type { get; set; } = EntityType.Other;

    /// <summary>
    /// 实体属性
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = new();

    /// <summary>
    /// 实体关系
    /// </summary>
    public List<string> Relations { get; set; } = new();

    /// <summary>
    /// 首次提及时间
    /// </summary>
    public string FirstMentioned { get; set; } = string.Empty;

    /// <summary>
    /// 最后提及时间
    /// </summary>
    public string LastMentioned { get; set; } = string.Empty;

    /// <summary>
    /// 提及次数
    /// </summary>
    public int MentionCount { get; set; } = 1;

    /// <summary>
    /// 活力值，用于决定实体是否应自然保留
    /// </summary>
    public int Vitality { get; set; } = 100;

    /// <summary>
    /// 活力上次更新时间
    /// </summary>
    public string VitalityUpdatedAt { get; set; } = string.Empty;
}
