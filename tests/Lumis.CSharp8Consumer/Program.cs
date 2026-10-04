using System;
using Lumis;

internal static class Program
{
    private static void Main()
    {
        var processDetection = new ProcessDetectionSettings
        {
            Enabled = true,
            DetectCheatEngine = true,
            InspectExecutableMetadata = true,
            BlockedProcessNames = new[] { "MyGameTrainer.exe" },
            BlockedExecutablePathFragments = new[] { "tools/trainer" }
        };

        var antiCheat = new AntiCheatSettings
        {
            Enabled = true,
            ProcessDetection = processDetection,
            DebuggerDetection = new DebuggerDetectionSettings
            {
                Enabled = false,
                DetectManagedDebugger = true,
                DetectNativeDebugger = true
            },
            TimeManipulation = new TimeManipulationSettings
            {
                Enabled = true,
                ObservationWindow = TimeSpan.FromSeconds(2),
                MaxGameTimeRatio = 1.75,
                RequiredConsecutiveDetections = 2
            },
            FileIntegrity = new FileIntegritySettings
            {
                Enabled = false,
                CheckOnStartup = true,
                MonitorDuringGame = false
            },
            AssemblyIntegrity = new AssemblyIntegritySettings
            {
                Enabled = false,
                CheckOnStartup = true,
                MonitorDuringGame = true
            },
            MonitorDuringGame = true,
            RuntimeScanInterval = TimeSpan.FromSeconds(2)
        };

        var settings = new GameSettings
        {
            Title = "C# 8 compatibility",
            Width = 960,
            Height = 540,
            TargetFps = 60,
            AntiCheat = antiCheat
        };

        var color = new Color(96, 210, 255);
        var rect = new Rect(0f, 0f, 32f, 32f);
        var secureInt = new SecureInt(100);
        var secureLong = new SecureLong(100L);
        var secureFloat = new SecureFloat(1.5f);
        var secureDouble = new SecureDouble(2.5d);
        byte[] saveKey = new byte[32];

        using (var saveProtector = new SaveDataProtector(saveKey))
        {
            string protectedSave = saveProtector.ProtectString("compile-only");
            Console.WriteLine(protectedSave.Length);
        }

        AntiCheatViolationType violationType = AntiCheatViolationType.MemoryTampering;
        AntiCheatViolationPhase violationPhase = AntiCheatViolationPhase.Runtime;

        Console.WriteLine(settings.Title);
        Console.WriteLine(color.A);
        Console.WriteLine(rect.Width);
        Console.WriteLine(secureInt.Value);
        Console.WriteLine(secureLong.Value);
        Console.WriteLine(secureFloat.Value);
        Console.WriteLine(secureDouble.Value);
        Console.WriteLine(violationType);
        Console.WriteLine(violationPhase);
        Func<string, string> hashFile = FileIntegrityService.ComputeSha256;
        Console.WriteLine(hashFile.Method.Name);
    }
}
