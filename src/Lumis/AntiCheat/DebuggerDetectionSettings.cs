namespace Lumis;

/// <summary>Configures debugger detection for anti-cheat startup and runtime checks.</summary>
public sealed class DebuggerDetectionSettings
{
    /// <summary>
    /// Gets or sets whether debugger detection is enabled.
    /// Disabled by default so normal development and debugging are unaffected.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets whether the managed debugger state is checked.</summary>
    public bool DetectManagedDebugger { get; set; } = true;

    /// <summary>
    /// Gets or sets whether platform-native debugger signals are checked when supported.
    /// Windows uses IsDebuggerPresent and Linux checks TracerPid; other platforms fall back
    /// to the managed debugger signal.
    /// </summary>
    public bool DetectNativeDebugger { get; set; } = true;
}
