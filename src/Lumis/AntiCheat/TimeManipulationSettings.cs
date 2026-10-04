namespace Lumis;

/// <summary>Configures runtime detection of suspicious game-time acceleration.</summary>
public sealed class TimeManipulationSettings
{
    /// <summary>Gets or sets whether time-manipulation detection is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the minimum wall-clock observation window used for each comparison.</summary>
    public TimeSpan ObservationWindow { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the maximum allowed ratio of accumulated game delta time to monotonic wall time.
    /// Values above this ratio count as suspicious.
    /// </summary>
    public double MaxGameTimeRatio { get; set; } = 1.75d;

    /// <summary>Gets or sets how many suspicious observation windows must occur consecutively.</summary>
    public int RequiredConsecutiveDetections { get; set; } = 2;

    internal void Validate()
    {
        if (ObservationWindow <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(ObservationWindow), "ObservationWindow must be greater than zero.");
        if (!double.IsFinite(MaxGameTimeRatio) || MaxGameTimeRatio <= 1d)
            throw new ArgumentOutOfRangeException(nameof(MaxGameTimeRatio), "MaxGameTimeRatio must be finite and greater than 1.");
        if (RequiredConsecutiveDetections < 1)
            throw new ArgumentOutOfRangeException(nameof(RequiredConsecutiveDetections), "RequiredConsecutiveDetections must be at least 1.");
    }
}
