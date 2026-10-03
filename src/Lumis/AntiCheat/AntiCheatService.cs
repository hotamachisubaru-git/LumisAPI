namespace Lumis;

/// <summary>Runs configured anti-cheat checks for a game and reports detected violations.</summary>
public sealed class AntiCheatService
{
    private readonly AntiCheatSettings settings;
    private readonly Func<IReadOnlyList<ProcessSnapshot>> runningProcessesProvider;
    private readonly Func<bool> debuggerAttachedProvider;
    private double runtimeScanAccumulator;

    internal AntiCheatService(AntiCheatSettings settings)
        : this(
            settings,
            () => ProcessDetector.GetRunningProcesses(settings.ProcessDetection),
            () => DebuggerDetector.IsDebuggerAttached(settings.DebuggerDetection))
    {
    }

    internal AntiCheatService(
        AntiCheatSettings settings,
        Func<IReadOnlyList<ProcessSnapshot>> runningProcessesProvider,
        Func<bool>? debuggerAttachedProvider = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(runningProcessesProvider);

        this.settings = settings;
        this.runningProcessesProvider = runningProcessesProvider;
        this.debuggerAttachedProvider = debuggerAttachedProvider ?? (() => false);
    }

    /// <summary>Occurs immediately before an enabled anti-cheat violation aborts startup or runtime execution.</summary>
    public event EventHandler<AntiCheatViolationEventArgs>? ViolationDetected;

    internal void CheckStartup()
    {
        Check(AntiCheatViolationPhase.Startup);
    }

    internal void Update(float deltaTime)
    {
        if (!settings.Enabled || !settings.MonitorDuringGame)
            return;

        runtimeScanAccumulator += deltaTime;
        if (runtimeScanAccumulator < settings.RuntimeScanInterval.TotalSeconds)
            return;

        runtimeScanAccumulator = 0d;
        Check(AntiCheatViolationPhase.Runtime);
    }

    private void Check(AntiCheatViolationPhase phase)
    {
        if (!settings.Enabled)
            return;

        if (settings.DebuggerDetection.Enabled && debuggerAttachedProvider())
        {
            ThrowViolation(new AntiCheatViolationEventArgs(
                AntiCheatViolationType.DebuggerAttached,
                phase,
                phase == AntiCheatViolationPhase.Startup
                    ? "Anti-cheat blocked startup because an attached debugger was detected."
                    : "Anti-cheat stopped the game because an attached debugger was detected."));
        }

        if (!settings.ProcessDetection.Enabled)
            return;

        ProcessSnapshot? blockedProcess = ProcessDetector.FindBlockedProcess(
            runningProcessesProvider(),
            settings.ProcessDetection);

        if (blockedProcess is null)
            return;

        ThrowViolation(new AntiCheatViolationEventArgs(
            AntiCheatViolationType.BlockedProcess,
            phase,
            phase == AntiCheatViolationPhase.Startup
                ? $"Anti-cheat blocked startup because process '{blockedProcess.Name}' was detected."
                : $"Anti-cheat stopped the game because process '{blockedProcess.Name}' was detected.",
            blockedProcess.Name,
            blockedProcess.Id,
            blockedProcess.ExecutablePath));
    }

    private void ThrowViolation(AntiCheatViolationEventArgs violation)
    {
        ViolationDetected?.Invoke(this, violation);
        throw new AntiCheatException(violation);
    }
}
