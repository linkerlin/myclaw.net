using MyClaw.Core.Affect;

namespace MyClaw.Core.Tests.Affect;

public class AffectManagerTests
{
    [Fact]
    public void CreateDefault_ShouldReturnValidBaseline()
    {
        var state = AffectState.CreateDefault();

        Assert.Equal(0.3, state.Alertness);
        Assert.Equal(0.5, state.Mood);
        Assert.Equal(0.5, state.Curiosity);
        Assert.Equal(0.7, state.Confidence);
        Assert.True(state.LastUpdate <= DateTime.UtcNow);
    }

    [Fact]
    public void Clone_ShouldCreateIndependentCopy()
    {
        var original = AffectState.CreateDefault();
        var clone = original.Clone();

        clone.Alertness = 0.9;

        Assert.NotEqual(original.Alertness, clone.Alertness);
        Assert.Equal(0.3, original.Alertness);
        Assert.Equal(0.9, clone.Alertness);
    }

    [Fact]
    public void DeriveMode_WithHighAlertnessLowConfidence_ShouldReturnCautious()
    {
        var state = new AffectState
        {
            Alertness = 0.8,
            Confidence = 0.3,
            Mood = 0.5,
            Curiosity = 0.5
        };

        var mode = AffectModeExtensions.DeriveMode(state);

        Assert.Equal(AffectMode.Cautious, mode);
    }

    [Fact]
    public void DeriveMode_WithHighCuriosityLowAlertness_ShouldReturnExploration()
    {
        var state = new AffectState
        {
            Alertness = 0.3,
            Confidence = 0.6,
            Mood = 0.5,
            Curiosity = 0.8
        };

        var mode = AffectModeExtensions.DeriveMode(state);

        Assert.Equal(AffectMode.Exploration, mode);
    }

    [Fact]
    public void DeriveMode_WithHighConfidenceModerateAlertness_ShouldReturnExecution()
    {
        var state = new AffectState
        {
            Alertness = 0.4,
            Confidence = 0.8,
            Mood = 0.5,
            Curiosity = 0.5
        };

        var mode = AffectModeExtensions.DeriveMode(state);

        Assert.Equal(AffectMode.Execution, mode);
    }

    [Fact]
    public void DeriveMode_WithLowAllMetrics_ShouldReturnRest()
    {
        var state = new AffectState
        {
            Alertness = 0.1,
            Confidence = 0.2,
            Mood = 0.1,
            Curiosity = 0.1
        };

        var mode = AffectModeExtensions.DeriveMode(state);

        Assert.Equal(AffectMode.Rest, mode);
    }

    [Fact]
    public void GetDisplayInfo_ShouldReturnEmojiAndLabel()
    {
        var (emoji, label) = AffectMode.Exploration.GetDisplayInfo();

        Assert.False(string.IsNullOrEmpty(emoji));
        Assert.Equal("Exploration", label);
    }

    [Fact]
    public void AffectManager_UpdateAffect_ShouldBlendSmoothly()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"affect_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new AffectManager(tempFile);

            // 更新警觉度从 0.3 到 0.9，混合因子 0.5
            manager.UpdateAffect(alertness: 0.9, blendFactor: 0.5);

            // 预期: 0.3 + (0.9 - 0.3) * 0.5 = 0.6
            Assert.Equal(0.6, manager.CurrentState.Alertness, precision: 2);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void AffectManager_ApplyPain_ShouldIncreaseAlertnessAndDecreaseOthers()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"affect_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new AffectManager(tempFile);
            var before = manager.CurrentState;

            manager.ApplyPain(0.5);

            var after = manager.CurrentState;

            Assert.True(after.Alertness > before.Alertness, "Alertness should increase");
            Assert.True(after.Mood < before.Mood, "Mood should decrease");
            Assert.True(after.Curiosity < before.Curiosity, "Curiosity should decrease");
            Assert.True(after.Confidence < before.Confidence, "Confidence should decrease");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void AffectManager_ApplySuccess_ShouldIncreaseConfidenceAndMood()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"affect_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new AffectManager(tempFile);
            var before = manager.CurrentState;

            manager.ApplySuccess(0.5);

            var after = manager.CurrentState;

            Assert.True(after.Confidence > before.Confidence, "Confidence should increase");
            Assert.True(after.Mood > before.Mood, "Mood should increase");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void AffectManager_PulseRecovery_ShouldDriftTowardBaseline()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"affect_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new AffectManager(tempFile);

            // 设置极端值
            manager.UpdateAffect(alertness: 0.9, mood: -0.8, curiosity: 0.1, confidence: 0.1, blendFactor: 1.0);

            var before = manager.CurrentState;

            // 执行脉冲恢复
            manager.PulseRecovery();

            var after = manager.CurrentState;

            // 警觉度应该下降 (向 0.3 靠近)
            Assert.True(after.Alertness < before.Alertness, "Alertness should decrease toward baseline");
            // 情绪应该上升 (向 0.5 靠近)
            Assert.True(after.Mood > before.Mood, "Mood should increase toward baseline");
            // 好奇心应该上升 (向 0.5 靠近)
            Assert.True(after.Curiosity > before.Curiosity, "Curiosity should increase toward baseline");
            // 信心应该上升 (向 0.7 靠近)
            Assert.True(after.Confidence > before.Confidence, "Confidence should increase toward baseline");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void AffectManager_RecoverToBaseline_ShouldResetToDefault()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"affect_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new AffectManager(tempFile);

            // 设置极端值
            manager.UpdateAffect(alertness: 0.9, mood: -0.8, curiosity: 0.1, confidence: 0.1, blendFactor: 1.0);

            // 强制恢复
            manager.RecoverToBaseline();

            var state = manager.CurrentState;

            Assert.Equal(0.3, state.Alertness);
            Assert.Equal(0.5, state.Mood);
            Assert.Equal(0.5, state.Curiosity);
            Assert.Equal(0.7, state.Confidence);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void AffectManager_FormatForContext_ShouldContainModeAndMetrics()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"affect_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new AffectManager(tempFile);
            var formatted = manager.FormatForContext();

            Assert.Contains("AFFECT:", formatted);
            Assert.Contains("alertness:", formatted);
            Assert.Contains("mood:", formatted);
            Assert.Contains("curiosity:", formatted);
            Assert.Contains("confidence:", formatted);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void AffectManager_ValuesShouldBeClamped()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"affect_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new AffectManager(tempFile);

            // 尝试设置超出范围的值
            manager.UpdateAffect(alertness: 2.0, mood: 2.0, curiosity: 2.0, confidence: 2.0, blendFactor: 1.0);

            var state = manager.CurrentState;

            Assert.True(state.Alertness <= 1.0, "Alertness should be clamped to max 1.0");
            Assert.True(state.Mood <= 1.0, "Mood should be clamped to max 1.0");
            Assert.True(state.Curiosity <= 1.0, "Curiosity should be clamped to max 1.0");
            Assert.True(state.Confidence <= 1.0, "Confidence should be clamped to max 1.0");

            // 测试负值
            manager.UpdateAffect(alertness: -1.0, mood: -2.0, curiosity: -1.0, confidence: -1.0, blendFactor: 1.0);

            state = manager.CurrentState;

            Assert.True(state.Alertness >= 0, "Alertness should be clamped to min 0");
            Assert.True(state.Mood >= -1.0, "Mood should be clamped to min -1");
            Assert.True(state.Curiosity >= 0, "Curiosity should be clamped to min 0");
            Assert.True(state.Confidence >= 0, "Confidence should be clamped to min 0");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
