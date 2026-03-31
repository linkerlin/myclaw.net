using System.Text;

namespace MyClaw.Skills;

public static class SkillDocumentBuilder
{
    public static string BuildMarkdown(
        string name,
        string description,
        string content,
        IEnumerable<string>? keywords = null,
        IEnumerable<string>? hooks = null,
        IEnumerable<string>? filePatterns = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var keywordList = SanitizeList(keywords);
        var hookList = SanitizeList(hooks);
        var filePatternList = SanitizeList(filePatterns);

        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"name: {FormatScalarValue(name)}");
        sb.AppendLine($"description: {FormatScalarValue(description)}");

        if (keywordList.Count > 0)
        {
            sb.AppendLine("keywords:");
            foreach (var keyword in keywordList)
            {
                sb.AppendLine($"  - {FormatScalarValue(keyword)}");
            }
        }

        if (hookList.Count > 0)
        {
            sb.AppendLine("hooks:");
            foreach (var hook in hookList)
            {
                sb.AppendLine($"  - {FormatScalarValue(hook)}");
            }
        }

        if (filePatternList.Count > 0)
        {
            sb.AppendLine("filePatterns:");
            foreach (var pattern in filePatternList)
            {
                sb.AppendLine($"  - {FormatScalarValue(pattern)}");
            }
        }

        if (metadata != null)
        {
            foreach (var pair in metadata.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                {
                    continue;
                }

                sb.AppendLine($"{pair.Key}: {FormatScalarValue(pair.Value)}");
            }
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(content.Trim());
        return sb.ToString();
    }

    private static List<string> SanitizeList(IEnumerable<string>? values)
    {
        if (values == null)
        {
            return new List<string>();
        }

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string FormatScalarValue(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return "\"\"";
        }

        var requiresQuotes = trimmed.Any(char.IsWhiteSpace)
            || trimmed.Contains(':')
            || trimmed.Contains('#')
            || trimmed.Contains('"')
            || trimmed.StartsWith("-", StringComparison.Ordinal)
            || trimmed.StartsWith("{", StringComparison.Ordinal)
            || trimmed.StartsWith("[", StringComparison.Ordinal);

        if (!requiresQuotes)
        {
            return trimmed;
        }

        var escaped = trimmed
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
        return $"\"{escaped}\"";
    }
}