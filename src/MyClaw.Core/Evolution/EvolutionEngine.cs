using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MyClaw.Core.Analytics;
using MyClaw.Core.Epigenetics;
using MyClaw.Core.Ribosome;

namespace MyClaw.Core.Evolution;

/// <summary>
/// 进化引擎 - 核心隐式学习机制
/// Evolution Engine - Core mechanism for implicit learning
/// </summary>
public class EvolutionEngine
{
    /// <summary>
    /// 最小置信度阈值
    /// </summary>
    public const double MinConfidence = 0.75;

    /// <summary>
    /// 最小模式数量
    /// </summary>
    public const int MinPatterns = 2;

    /// <summary>
    /// 进化冷却期 (小时)
    /// </summary>
    public const double CooldownHours = 24;

    /// <summary>
    /// 分析模式时使用的记忆天数（与 v3 方案对齐：扩展至 30 天）
    /// </summary>
    public const int MemoryDaysForAnalysis = 30;

    private readonly string _myclawDir;
    private readonly string _stateFile;
    private readonly string _patternsFile;
    private readonly MethylationManager? _methylationManager;
    private readonly EvolutionHistory? _evolutionHistory;
    private readonly RibosomePruner? _ribosomePruner;
    private readonly AnalyticsService? _analyticsService;

