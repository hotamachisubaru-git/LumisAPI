namespace Lumis;

/// <summary>Provides information about a detected anti-cheat violation.</summary>
public sealed class AntiCheatViolationEventArgs : EventArgs
{
    internal AntiCheatViolationEventArgs(
        AntiCheatViolationType type,
        AntiCheatViolationPhase phase,
        string message,
        string? processName = null,
        int? processId = null,
        string? executablePath = null)
    {
        Type = type;
        Phase = phase;
        Message = message;
        ProcessName = processName;
        ProcessId = processId;
        ExecutablePath = executablePath;
    }

    /// <summary>Gets the type of detected violation.</summary>
    public AntiCheatViolationType Type { get; }

    /// <summary>Gets whether the violation occurred during startup or runtime monitoring.</summary>
    public AntiCheatViolationPhase Phase { get; }

    /// <summary>Gets a human-readable description of the violation.</summary>
    public string Message { get; }

    /// <summary>Gets the detected process name when the violation is process-related.</summary>
    public string? ProcessName { get; }

    /// <summary>Gets the detected process ID when available.</summary>
    public int? ProcessId { get; }

    /// <summary>Gets the executable path when it could be read on the current platform.</summary>
    public string? ExecutablePath { get; }
}
