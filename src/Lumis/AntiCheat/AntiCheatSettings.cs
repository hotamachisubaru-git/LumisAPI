namespace Lumis;

/// <summary>Configures optional anti-cheat checks performed by <see cref="LumisGame"/>.</summary>
/// <remarks>
/// Anti-cheat checks are disabled by default so existing games keep their current behavior.
/// Normal setters keep this configuration consumable by C# 8 callers.
/// </remarks>
public sealed class AntiCheatSettings
{
    /// <summary>Gets or sets whether anti-cheat checks are enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the process-detection settings.</summary>
    public ProcessDetectionSettings ProcessDetection { get; set; } = new ProcessDetectionSettings();

    /// <summary>Gets or sets the debugger-detection settings.</summary>
    public DebuggerDetectionSettings DebuggerDetection { get; set; } = new DebuggerDetectionSettings();

    /// <summary>Gets or sets the runtime time-manipulation detection settings.</summary>
    public TimeManipulationSettings TimeManipulation { get; set; } = new TimeManipulationSettings();

    /// <summary>Gets or sets SHA-256 file-integrity verification settings.</summary>
    public FileIntegritySettings FileIntegrity { get; set; } = new FileIntegritySettings();

    /// <summary>Gets or sets whether enabled checks are repeated while the game is running.</summary>
    public bool MonitorDuringGame { get; set; } = true;

    /// <summary>Gets or sets the interval between runtime anti-cheat scans.</summary>
    public TimeSpan RuntimeScanInterval { get; set; } = TimeSpan.FromSeconds(2);

    internal void Validate()
    {
        ArgumentNullException.ThrowIfNull(ProcessDetection);
        ArgumentNullException.ThrowIfNull(DebuggerDetection);
        ArgumentNullException.ThrowIfNull(TimeManipulation);
        ArgumentNullException.ThrowIfNull(FileIntegrity);
        ProcessDetection.Validate();
        TimeManipulation.Validate();

        if (RuntimeScanInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(RuntimeScanInterval), "RuntimeScanInterval must be greater than zero.");
    }
}