    public EvolutionEngine(
        string myclawDir,
        MethylationManager? methylationManager = null,
        EvolutionHistory? evolutionHistory = null,
        RibosomePruner? ribosomePruner = null,
        AnalyticsService? analyticsService = null)
    {
        _myclawDir = myclawDir;
        _stateFile = Path.Combine(myclawDir, "observer-state.json");
        _patternsFile = Path.Combine(myclawDir, "observer-patterns.json");
        _methylationManager = methylationManager;
        _evolutionHistory = evolutionHistory ?? new EvolutionHistory(myclawDir);
        _ribosomePruner = ribosomePruner;
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// 分析记忆文件中的模式
    /// Analyze patterns from memory files
    /// </summary>
    public async Task<List<DetectedPattern>> AnalyzePatternsAsync()
    {
        var memoryDir = Path.Combine(_myclawDir, "memory");
        var patterns = new List<DetectedPattern>();

        if (!Directory.Exists(memoryDir))
            return patterns;

        // 按日期取最近 MemoryDaysForAnalysis 天的记忆文件（与 v3 方案对齐）
        var cutoff = DateTime.UtcNow.Date.AddDays(-MemoryDaysForAnalysis);
        var mdFiles = Directory.GetFiles(memoryDir, "*.md")
            .Where(f => !f.Contains("archived") && TryParseDateFromFileName(f, out var d) && d >= cutoff)
            .OrderBy(f => f)
            .ToList();

        if (mdFiles.Count == 0)
            return patterns;

        var combined = new StringBuilder();
        foreach (var file in mdFiles)
        {
            try
            {
                combined.AppendLine(await File.ReadAllTextAsync(file));
            }
            catch { /* 忽略读取错误 */ }
        }

        var content = combined.ToString();
        if (string.IsNullOrWhiteSpace(content))
            return patterns;

        // 问题模式检测
        var questions = CountMatches(content, @"用户问|问|how to|怎么|how do I");
        if (questions > 5)
        {
            patterns.Add(new DetectedPattern
            {
                Type = PatternType.Repetition,
                Confidence = Math.Min(0.9, questions / 10.0),
                Description = $"{questions} question patterns detected",
                Suggestion = "Consider creating skills for FAQs"
            });
        }

        // 工具使用模式
        var toolCounts = ExtractToolCounts(content);
        var freqTools = toolCounts.Where(kvp => kvp.Value > 3).ToList();
        if (freqTools.Count > 0)
        {
            patterns.Add(new DetectedPattern
            {
                Type = PatternType.Preference,
                Confidence = 0.8,
                Description = $"Frequent tools: {string.Join(", ", freqTools.Select(t => t.Key))}"
            });
        }

        // 时间模式
        var timeMatches = ExtractTimePatterns(content);
        if (timeMatches.Count > 5)
        {
            var peakHour = timeMatches.GroupBy(h => h)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;
            if (peakHour != null)
            {
                patterns.Add(new DetectedPattern
                {
                    Type = PatternType.Temporal,
                    Confidence = 0.75,
                    Description = $"Peak activity at {peakHour}:00"
                });
            }
        }

        // 工作流模式
        var workflows = DetectWorkflowPatterns(content);
        if (workflows.Count > 0)
        {
            patterns.Add(new DetectedPattern
            {
                Type = PatternType.Workflow,
                Confidence = 0.7,
                Description = $"Workflow: {workflows[0]}"
            });
        }

        // 情感模式
        var positive = CountMatches(content, @"谢谢|感谢|很好|不错|perfect|great|thanks");
        var negative = CountMatches(content, @"不对|错了|糟糕|wrong|bad|error");
        if (positive > 3 || negative > 3)
        {
            patterns.Add(new DetectedPattern
            {
                Type = PatternType.Sentiment,
                Confidence = 0.65,
                Description = $"Feedback: {(positive > negative ? "positive" : "negative")}"
            });
        }

        // 错误模式
        var errors = CountMatches(content, @"error|failed|exception|crash|失败|错误");
        if (errors > 3)
        {
            patterns.Add(new DetectedPattern
            {
                Type = PatternType.ErrorPattern,
                Confidence = 0.7,
                Description = $"{errors} errors detected"
            });
        }

        // 相似模式合并（同类型且描述相近的合并，与 v3 方案对齐）
        patterns = MergeSimilarPatternsGlobal(patterns);

        // 保存模式
        try
        {
            var patternsData = new
            {
                Timestamp = DateTime.UtcNow,
                Patterns = patterns
            };
            await File.WriteAllTextAsync(_patternsFile,
                JsonSerializer.Serialize(patternsData, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* 忽略保存错误 */ }

        return patterns;
    }

    private static bool TryParseDateFromFileName(string filePath, out DateTime date)
    {
        date = default;
        var name = Path.GetFileNameWithoutExtension(filePath);
        return name.Length >= 10 && DateTime.TryParse(name.Substring(0, 10), out date);
    }

    /// <summary>
    /// 跨类型列表的相似模式合并（同类型 + 描述关键词重叠则合并）
    /// </summary>
    private static List<DetectedPattern> MergeSimilarPatternsGlobal(List<DetectedPattern> patterns)
    {
        if (patterns.Count <= 1) return patterns;
        var result = new List<DetectedPattern>();
        var used = new HashSet<int>();
        for (var i = 0; i < patterns.Count; i++)
        {
            if (used.Contains(i)) continue;
            var p = patterns[i];
            var group = new List<DetectedPattern> { p };
            for (var j = i + 1; j < patterns.Count; j++)
            {
                if (used.Contains(j)) continue;
                var q = patterns[j];
                if (p.Type != q.Type) continue;
                if (DescriptionSimilarity(p.Description, q.Description) < 0.4) continue;
                group.Add(q);
                used.Add(j);
            }
            used.Add(i);
            result.Add(MergeSimilarPatternsInstance(group));
        }
        return result;
    }

    private static double DescriptionSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
        var ta = new HashSet<string>(Regex.Split(a.ToLowerInvariant(), @"\W+").Where(s => s.Length > 1));
        var tb = new HashSet<string>(Regex.Split(b.ToLowerInvariant(), @"\W+").Where(s => s.Length > 1));
        if (ta.Count == 0) return 0;
        var intersect = ta.Intersect(tb).Count();
        return (double)intersect / Math.Max(ta.Count, tb.Count);
    }

    /// <summary>
    /// 触发进化
    /// Trigger evolution process
    /// </summary>
    public async Task<EvolutionResult> TriggerEvolutionAsync()
    {
        var state = await LoadStateAsync();

        // 检查冷却期
        if (state.LastEvolution.HasValue)
        {
            var hoursSince = (DateTime.UtcNow - state.LastEvolution.Value).TotalHours;
            if (hoursSince < CooldownHours)
            {
                var remaining = Math.Round(CooldownHours - hoursSince);
                return new EvolutionResult
                {
                    Evolved = false,
                    Message = $"Cooldown active. {remaining} hours remaining."
                };
            }
        }

        // 加载模式
        List<DetectedPattern> patterns;
        try
        {
            if (!File.Exists(_patternsFile))
            {
                return new EvolutionResult { Evolved = false, Message = "No patterns to evolve from" };
            }

            var json = await File.ReadAllTextAsync(_patternsFile);
            var data = JsonSerializer.Deserialize<PatternsData>(json);
            patterns = data?.Patterns ?? new List<DetectedPattern>();
        }
        catch
        {
            return new EvolutionResult { Evolved = false, Message = "Failed to load patterns" };
        }

        // 过滤强模式
        var strongPatterns = patterns.Where(p => p.Confidence >= MinConfidence).ToList();
        if (strongPatterns.Count < MinPatterns)
        {
            return new EvolutionResult
            {
                Evolved = false,
                Message = $"Insufficient strong patterns ({strongPatterns.Count}/{MinPatterns})"
            };
        }

        // 应用进化
        var appliedMutations = new List<MutationRecord>();
        var patternsByType = strongPatterns.GroupBy(p => p.Type)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (type, typePatterns) in patternsByType)
        {
            var merged = MergeSimilarPatternsInstance(typePatterns);

            // 检查是否应该甲基化
            if (_methylationManager != null)
            {
                var (should, trait, value) = _methylationManager.ShouldMethylate(
                    type.ToString().ToLower(),
                    merged.Confidence,
                    merged.MergedCount,
                    merged.Description);

                if (should && !string.IsNullOrEmpty(trait) && !string.IsNullOrEmpty(value))
                {
                    _methylationManager.MethylateTrait(trait, value, merged.Description, merged.MergedCount);
                    appliedMutations.Add(new MutationRecord
                    {
                        Target = "SOUL.md (methylation)",
                        Change = $"Methylated {trait} → {value}",
                        Confidence = merged.Confidence
                    });
                }
            }

            // 根据模式类型更新不同文件
            switch (type)
            {
                case PatternType.Preference:
                case PatternType.Sentiment:
                    await UpdateDnaFileAsync("SOUL.md", merged, appliedMutations);
                    if (type == PatternType.Sentiment)
                        await UpdateReflectionAsync("emotional_adaptation", merged.Description, appliedMutations);
                    break;

                case PatternType.Temporal:
                    await UpdateDnaFileAsync("USER.md", merged, appliedMutations);
                    break;

                case PatternType.Workflow:
                    await UpdateDnaFileAsync("AGENTS.md", merged, appliedMutations);
                    break;

                case PatternType.Repetition:
                    await UpdateDnaFileAsync("TOOLS.md", merged, appliedMutations);
                    await ExtractConceptsAsync(merged, appliedMutations);
                    break;

                case PatternType.ErrorPattern:
                    await UpdateReflectionAsync("error_improvement", merged.Description, appliedMutations);
                    break;

                case PatternType.Concept:
                    await ExtractConceptsAsync(merged, appliedMutations);
                    break;
            }
        }

        // 更新状态
        state.LastEvolution = DateTime.UtcNow;
        state.TotalEvolutions++;
        await SaveStateAsync(state);

        // 检查里程碑
        await CheckMilestonesAsync(state.TotalEvolutions, appliedMutations);

        // 记录进化日志
        await LogEvolutionAsync(state.TotalEvolutions, appliedMutations.Count, strongPatterns);

        // 写入进化历史（与 v3 方案对齐）
        if (_evolutionHistory != null)
            await _evolutionHistory.RecordAsync(state.TotalEvolutions, appliedMutations.Count, strongPatterns);

        // 器官退化 - 用进废退机制（与 MiniClaw v0.9.5 对齐）
        if (_ribosomePruner != null)
        {
            try
            {
                if (_analyticsService != null)
                {
                    var analytics = _analyticsService.GetAnalytics();
                    await _ribosomePruner.PruneAsync(
                        analytics.BootCount,
                        new Dictionary<string, int>(analytics.ToolCalls));
                }
            }
            catch (Exception ex)
            {
                // 忽略异常，避免影响进化流程
                _ = ex; // Suppress warning
            }
        }

        return new EvolutionResult
        {
            Evolved = true,
            Message = $"Applied {appliedMutations.Count} mutations",
            AppliedPatterns = strongPatterns,
            TotalEvolutions = state.TotalEvolutions
        };
    }

    /// <summary>
    /// 获取进化状态
    /// Get evolution state
    /// </summary>
    public async Task<EvolutionStateInfo> GetStateAsync()
    {
        var state = await LoadStateAsync();
        var hoursSinceLast = state.LastEvolution.HasValue
            ? (DateTime.UtcNow - state.LastEvolution.Value).TotalHours
            : double.MaxValue;

        return new EvolutionStateInfo
        {
            LastEvolution = state.LastEvolution,
            TotalEvolutions = state.TotalEvolutions,
            HoursSinceLastEvolution = hoursSinceLast,
            CanEvolve = hoursSinceLast >= CooldownHours,
            RemainingCooldownHours = Math.Max(0, CooldownHours - hoursSinceLast)
        };
    }

    private async Task UpdateDnaFileAsync(string fileName, DetectedPattern pattern, List<MutationRecord> mutations)
    {
        var filePath = Path.Combine(_myclawDir, fileName);
        if (!File.Exists(filePath))
            return;

        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            var keyConcept = pattern.Description.Substring(0, Math.Min(50, pattern.Description.Length)).Trim();
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var confidence = (int)(pattern.Confidence * 100);

            var newLine = $"- [AUTO-EVOLVED] {pattern.Description} (confidence: {confidence}%, detections: {pattern.MergedCount}, first: {timestamp})";

            // 检查是否已存在相似内容
            if (content.Contains(keyConcept.Substring(0, Math.Min(30, keyConcept.Length))))
                return;

            content = content.TrimEnd() + "\n" + newLine + "\n";
            await File.WriteAllTextAsync(filePath, content);

            mutations.Add(new MutationRecord
            {
                Target = fileName,
                Change = pattern.Description,
                Confidence = pattern.Confidence
            });
        }
        catch { /* 忽略错误 */ }
    }

