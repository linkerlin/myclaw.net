using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MyClaw.Skills;

public sealed class SubstrateHarvestOptions
{
    public bool Apply { get; init; }

    public bool OverwriteExisting { get; init; }

    public List<string> Sources { get; init; } = new();

    public int MaxFileBytes { get; init; } = 64 * 1024;
}

public sealed class SubstrateHarvestCandidate
{
    public string SourceTool { get; init; } = string.Empty;

    public string SourceLabel { get; init; } = string.Empty;

    public string RelativeSourcePath { get; init; } = string.Empty;

    public string SkillName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string DestinationPath { get; init; } = string.Empty;

    public List<string> Keywords { get; init; } = new();

    public List<string> OriginalFilePatterns { get; init; } = new();

    public string Content { get; init; } = string.Empty;
}

public sealed class SubstrateHarvestImport
{
    public string SkillName { get; init; } = string.Empty;

    public string SourceTool { get; init; } = string.Empty;

    public string SourcePath { get; init; } = string.Empty;

    public string DestinationPath { get; init; } = string.Empty;
}

public sealed class SubstrateHarvestResult
{
    public bool ApplyRequested { get; init; }

    public bool OverwriteExisting { get; init; }

    public int MaxFileBytes { get; init; }

    public List<string> Sources { get; init; } = new();

    public int ScannedFiles { get; set; }

    public List<SubstrateHarvestCandidate> Candidates { get; init; } = new();

    public List<SubstrateHarvestImport> ImportedSkills { get; init; } = new();

    public List<string> SkippedFiles { get; init; } = new();

    public List<string> Warnings { get; init; } = new();

    public string ToDisplayString()
    {
        var lines = new List<string>
        {
            $"## Substrate Harvest ({(ApplyRequested ? "apply" : "dry-run")})",
            $"- Sources: {(Sources.Count == 0 ? "all" : string.Join(", ", Sources))}",
            $"- Scanned files: {ScannedFiles}",
            $"- Candidates: {Candidates.Count}",
            $"- Imported: {ImportedSkills.Count}",
            $"- Skipped: {SkippedFiles.Count}",
            $"- Boundary policy: whitelist-only, max {MaxFileBytes} bytes, text files only, no auto-hooks, {(OverwriteExisting ? "overwrite enabled" : "overwrite disabled")}" 
        };

        if (Candidates.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("### Candidates");
            lines.AddRange(Candidates.Take(10).Select(candidate =>
                $"- {candidate.SkillName} <= {candidate.SourceTool}:{candidate.RelativeSourcePath}"));

            if (Candidates.Count > 10)
            {
                lines.Add($"- ... and {Candidates.Count - 10} more");
            }
        }

        if (ImportedSkills.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("### Imported");
            lines.AddRange(ImportedSkills.Select(imported =>
                $"- {imported.SkillName} -> {imported.DestinationPath}"));
        }

        if (SkippedFiles.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("### Skipped");
            lines.AddRange(SkippedFiles.Take(10).Select(skipped => $"- {skipped}"));

            if (SkippedFiles.Count > 10)
            {
                lines.Add($"- ... and {SkippedFiles.Count - 10} more");
            }
        }

        if (Warnings.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("### Warnings");
            lines.AddRange(Warnings.Take(10).Select(warning => $"- {warning}"));

            if (Warnings.Count > 10)
            {
                lines.Add($"- ... and {Warnings.Count - 10} more");
            }
        }

        return string.Join("\n", lines);
    }
}

