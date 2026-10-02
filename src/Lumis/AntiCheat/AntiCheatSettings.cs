namespace Lumis;

/// <summary>Configures optional anti-cheat checks performed by <see cref="LumisGame"/>.</summary>
/// <remarks>Anti-cheat checks are disabled by default so existing games keep their current startup behavior.</remarks>
public sealed record AntiCheatSettings
{
    /// <summary>Gets whether anti-cheat checks are enabled.</summary>
    public bool Enabled { get; init; }

    /// <summary>Gets the process-detection settings used during startup.</summary>
    public ProcessDetectionSettings ProcessDetection { get; init; } = new();

    internal void Validate()
    {
        ArgumentNullException.ThrowIfNull(ProcessDetection);
        ProcessDetection.Validate();
    }
}
