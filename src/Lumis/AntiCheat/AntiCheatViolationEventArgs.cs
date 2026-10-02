namespace Lumis;

/// <summary>Provides information about a detected anti-cheat violation.</summary>
public sealed class AntiCheatViolationEventArgs : EventArgs
{
    internal AntiCheatViolationEventArgs(AntiCheatViolationType type, string message, string? processName)
    {
        Type = type;
        Message = message;
        ProcessName = processName;
    }

    /// <summary>Gets the type of detected violation.</summary>
    public AntiCheatViolationType Type { get; }

    /// <summary>Gets a human-readable description of the violation.</summary>
    public string Message { get; }

    /// <summary>Gets the detected process name when the violation is process-related.</summary>
    public string? ProcessName { get; }
}
