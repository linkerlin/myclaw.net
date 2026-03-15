namespace MyClaw.Heartbeat;

/// <summary>
/// 单次心跳任务（来自 HEARTBEAT.md 内容）
/// </summary>
public sealed class HeartbeatTask
{
    /// <summary>HEARTBEAT.md 的原始清单内容</summary>
    public string Content { get; set; } = "";

    /// <summary>组装好的完整提示词（含指令与 HEARTBEAT_OK 说明）</summary>
    public string Prompt { get; set; } = "";
}
