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
