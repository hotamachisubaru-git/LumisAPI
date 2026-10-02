namespace Lumis;

/// <summary>Runs configured anti-cheat checks for a game and reports detected violations.</summary>
public sealed class AntiCheatService
{
    private readonly AntiCheatSettings settings;
    private readonly Func<IReadOnlyList<string>> runningProcessNamesProvider;

    internal AntiCheatService(AntiCheatSettings settings)
        : this(settings, ProcessDetector.GetRunningProcessNames)
    {
    }

    internal AntiCheatService(
        AntiCheatSettings settings,
        Func<IReadOnlyList<string>> runningProcessNamesProvider)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(runningProcessNamesProvider);

        this.settings = settings;
        this.runningProcessNamesProvider = runningProcessNamesProvider;
    }

    /// <summary>Occurs when an enabled anti-cheat rule detects a violation.</summary>
    /// <remarks>The startup check still aborts after this event returns.</remarks>
    public event EventHandler<AntiCheatViolationEventArgs>? ViolationDetected;

    internal void CheckStartup()
    {
        if (!settings.Enabled || !settings.ProcessDetection.Enabled)
            return;

        string? blockedProcess = ProcessDetector.FindBlockedProcess(
            runningProcessNamesProvider(),
            settings.ProcessDetection);

        if (blockedProcess is null)
            return;

        var violation = new AntiCheatViolationEventArgs(
            AntiCheatViolationType.BlockedProcess,
            $"Anti-cheat blocked startup because process '{blockedProcess}' was detected.",
            blockedProcess);

        ViolationDetected?.Invoke(this, violation);
        throw new AntiCheatException(violation);
    }
}
