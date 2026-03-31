using MyClaw.Core.Curiosity;
using MyClaw.Core.Mycelium;

namespace MyClaw.Gateway;

/// <summary>
/// 格式化 heartbeat 附加上下文，默认输出低噪音摘要，详细内容仅在 verbose 模式下展开。
/// </summary>
public static class HeartbeatSupplementalContextFormatter
{
    public static string? FormatMycelium(AbsorptionResult absorption, bool verbose = false)
    {
        if (!absorption.HasAbsorbed)
        {
            return null;
        }

        var lines = new List<string>
        {
            "## MYCELIUM",
            $"- Absorbed {absorption.AbsorbedSpores.Count} spores",
            $"- Nociception memories: {absorption.NociceptionCount}",
            $"- Tool antibodies: {absorption.ToolsCount}"
        };

        if (verbose)
        {
            var sources = absorption.AbsorbedSpores
                .Select(spore => spore.SenderId)
                .Where(senderId => !string.IsNullOrWhiteSpace(senderId))
                .Distinct(StringComparer.Ordinal)
                .Take(3)
                .ToList();

            if (sources.Count > 0)
            {
                lines.Add($"- Sources: {string.Join(", ", sources)}");
            }
        }

        return string.Join("\n", lines);
    }

    public static string? FormatBoredom(BoredomResult boredom, string workspacePath, bool verbose = false)
    {
        if (!boredom.Success)
        {
            return null;
        }

        var file = boredom.ScannedFile != null
            ? Path.GetRelativePath(workspacePath, boredom.ScannedFile)
            : "unknown";

        var lines = new List<string>
        {
            "## BOREDOM",
            $"- Scanned file: {file}"
        };

        if (verbose)
        {
            lines.Add("- Found todos:");
            lines.AddRange(boredom.Todos.Select(todo => $"- {todo}"));
        }
        else
        {
            lines.Add($"- Captured {boredom.Todos.Count} todo item(s); details archived to HORIZONS.md");

            if (boredom.Todos.Count > 0)
            {
                lines.Add($"- Lead item: {boredom.Todos[0]}");
            }
        }

        return string.Join("\n", lines);
    }
}