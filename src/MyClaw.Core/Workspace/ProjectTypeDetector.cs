namespace MyClaw.Core.Workspace;

/// <summary>
/// 项目类型检测结果
/// </summary>
public class ProjectTypeInfo
{
    public ProjectType Type { get; set; }
    public string Name { get; set; } = "Unknown";
    public double Confidence { get; set; }
}

/// <summary>
/// 项目类型检测器 - 通过文件签名自动识别项目类型（与 MiniClaw 工作区感知对齐）
/// </summary>
public static class ProjectTypeDetector
{
    private static readonly Dictionary<ProjectType, string[]> Signatures = new()
    {
        [ProjectType.React] = new[] { "package.json", "src/App.tsx", "src/App.jsx", "vite.config.ts", "next.config.js" },
        [ProjectType.Vue] = new[] { "package.json", "vue.config.js", "src/App.vue", "nuxt.config.ts" },
        [ProjectType.Node] = new[] { "package.json", "node_modules" },
        [ProjectType.Python] = new[] { "requirements.txt", "setup.py", "pyproject.toml", "Pipfile" },
        [ProjectType.Go] = new[] { "go.mod", "go.sum" },
        [ProjectType.Rust] = new[] { "Cargo.toml", "Cargo.lock" },
        [ProjectType.DotNet] = new[] { "*.csproj", "*.sln", "*.slnx", "Program.cs" },
        [ProjectType.Java] = new[] { "pom.xml", "build.gradle", "build.gradle.kts" },
        [ProjectType.Docker] = new[] { "Dockerfile", "docker-compose.yml", "docker-compose.yaml" },
        [ProjectType.Angular] = new[] { "angular.json", "package.json" },
        [ProjectType.Svelte] = new[] { "svelte.config.js", "package.json" },
        [ProjectType.Ruby] = new[] { "Gemfile", "Gemfile.lock" },
        [ProjectType.PHP] = new[] { "composer.json", "artisan" },
    };

    /// <summary>
    /// 检测工作区项目类型及置信度
    /// </summary>
    public static ProjectTypeInfo Detect(string workspacePath)
    {
        if (!Directory.Exists(workspacePath))
            return new ProjectTypeInfo { Type = ProjectType.Unknown, Name = "Unknown", Confidence = 0 };

        var topFiles = GetTopLevelFilesAndDirs(workspacePath);
        ProjectType bestType = ProjectType.Unknown;
        int bestScore = 0;
        int bestTotal = 0;

        foreach (var (projectType, signatures) in Signatures)
        {
            var (score, total) = ScoreSignatures(topFiles, signatures);
            if (total > 0 && score > bestScore)
            {
                bestScore = score;
                bestTotal = total;
                bestType = projectType;
            }
        }

        var confidence = bestTotal > 0 ? (double)bestScore / bestTotal : 0;
        var name = bestType == ProjectType.Unknown ? "Unknown" : bestType.ToString();
        return new ProjectTypeInfo { Type = bestType, Name = name, Confidence = Math.Min(1, confidence) };
    }

    private static HashSet<string> GetTopLevelFilesAndDirs(string workspacePath)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var f in Directory.GetFiles(workspacePath))
                set.Add(Path.GetFileName(f));
            foreach (var d in Directory.GetDirectories(workspacePath))
                set.Add(Path.GetFileName(d));
        }
        catch { /* ignore */ }

        // 通配符匹配：检查是否存在任意 .csproj / .sln
        try
        {
            if (Directory.GetFiles(workspacePath, "*.csproj").Length > 0) set.Add("*.csproj");
            if (Directory.GetFiles(workspacePath, "*.sln").Length > 0) set.Add("*.sln");
            if (Directory.GetFiles(workspacePath, "*.slnx").Length > 0) set.Add("*.slnx");
        }
        catch { /* ignore */ }

        return set;
    }

    private static (int Score, int Total) ScoreSignatures(HashSet<string> topFiles, string[] signatures)
    {
        int score = 0;
        foreach (var sig in signatures)
        {
            if (sig.Contains('*'))
            {
                if (topFiles.Contains("*.csproj") && sig == "*.csproj") score++;
                else if (topFiles.Contains("*.sln") && sig == "*.sln") score++;
                else if (topFiles.Contains("*.slnx") && sig == "*.slnx") score++;
            }
            else if (topFiles.Contains(sig))
            {
                score++;
            }
        }
        return (score, signatures.Length);
    }
}
