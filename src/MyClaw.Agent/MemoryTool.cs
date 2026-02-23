using AgentScope.Core.Tool;
using MyClaw.Memory;

namespace MyClaw.Agent;

public class MemoryTool : ToolBase
{
    private readonly MemoryStore _memoryStore;

    public MemoryTool(MemoryStore memoryStore) : base("memory", "记录重要信息到长期记忆，如用户姓名、偏好、约定等")
    {
        _memoryStore = memoryStore;
    }

    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["content"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "要记录的内容，简洁明了，不超过50字"
                },
                ["category"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "分类：个人信息、偏好、约定、其他",
                    ["enum"] = new List<string> { "个人信息", "偏好", "约定", "其他" }
                }
            },
            ["required"] = new List<string> { "content", "category" }
        };
    }

    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var content = parameters.TryGetValue("content", out var c) ? c?.ToString() : null;
        var category = parameters.TryGetValue("category", out var cat) ? cat?.ToString() : "其他";

        if (string.IsNullOrEmpty(content))
        {
            return Task.FromResult(new ToolResult { Error = "内容不能为空" });
        }

        var timestamp = DateTime.Now.ToString("HH:mm");
        var entry = $"- [{timestamp}] [{category}] {content}";
        
        _memoryStore.AppendToday(entry);
        Console.WriteLine($"[Memory] 已记录: {content}");

        return Task.FromResult(ToolResult.Ok($"已记录到记忆: {content}"));
    }
}
