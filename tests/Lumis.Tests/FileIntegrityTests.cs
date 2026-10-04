using System.Text;

namespace Lumis.Tests;

public sealed class FileIntegrityTests
{
    [Fact]
    public void TrustedHashVerifiesSuccessfully()
    {
        string path = CreateTempFile("trusted");

        try
        {
            string hash = FileIntegrityService.ComputeSha256(path);
            var service = new FileIntegrityService(new FileIntegritySettings { Enabled = true });
            service.RegisterFile(path, hash);

            Assert.Null(service.Verify());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ModifiedFileIsDetected()
    {
        string path = CreateTempFile("original");

        try
        {
            var service = new FileIntegrityService(new FileIntegritySettings { Enabled = true });
            service.RegisterCurrentFile(path);
            File.WriteAllText(path, "modified", Encoding.UTF8);

            FileIntegrityFailure failure = Assert.IsType<FileIntegrityFailure>(service.Verify());

            Assert.Equal(Path.GetFullPath(path), failure.FilePath);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MissingFileIsDetectedByDefault()
    {
        string path = CreateTempFile("delete-me");
        var service = new FileIntegrityService(new FileIntegritySettings { Enabled = true });
        service.RegisterCurrentFile(path);
        File.Delete(path);

        FileIntegrityFailure failure = Assert.IsType<FileIntegrityFailure>(service.Verify());

        Assert.Equal(Path.GetFullPath(path), failure.FilePath);
    }

    [Fact]
    public void DirectorySnapshotRegistersMatchingFiles()
    {
        string directory = Path.Combine(Path.GetTempPath(), "LumisIntegrity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(Path.Combine(directory, "a.dat"), "A");
            File.WriteAllText(Path.Combine(directory, "b.dat"), "B");
            File.WriteAllText(Path.Combine(directory, "ignored.txt"), "C");

            var service = new FileIntegrityService(new FileIntegritySettings { Enabled = true });
            int count = service.RegisterDirectorySnapshot(directory, "*.dat", recursive: false);

            Assert.Equal(2, count);
            Assert.Equal(2, service.RegisteredFileCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void AntiCheatStartupReportsFileIntegrityViolation()
    {
        string path = CreateTempFile("before");

        try
        {
            var settings = new AntiCheatSettings
            {
                Enabled = true,
                ProcessDetection = new ProcessDetectionSettings { Enabled = false },
                DebuggerDetection = new DebuggerDetectionSettings { Enabled = false },
                FileIntegrity = new FileIntegritySettings
                {
                    Enabled = true,
                    CheckOnStartup = true
                }
            };
            var antiCheat = new AntiCheatService(settings, () => Array.Empty<ProcessSnapshot>());
            antiCheat.FileIntegrity.RegisterCurrentFile(path);
            File.WriteAllText(path, "after", Encoding.UTF8);

            AntiCheatException failure = Assert.Throws<AntiCheatException>(antiCheat.CheckStartup);

            Assert.Equal(AntiCheatViolationType.FileIntegrity, failure.Type);
            Assert.Equal(AntiCheatViolationPhase.Startup, failure.Phase);
            Assert.Equal(Path.GetFullPath(path), failure.FilePath);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateTempFile(string contents)
    {
        string path = Path.Combine(Path.GetTempPath(), "LumisIntegrity-" + Guid.NewGuid().ToString("N") + ".tmp");
        File.WriteAllText(path, contents, Encoding.UTF8);
        return path;
    }
}
