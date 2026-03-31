using System.Text.RegularExpressions;

namespace MyClaw.Skills;

/// <summary>
/// Skill 加载器 - 从 SKILL.md 文件加载技能
/// </summary>
public static class SkillLoader
{
    private static readonly Regex FrontmatterRegex = new(
        @"^---\s*\n(.*?)\n---\s*\n(.*)$",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// 从目录加载所有 Skills
    /// </summary>
    public static List<Skill> LoadSkills(string skillsDir)
    {
        var skills = new List<Skill>();

        if (!Directory.Exists(skillsDir))
        {
            return skills;
        }

        var directories = Directory.GetDirectories(skillsDir)
            .OrderBy(d => d)
            .ToList();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dir in directories)
        {
            var skillMdPath = Path.Combine(dir, "SKILL.md");
            if (!File.Exists(skillMdPath))
            {
                continue;
            }

            try
            {
                var skill = LoadFromFile(skillMdPath);
                if (skill == null) continue;

                // 检查重复名称
                if (seen.Contains(skill.Name))
                {
                    Console.Error.WriteLine($"[skills] Warning: Duplicate skill name '{skill.Name}' at {skillMdPath}");
                    continue;
                }

                seen.Add(skill.Name);
                skills.Add(skill);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[skills] Warning: Failed to load skill from {skillMdPath}: {ex.Message}");
            }
        }

        return skills;
    }

    /// <summary>
    /// 从文件加载单个 Skill
    /// </summary>
    public static Skill? LoadFromFile(string path)
    {
        var content = File.ReadAllText(path);
        return Parse(content, path);
    }

    /// <summary>
    /// 解析 Skill 内容
    /// </summary>
    public static Skill? Parse(string content, string sourcePath)
    {
        content = content.TrimStart();

        // 移除 BOM
        if (content.Length > 0 && content[0] == '\uFEFF')
        {
            content = content[1..];
        }

        var match = FrontmatterRegex.Match(content);
        if (!match.Success)
        {
            Console.Error.WriteLine($"[skills] Warning: {sourcePath} missing YAML frontmatter");
            return null;
        }

        var frontmatter = match.Groups[1].Value;
        var body = match.Groups[2].Value.Trim();

        // 解析 YAML Frontmatter
        var metadata = ParseYaml(frontmatter);
        if (string.IsNullOrWhiteSpace(metadata.Name))
        {
            Console.Error.WriteLine($"[skills] Warning: {sourcePath} missing name");
            return null;
        }

        return new Skill
        {
            Name = metadata.Name.Trim(),
            Description = metadata.Description.Trim(),
            Keywords = SanitizeKeywords(metadata.Keywords),
            Hooks = ParseHooks(metadata),
            FilePatterns = SanitizeFilePatterns(metadata.FilePatterns),
            Content = body,
            SourcePath = sourcePath,
            Directory = Path.GetDirectoryName(sourcePath) ?? string.Empty
        };
    }

