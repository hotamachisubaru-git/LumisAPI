namespace Lumis;

/// <summary>Identifies when an anti-cheat violation was detected.</summary>
public enum AntiCheatViolationPhase
{
    /// <summary>The violation was detected before the native game window was created.</summary>
    Startup,

    /// <summary>The violation was detected while the game loop was running.</summary>
    Runtime
}
