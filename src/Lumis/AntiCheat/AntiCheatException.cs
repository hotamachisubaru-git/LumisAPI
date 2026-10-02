namespace Lumis;

/// <summary>Thrown when an enabled anti-cheat check prevents game startup.</summary>
public sealed class AntiCheatException : InvalidOperationException
{
    internal AntiCheatException(AntiCheatViolationEventArgs violation)
        : base(violation.Message)
    {
        Type = violation.Type;
        ProcessName = violation.ProcessName;
    }

    /// <summary>Gets the type of violation that prevented startup.</summary>
    public AntiCheatViolationType Type { get; }

    /// <summary>Gets the detected process name when the violation is process-related.</summary>
    public string? ProcessName { get; }
}
