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
        AntiCheatViolationType violationType = AntiCheatViolationType.BlockedProcess;
        AntiCheatViolationPhase violationPhase = AntiCheatViolationPhase.Runtime;

        Console.WriteLine(settings.Title);
        Console.WriteLine(color.A);
        Console.WriteLine(rect.Width);
        Console.WriteLine(violationType);
        Console.WriteLine(violationPhase);
    }
}
