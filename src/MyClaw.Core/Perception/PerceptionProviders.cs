using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace MyClaw.Core.Perception;

/// <summary>
/// 平台感知 Provider 接口
/// </summary>
public interface IPerceptionProvider
{
    string Name { get; }

    Task<PerceptionSnapshot> CaptureAsync(CancellationToken cancellationToken = default);
}

public static class PerceptionProviderFactory
{
    public static IPerceptionProvider CreateDefault()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsPerceptionProvider();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacOsPerceptionProvider();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxPerceptionProvider();
        }

        return new GenericPerceptionProvider();
    }
}

internal abstract class BasePerceptionProvider : IPerceptionProvider
{
    public abstract string Name { get; }

    protected abstract string Platform { get; }

    protected virtual string CaptureFocusMode() => "unavailable";

    protected virtual string CaptureBattery() => "unavailable";

    protected virtual IEnumerable<string> CaptureNotes(string focusMode, string battery, IReadOnlyList<string> activeApplications) =>
        new[] { "Provider scaffold active; detailed platform signals land in P2-2." };

    public virtual Task<PerceptionSnapshot> CaptureAsync(CancellationToken cancellationToken = default)
    {
        var focusMode = CaptureFocusMode();
        var battery = CaptureBattery();
        var activeApplications = CaptureActiveApplications();
        var notes = CaptureNotes(focusMode, battery, activeApplications)
            .Where(note => !string.IsNullOrWhiteSpace(note))
            .ToList();

        return Task.FromResult(new PerceptionSnapshot
        {
            Platform = Platform,
            Provider = Name,
            PriorityHint = DeterminePriorityHint(focusMode, battery, activeApplications),
            FocusMode = focusMode,
            Battery = battery,
            ActiveApplications = activeApplications,
            Notes = notes,
            CapturedAt = DateTime.UtcNow
        });
    }

    protected virtual List<string> CaptureActiveApplications()
    {
        var processName = Process.GetCurrentProcess().ProcessName;
        return string.IsNullOrWhiteSpace(processName)
            ? new List<string>()
            : new List<string> { processName };
    }

    protected virtual int DeterminePriorityHint(string focusMode, string battery, IReadOnlyList<string> activeApplications)
    {
        var richness = 0;

        if (HasConcreteSignal(focusMode)) richness++;
        if (HasConcreteSignal(battery)) richness++;
        if (activeApplications.Count > 0) richness++;

        return richness switch
        {
            >= 3 => 7,
            2 => 6,
            _ => 5
        };
    }

    protected static bool HasConcreteSignal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.ToLowerInvariant();
        return !normalized.Contains("unavailable", StringComparison.Ordinal)
            && !normalized.Contains("unknown", StringComparison.Ordinal)
            && !normalized.Contains("degraded", StringComparison.Ordinal)
            && !normalized.Contains("pending", StringComparison.Ordinal);
    }

    protected static string? RunCommand(string fileName, string arguments, int timeoutMs = 1500)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            if (!process.WaitForExit(timeoutMs))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                }

                return null;
            }

            var stdout = process.StandardOutput.ReadToEnd().Trim();
            return string.IsNullOrWhiteSpace(stdout) ? null : stdout;
        }
        catch
        {
            return null;
        }
    }
}

internal sealed class WindowsPerceptionProvider : BasePerceptionProvider
{
    public override string Name => "windows";

    protected override string Platform => "Windows";

    protected override string CaptureFocusMode()
    {
        if (SHQueryUserNotificationState(out var state) != 0)
        {
            return "unknown (notification state unavailable)";
        }

        return state switch
        {
            QueryUserNotificationState.AcceptsNotifications => "notifications-enabled",
            QueryUserNotificationState.Busy => "busy",
            QueryUserNotificationState.PresentationMode => "presentation",
            QueryUserNotificationState.QuietTime => "quiet-time",
            QueryUserNotificationState.RunningD3DFullScreen => "fullscreen",
            QueryUserNotificationState.App => "app-controlled",
            QueryUserNotificationState.NotPresent => "not-present",
            _ => "unknown"
        };
    }