    private async Task UpdateReflectionAsync(string type, string description, List<MutationRecord> mutations)
    {
        var filePath = Path.Combine(_myclawDir, "REFLECTION.md");
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var line = $"- [AUTO-EVOLVED] {type}: {description} (reflected: {timestamp})";

        try
        {
            var content = File.Exists(filePath) ? await File.ReadAllTextAsync(filePath) : "";

            if (content.Contains(description.Substring(0, Math.Min(40, description.Length))))
                return;

            content = content.TrimEnd() + "\n" + line + "\n";
            await File.WriteAllTextAsync(filePath, content);

            mutations.Add(new MutationRecord
            {
                Target = "REFLECTION.md",
                Change = $"{type}: {description}"
            });
        }
        catch { /* 忽略错误 */ }
    }

    private async Task ExtractConceptsAsync(DetectedPattern pattern, List<MutationRecord> mutations)
    {
        var filePath = Path.Combine(_myclawDir, "CONCEPTS.md");
        var matches = Regex.Matches(pattern.Description, @"([A-Z][a-zA-Z]+(?:\s+[A-Z][a-zA-Z]+)*)");

        try
        {
            var content = File.Exists(filePath) ? await File.ReadAllTextAsync(filePath) : "";

            foreach (Match match in matches.Cast<Match>().Take(3))
            {
                var concept = match.Value;
                if (concept.Length <= 3 || content.Contains(concept))
                    continue;

                var line = $"- **{concept}**: [AUTO-EVOLVED] Concept.";
                content = content.TrimEnd() + "\n" + line + "\n";

                mutations.Add(new MutationRecord
                {
                    Target = "CONCEPTS.md",
                    Change = $"Added concept: {concept}"
                });
            }

            await File.WriteAllTextAsync(filePath, content);
        }
        catch { /* 忽略错误 */ }
    }

