namespace Lumis.Tests;

public sealed class TimeManipulationTests
{
    [Fact]
    public void NormalGameTimeDoesNotTriggerDetection()
    {
        double now = 0d;
        var settings = new TimeManipulationSettings
        {
            ObservationWindow = TimeSpan.FromSeconds(1),
            MaxGameTimeRatio = 1.5d,
            RequiredConsecutiveDetections = 2
        };
        var detector = new TimeManipulationDetector(settings, () => now);

        now = 1d;
        Assert.False(detector.Observe(1f));

        now = 2d;
        Assert.False(detector.Observe(1f));
    }

    [Fact]
    public void RepeatedAcceleratedGameTimeTriggersDetection()
    {
        double now = 0d;
        var settings = new TimeManipulationSettings
        {
            ObservationWindow = TimeSpan.FromSeconds(1),
            MaxGameTimeRatio = 1.5d,
            RequiredConsecutiveDetections = 2
        };
        var detector = new TimeManipulationDetector(settings, () => now);

        now = 1d;
        Assert.False(detector.Observe(2f));

        now = 2d;
        Assert.True(detector.Observe(2f));
    }

    [Fact]
    public void SingleSuspiciousWindowIsClearedByNormalTiming()
    {
        double now = 0d;
        var settings = new TimeManipulationSettings
        {
            ObservationWindow = TimeSpan.FromSeconds(1),
            MaxGameTimeRatio = 1.5d,
            RequiredConsecutiveDetections = 2
        };
        var detector = new TimeManipulationDetector(settings, () => now);

        now = 1d;
        Assert.False(detector.Observe(2f));

        now = 2d;
        Assert.False(detector.Observe(1f));

        now = 3d;
        Assert.False(detector.Observe(2f));
    }

    [Fact]
    public void AntiCheatServiceRaisesRuntimeTimeManipulationViolation()
    {
        double now = 0d;
        var settings = new AntiCheatSettings
        {
            Enabled = true,
            MonitorDuringGame = true,
            RuntimeScanInterval = TimeSpan.FromSeconds(100),
            ProcessDetection = new ProcessDetectionSettings { Enabled = false },
            DebuggerDetection = new DebuggerDetectionSettings { Enabled = false },
            TimeManipulation = new TimeManipulationSettings
            {
                ObservationWindow = TimeSpan.FromSeconds(1),
                MaxGameTimeRatio = 1.5d,
                RequiredConsecutiveDetections = 2
            }
        };

        var service = new AntiCheatService(
            settings,
            () => Array.Empty<ProcessSnapshot>(),
            () => false,
            () => now);

        now = 1d;
        service.Update(2f);

        now = 2d;
        AntiCheatException failure =
            Assert.Throws<AntiCheatException>(() => service.Update(2f));

        Assert.Equal(AntiCheatViolationType.TimeManipulation, failure.Type);
        Assert.Equal(AntiCheatViolationPhase.Runtime, failure.Phase);
    }
}
