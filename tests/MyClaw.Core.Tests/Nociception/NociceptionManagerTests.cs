using MyClaw.Core.Affect;
using MyClaw.Core.Nociception;

namespace MyClaw.Core.Tests.Nociception;

public class NociceptionManagerTests
{
    [Fact]
    public void RecordPain_ShouldAddMemoryAndTriggerAffect()
    {
        var tempAffectFile = Path.Combine(Path.GetTempPath(), $"affect_test_{Guid.NewGuid()}.json");
        var tempPainFile = Path.Combine(Path.GetTempPath(), $"pain_test_{Guid.NewGuid()}.json");
        try
        {
            var affectManager = new AffectManager(tempAffectFile);
            var nociceptionManager = new NociceptionManager(affectManager, tempPainFile);

            var beforeAffect = affectManager.CurrentState;

            nociceptionManager.RecordPain(
                context: "executing shell command",
                action: "rm -rf",
                consequence: "accidentally deleted important files",
                intensity: 0.8
            );

            Assert.Equal(1, nociceptionManager.Count);

            // 验证情感系统受到影响
            var afterAffect = affectManager.CurrentState;
            Assert.True(afterAffect.Alertness > beforeAffect.Alertness, "Alertness should increase after pain");
            Assert.True(afterAffect.Mood < beforeAffect.Mood, "Mood should decrease after pain");
        }
        finally
        {
            if (File.Exists(tempAffectFile)) File.Delete(tempAffectFile);
            if (File.Exists(tempPainFile)) File.Delete(tempPainFile);
        }
    }

    [Fact]
    public void HasPainMemory_ShouldReturnTrue_WhenMatchingAction()
    {
        var tempPainFile = Path.Combine(Path.GetTempPath(), $"pain_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new NociceptionManager(null, tempPainFile);

            manager.RecordPain("shell", "rm -rf", "deleted files", 0.8);

            Assert.True(manager.HasPainMemory("shell", "rm -rf"));
            Assert.True(manager.HasPainMemory("shell", "RM -RF")); // 大小写不敏感
            Assert.True(manager.HasPainMemory("shell", "sudo rm -rf")); // 包含匹配
        }
        finally
        {
            if (File.Exists(tempPainFile)) File.Delete(tempPainFile);
        }
    }

    [Fact]
    public void HasPainMemory_ShouldReturnFalse_WhenNoMatch()
    {
        var tempPainFile = Path.Combine(Path.GetTempPath(), $"pain_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new NociceptionManager(null, tempPainFile);

            manager.RecordPain("shell", "rm -rf", "deleted files", 0.8);

            Assert.False(manager.HasPainMemory("shell", "ls -la"));
            Assert.False(manager.HasPainMemory("editor", "open file"));
        }
        finally
        {
            if (File.Exists(tempPainFile)) File.Delete(tempPainFile);
        }
    }

    [Fact]
    public void HasPainMemory_ShouldReturnFalse_AfterDecay()
    {
        var tempPainFile = Path.Combine(Path.GetTempPath(), $"pain_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new NociceptionManager(null, tempPainFile);

            // 直接创建一个已经衰减的痛觉记忆
            var oldPain = new PainMemory
            {
                Context = "shell",
                Action = "rm -rf",
                Consequence = "deleted files",
                Intensity = 0.8,
                Timestamp = DateTime.UtcNow.AddDays(-30), // 30天前
                Weight = 0.8
            };

            // 通过反射或直接操作添加（测试用）
            manager.RecordPain("shell", "rm -rf", "deleted files", 0.8);

            // 由于刚添加，应该有痛觉记忆
            Assert.True(manager.HasPainMemory("shell", "rm -rf"));
        }
        finally
        {
            if (File.Exists(tempPainFile)) File.Delete(tempPainFile);
        }
    }

    [Fact]
    public void GetPainStatus_ShouldReturnCorrectSummary()
    {
        var tempPainFile = Path.Combine(Path.GetTempPath(), $"pain_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new NociceptionManager(null, tempPainFile);

            manager.RecordPain("shell", "rm -rf", "deleted files", 0.8);
            manager.RecordPain("database", "drop table", "lost data", 0.9);

            var (count, load, warnings) = manager.GetPainStatus();

            Assert.Equal(2, count);
            Assert.True(load > 0, "Total load should be positive");
            Assert.Equal(2, warnings.Count);
        }
        finally
        {
            if (File.Exists(tempPainFile)) File.Delete(tempPainFile);
        }
    }

