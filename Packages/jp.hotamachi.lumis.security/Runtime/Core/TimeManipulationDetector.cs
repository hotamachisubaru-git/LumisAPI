#nullable enable
using System;
using System.Diagnostics;

namespace Lumis
{
    internal sealed class TimeManipulationDetector
    {
        private readonly TimeManipulationSettings settings;
        private readonly Func<double> clock;
        private double windowStart;
        private double gameTime;
        private int consecutive;
        internal TimeManipulationDetector(TimeManipulationSettings settings, Func<double>? monotonicSecondsProvider = null)
        {
            SecurityCompat.NotNull(settings, nameof(settings));
            this.settings = settings;
            clock = monotonicSecondsProvider ?? (() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
            Reset();
        }
        internal void Reset() { windowStart = clock(); gameTime = 0; consecutive = 0; }
        internal bool Observe(float deltaTime)
        {
            if (!settings.Enabled) return false;
            if (!SecurityCompat.IsFinite(deltaTime) || deltaTime < 0) return false;
            double now = clock();
            double elapsed = now - windowStart;
            if (!SecurityCompat.IsFinite(elapsed) || elapsed < 0) { Reset(); return false; }
            gameTime += deltaTime;
            if (elapsed < settings.ObservationWindow.TotalSeconds) return false;
            bool suspicious = gameTime / elapsed > settings.MaxGameTimeRatio;
            gameTime = 0; windowStart = now;
            consecutive = suspicious ? consecutive + 1 : 0;
            return consecutive >= settings.RequiredConsecutiveDetections;
        }
    }
}
