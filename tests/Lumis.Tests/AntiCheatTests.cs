namespace Lumis.Tests;

public sealed class AntiCheatTests
{
    [Theory]
    [InlineData("cheatengine")]
    [InlineData("CheatEngine.exe")]
    [InlineData("cheatengine-x86_64")]
    [InlineData("cheatengine-i386.exe")]
    [InlineData("Cheat Engine 7.5")]
    public void CheatEngineVariantsAreDetected(string processName)
    {
        var settings = new ProcessDetectionSettings();

        string? detected = ProcessDetector.FindBlockedProcess([processName], settings);

        Assert.Equal(processName, detected);
    }

    [Theory]
    [InlineData("cheatengineer")]
    [InlineData("cheatengines")]
    [InlineData("my-cheatengine")]
    public void SimilarProcessNamesAreNotTreatedAsCheatEngine(string processName)
    {
        var settings = new ProcessDetectionSettings();

        string? detected = ProcessDetector.FindBlockedProcess([processName], settings);

        Assert.Null(detected);
    }

    [Fact]
    public void CustomBlockedProcessNamesIgnoreCaseAndExeSuffix()
    {
        var settings = new ProcessDetectionSettings
        {
            DetectCheatEngine = false,
            BlockedProcessNames = ["MyGameTrainer.exe"]
        };

        string? detected = ProcessDetector.FindBlockedProcess(["mygametrainer"], settings);

        Assert.Equal("mygametrainer", detected);
    }

    [Fact]
    public void DisablingCheatEngineDetectionAllowsCheatEngineName()
    {
        var settings = new ProcessDetectionSettings
        {
            DetectCheatEngine = false
        };

        string? detected = ProcessDetector.FindBlockedProcess(["cheatengine-x86_64"], settings);

        Assert.Null(detected);
    }

    [Fact]
    public void DisabledAntiCheatDoesNotEnumerateProcesses()
    {
        bool enumerated = false;
        var service = new AntiCheatService(
            new AntiCheatSettings { Enabled = false },
            () =>
            {
                enumerated = true;
                return ["cheatengine"];
            });

        service.CheckStartup();

        Assert.False(enumerated);
    }

    [Fact]
    public void BlockedProcessRaisesEventAndPreventsStartup()
    {
        var service = new AntiCheatService(
            new AntiCheatSettings { Enabled = true },
            () => ["CheatEngine.exe"]);

        AntiCheatViolationEventArgs? reported = null;
        service.ViolationDetected += (_, violation) => reported = violation;

        var failure = Assert.Throws<AntiCheatException>(service.CheckStartup);

        Assert.NotNull(reported);
        Assert.Equal(AntiCheatViolationType.BlockedProcess, reported.Type);
        Assert.Equal("CheatEngine.exe", reported.ProcessName);
        Assert.Equal(AntiCheatViolationType.BlockedProcess, failure.Type);
        Assert.Equal("CheatEngine.exe", failure.ProcessName);
    }
}
