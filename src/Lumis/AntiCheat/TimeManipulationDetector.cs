using System.Diagnostics;

namespace Lumis;

internal sealed class TimeManipulationDetector
{
    private readonly TimeManipulationSettings settings;
    private readonly Func<double> monotonicSecondsProvider;
    private double windowStartedAt;
    private double accumulatedGameTime;
    private int consecutiveDetections;

    internal TimeManipulationDetector(
        TimeManipulationSettings settings,
        Func<double>? monotonicSecondsProvider = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        this.settings = settings;
        this.monotonicSecondsProvider = monotonicSecondsProvider ?? GetMonotonicSeconds;
        windowStartedAt = this.monotonicSecondsProvider();
    }

    internal bool Observe(float deltaTime)
    {
        if (!settings.Enabled)
            return false;

        if (!float.IsFinite(deltaTime) || deltaTime < 0f)
            return false;

        accumulatedGameTime += deltaTime;

        double now = monotonicSecondsProvider();
        double wallElapsed = now - windowStartedAt;
        if (!double.IsFinite(wallElapsed) || wallElapsed <= 0d)
        {
            ResetWindow(now);
            consecutiveDetections = 0;
            return false;
        }

        if (wallElapsed < settings.ObservationWindow.TotalSeconds)
            return false;

        double ratio = accumulatedGameTime / wallElapsed;
        bool suspicious = double.IsFinite(ratio) && ratio > settings.MaxGameTimeRatio;

        ResetWindow(now);

        if (!suspicious)
        {
            consecutiveDetections = 0;
            return false;
        }

        consecutiveDetections++;
        return consecutiveDetections >= settings.RequiredConsecutiveDetections;
    }

    private void ResetWindow(double now)
    {
        windowStartedAt = now;
        accumulatedGameTime = 0d;
    }

    private static double GetMonotonicSeconds()
    {
        return Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
    }
}
