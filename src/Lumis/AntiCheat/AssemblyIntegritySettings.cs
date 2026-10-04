namespace Lumis;

/// <summary>Configures SHA-256 integrity verification of the entry assembly file.</summary>
public sealed class AssemblyIntegritySettings
{
    /// <summary>Gets or sets whether entry-assembly integrity verification is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets whether the entry assembly is checked during startup.</summary>
    public bool CheckOnStartup { get; set; } = true;

    /// <summary>Gets or sets whether the entry assembly is re-checked during runtime scans.</summary>
    public bool MonitorDuringGame { get; set; } = true;

    /// <summary>
    /// Gets or sets a trusted build-time SHA-256 hash for the entry assembly.
    /// When omitted, Lumis snapshots the current entry assembly on the first check and can only
    /// detect changes that occur after that baseline is captured.
    /// </summary>
    public string? ExpectedEntryAssemblySha256 { get; set; }

    /// <summary>
    /// Gets or sets whether missing/unavailable entry-assembly file information is a violation.
    /// Keep this disabled for deployment modes such as single-file publishing where Location may be unavailable.
    /// </summary>
    public bool FailIfUnavailable { get; set; }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ExpectedEntryAssemblySha256))
            return;

        try
        {
            byte[] hash = Convert.FromHexString(ExpectedEntryAssemblySha256);
            if (hash.Length != 32)
                throw new FormatException();
        }
        catch (FormatException ex)
        {
            throw new ArgumentException(
                "ExpectedEntryAssemblySha256 must contain exactly 64 hexadecimal characters.",
                nameof(ExpectedEntryAssemblySha256),
                ex);
        }
    }
}
