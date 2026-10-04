namespace Lumis;

/// <summary>Identifies the kind of anti-cheat rule that was violated.</summary>
public enum AntiCheatViolationType
{
    /// <summary>A configured or built-in blocked process was detected.</summary>
    BlockedProcess,

    /// <summary>An attached debugger was detected.</summary>
    DebuggerAttached,

    /// <summary>A protected in-memory value failed its integrity check.</summary>
    MemoryTampering,

    /// <summary>Game time advanced suspiciously faster than monotonic wall time.</summary>
    TimeManipulation,

    /// <summary>A registered file was missing or failed its integrity check.</summary>
    FileIntegrity,

    /// <summary>Protected save data failed authentication.</summary>
    SaveDataTampering,

    /// <summary>The entry assembly file failed its integrity check.</summary>
    AssemblyIntegrity
}
