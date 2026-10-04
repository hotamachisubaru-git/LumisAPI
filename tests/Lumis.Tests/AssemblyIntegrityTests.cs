using System.Text;

namespace Lumis.Tests;

public sealed class AssemblyIntegrityTests
{
    [Fact]
    public void SnapshotBaselineDetectsLaterModification()
    {
        string path = CreateTempFile("assembly-v1");

        try
        {
            var settings = new AssemblyIntegritySettings
            {
                Enabled = true,
                CheckOnStartup = true,
                MonitorDuringGame = true
            };
            var monitor = new AssemblyIntegrityMonitor(settings, () => path);

            Assert.Null(monitor.Verify());

            File.WriteAllText(path, "assembly-v2", Encoding.UTF8);
            AssemblyIntegrityFailure failure =
                Assert.IsType<AssemblyIntegrityFailure>(monitor.Verify());

            Assert.Equal(Path.GetFullPath(path), failure.FilePath);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TrustedExpectedHashDetectsModificationAtFirstCheck()
    {
        string path = CreateTempFile("trusted-build");

        try
        {
            string trustedHash = FileIntegrityService.ComputeSha256(path);
            File.WriteAllText(path, "tampered-build", Encoding.UTF8);

            var settings = new AssemblyIntegritySettings
            {
                Enabled = true,
                ExpectedEntryAssemblySha256 = trustedHash
            };
            var monitor = new AssemblyIntegrityMonitor(settings, () => path);

            Assert.IsType<AssemblyIntegrityFailure>(monitor.Verify());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void UnavailableAssemblyLocationCanBeRequired()
    {
        var monitor = new AssemblyIntegrityMonitor(
            new AssemblyIntegritySettings
            {
                Enabled = true,
                FailIfUnavailable = true
            },
            () => null);

        Assert.IsType<AssemblyIntegrityFailure>(monitor.Verify());
    }

    [Fact]
    public void AntiCheatServiceReportsAssemblyIntegrityViolation()
    {
        string path = CreateTempFile("baseline");

        try
        {
            double now = 0d;
            var settings = new AntiCheatSettings
            {
                Enabled = true,
                RuntimeScanInterval = TimeSpan.FromSeconds(1),
                ProcessDetection = new ProcessDetectionSettings { Enabled = false },
                DebuggerDetection = new DebuggerDetectionSettings { Enabled = false },
                TimeManipulation = new TimeManipulationSettings { Enabled = false },
                AssemblyIntegrity = new AssemblyIntegritySettings
                {
                    Enabled = true,
                    CheckOnStartup = true,
                    MonitorDuringGame = true
                }
            };
            var antiCheat = new AntiCheatService(
                settings,
                () => Array.Empty<ProcessSnapshot>(),
                () => false,
                () => now,
                () => path);

            antiCheat.CheckStartup();
            File.WriteAllText(path, "changed", Encoding.UTF8);

            now = 1d;
            AntiCheatException failure =
                Assert.Throws<AntiCheatException>(() => antiCheat.Update(1f));

            Assert.Equal(AntiCheatViolationType.AssemblyIntegrity, failure.Type);
            Assert.Equal(AntiCheatViolationPhase.Runtime, failure.Phase);
            Assert.Equal(Path.GetFullPath(path), failure.FilePath);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateTempFile(string contents)
    {
        string path = Path.Combine(Path.GetTempPath(), "LumisAssembly-" + Guid.NewGuid().ToString("N") + ".tmp");
        File.WriteAllText(path, contents, Encoding.UTF8);
        return path;
    }
}
