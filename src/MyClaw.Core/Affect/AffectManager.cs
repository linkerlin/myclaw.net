using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyClaw.Core.Affect;

/// <summary>
/// 情感管理器 - 管理统一情感状态，提供恢复和应用功能
/// Affect Manager - manages unified affect state with recovery and application
/// </summary>
public class AffectManager
{
    private readonly string _stateFilePath;
    private readonly AffectState _baseline;
    private AffectState _currentState;
    private readonly object _lock = new();

    /// <summary>
    /// 恢复率 - 每个 Pulse 周期向基线恢复的比例 (默认 10%)
    /// Recovery rate - fraction of recovery toward baseline per pulse
    /// </summary>
    public const double RecoveryRate = 0.1;

    /// <summary>
    /// 当前情感状态
    /// </summary>
    public AffectState CurrentState
    {
        get
        {
            lock (_lock)
            {
                return _currentState.Clone();
            }
        }
    }

    /// <summary>
    /// 当前行为模式
    /// </summary>
    public AffectMode CurrentMode => AffectModeExtensions.DeriveMode(_currentState);

    public AffectManager(string? stateFilePath = null)
    {
        _stateFilePath = stateFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".myclaw",
            "affect.json"
        );
        _baseline = AffectState.CreateDefault();
        _currentState = LoadOrCreateState();
    }

    /// <summary>
    /// 更新情感状态 - 使用平滑混合
    /// Update affect state with smooth blending (momentum)
    /// </summary>
    /// <param name="alertness">新警觉度 (null 保持不变)</param>
    /// <param name="mood">新情绪 (null 保持不变)</param>
    /// <param name="curiosity">新好奇心 (null 保持不变)</param>
    /// <param name="confidence">新信心 (null 保持不变)</param>
    /// <param name="blendFactor">混合因子 (0-1, 1=直接替换)</param>
    public void UpdateAffect(
        double? alertness = null,
        double? mood = null,
        double? curiosity = null,
        double? confidence = null,
        double blendFactor = 0.3)
    {
        lock (_lock)
        {
            if (alertness.HasValue)
            {
                _currentState.Alertness = Blend(_currentState.Alertness, Clamp(alertness.Value, 0, 1), blendFactor);
            }
            if (mood.HasValue)
            {
                _currentState.Mood = Blend(_currentState.Mood, Clamp(mood.Value, -1, 1), blendFactor);
            }
            if (curiosity.HasValue)
            {
                _currentState.Curiosity = Blend(_currentState.Curiosity, Clamp(curiosity.Value, 0, 1), blendFactor);
            }
            if (confidence.HasValue)
            {
                _currentState.Confidence = Blend(_currentState.Confidence, Clamp(confidence.Value, 0, 1), blendFactor);
            }
            _currentState.LastUpdate = DateTime.UtcNow;
        }
        SaveState();
    }

    /// <summary>
    /// 应用痛觉 - 影响所有情感维度
    /// Apply pain stimulus - affects all emotional dimensions
    /// </summary>
    /// <param name="intensity">痛觉强度 (0-1)</param>
    public void ApplyPain(double intensity)
    {
        intensity = Clamp(intensity, 0, 1);
        lock (_lock)
        {
            // 痛觉影响：警觉↑ 情绪↓ 好奇↓ 信心↓
            _currentState.Alertness = Math.Min(1.0, _currentState.Alertness + intensity * 0.5);
            _currentState.Mood = Math.Max(-1.0, _currentState.Mood - intensity * 0.6);
            _currentState.Curiosity = Math.Max(0, _currentState.Curiosity - intensity * 0.4);
            _currentState.Confidence = Math.Max(0, _currentState.Confidence - intensity * 0.3);
            _currentState.LastUpdate = DateTime.UtcNow;
        }
        SaveState();
    }

    /// <summary>
    /// 应用成功 - 提升信心和情绪
    /// Apply success stimulus - boosts confidence and mood
    /// </summary>
    /// <param name="magnitude">成功程度 (0-1)</param>
    public void ApplySuccess(double magnitude)
    {
        magnitude = Clamp(magnitude, 0, 1);
        lock (_lock)
        {
            _currentState.Confidence = Math.Min(1.0, _currentState.Confidence + magnitude * 0.2);
            _currentState.Mood = Math.Min(1.0, _currentState.Mood + magnitude * 0.3);
            _currentState.LastUpdate = DateTime.UtcNow;
        }
        SaveState();
    }

    /// <summary>
    /// 脉冲恢复 - 每个 Pulse 周期调用，情感向基线恢复
    /// Pulse recovery - emotions drift back to baseline over time (homeostasis)
    /// </summary>
    public void PulseRecovery()
    {
        lock (_lock)
        {
            _currentState.Alertness += (_baseline.Alertness - _currentState.Alertness) * RecoveryRate;
            _currentState.Mood += (_baseline.Mood - _currentState.Mood) * RecoveryRate;
            _currentState.Curiosity += (_baseline.Curiosity - _currentState.Curiosity) * RecoveryRate;
            _currentState.Confidence += (_baseline.Confidence - _currentState.Confidence) * RecoveryRate;
            _currentState.LastUpdate = DateTime.UtcNow;
        }
        SaveState();
    }

    /// <summary>
    /// 强制恢复到基线 - 立即重置
    /// Force recovery to baseline - immediate reset
    /// </summary>
    public void RecoverToBaseline()
    {
        lock (_lock)
        {
            _currentState = _baseline.Clone();
            _currentState.LastUpdate = DateTime.UtcNow;
        }
        SaveState();
    }

    /// <summary>
    /// 格式化为上下文字符串
    /// Format as context string for ACE
    /// </summary>
    public string FormatForContext()
    {
        var mode = CurrentMode;
        var (emoji, label) = mode.GetDisplayInfo();
        return $"## AFFECT: {emoji} {label} (alertness: {_currentState.Alertness:F1}, mood: {_currentState.Mood:F1}, curiosity: {_currentState.Curiosity:F1}, confidence: {_currentState.Confidence:F1})";
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    private static double Blend(double current, double target, double factor)
    {
        return current + (target - current) * factor;
    }

    private AffectState LoadOrCreateState()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                var json = File.ReadAllText(_stateFilePath);
                var state = JsonSerializer.Deserialize<AffectState>(json);
                if (state != null)
                {
                    return state;
                }
            }
        }
        catch
        {
            // 加载失败，使用默认值
        }
        return AffectState.CreateDefault();
    }

    private void SaveState()
    {
        try
        {
            var directory = Path.GetDirectoryName(_stateFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_currentState, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_stateFilePath, json);
        }
        catch
        {
            // 保存失败，忽略
        }
    }
}