public sealed class SubstrateHarvester
{
    private static readonly Regex FrontmatterRegex = new(
        @"^---\s*\n(?<frontmatter>.*?)\n---\s*\n(?<body>.*)$",
        RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex HeadingRegex = new(@"^#\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);
    private readonly string _workspacePath;
    private readonly string _skillsDirectory;

    private static readonly IReadOnlyList<SubstrateSourceDefinition> SourceDefinitions = new[]
    {
        new SubstrateSourceDefinition(
            "copilot",
            "GitHub Copilot",
            new[] { ".github/copilot-instructions.md", "copilot-instructions.md" },
            Array.Empty<SubstrateDirectoryDefinition>()),
        new SubstrateSourceDefinition(
            "cursor",
            "Cursor",
            new[] { ".cursorrules" },
            new[] { new SubstrateDirectoryDefinition(".cursor/rules", new[] { ".md", ".mdc" }) }),
        new SubstrateSourceDefinition(
            "claude",
            "Claude",
            new[] { "CLAUDE.md" },
            new[] { new SubstrateDirectoryDefinition(".claude/commands", new[] { ".md" }) }),
        new SubstrateSourceDefinition(
            "windsurf",
            "Windsurf",
            new[] { ".windsurfrules" },
            new[] { new SubstrateDirectoryDefinition(".windsurf/rules", new[] { ".md" }) })
    };

    public static IReadOnlyList<string> SupportedSources => SourceDefinitions.Select(definition => definition.Name).ToList();

    public SubstrateHarvester(string workspacePath, string skillsDirectory)
    {
        _workspacePath = Path.GetFullPath(workspacePath);
        _skillsDirectory = Path.GetFullPath(skillsDirectory);
    }

    public SubstrateHarvestResult Harvest(SubstrateHarvestOptions? options = null)
    {
        options ??= new SubstrateHarvestOptions();

        var requestedSources = NormalizeSources(options.Sources);
        var result = new SubstrateHarvestResult
        {
            ApplyRequested = options.Apply,
            OverwriteExisting = options.OverwriteExisting,
            MaxFileBytes = options.MaxFileBytes,
            Sources = requestedSources.OrderBy(source => source, StringComparer.OrdinalIgnoreCase).ToList()
        };

        var knownSources = new HashSet<string>(SupportedSources, StringComparer.OrdinalIgnoreCase);
        foreach (var unknown in requestedSources.Where(source => !knownSources.Contains(source)))
        {
            result.Warnings.Add($"Unsupported source '{unknown}' ignored. Supported: {string.Join(", ", SupportedSources)}");
        }

        var selectedDefinitions = SourceDefinitions
            .Where(definition => requestedSources.Count == 0 || requestedSources.Contains(definition.Name))
            .ToList();

        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenSkillNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in selectedDefinitions)
        {
            foreach (var fullPath in EnumerateSourceFiles(definition))
            {
                if (!seenPaths.Add(fullPath))
                {
                    continue;
                }

                result.ScannedFiles++;
                var candidate = TryCreateCandidate(definition, fullPath, options, result);
                if (candidate == null)
                {
                    continue;
                }

                if (!seenSkillNames.Add(candidate.SkillName))
                {
                    result.SkippedFiles.Add($"{candidate.SourceTool}:{candidate.RelativeSourcePath} (duplicate skill name '{candidate.SkillName}')");
                    continue;
                }

                result.Candidates.Add(candidate);

                if (options.Apply)
                {
                    ApplyCandidate(candidate, options, result);
                }
            }
        }

        return result;
    }

    private IEnumerable<string> EnumerateSourceFiles(SubstrateSourceDefinition definition)
    {
        foreach (var relativePath in definition.ExactFiles)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_workspacePath, relativePath));
            if (File.Exists(fullPath) && IsWithinWorkspace(fullPath))
            {
                yield return fullPath;
            }
        }

        foreach (var directory in definition.Directories)
        {
            var fullDirectory = Path.GetFullPath(Path.Combine(_workspacePath, directory.RelativePath));
            if (!Directory.Exists(fullDirectory) || !IsWithinWorkspace(fullDirectory))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(fullDirectory, "*", SearchOption.AllDirectories)
                         .Where(file => directory.Extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)))
            {
                var fullPath = Path.GetFullPath(file);
                if (IsWithinWorkspace(fullPath))
                {
                    yield return fullPath;
                }
            }
        }
    }

    private SubstrateHarvestCandidate? TryCreateCandidate(
        SubstrateSourceDefinition definition,
        string fullPath,
        SubstrateHarvestOptions options,
        SubstrateHarvestResult result)
    {
        try
        {
            var attributes = File.GetAttributes(fullPath);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                result.SkippedFiles.Add($"{MakeRelativePath(fullPath)} (reparse point skipped)");
                return null;
            }

            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length > options.MaxFileBytes)
            {
                result.SkippedFiles.Add($"{MakeRelativePath(fullPath)} (exceeds {options.MaxFileBytes} bytes)");
                return null;
            }

            var bytes = File.ReadAllBytes(fullPath);
            if (bytes.Any(b => b == 0))
            {
                result.SkippedFiles.Add($"{MakeRelativePath(fullPath)} (binary content skipped)");
                return null;
            }

            var rawContent = Encoding.UTF8.GetString(bytes).Trim();
            if (string.IsNullOrWhiteSpace(rawContent))
            {
                result.SkippedFiles.Add($"{MakeRelativePath(fullPath)} (empty content)");
                return null;
            }

            var relativePath = MakeRelativePath(fullPath);
            var extracted = ExtractSourceDocument(rawContent);
            var skillName = GenerateSkillName(definition.Name, relativePath);
            var description = BuildDescription(definition.DisplayName, relativePath, extracted);
            var destination = SkillPaths.ResolveSkillFilePath(_workspacePath, skillName, _skillsDirectory);

            return new SubstrateHarvestCandidate
            {
                SourceTool = definition.Name,
                SourceLabel = definition.DisplayName,
                RelativeSourcePath = relativePath,
                SkillName = skillName,
                Description = description,
                DestinationPath = MakeDisplayPath(destination),
                Keywords = BuildKeywords(definition.Name, relativePath),
                OriginalFilePatterns = extracted.Globs,
                Content = BuildImportedContent(definition.DisplayName, relativePath, extracted)
            };
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"Failed to inspect {MakeRelativePath(fullPath)}: {ex.Message}");
            return null;
        }
    }

    private void ApplyCandidate(SubstrateHarvestCandidate candidate, SubstrateHarvestOptions options, SubstrateHarvestResult result)
    {
        var destination = SkillPaths.ResolveSkillFilePath(_workspacePath, candidate.SkillName, _skillsDirectory);
        if (File.Exists(destination) && !options.OverwriteExisting)
        {
            result.SkippedFiles.Add($"{candidate.RelativeSourcePath} (destination exists: {MakeDisplayPath(destination)})");
            return;
        }

        var metadata = new Dictionary<string, string>
        {
            ["managedBy"] = "substrate-harvest",
            ["sourcePath"] = candidate.RelativeSourcePath,
            ["sourceTool"] = candidate.SourceTool
        };

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var markdown = SkillDocumentBuilder.BuildMarkdown(
            candidate.SkillName,
            candidate.Description,
            candidate.Content,
            candidate.Keywords,
            metadata: metadata);
        File.WriteAllText(destination, markdown);

        result.ImportedSkills.Add(new SubstrateHarvestImport
        {
            SkillName = candidate.SkillName,
            SourceTool = candidate.SourceTool,
            SourcePath = candidate.RelativeSourcePath,
            DestinationPath = MakeDisplayPath(destination)
        });
    }

    private bool IsWithinWorkspace(string path)
    {
        var fullPath = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var root = _workspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return fullPath.Equals(root, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private string MakeRelativePath(string fullPath)
    {
        return Path.GetRelativePath(_workspacePath, fullPath).Replace('\\', '/');
    }

    private string MakeDisplayPath(string fullPath)
    {
        return IsWithinWorkspace(fullPath) ? MakeRelativePath(fullPath) : fullPath;
    }

    private static HashSet<string> NormalizeSources(IEnumerable<string>? sources)
    {
        if (sources == null)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return sources
            .SelectMany(source => source.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Select(source => source.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static ImportedSourceDocument ExtractSourceDocument(string content)
    {
        var trimmed = content.Trim();
        var match = FrontmatterRegex.Match(trimmed);
        if (!match.Success)
        {
            return new ImportedSourceDocument(null, new List<string>(), null, trimmed);
        }

        var frontmatter = match.Groups["frontmatter"].Value;
        var body = match.Groups["body"].Value.Trim();
        string? description = null;
        List<string> globs = new();
        bool? alwaysApply = null;
        List<string>? currentList = null;
        string? currentKey = null;

        foreach (var rawLine in frontmatter.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("- ", StringComparison.Ordinal) && currentKey != null)
            {
                currentList ??= new List<string>();
                currentList.Add(TrimQuotes(line[2..].Trim()));
                continue;
            }

            if (currentList != null && currentKey != null)
            {
                AssignFrontmatterList(currentKey, currentList, ref globs);
                currentList = null;
                currentKey = null;
            }

            var colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
            {
                continue;
            }

            var key = line[..colonIndex].Trim().ToLowerInvariant();
            var value = TrimQuotes(line[(colonIndex + 1)..].Trim());

            switch (key)
            {
                case "description":
                    description = value;
                    break;
                case "globs":
                case "files":
                case "filepatterns":
                case "file_patterns":
                    currentKey = key;
                    if (!string.IsNullOrWhiteSpace(value) && value.StartsWith('[') && value.EndsWith(']'))
                    {
                        AssignFrontmatterList(key, value[1..^1]
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(TrimQuotes)
                            .ToList(), ref globs);
                        currentKey = null;
                    }
                    break;
                case "alwaysapply":
                    alwaysApply = ParseBoolean(value);
                    break;
            }
        }

        if (currentList != null && currentKey != null)
        {
            AssignFrontmatterList(currentKey, currentList, ref globs);
        }

        return new ImportedSourceDocument(description, globs, alwaysApply, string.IsNullOrWhiteSpace(body) ? trimmed : body);
    }

    private static void AssignFrontmatterList(string key, List<string> values, ref List<string> globs)
    {
        if (key is "globs" or "files" or "filepatterns" or "file_patterns")
        {
            globs = values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Replace('\\', '/').Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    private static string BuildDescription(string sourceLabel, string relativePath, ImportedSourceDocument extracted)
    {
        if (!string.IsNullOrWhiteSpace(extracted.Description))
        {
            return extracted.Description!;
        }

        var heading = HeadingRegex.Match(extracted.Body);
        if (heading.Success)
        {
            return heading.Groups[1].Value.Trim();
        }

        return $"Imported from {sourceLabel}: {relativePath}";
    }

    private static List<string> BuildKeywords(string sourceTool, string relativePath)
    {
        var tokens = Regex.Split(Path.GetFileNameWithoutExtension(relativePath).ToLowerInvariant(), @"[^a-z0-9]+")
            .Where(token => token.Length >= 3)
            .ToList();

        return new[] { "imported", "substrate", sourceTool }
            .Concat(tokens)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(token => token, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildImportedContent(string sourceLabel, string relativePath, ImportedSourceDocument extracted)
    {
        var lines = new List<string>
        {
            $"# Imported from {sourceLabel}",
            string.Empty,
            $"- Source file: {relativePath}",
            "- Import mode: passive reusable skill",
            "- Safety: hooks are not auto-enabled during harvest"
        };

        if (extracted.Globs.Count > 0)
        {
            lines.Add($"- Original file patterns: {string.Join(", ", extracted.Globs)}");
        }

        if (extracted.AlwaysApply.HasValue)
        {
            lines.Add($"- Original alwaysApply: {extracted.AlwaysApply.Value.ToString().ToLowerInvariant()}");
        }

        lines.Add(string.Empty);
        lines.Add(extracted.Body.Trim());
        return string.Join("\n", lines);
    }

    private static string GenerateSkillName(string sourceTool, string relativePath)
    {
        var slugSource = Path.ChangeExtension(relativePath, null)?.ToLowerInvariant() ?? relativePath.ToLowerInvariant();
        var slug = Regex.Replace(slugSource.Replace('\\', '/'), @"[^a-z0-9]+", "_")
            .Trim('_');

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "rule";
        }

        var baseName = $"import_{sourceTool}_{slug}";
        if (baseName.Length <= 80)
        {
            return baseName;
        }

        using var sha256 = SHA256.Create();
        var hash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(relativePath))).ToLowerInvariant()[..8];
        return $"import_{sourceTool}_{slug[..Math.Min(60, slug.Length)]}_{hash}";
    }

    private static string TrimQuotes(string value)
    {
        if ((value.StartsWith('"') && value.EndsWith('"')) ||
            (value.StartsWith('\'') && value.EndsWith('\'')))
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

    private sealed record SubstrateSourceDefinition(
        string Name,
        string DisplayName,
        IReadOnlyList<string> ExactFiles,
        IReadOnlyList<SubstrateDirectoryDefinition> Directories);

    private sealed record SubstrateDirectoryDefinition(string RelativePath, IReadOnlyList<string> Extensions);

    private sealed record ImportedSourceDocument(
        string? Description,
        List<string> Globs,
        bool? AlwaysApply,
        string Body);
}