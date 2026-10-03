namespace Lumis;

/// <summary>Thrown when an enabled anti-cheat check rejects startup or stops a running game.</summary>
public sealed class AntiCheatException : InvalidOperationException
{
    internal AntiCheatException(AntiCheatViolationEventArgs violation)
        : base(violation.Message)
    {
        Type = violation.Type;
        Phase = violation.Phase;
        ProcessName = violation.ProcessName;
        ProcessId = violation.ProcessId;
        ExecutablePath = violation.ExecutablePath;
    }

    /// <summary>Gets the type of detected violation.</summary>
    public AntiCheatViolationType Type { get; }

    /// <summary>Gets whether the violation occurred during startup or runtime monitoring.</summary>
    public AntiCheatViolationPhase Phase { get; }

    /// <summary>Gets the detected process name when the violation is process-related.</summary>
    public string? ProcessName { get; }

    /// <summary>Gets the detected process ID when available.</summary>
    public int? ProcessId { get; }

    /// <summary>Gets the executable path when it could be read on the current platform.</summary>
    public string? ExecutablePath { get; }
}
