namespace Lumis.Tests;

public sealed class AntiCheatTests
{
    [Theory]
    [InlineData("cheatengine")]
    [InlineData("CheatEngine.exe")]
    [InlineData("cheatengine-x86_64")]
    [InlineData("cheatengine-i386.exe")]
    [InlineData("cheatengine-x86_64-SSE4-AVX2.exe")]
    [InlineData("Cheat Engine 7.5")]
    public void CheatEngineVariantsAreDetected(string processName)
    {
        var settings = new ProcessDetectionSettings();

        ProcessSnapshot? detected = ProcessDetector.FindBlockedProcess(
            new[] { new ProcessSnapshot(10, processName) },
            settings);

        Assert.NotNull(detected);
        Assert.Equal(processName, detected.Name);
    }

    [Theory]
    [InlineData("cheatengineer")]
    [InlineData("cheatengines")]
    [InlineData("my-cheatengine")]
    public void SimilarProcessNamesAreNotTreatedAsCheatEngine(string processName)
    {
        var settings = new ProcessDetectionSettings();

        ProcessSnapshot? detected = ProcessDetector.FindBlockedProcess(
            new[] { new ProcessSnapshot(10, processName) },
            settings);

        Assert.Null(detected);
    }

    [Fact]
    public void CustomBlockedProcessNamesIgnoreCaseAndExeSuffix()
    {
        var settings = new ProcessDetectionSettings
        {
            DetectCheatEngine = false,
            BlockedProcessNames = new[] { "MyGameTrainer.exe" }
        };

        ProcessSnapshot? detected = ProcessDetector.FindBlockedProcess(
            new[] { new ProcessSnapshot(20, "mygametrainer") },
            settings);

        Assert.NotNull(detected);
        Assert.Equal("mygametrainer", detected.Name);
    }

    [Fact]
    public void CustomExecutablePathFragmentsAreDetected()
    {
        var settings = new ProcessDetectionSettings
        {
            DetectCheatEngine = false,
            BlockedExecutablePathFragments = new[] { "tools/trainer" }
        };

        ProcessSnapshot? detected = ProcessDetector.FindBlockedProcess(
            new[] { new ProcessSnapshot(30, "renamed", "/home/user/tools/trainer/renamed") },
            settings);

        Assert.NotNull(detected);
        Assert.Equal(30, detected.Id);
    }

    [Fact]
    public void RenamedCheatEngineIsDetectedByVersionMetadata()
    {
        var settings = new ProcessDetectionSettings();

        ProcessSnapshot? detected = ProcessDetector.FindBlockedProcess(
            new[]
            {
                new ProcessSnapshot(
                    40,
                    "notepad",
                    @"C:\Tools\renamed.exe",
                    "Cheat Engine",
                    "Cheat Engine",
                    "cheatengine-x86_64.exe")
            },
            settings);

        Assert.NotNull(detected);
        Assert.Equal("notepad", detected.Name);
    }

    [Fact]
    public void RenamedCheatEngineIsDetectedByInstallPath()
    {
        var settings = new ProcessDetectionSettings();

        ProcessSnapshot? detected = ProcessDetector.FindBlockedProcess(
            new[]
            {
                new ProcessSnapshot(
                    41,
                    "renamed",
                    @"C:\Program Files\Cheat Engine 7.5\renamed.exe")
            },
            settings);

        Assert.NotNull(detected);
    }

    [Fact]
    public void DisablingMetadataInspectionAllowsRenamedCheatEngine()
    {
        var settings = new ProcessDetectionSettings
        {
            InspectExecutableMetadata = false
        };

        ProcessSnapshot? detected = ProcessDetector.FindBlockedProcess(
            new[]
            {
                new ProcessSnapshot(
                    42,
                    "renamed",
                    @"C:\Program Files\Cheat Engine 7.5\renamed.exe",
                    "Cheat Engine",
                    "Cheat Engine",
                    "cheatengine-x86_64.exe")
            },
            settings);

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
                return new[] { new ProcessSnapshot(50, "cheatengine") };
            });

        service.CheckStartup();

        Assert.False(enumerated);
    }

    [Fact]
    public void BlockedProcessRaisesEventAndPreventsStartup()
    {
        var service = new AntiCheatService(
            new AntiCheatSettings { Enabled = true },
            () => new[] { new ProcessSnapshot(60, "CheatEngine.exe", @"C:\CE\CheatEngine.exe") });

        AntiCheatViolationEventArgs? reported = null;
        service.ViolationDetected += (_, violation) => reported = violation;

        var failure = Assert.Throws<AntiCheatException>(service.CheckStartup);

        AntiCheatViolationEventArgs violation =
            Assert.IsType<AntiCheatViolationEventArgs>(reported);
        Assert.Equal(AntiCheatViolationType.BlockedProcess, violation.Type);
        Assert.Equal(AntiCheatViolationPhase.Startup, violation.Phase);
        Assert.Equal("CheatEngine.exe", violation.ProcessName);
        Assert.Equal(60, violation.ProcessId);
        Assert.Equal(@"C:\CE\CheatEngine.exe", violation.ExecutablePath);
        Assert.Equal(AntiCheatViolationType.BlockedProcess, failure.Type);
        Assert.Equal(AntiCheatViolationPhase.Startup, failure.Phase);
    }

    [Fact]
    public void EnabledDebuggerDetectionPreventsStartup()
    {
        bool enumerated = false;
        var settings = new AntiCheatSettings
        {
            Enabled = true,
            ProcessDetection = new ProcessDetectionSettings { Enabled = false },
            DebuggerDetection = new DebuggerDetectionSettings { Enabled = true }
        };
        var service = new AntiCheatService(
            settings,
            () =>
            {
                enumerated = true;
                return Array.Empty<ProcessSnapshot>();
            },
            () => true);

        var failure = Assert.Throws<AntiCheatException>(service.CheckStartup);

        Assert.Equal(AntiCheatViolationType.DebuggerAttached, failure.Type);
        Assert.Equal(AntiCheatViolationPhase.Startup, failure.Phase);
        Assert.False(enumerated);
    }

    [Fact]
    public void RuntimeMonitoringWaitsForConfiguredInterval()
    {
        int scans = 0;
        var settings = new AntiCheatSettings
        {
            Enabled = true,
            MonitorDuringGame = true,
            RuntimeScanInterval = TimeSpan.FromSeconds(1),
            DebuggerDetection = new DebuggerDetectionSettings { Enabled = false }
        };
        var service = new AntiCheatService(
            settings,
            () =>
            {
                scans++;
                return scans == 1
                    ? Array.Empty<ProcessSnapshot>()
                    : new[] { new ProcessSnapshot(70, "cheatengine-x86_64") };
            });

        service.Update(0.5f);
        Assert.Equal(0, scans);

        service.Update(0.5f);
        Assert.Equal(1, scans);

        service.Update(0.5f);
        Assert.Equal(1, scans);

        var failure = Assert.Throws<AntiCheatException>(() => service.Update(0.5f));
        Assert.Equal(AntiCheatViolationPhase.Runtime, failure.Phase);
        Assert.Equal(2, scans);
    }

    [Fact]
    public void RuntimeMonitoringCanBeDisabled()
    {
        int scans = 0;
        var settings = new AntiCheatSettings
        {
            Enabled = true,
            MonitorDuringGame = false
        };
        var service = new AntiCheatService(
            settings,
            () =>
            {
                scans++;
                return new[] { new ProcessSnapshot(80, "cheatengine") };
            });

        service.Update(10f);

        Assert.Equal(0, scans);
    }
}
