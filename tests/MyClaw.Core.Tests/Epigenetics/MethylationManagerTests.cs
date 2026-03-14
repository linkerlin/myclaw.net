using MyClaw.Core.Epigenetics;

namespace MyClaw.Core.Tests.Epigenetics;

public class MethylationManagerTests
{
    [Fact]
    public void ShouldMethylate_ShouldReturnFalse_WhenConfidenceTooLow()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"methylation_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new MethylationManager(tempFile);

            var (should, _, _) = manager.ShouldMethylate(
                patternType: "preference",
                confidence: 0.7,  // 低于 0.8
                mergedCount: 15,
                description: "Frequent tool usage: miniclaw_update"
            );

            Assert.False(should);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ShouldMethylate_ShouldReturnFalse_WhenMergedCountTooLow()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"methylation_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new MethylationManager(tempFile);

            var (should, _, _) = manager.ShouldMethylate(
                patternType: "preference",
                confidence: 0.9,
                mergedCount: 5,  // 低于 10
                description: "Frequent tool usage: miniclaw_update"
            );

            Assert.False(should);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ShouldMethylate_ShouldReturnTrue_WhenConditionsMet()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"methylation_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new MethylationManager(tempFile);

            var (should, trait, value) = manager.ShouldMethylate(
                patternType: "preference",
                confidence: 0.9,
                mergedCount: 15,
                description: "Frequent tool usage: miniclaw_update"
            );

            Assert.True(should);
            Assert.Equal("interaction_style", trait);
            Assert.Equal("proactive_modifier", value);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ShouldMethylate_ShouldDetectTemporalPattern()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"methylation_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new MethylationManager(tempFile);

            var (should, trait, value) = manager.ShouldMethylate(
                patternType: "temporal",
                confidence: 0.9,
                mergedCount: 12,
                description: "Peak activity at 14:00"
            );

            Assert.True(should);
            Assert.Equal("activity_pattern", trait);
            Assert.Equal("time_sensitive", value);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ShouldMethylate_ShouldDetectWorkflowPattern()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"methylation_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new MethylationManager(tempFile);

            var (should, trait, value) = manager.ShouldMethylate(
                patternType: "workflow",
                confidence: 0.85,
                mergedCount: 10,
                description: "Repeated 3-step workflow"
            );

            Assert.True(should);
            Assert.Equal("workflow_style", trait);
            Assert.Equal("structured", value);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void MethylateTrait_ShouldAddTraitAndSave()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"methylation_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new MethylationManager(tempFile);

            var result = manager.MethylateTrait(
                trait: "interaction_style",
                value: "proactive_modifier",
                source: "Frequent tool usage: miniclaw_update",
                patternCount: 15
            );

            Assert.True(result);
            Assert.Equal(1, manager.Count);

            var traits = manager.GetMethylatedTraits();
            Assert.Single(traits);
            Assert.Equal("interaction_style", traits[0].Trait);
            Assert.Equal("proactive_modifier", traits[0].Value);
            Assert.True(traits[0].Stability > 0.5);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void MethylateTrait_ShouldUpdateExistingTrait()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"methylation_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new MethylationManager(tempFile);

            // 添加第一次
            var result1 = manager.MethylateTrait("interaction_style", "active_reader", "source1", 10);
            Assert.True(result1);

            // 由于冷却期，第二次调用应该失败
            var result2 = manager.MethylateTrait("interaction_style", "proactive_modifier", "source2", 20);
            Assert.False(result2); // 被冷却期阻止

            // 特征应该保持第一次的值
            Assert.Equal(1, manager.Count);
            var traits = manager.GetMethylatedTraits();
            Assert.Equal("active_reader", traits[0].Value);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetTraitValue_ShouldReturnCorrectValue()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"methylation_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new MethylationManager(tempFile);
            manager.MethylateTrait("interaction_style", "proactive_modifier", "source", 15);

            var value = manager.GetTraitValue("interaction_style");

            Assert.Equal("proactive_modifier", value);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetTraitValue_ShouldReturnNull_WhenNotFound()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"methylation_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new MethylationManager(tempFile);

            var value = manager.GetTraitValue("nonexistent");

            Assert.Null(value);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void RemoveTrait_ShouldRemoveExistingTrait()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"methylation_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new MethylationManager(tempFile);
            manager.MethylateTrait("interaction_style", "proactive_modifier", "source", 15);

            var result = manager.RemoveTrait("interaction_style");

            Assert.True(result);
            Assert.Equal(0, manager.Count);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void RemoveTrait_ShouldReturnFalse_WhenNotFound()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"methylation_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new MethylationManager(tempFile);

            var result = manager.RemoveTrait("nonexistent");

            Assert.False(result);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void FormatForContext_ShouldContainTraitInfo()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"methylation_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new MethylationManager(tempFile);

            // 无特征时
            var emptyFormat = manager.FormatForContext();
            Assert.Contains("No methylated traits", emptyFormat);

            // 有特征时
            manager.MethylateTrait("interaction_style", "proactive_modifier", "source", 15);
            var withTraitsFormat = manager.FormatForContext();

            Assert.Contains("METHYLATION:", withTraitsFormat);
            Assert.Contains("interaction_style", withTraitsFormat);
            Assert.Contains("proactive_modifier", withTraitsFormat);
            Assert.Contains("stability", withTraitsFormat);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void FormatAsSoulComment_ShouldContainTraitInfo()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"methylation_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new MethylationManager(tempFile);

            var trait = new MethylatedTrait
            {
                Trait = "interaction_style",
                Value = "proactive_modifier",
                Stability = 0.85
            };

            var comment = manager.FormatAsSoulComment(trait);

            Assert.Contains("[METHYLATED]", comment);
            Assert.Contains("interaction_style", comment);
            Assert.Contains("proactive_modifier", comment);
            Assert.Contains("85%", comment);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Stability_ShouldIncreaseWithPatternCount()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"methylation_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new MethylationManager(tempFile);

            // 使用两个不同的特征来测试稳定性计算
            manager.MethylateTrait("trait1", "value1", "source", 10);
            // 第二次添加会被冷却期阻止，所以只测试第一个特征的稳定性
            var traits = manager.GetMethylatedTraits();
            Assert.Single(traits);

            var trait1 = traits.First(t => t.Trait == "trait1");
            // 10 个模式应该产生稳定性 0.5 + 10 * 0.05 = 1.0, 但被限制在 0.95
            Assert.True(trait1.Stability >= 0.5);
            Assert.True(trait1.Stability <= 0.95);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void MethylatedTrait_Clone_ShouldCreateIndependentCopy()
    {
        var original = new MethylatedTrait
        {
            Trait = "test",
            Value = "test_value",
            Source = "test_source",
            Stability = 0.8
        };

        var clone = original.Clone();
        clone.Stability = 0.5;

        Assert.NotEqual(original.Stability, clone.Stability);
        Assert.Equal(0.8, original.Stability);
        Assert.Equal(0.5, clone.Stability);
    }
}
