#nullable enable
using System;
using System.Collections.Generic;

namespace Lumis
{
    /// <summary>Engine-independent checks. The host calls CheckStartup, Update and ResetTiming on its main thread.</summary>
    /// <remarks>No engine window, background thread, process termination or network telemetry is created.</remarks>
    public sealed class AntiCheatService
    {
        private readonly AntiCheatSettings settings;
        private readonly Func<IReadOnlyList<ProcessSnapshot>> processes;
        private readonly Func<bool> debugger;
        private readonly TimeManipulationDetector time;
        private readonly AssemblyIntegrityMonitor assembly;
        private readonly int ownerThread = Environment.CurrentManagedThreadId;
        private double scanAccumulator;
        private bool checking;

        /// <summary>Copies and validates configuration. Does not enumerate processes until checks are requested.</summary>
        public AntiCheatService(AntiCheatSettings settings) : this(settings, null, null, null, null) { }

        internal AntiCheatService(AntiCheatSettings settings,
            Func<IReadOnlyList<ProcessSnapshot>>? runningProcessesProvider,
            Func<bool>? debuggerAttachedProvider = null, Func<double>? monotonicSecondsProvider = null,
            Func<string?>? entryAssemblyPathProvider = null)
        {
            SecurityCompat.NotNull(settings, nameof(settings));
            this.settings = settings.Copy();
            processes = runningProcessesProvider ?? (() => ProcessDetector.GetRunningProcesses(this.settings.ProcessDetection));
            debugger = debuggerAttachedProvider ?? (() => DebuggerDetector.IsDebuggerAttached(this.settings.DebuggerDetection));
            time = new TimeManipulationDetector(this.settings.TimeManipulation, monotonicSecondsProvider);
            assembly = new AssemblyIntegrityMonitor(this.settings.AssemblyIntegrity, entryAssemblyPathProvider);
            FileIntegrity = new FileIntegrityService(this.settings.FileIntegrity);
        }
        /// <summary>Explicitly register protected files before checking startup.</summary>
        public FileIntegrityService FileIntegrity { get; }
        /// <summary>Raised before a service violation is thrown. Independent value/save helpers throw directly.</summary>
        public event EventHandler<AntiCheatViolationEventArgs>? ViolationDetected;

        /// <summary>Performs enabled startup checks. Unity's engine window already exists at this point.</summary>
        public void CheckStartup()
        {
            EnsureOwner(); Check(AntiCheatViolationPhase.Startup); ResetTiming();
        }
        /// <summary>Advances monitoring using finite, nonnegative unscaled host seconds. Call once per frame.</summary>
        public void Update(float deltaTime)
        {
            EnsureOwner();
            if (!SecurityCompat.IsFinite(deltaTime) || deltaTime < 0) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!settings.Enabled || !settings.MonitorDuringGame) return;
            if (time.Observe(deltaTime))
                ThrowViolation(new AntiCheatViolationEventArgs(AntiCheatViolationType.TimeManipulation,
                    AntiCheatViolationPhase.Runtime, "Suspicious host-time acceleration was detected."));
            scanAccumulator += deltaTime;
            if (scanAccumulator < settings.RuntimeScanInterval.TotalSeconds) return;
            scanAccumulator = 0;
            Check(AntiCheatViolationPhase.Runtime);
        }
        /// <summary>Discards timing history after loading, pause/resume or loss/regain of focus. Does not alter file baselines.</summary>
        public void ResetTiming() { EnsureOwner(); scanAccumulator = 0; time.Reset(); }

        private void EnsureOwner()
        {
            if (Environment.CurrentManagedThreadId != ownerThread)
                throw new InvalidOperationException("Use the anti-cheat service on the thread that created it.");
            if (checking) throw new InvalidOperationException("Anti-cheat callbacks must not re-enter service checks.");
        }
        private void Check(AntiCheatViolationPhase phase)
        {
            if (!settings.Enabled) return;
            checking = true;
            try
            {
                if (settings.DebuggerDetection.Enabled && debugger())
                    ThrowViolation(new AntiCheatViolationEventArgs(AntiCheatViolationType.DebuggerAttached, phase, "An attached debugger was detected."));
                if (settings.ProcessDetection.Enabled)
                {
                    ProcessSnapshot? match = ProcessDetector.FindBlockedProcess(processes(), settings.ProcessDetection);
                    if (match != null)
                        ThrowViolation(new AntiCheatViolationEventArgs(AntiCheatViolationType.BlockedProcess, phase,
                            "A configured process was detected: " + match.Name, match.Name, match.Id, match.ExecutablePath));
                }
                bool startup = phase == AntiCheatViolationPhase.Startup;
                if (settings.FileIntegrity.Enabled && (startup ? settings.FileIntegrity.CheckOnStartup : settings.FileIntegrity.MonitorDuringGame))
                {
                    FileIntegrityFailure? failure = FileIntegrity.Verify();
                    if (failure != null)
                        ThrowViolation(new AntiCheatViolationEventArgs(AntiCheatViolationType.FileIntegrity, phase, failure.Message, filePath: failure.FilePath));
                }
                if (settings.AssemblyIntegrity.Enabled && (startup ? settings.AssemblyIntegrity.CheckOnStartup : settings.AssemblyIntegrity.MonitorDuringGame))
                {
                    AssemblyIntegrityFailure? failure = assembly.Verify();
                    if (failure != null)
                        ThrowViolation(new AntiCheatViolationEventArgs(AntiCheatViolationType.AssemblyIntegrity, phase, failure.Message, filePath: failure.FilePath));
                }
            }
            finally { checking = false; }
        }
        private void ThrowViolation(AntiCheatViolationEventArgs violation)
        {
            try { ViolationDetected?.Invoke(this, violation); }
            catch (Exception observerError) { throw new AntiCheatException(violation, observerError); }
            throw new AntiCheatException(violation);
        }
    }
}