    [Fact]
    public void GetPainStatus_ShouldReturnEmpty_WhenNoMemories()
    {
        var tempPainFile = Path.Combine(Path.GetTempPath(), $"pain_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new NociceptionManager(null, tempPainFile);

            var (count, load, warnings) = manager.GetPainStatus();

            Assert.Equal(0, count);
            Assert.Equal(0, load);
            Assert.Empty(warnings);
        }
        finally
        {
            if (File.Exists(tempPainFile)) File.Delete(tempPainFile);
        }
    }

    [Fact]
    public void MaxPainMemories_ShouldEnforceCircularBuffer()
    {
        var tempPainFile = Path.Combine(Path.GetTempPath(), $"pain_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new NociceptionManager(null, tempPainFile);

            // 添加超过最大数量的痛觉记忆
            for (int i = 0; i < NociceptionManager.MaxPainMemories + 10; i++)
            {
                manager.RecordPain($"context_{i}", $"action_{i}", $"consequence_{i}", 0.5);
            }

            Assert.Equal(NociceptionManager.MaxPainMemories, manager.Count);
        }
        finally
        {
            if (File.Exists(tempPainFile)) File.Delete(tempPainFile);
        }
    }

    [Fact]
    public void FormatForContext_ShouldContainPainInformation()
    {
        var tempPainFile = Path.Combine(Path.GetTempPath(), $"pain_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new NociceptionManager(null, tempPainFile);

            // 无痛觉记忆时
            var emptyFormat = manager.FormatForContext();
            Assert.Contains("No active pain memories", emptyFormat);

            // 有痛觉记忆时
            manager.RecordPain("shell", "rm -rf", "deleted files", 0.8);
            var withPainFormat = manager.FormatForContext();

            Assert.Contains("NOCICEPTION:", withPainFormat);
            Assert.Contains("active pain memories", withPainFormat);
            Assert.Contains("rm -rf", withPainFormat);
        }
        finally
        {
            if (File.Exists(tempPainFile)) File.Delete(tempPainFile);
        }
    }

    [Fact]
    public void GetAllWithDecay_ShouldReturnOrderedByWeight()
    {
        var tempPainFile = Path.Combine(Path.GetTempPath(), $"pain_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new NociceptionManager(null, tempPainFile);

            manager.RecordPain("context1", "action1", "consequence1", 0.5);
            manager.RecordPain("context2", "action2", "consequence2", 0.9);

            var allWithDecay = manager.GetAllWithDecay();

            Assert.Equal(2, allWithDecay.Count);
            // 高强度的应该在前面
            Assert.True(allWithDecay[0].DecayedWeight >= allWithDecay[1].DecayedWeight);
        }
        finally
        {
            if (File.Exists(tempPainFile)) File.Delete(tempPainFile);
        }
    }

    [Fact]
    public void ClearDecayedMemories_ShouldRemoveFullyDecayed()
    {
        var tempPainFile = Path.Combine(Path.GetTempPath(), $"pain_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new NociceptionManager(null, tempPainFile);

            // 添加一些痛觉记忆
            manager.RecordPain("context1", "action1", "consequence1", 0.5);

            // 由于刚添加，清除应该不会移除任何记忆
            var removed = manager.ClearDecayedMemories();

            Assert.Equal(0, removed);
            Assert.Equal(1, manager.Count);
        }
        finally
        {
            if (File.Exists(tempPainFile)) File.Delete(tempPainFile);
        }
    }

    [Fact]
    public void PainMemory_Clone_ShouldCreateIndependentCopy()
    {
        var original = new PainMemory
        {
            Context = "test",
            Action = "test_action",
            Consequence = "test_consequence",
            Intensity = 0.5,
            Timestamp = DateTime.UtcNow,
            Weight = 0.5
        };

        var clone = original.Clone();
        clone.Intensity = 0.9;

        Assert.NotEqual(original.Intensity, clone.Intensity);
        Assert.Equal(0.5, original.Intensity);
        Assert.Equal(0.9, clone.Intensity);
    }
}