    private async Task CheckMilestonesAsync(int totalEvolutions, List<MutationRecord> mutations)
    {
        var milestones = new[] { 1, 5, 10 };
        var filePath = Path.Combine(_myclawDir, "HORIZONS.md");

        try
        {
            var content = File.Exists(filePath) ? await File.ReadAllTextAsync(filePath) : "";

            foreach (var m in milestones)
            {
                if (totalEvolutions != m)
                    continue;

                var desc = m == 1 ? "First Evolution" : $"{m}th Gen Evolution";
                var line = $"- [AUTO-EVOLVED] Milestone: {desc} (G{totalEvolutions}, {DateTime.UtcNow:yyyy-MM-dd})";

                if (content.Contains(desc))
                    continue;

                content = content.TrimEnd() + "\n" + line + "\n";
                mutations.Add(new MutationRecord
                {
                    Target = "HORIZONS.md",
                    Change = $"Milestone: {desc}"
                });
            }

            await File.WriteAllTextAsync(filePath, content);
        }
        catch { /* 忽略错误 */ }
    }

    private async Task LogEvolutionAsync(int generation, int mutationCount, List<DetectedPattern> patterns)
    {
        var memoryDir = Path.Combine(_myclawDir, "memory");
        if (!Directory.Exists(memoryDir))
            Directory.CreateDirectory(memoryDir);

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var memoryFile = Path.Combine(memoryDir, $"{today}.md");

        var log = $"\n## 🧬 Evolution G{generation}\n- Applied {mutationCount} mutations\n- Patterns: {string.Join(", ", patterns.Select(p => p.Type))}\n";

        try
        {
            await File.AppendAllTextAsync(memoryFile, log);
        }
        catch { /* 忽略错误 */ }
    }

