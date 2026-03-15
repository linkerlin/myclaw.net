using System.Diagnostics;

namespace MyClaw.Core.Workspace;

/// <summary>
/// 工作区检测器 - 自动检测当前工作区的项目信息、Git状态和技术栈
/// </summary>
public class WorkspaceDetector
{
    private readonly string _workspacePath;

    public WorkspaceDetector(string? workspacePath = null)
    {
        _workspacePath = workspacePath ?? Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// 检测工作区信息
    /// </summary>
    public async Task<WorkspaceInfo> DetectAsync()
    {
        var info = new WorkspaceInfo
        {
            Name = Path.GetFileName(_workspacePath),
            Path = _workspacePath,
            DetectedAt = DateTime.Now
        };

        // 并行检测：项目类型、Git、技术栈
        var gitTask = DetectGitAsync();
        var techTask = Task.Run(() => TechStackDetector.Detect(_workspacePath));
        var projectTypeTask = Task.Run(() => ProjectTypeDetector.Detect(_workspacePath));
        
        await Task.WhenAll(gitTask, techTask, projectTypeTask);

        info.Git = await gitTask;
        info.TechStack = await techTask;
        info.DetectedProjectType = await projectTypeTask;

        return info;
    }

    /// <summary>
    /// 快速检测（仅基本信息，不执行命令）
    /// </summary>
    public WorkspaceInfo DetectQuick()
    {
        return new WorkspaceInfo
        {
            Name = Path.GetFileName(_workspacePath),
            Path = _workspacePath,
            TechStack = TechStackDetector.Detect(_workspacePath),
            DetectedProjectType = ProjectTypeDetector.Detect(_workspacePath),
            DetectedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 检测 Git 仓库信息
    /// </summary>
    private async Task<GitInfo> DetectGitAsync()
    {
        var gitInfo = new GitInfo();

        try
        {
            // 检查是否是 Git 仓库
            var gitDir = Path.Combine(_workspacePath, ".git");
            if (!Directory.Exists(gitDir) && !File.Exists(gitDir))
            {
                return gitInfo;
            }

            gitInfo.IsRepo = true;

            // 获取当前分支
            var branchResult = await ExecuteGitCommandAsync("branch --show-current");
            if (branchResult.ExitCode == 0)
            {
                gitInfo.Branch = branchResult.Output.Trim();
            }
            else
            {
                // 可能是 detached HEAD 状态
                var symbolicRef = await ExecuteGitCommandAsync("symbolic-ref --short HEAD");
                if (symbolicRef.ExitCode == 0)
                {
                    gitInfo.Branch = symbolicRef.Output.Trim();
                }
                else
                {
                    gitInfo.Branch = "(detached HEAD)";
                }
            }

            // 获取仓库状态
            var statusResult = await ExecuteGitCommandAsync("status --short");
            if (statusResult.ExitCode == 0)
            {
                var statusLines = statusResult.Output
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();
                
                gitInfo.UncommittedChanges = statusLines.Count;
                gitInfo.Status = statusLines.Count > 0 ? "dirty" : "clean";
            }

            // 获取最近提交
            var logResult = await ExecuteGitCommandAsync("log --oneline -3 --no-decorate");
            if (logResult.ExitCode == 0)
            {
                gitInfo.RecentCommits = logResult.Output.Trim();
            }

            // 获取远程仓库 URL
            var remoteResult = await ExecuteGitCommandAsync("remote get-url origin");
            if (remoteResult.ExitCode == 0)
            {
                gitInfo.RemoteUrl = remoteResult.Output.Trim();
            }
        }
        catch (Exception ex)
        {
            // Git 检测失败，但不影响整体功能
            gitInfo.IsRepo = false;
            Debug.WriteLine($"Git detection failed: {ex.Message}");
        }

        return gitInfo;
    }

    /// <summary>
    /// 执行 Git 命令
    /// </summary>
    private async Task<(string Output, int ExitCode)> ExecuteGitCommandAsync(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = _workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return (string.Empty, -1);
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(outputTask, errorTask);
            process.WaitForExit(5000); // 5秒超时

            var output = await outputTask;
            var error = await errorTask;

            return (string.IsNullOrEmpty(output) ? error : output, process.ExitCode);
        }
        catch (Exception ex)
        {
            return (ex.Message, -1);
        }
    }

    /// <summary>
    /// 获取最近更改的文件列表
    /// </summary>
    public async Task<List<string>> GetRecentChangesAsync(int count = 10)
    {
        try
        {
            var result = await ExecuteGitCommandAsync($"diff --name-only HEAD~{count}..HEAD");
            if (result.ExitCode == 0)
            {
                return result.Output
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim())
                    .Where(f => !string.IsNullOrEmpty(f))
                    .ToList();
            }
        }
        catch { /* ignore */ }

        return new List<string>();
    }

    /// <summary>
    /// 获取仓库统计信息
    /// </summary>
    public async Task<(int CommitCount, int ContributorCount, DateTime? LastCommitDate)> GetRepositoryStatsAsync()
    {
        try
        {
            // 提交数量
            var countResult = await ExecuteGitCommandAsync("rev-list --count HEAD");
            int commitCount = countResult.ExitCode == 0 && int.TryParse(countResult.Output.Trim(), out var cc) ? cc : 0;

            // 贡献者数量
            var contributorsResult = await ExecuteGitCommandAsync("log --format='%an' | sort -u | wc -l");
            int contributorCount = 0;
            if (contributorsResult.ExitCode == 0)
            {
                var match = System.Text.RegularExpressions.Regex.Match(contributorsResult.Output.Trim(), @"\d+");
                if (match.Success) int.TryParse(match.Value, out contributorCount);
            }

            // 最后提交时间
            var lastCommitResult = await ExecuteGitCommandAsync("log -1 --format='%ct'");
            DateTime? lastCommitDate = null;
            if (lastCommitResult.ExitCode == 0 && long.TryParse(lastCommitResult.Output.Trim(), out var timestamp))
            {
                lastCommitDate = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
            }

            return (commitCount, contributorCount, lastCommitDate);
        }
        catch
        {
            return (0, 0, null);
        }
    }
}
