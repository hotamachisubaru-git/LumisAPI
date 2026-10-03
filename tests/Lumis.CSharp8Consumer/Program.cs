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
            BlockedProcessNames = new[] { "MyGameTrainer.exe" }
        };

        var antiCheat = new AntiCheatSettings
        {
            Enabled = true,
            ProcessDetection = processDetection
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

        Console.WriteLine(settings.Title);
        Console.WriteLine(color.A);
        Console.WriteLine(rect.Width);
    }
}