    private static DetectedPattern MergeSimilarPatternsInstance(List<DetectedPattern> patterns)
    {
        if (patterns.Count == 1)
            return patterns[0];

        // 简单合并：取最高置信度的描述
        var sorted = patterns.OrderByDescending(p => p.Confidence).ToList();
        var merged = sorted[0];
        merged.MergedCount = patterns.Count;
        merged.AvgConfidence = patterns.Average(p => p.Confidence);
        merged.Description += $" (merged from {patterns.Count})";
        return merged;
    }

    private int CountMatches(string content, string pattern)
    {
        try
        {
            return Regex.Matches(content, pattern, RegexOptions.IgnoreCase).Count;
        }
        catch
        {
            return 0;
        }
    }

    private Dictionary<string, int> ExtractToolCounts(string content)
    {
        var counts = new Dictionary<string, int>();
        try
        {
            var matches = Regex.Matches(content, @"miniclaw_[a-z_]+");
            foreach (Match match in matches)
            {
                var tool = match.Value;
                counts[tool] = counts.GetValueOrDefault(tool, 0) + 1;
            }
        }
        catch { }
        return counts;
    }

    private List<int> ExtractTimePatterns(string content)
    {
        var hours = new List<int>();
        try
        {
            var matches = Regex.Matches(content, @"\[(\d{2}):(\d{2})");
            foreach (Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out var hour))
                    hours.Add(hour);
            }
        }
        catch { }
        return hours;
    }

    private List<string> DetectWorkflowPatterns(string content)
    {
        var workflows = new List<string>();
        try
        {
            var matches = Regex.Matches(content, @"miniclaw_(\w+)");
            var toolSequence = matches.Cast<Match>().Select(m => m.Value).ToList();

            if (toolSequence.Count >= 6)
            {
                // 检测 2-3 步重复序列
                for (var len = 2; len <= 3; len++)
                {
                    var sequences = new Dictionary<string, int>();
                    for (var i = 0; i <= toolSequence.Count - len; i++)
                    {
                        var seq = string.Join(" → ", toolSequence.Skip(i).Take(len));
                        sequences[seq] = sequences.GetValueOrDefault(seq, 0) + 1;
                    }

                    var repeated = sequences.Where(kvp => kvp.Value >= 2).ToList();
                    if (repeated.Count > 0)
                    {
                        var top = repeated.OrderByDescending(kvp => kvp.Value).First();
                        workflows.Add($"Repeated {len}-step workflow: {top.Key}");
                    }
                }
            }
        }
        catch { }
        return workflows;
    }

    private async Task<EvolutionState> LoadStateAsync()
    {
        try
        {
            if (File.Exists(_stateFile))
            {
                var json = await File.ReadAllTextAsync(_stateFile);
                return JsonSerializer.Deserialize<EvolutionState>(json) ?? new EvolutionState();
            }
        }
        catch { }
        return new EvolutionState();
    }

    private async Task SaveStateAsync(EvolutionState state)
    {
        try
        {
            if (!Directory.Exists(_myclawDir))
                Directory.CreateDirectory(_myclawDir);

            await File.WriteAllTextAsync(_stateFile,
                JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}

/// <summary>
/// 进化状态持久化
/// </summary>
internal class EvolutionState
{
    public DateTime? LastEvolution { get; set; }
    public int TotalEvolutions { get; set; }
}

/// <summary>
/// 模式数据
/// </summary>
internal class PatternsData
{
    public List<DetectedPattern>? Patterns { get; set; }
}

/// <summary>
/// 进化状态信息
/// </summary>
public class EvolutionStateInfo
{
    public DateTime? LastEvolution { get; set; }
    public int TotalEvolutions { get; set; }
    public double HoursSinceLastEvolution { get; set; }
    public bool CanEvolve { get; set; }
    public double RemainingCooldownHours { get; set; }
}