    protected override string CaptureBattery()
    {
        if (!GetSystemPowerStatus(out var status))
        {
            return "unknown (power status unavailable)";
        }

        if ((status.BatteryFlag & 128) == 128)
        {
            return "no battery";
        }

        var percent = status.BatteryLifePercent == 255 ? "unknown" : $"{status.BatteryLifePercent}%";
        var state = status.ACLineStatus switch
        {
            1 => "charging",
            0 => "on battery",
            _ => "power state unknown"
        };

        return $"{percent} ({state})";
    }

    protected override List<string> CaptureActiveApplications()
    {
        try
        {
            var handle = GetForegroundWindow();
            if (handle == IntPtr.Zero)
            {
                return base.CaptureActiveApplications();
            }

            var titleBuilder = new StringBuilder(256);
            _ = GetWindowText(handle, titleBuilder, titleBuilder.Capacity);
            _ = GetWindowThreadProcessId(handle, out var processId);
            if (processId == 0)
            {
                return base.CaptureActiveApplications();
            }

            using var process = Process.GetProcessById((int)processId);
            var title = titleBuilder.ToString().Trim();
            var processName = process.ProcessName;
            var description = string.IsNullOrWhiteSpace(title)
                ? processName
                : $"{processName}: {title}";

            return new List<string> { description };
        }
        catch
        {
            return base.CaptureActiveApplications();
        }
    }

    protected override IEnumerable<string> CaptureNotes(string focusMode, string battery, IReadOnlyList<string> activeApplications) =>
        new[] { "Windows provider exposes foreground app, notification state and power status via Win32 APIs." };

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    private enum QueryUserNotificationState
    {
        NotPresent = 1,
        Busy = 2,
        RunningD3DFullScreen = 3,
        PresentationMode = 4,
        AcceptsNotifications = 5,
        QuietTime = 6,
        App = 7
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus systemPowerStatus);

    [DllImport("shell32.dll")]
    private static extern int SHQueryUserNotificationState(out QueryUserNotificationState queryUserNotificationState);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}

internal sealed class MacOsPerceptionProvider : BasePerceptionProvider
{
    private readonly Func<string, string, int, string?> _commandRunner;

    internal MacOsPerceptionProvider(Func<string, string, int, string?>? commandRunner = null)
    {
        _commandRunner = commandRunner ?? RunCommand;
    }

    public override string Name => "macos";

    protected override string Platform => "macOS";

    protected override string CaptureFocusMode()
    {
        foreach (var probe in GetFocusModeProbes())
        {
            var output = _commandRunner(probe.FileName, probe.Arguments, probe.TimeoutMs);
            var parsed = ParseFocusMode(output);
            if (!string.IsNullOrWhiteSpace(parsed))
            {
                return parsed;
            }
        }

        return "unknown (focus mode probe unavailable)";
    }

    protected override string CaptureBattery()
    {
        foreach (var probe in GetBatteryProbes())
        {
            var output = _commandRunner(probe.FileName, probe.Arguments, probe.TimeoutMs);
            var parsed = ParseBattery(output);
            if (!string.IsNullOrWhiteSpace(parsed))
            {
                return parsed;
            }
        }

        return "unknown (battery probe unavailable)";
    }

    protected override List<string> CaptureActiveApplications()
    {
        var appName = _commandRunner(
            "osascript",
            "-e \"tell application \\\"System Events\\\" to get name of first application process whose frontmost is true\"",
            1500);

        if (string.IsNullOrWhiteSpace(appName))
        {
            return base.CaptureActiveApplications();
        }

        var windowTitle = _commandRunner(
            "osascript",
            "-e \"tell application \\\"System Events\\\" to tell (first application process whose frontmost is true) to get name of front window\"",
            1500);

        var appDescription = string.IsNullOrWhiteSpace(windowTitle) || string.Equals(appName, windowTitle, StringComparison.OrdinalIgnoreCase)
            ? appName.Trim()
            : $"{appName.Trim()}: {windowTitle.Trim()}";

        return new List<string> { appDescription };
    }

