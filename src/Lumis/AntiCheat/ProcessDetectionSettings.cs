namespace Lumis;

/// <summary>Configures running-process checks performed by the anti-cheat service.</summary>
public sealed class ProcessDetectionSettings
{
    /// <summary>Gets or sets whether running-process detection is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets whether known Cheat Engine identifiers are blocked.</summary>
    public bool DetectCheatEngine { get; set; } = true;

    /// <summary>
    /// Gets or sets whether executable path and version metadata are inspected when available.
    /// This can detect some renamed Cheat Engine executables whose embedded metadata remains unchanged.
    /// </summary>
    public bool InspectExecutableMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets additional process names to block. Matching is case-insensitive and an optional
    /// <c>.exe</c> suffix is ignored.
    /// </summary>
    public IReadOnlyList<string> BlockedProcessNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets case-insensitive executable-path fragments to block when the process path is available.
    /// </summary>
    public IReadOnlyList<string> BlockedExecutablePathFragments { get; set; } = Array.Empty<string>();

    internal void Validate()
    {
        ArgumentNullException.ThrowIfNull(BlockedProcessNames);
        ArgumentNullException.ThrowIfNull(BlockedExecutablePathFragments);

        ValidateEntries(BlockedProcessNames, nameof(BlockedProcessNames));
        ValidateEntries(BlockedExecutablePathFragments, nameof(BlockedExecutablePathFragments));
    }

    private static void ValidateEntries(IEnumerable<string> entries, string parameterName)
    {
        foreach (string? entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry))
                throw new ArgumentException("Anti-cheat matching entries cannot be null, empty, or whitespace.", parameterName);
            if (entry.Contains('\0'))
                throw new ArgumentException("Anti-cheat matching entries cannot contain a null character.", parameterName);
        }
    }
}