    /// <summary>
    /// 简单 YAML 解析
    /// </summary>
    private static SkillMetadata ParseYaml(string yaml)
    {
        var metadata = new SkillMetadata();
        var lines = yaml.Split('\n');
        List<string>? currentList = null;
        string? currentKey = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // 列表项
            if (trimmed.StartsWith("- ") && currentKey != null)
            {
                var item = TrimQuotes(trimmed[2..].Trim());
                if (currentList == null)
                {
                    currentList = new List<string>();
                }
                currentList.Add(item);
                continue;
            }

            // 保存之前的列表
            if (currentList != null && currentKey != null)
            {
                AssignList(metadata, currentKey, currentList);
                currentList = null;
                currentKey = null;
            }

            // 键值对
            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = trimmed[..colonIndex].Trim().ToLowerInvariant();
                var value = TrimQuotes(trimmed[(colonIndex + 1)..].Trim());

                switch (key)
                {
                    case "name":
                        metadata.Name = value;
                        break;
                    case "description":
                        metadata.Description = value;
                        break;
                    case "keywords":
                    case "hooks":
                    case "filepatterns":
                    case "file_patterns":
                        currentKey = key;
                        if (!string.IsNullOrEmpty(value))
                        {
                            // 内联列表: [a, b, c]
                            if (value.StartsWith('[') && value.EndsWith(']'))
                            {
                                AssignList(metadata, key, ParseInlineList(value));
                                currentKey = null;
                            }
                        }
                        break;
                    case "onboot":
                        metadata.OnBoot = ParseBoolean(value);
                        break;
                    case "onheartbeat":
                        metadata.OnHeartbeat = ParseBoolean(value);
                        break;
                    case "onmemorywrite":
                        metadata.OnMemoryWrite = ParseBoolean(value);
                        break;
                    case "onfilechanged":
                        metadata.OnFileChanged = ParseBoolean(value);
                        break;
                }
            }
        }

        // 处理最后一行是列表的情况
        if (currentList != null && currentKey != null)
        {
            AssignList(metadata, currentKey, currentList);
        }

        return metadata;
    }

    private static void AssignList(SkillMetadata metadata, string key, List<string> values)
    {
        switch (key)
        {
            case "keywords":
                metadata.Keywords = values;
                break;
            case "hooks":
                metadata.Hooks = values;
                break;
            case "filepatterns":
            case "file_patterns":
                metadata.FilePatterns = values;
                break;
        }
    }

    private static List<string> ParseInlineList(string value)
    {
        return value[1..^1]
            .Split(',')
            .Select(item => TrimQuotes(item.Trim()))
            .Where(item => !string.IsNullOrEmpty(item))
            .ToList();
    }

    private static string TrimQuotes(string value)
    {
        if ((value.StartsWith('"') && value.EndsWith('"')) ||
            (value.StartsWith("'") && value.EndsWith("'")))
        {
            return value[1..^1];
        }

        return value;
    }

    private static bool ParseBoolean(string value)
    {
        return value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || value.Equals("1", StringComparison.OrdinalIgnoreCase);
    }

    private static List<SkillHookType> ParseHooks(SkillMetadata metadata)
    {
        var hooks = new HashSet<SkillHookType>();

        foreach (var hook in metadata.Hooks)
        {
            if (TryParseHook(hook, out var parsed))
            {
                hooks.Add(parsed);
            }
        }

        if (metadata.OnBoot) hooks.Add(SkillHookType.Boot);
        if (metadata.OnHeartbeat) hooks.Add(SkillHookType.Heartbeat);
        if (metadata.OnMemoryWrite) hooks.Add(SkillHookType.MemoryWrite);
        if (metadata.OnFileChanged) hooks.Add(SkillHookType.FileChanged);

        return hooks.OrderBy(hook => hook).ToList();
    }

    private static bool TryParseHook(string value, out SkillHookType hookType)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "onboot":
            case "boot":
                hookType = SkillHookType.Boot;
                return true;
            case "onheartbeat":
            case "heartbeat":
                hookType = SkillHookType.Heartbeat;
                return true;
            case "onmemorywrite":
            case "memorywrite":
            case "memory_write":
                hookType = SkillHookType.MemoryWrite;
                return true;
            case "onfilechanged":
            case "filechanged":
            case "file_changed":
                hookType = SkillHookType.FileChanged;
                return true;
            default:
                hookType = default;
                return false;
        }
    }

    /// <summary>
    /// 清理关键词
    /// </summary>
    private static List<string> SanitizeKeywords(List<string> keywords)
    {
        return SanitizeStrings(keywords, keyword => keyword.ToLowerInvariant());
    }

    private static List<string> SanitizeFilePatterns(List<string> patterns)
    {
        return SanitizeStrings(patterns, pattern => pattern.Replace('\\', '/'));
    }

    private static List<string> SanitizeStrings(List<string> values, Func<string, string> normalize)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var value in values)
        {
            var normalized = normalize(value).Trim();
            if (string.IsNullOrEmpty(normalized) || seen.Contains(normalized))
            {
                continue;
            }

            seen.Add(normalized);
            result.Add(normalized);
        }

        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }
}
