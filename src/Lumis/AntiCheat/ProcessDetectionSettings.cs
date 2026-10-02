namespace Lumis;

/// <summary>Configures running-process checks performed before the native game window is created.</summary>
public sealed record ProcessDetectionSettings
{
    /// <summary>Gets whether running-process detection is enabled.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Gets whether known Cheat Engine process names are blocked.</summary>
    public bool DetectCheatEngine { get; init; } = true;

    /// <summary>
    /// Gets additional process names to block. Matching is case-insensitive and an optional
    /// <c>.exe</c> suffix is ignored.
    /// </summary>
    public IReadOnlyList<string> BlockedProcessNames { get; init; } = Array.Empty<string>();

    internal void Validate()
    {
        ArgumentNullException.ThrowIfNull(BlockedProcessNames);

        foreach (string? processName in BlockedProcessNames)
        {
            if (string.IsNullOrWhiteSpace(processName))
                throw new ArgumentException("Blocked process names cannot be null, empty, or whitespace.", nameof(BlockedProcessNames));
            if (processName.Contains('\0'))
                throw new ArgumentException("Blocked process names cannot contain a null character.", nameof(BlockedProcessNames));
        }
    }
}
