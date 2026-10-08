#nullable enable
using System;

namespace Lumis
{
    /// <summary>Kind of local verification failure. It is not proof of intentional cheating.</summary>
    public enum AntiCheatViolationType
    {
        /// <summary>A configured process matched.</summary>
        BlockedProcess,
        /// <summary>A configured debugger signal was present.</summary>
        DebuggerAttached,
        /// <summary>A protected numeric value failed verification.</summary>
        MemoryTampering,
        /// <summary>Game time advanced faster than the local comparison clock.</summary>
        TimeManipulation,
        /// <summary>A protected file could not be verified.</summary>
        FileIntegrity,
        /// <summary>Save data failed authentication (also possible with the wrong key).</summary>
        SaveDataTampering,
        /// <summary>The entry assembly could not be verified.</summary>
        AssemblyIntegrity
    }
    /// <summary>Host lifecycle phase.</summary>
    public enum AntiCheatViolationPhase
    {
        /// <summary>Before host gameplay begins.</summary>
        Startup,
        /// <summary>During host gameplay or an independent helper operation.</summary>
        Runtime
    }
    /// <summary>Details reported by an anti-cheat service.</summary>
    public sealed class AntiCheatViolationEventArgs : EventArgs
    {
        internal AntiCheatViolationEventArgs(AntiCheatViolationType type, AntiCheatViolationPhase phase,
            string message, string? processName = null, int? processId = null,
            string? executablePath = null, string? filePath = null)
        {
            Type = type; Phase = phase; Message = message; ProcessName = processName;
            ProcessId = processId; ExecutablePath = executablePath; FilePath = filePath;
        }
        /// <summary>Kind of failure.</summary>
        public AntiCheatViolationType Type { get; }
        /// <summary>Host lifecycle phase.</summary>
        public AntiCheatViolationPhase Phase { get; }
        /// <summary>Description; callers decide whether to show paths to end users.</summary>
        public string Message { get; }
        /// <summary>Process name, when available.</summary>
        public string? ProcessName { get; }
        /// <summary>Process identifier, when available.</summary>
        public int? ProcessId { get; }
        /// <summary>Executable path, when available.</summary>
        public string? ExecutablePath { get; }
        /// <summary>Registered file path, when available.</summary>
        public string? FilePath { get; }
    }
    /// <summary>Verification failure. Hosts must explicitly decide how gameplay is stopped.</summary>
    public sealed class AntiCheatException : InvalidOperationException
    {
        internal AntiCheatException(AntiCheatViolationEventArgs violation, Exception? inner = null)
            : base(violation.Message, inner)
        { Violation = violation; }
        /// <summary>Full verification-failure details.</summary>
        public AntiCheatViolationEventArgs Violation { get; }
        /// <summary>Kind of failure.</summary>
        public AntiCheatViolationType Type => Violation.Type;
        /// <summary>Host lifecycle phase.</summary>
        public AntiCheatViolationPhase Phase => Violation.Phase;
        /// <summary>Process name, when available.</summary>
        public string? ProcessName => Violation.ProcessName;
        /// <summary>Process identifier, when available.</summary>
        public int? ProcessId => Violation.ProcessId;
        /// <summary>Executable path, when available.</summary>
        public string? ExecutablePath => Violation.ExecutablePath;
        /// <summary>Protected file path, when available.</summary>
        public string? FilePath => Violation.FilePath;
    }
}
