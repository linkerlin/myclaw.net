using MyClaw.Core.Evolution;

namespace MyClaw.Core.Tests.Evolution;

public class SignalDetectorTests
{
    private readonly SignalDetector _detector = new();

    #region User Preference Tests

    [Theory]
    [InlineData("我喜欢使用VS Code")]
    [InlineData("I like the dark theme")]
    [InlineData("don't use TypeScript")]
    [InlineData("以后请使用中文回答")]
    public void DetectSignals_UserPreferenceIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.UserPreference);
    }

    [Theory]
    [InlineData("How do I configure this?")]
    [InlineData("What's the weather today")]
    [InlineData("Tell me a joke")]
    public void DetectSignals_NoPreferenceIndicators_ShouldNotReturnUserPreference(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.DoesNotContain(signals, s => s.SignalType == EvolutionSignal.UserPreference);
    }

    #endregion

    #region Personality Correction Tests

    [Theory]
    [InlineData("别那么严肃")]
    [InlineData("more lively")]
    [InlineData("你是一个")]
    [InlineData("change personality")]
    public void DetectSignals_PersonalityIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.PersonalityCorrection);
    }

    #endregion

    #region Environment Config Tests

    [Theory]
    [InlineData("项目用的是")]
    [InlineData("server IP")]
    [InlineData("API key")]
    public void DetectSignals_ConfigIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.EnvironmentConfig);
    }

    #endregion

    #region Tool Experience Tests

    [Theory]
    [InlineData("这个工具的参数")]
    [InlineData("踩坑记录")]
    public void DetectSignals_ToolExperienceIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.ToolExperience);
    }

    #endregion

    #region Identity Change Tests

    [Theory]
    [InlineData("叫你自己Claw")]
    [InlineData("your name is")]
    [InlineData("改名")]
    public void DetectSignals_IdentityIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.IdentityChange);
    }

    #endregion

    #region Workflow Learned Tests

    [Theory]
    [InlineData("最好的实践是")]
    [InlineData("以后都按这个流程")]
    [InlineData("标准化")]
    public void DetectSignals_WorkflowIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.WorkflowLearned);
    }

    #endregion

    #region Important Fact Tests

    [Theory]
    [InlineData("重要")]
    [InlineData("记住这个")]
    [InlineData("别忘了")]
    [InlineData("mark this")]
    public void DetectSignals_ImportantFactIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.ImportantFact);
    }

    #endregion

    #region Distinct Signals Tests

    [Fact]
    public void DetectSignals_WithMultipleMatchesSameType_ShouldReturnDistinctSignals()
    {
        // "我喜欢" 和 "别那么严肃" 是不同信号类型
        var input = "我喜欢用Python，别那么严肃";
        var signals = _detector.DetectSignals(input);

        // 应该检测到两种不同信号
        var signalTypes = signals.Select(s => s.SignalType).ToList();
        Assert.Contains(EvolutionSignal.UserPreference, signalTypes);
        Assert.Contains(EvolutionSignal.PersonalityCorrection, signalTypes);
    }

    [Fact]
    public void DetectSignals_EmptyInput_ShouldReturnEmptyList()
    {
        var signals = _detector.DetectSignals("");

        Assert.Empty(signals);
    }

    [Fact]
    public void DetectSignals_NullInput_ShouldReturnEmptyList()
    {
        var signals = _detector.DetectSignals(null!);

        Assert.Empty(signals);
    }

    #endregion

    #region Signal Properties Tests

    [Fact]
    public void DetectSignals_ShouldSetCorrectProperties()
    {
        var signals = _detector.DetectSignals("我喜欢用 dark theme");

        var signal = signals.First(s => s.SignalType == EvolutionSignal.UserPreference);
        Assert.Equal("USER.md", signal.TargetFile);
        Assert.Equal("miniclaw_update", signal.SuggestedTool);
        Assert.True(signal.Confidence > 0);
        Assert.NotEmpty(signal.MatchedContent);
    }

    #endregion

    #region GenerateEvolutionAdvice Tests

    [Fact]
    public void GenerateEvolutionAdvice_WithSignals_ShouldReturnAdvice()
    {
        var signals = _detector.DetectSignals("我喜欢用 dark theme");
        var advice = _detector.GenerateEvolutionAdvice(signals);

        Assert.Contains("进化信号", advice);
        Assert.Contains("USER.md", advice);
    }

    [Fact]
    public void GenerateEvolutionAdvice_NoSignals_ShouldReturnEmpty()
    {
        var advice = _detector.GenerateEvolutionAdvice(new List<DetectedSignal>());

        Assert.Empty(advice);
    }

    #endregion

    #region Phase 2.2 New Signal Types Tests

    [Theory]
    [InlineData("谢谢帮助！")]
    [InlineData("Great job!")]
    [InlineData("太棒了")]
    [InlineData("做得好")]
    public void DetectSignals_PositiveFeedbackIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.PositiveFeedback);
    }

    [Theory]
    [InlineData("不对，这是错的")]
    [InlineData("Wrong answer")]
    [InlineData("我不满意")]
    public void DetectSignals_NegativeFeedbackIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.NegativeFeedback);
    }

    [Theory]
    [InlineData("遇到一个 error")]
    [InlineData("Failed to execute")]
    [InlineData("出现 exception")]
    public void DetectSignals_ErrorPatternIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.ErrorPattern);
    }

    [Theory]
    [InlineData("应该有个工具自动完成这个")]
    [InlineData("创建技能来处理这个")]
    public void DetectSignals_SkillSuggestionIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.SkillSuggestion);
    }

    [Theory]
    [InlineData("我想知道为什么")]
    [InlineData("I wonder how it works")]
    [InlineData("探索一下")]
    public void DetectSignals_CuriosityTriggerIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.CuriosityTrigger);
    }

    [Theory]
    [InlineData("又遇到这个问题了")]
    [InlineData("Again!")]
    [InlineData("每次都是这样")]
    public void DetectSignals_RepetitionPatternIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.RepetitionPattern);
    }

    [Theory]
    [InlineData("完成了这个任务")]
    [InlineData("Achieved the goal")]
    [InlineData("达成里程碑")]
    public void DetectSignals_MilestoneIndicators_ShouldReturnSignal(string input)
    {
        var signals = _detector.DetectSignals(input);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.Milestone);
    }

    #endregion

    #region Confidence Calculation Tests

    [Fact]
    public void DetectSignals_WithVeryImportantKeyword_ShouldHaveHigherConfidence()
    {
        var normalSignal = _detector.DetectSignals("我喜欢这个").First(s => s.SignalType == EvolutionSignal.UserPreference);
        var boostedSignal = _detector.DetectSignals("一定要记住我喜欢这个").First(s => s.SignalType == EvolutionSignal.UserPreference);

        Assert.True(boostedSignal.Confidence >= normalSignal.Confidence);
    }

    [Fact]
    public void DetectSignals_ConfidenceShouldBeCapped()
    {
        var signals = _detector.DetectSignals("一定要非常重要请记住我喜欢这个配置");

        foreach (var signal in signals)
        {
            Assert.True(signal.Confidence <= 0.98);
        }
    }

    #endregion

    #region DetectRepetitionPatterns Tests

    [Fact]
    public void DetectRepetitionPatterns_WithRepeatedWords_ShouldReturnSignal()
    {
        // 使用相同的词重复3次以上
        var inputs = new List<string>
        {
            "configuration settings need update",
            "update configuration settings now",
            "check configuration settings please",
            "configuration settings again"
        };

        var signals = _detector.DetectRepetitionPatterns(inputs);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.RepetitionPattern);
    }

    [Fact]
    public void DetectRepetitionPatterns_WithInsufficientInputs_ShouldReturnEmpty()
    {
        var inputs = new List<string> { "只有一个输入" };

        var signals = _detector.DetectRepetitionPatterns(inputs);

        Assert.Empty(signals);
    }

    #endregion

    #region DetectToolSequencePatterns Tests

    [Fact]
    public void DetectToolSequencePatterns_WithRepeatedSequence_ShouldReturnSignal()
    {
        var content = @"
调用 miniclaw_read 读取配置
调用 miniclaw_update 更新
调用 miniclaw_exec 执行
调用 miniclaw_read 再次读取
调用 miniclaw_update 更新
调用 miniclaw_exec 执行
调用 miniclaw_read 读取
";

        var signals = _detector.DetectToolSequencePatterns(content);

        Assert.Contains(signals, s => s.SignalType == EvolutionSignal.WorkflowLearned);
    }

    [Fact]
    public void DetectToolSequencePatterns_WithInsufficientTools_ShouldReturnEmpty()
    {
        var content = "调用 miniclaw_read 一次";

        var signals = _detector.DetectToolSequencePatterns(content);

        Assert.Empty(signals);
    }

    #endregion
}
