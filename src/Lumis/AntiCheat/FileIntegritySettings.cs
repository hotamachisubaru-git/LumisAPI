namespace Lumis;

/// <summary>Configures SHA-256 integrity verification for registered files.</summary>
public sealed class FileIntegritySettings
{
    /// <summary>Gets or sets whether registered file integrity is checked.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets whether registered files are verified before the native window opens.</summary>
    public bool CheckOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets whether registered files are re-verified during runtime anti-cheat scans.
    /// Disabled by default because hashing large files repeatedly can be expensive.
    /// </summary>
    public bool MonitorDuringGame { get; set; }

    /// <summary>Gets or sets whether a registered file disappearing is treated as a violation.</summary>
    public bool FailOnMissingFile { get; set; } = true;
}
