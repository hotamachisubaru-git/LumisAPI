namespace Lumis.Tests;

public sealed class GameSettingsTests
{
    [Fact]
    public void DefaultSettingsAreValid()
    {
        new GameSettings().Validate();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    public void FrameRateLimitsAreInclusive(int framesPerSecond)
    {
        new GameSettings { Width = 1, Height = 1, TargetFps = framesPerSecond }.Validate();
    }

    [Theory]
    [MemberData(nameof(InvalidSettings))]
    public void InvalidNativeWindowSettingsAreRejected(GameSettings settings, string parameter)
    {
        var failure = Assert.ThrowsAny<ArgumentException>(() => settings.Validate());

        Assert.Equal(parameter, failure.ParamName);
    }

    [Fact]
    public void NullAntiCheatSettingsAreRejected()
    {
        var failure = Assert.Throws<ArgumentNullException>(
            () => new GameSettings { AntiCheat = null! }.Validate());

        Assert.Equal(nameof(GameSettings.AntiCheat), failure.ParamName);
    }

    [Fact]
    public void NullProcessDetectionSettingsAreRejected()
    {
        var failure = Assert.Throws<ArgumentNullException>(
            () => new GameSettings
            {
                AntiCheat = new AntiCheatSettings { ProcessDetection = null! }
            }.Validate());

        Assert.Equal(nameof(AntiCheatSettings.ProcessDetection), failure.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad\0name")]
    public void InvalidBlockedProcessNamesAreRejected(string processName)
    {
        var failure = Assert.Throws<ArgumentException>(
            () => new GameSettings
            {
                AntiCheat = new AntiCheatSettings
                {
                    ProcessDetection = new ProcessDetectionSettings
                    {
                        BlockedProcessNames = [processName]
                    }
                }
            }.Validate());

        Assert.Equal(nameof(ProcessDetectionSettings.BlockedProcessNames), failure.ParamName);
    }

    [Fact]
    public void InvalidRuntimeScanIntervalIsRejected()
    {
        var failure = Assert.Throws<ArgumentOutOfRangeException>(
            () => new GameSettings
            {
                AntiCheat = new AntiCheatSettings
                {
                    RuntimeScanInterval = TimeSpan.Zero
                }
            }.Validate());

        Assert.Equal(nameof(AntiCheatSettings.RuntimeScanInterval), failure.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad\0path")]
    public void InvalidBlockedPathFragmentsAreRejected(string fragment)
    {
        var failure = Assert.Throws<ArgumentException>(
            () => new GameSettings
            {
                AntiCheat = new AntiCheatSettings
                {
                    ProcessDetection = new ProcessDetectionSettings
                    {
                        BlockedExecutablePathFragments = new[] { fragment }
                    }
                }
            }.Validate());

        Assert.Equal(nameof(ProcessDetectionSettings.BlockedExecutablePathFragments), failure.ParamName);
    }

    public static IEnumerable<object[]> InvalidSettings()
    {
        yield return [new GameSettings { Width = 0 }, nameof(GameSettings.Width)];
        yield return [new GameSettings { Width = -1 }, nameof(GameSettings.Width)];
        yield return [new GameSettings { Height = 0 }, nameof(GameSettings.Height)];
        yield return [new GameSettings { Height = -1 }, nameof(GameSettings.Height)];
        yield return [new GameSettings { TargetFps = 0 }, nameof(GameSettings.TargetFps)];
        yield return [new GameSettings { TargetFps = 1001 }, nameof(GameSettings.TargetFps)];
        yield return [new GameSettings { Title = null! }, nameof(GameSettings.Title)];
        yield return [new GameSettings { Title = "" }, nameof(GameSettings.Title)];
        yield return [new GameSettings { Title = " \t" }, nameof(GameSettings.Title)];
        yield return [new GameSettings { Title = "game\0ignored" }, nameof(GameSettings.Title)];
    }
}
