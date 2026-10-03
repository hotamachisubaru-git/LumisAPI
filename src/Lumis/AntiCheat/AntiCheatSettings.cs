namespace Lumis;

/// <summary>Configures optional anti-cheat checks performed by <see cref="LumisGame"/>.</summary>
/// <remarks>
/// Anti-cheat checks are disabled by default so existing games keep their current startup behavior.
/// Normal setters are used to keep this configuration consumable by C# 8 callers.
/// </remarks>
public sealed class AntiCheatSettings
{
    /// <summary>Gets or sets whether anti-cheat checks are enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the process-detection settings used during startup.</summary>
    public ProcessDetectionSettings ProcessDetection { get; set; } = new ProcessDetectionSettings();

    internal void Validate()
    {
        ArgumentNullException.ThrowIfNull(ProcessDetection);
        ProcessDetection.Validate();
    }
}