    protected override IEnumerable<string> CaptureNotes(string focusMode, string battery, IReadOnlyList<string> activeApplications)
    {
        if (HasConcreteSignal(focusMode) && HasConcreteSignal(battery) && activeApplications.Count > 0)
        {
            return new[] { "macOS provider exposes focus state, battery status and frontmost app via native system utilities." };
        }

        return new[] { "macOS provider uses multi-probe command fallbacks; some signals are degraded when host utilities are unavailable." };
    }

    internal static string? ParseFocusMode(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        var normalized = output.Trim();
        if (Regex.IsMatch(normalized, @"(doNotDisturb|enabled|active|focus)[^\n\r]*[=:]\s*(1|true)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            || Regex.IsMatch(normalized, "^(1|true)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return "dnd-enabled";
        }

        if (Regex.IsMatch(normalized, @"(doNotDisturb|enabled|active|focus)[^\n\r]*[=:]\s*(0|false)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            || Regex.IsMatch(normalized, "^(0|false)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return "notifications-enabled";
        }

        if (normalized.Contains("do not disturb", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("focus on", StringComparison.OrdinalIgnoreCase))
        {
            return "dnd-enabled";
        }

        if (normalized.Contains("focus off", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("notifications", StringComparison.OrdinalIgnoreCase))
        {
            return "notifications-enabled";
        }

        return null;
    }

    internal static string? ParseBattery(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        var normalized = output.Trim();
        if (normalized.Contains("no battery", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("battery installed: no", StringComparison.OrdinalIgnoreCase))
        {
            return "no battery";
        }

        var percentMatch = Regex.Match(normalized, "(?<percent>\\d+)%", RegexOptions.CultureInvariant);
        var chargeMatch = Regex.Match(
            normalized,
            "State of Charge \\(%\\)\\s*:\\s*(?<charge>\\d+)",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        var state = normalized.Contains("discharging", StringComparison.OrdinalIgnoreCase)
                ? "on battery"
            : normalized.Contains("charging", StringComparison.OrdinalIgnoreCase)
                ? "charging"
                : normalized.Contains("charged", StringComparison.OrdinalIgnoreCase)
                    ? "charged"
                    : normalized.Contains("ac attached", StringComparison.OrdinalIgnoreCase)
                        ? "charging"
                        : "power state unknown";

        if (percentMatch.Success || chargeMatch.Success)
        {
            var percent = percentMatch.Groups["percent"].Success
                ? percentMatch.Groups["percent"].Value
                : chargeMatch.Groups["charge"].Value;

            return $"{percent}% ({state})";
        }

        return state == "power state unknown" ? null : $"unknown ({state})";
    }

    private static IReadOnlyList<MacOsProbe> GetFocusModeProbes() =>
        new[]
        {
            new MacOsProbe("defaults", "read com.apple.controlcenter FocusModes"),
            new MacOsProbe("defaults", "-currentHost read com.apple.controlcenter FocusModes"),
            new MacOsProbe("defaults", "read com.apple.notificationcenterui doNotDisturb"),
            new MacOsProbe("defaults", "-currentHost read com.apple.notificationcenterui doNotDisturb")
        };

    private static IReadOnlyList<MacOsProbe> GetBatteryProbes() =>
        new[]
        {
            new MacOsProbe("pmset", "-g batt"),
            new MacOsProbe("system_profiler", "SPPowerDataType -detailLevel mini", 2500)
        };

    private readonly record struct MacOsProbe(string FileName, string Arguments, int TimeoutMs = 1500);
}

internal sealed class LinuxPerceptionProvider : BasePerceptionProvider
{
    public override string Name => "linux";

    protected override string Platform => "Linux";

    protected override IEnumerable<string> CaptureNotes(string focusMode, string battery, IReadOnlyList<string> activeApplications) =>
        new[] { "Linux perception provider currently exposes platform-only context." };
}

internal sealed class GenericPerceptionProvider : BasePerceptionProvider
{
    public override string Name => "generic";

    protected override string Platform => RuntimeInformation.OSDescription;

    protected override IEnumerable<string> CaptureNotes(string focusMode, string battery, IReadOnlyList<string> activeApplications) =>
        new[] { "Generic perception provider active; no platform-specific capabilities available yet." };
}