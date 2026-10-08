#nullable enable
using System;
using System.Collections.Generic;

namespace Lumis
{
    /// <summary>Engine-independent, opt-in anti-cheat configuration. The service copies settings at construction.</summary>
    public sealed class AntiCheatSettings
    {
        /// <summary>Enables automatic checks; protected values and save helpers are independently usable.</summary>
        public bool Enabled { get; set; }
        /// <summary>Configures process inspection.</summary>
        public ProcessDetectionSettings ProcessDetection { get; set; } = new ProcessDetectionSettings();
        /// <summary>Configures debugger inspection.</summary>
        public DebuggerDetectionSettings DebuggerDetection { get; set; } = new DebuggerDetectionSettings();
        /// <summary>Configures the heuristic game-time comparison.</summary>
        public TimeManipulationSettings TimeManipulation { get; set; } = new TimeManipulationSettings();
        /// <summary>Configures registered-file checks.</summary>
        public FileIntegritySettings FileIntegrity { get; set; } = new FileIntegritySettings();
        /// <summary>Configures on-disk entry-assembly checks, not in-memory code verification.</summary>
        public AssemblyIntegritySettings AssemblyIntegrity { get; set; } = new AssemblyIntegritySettings();
        /// <summary>Repeats enabled checks during host updates.</summary>
        public bool MonitorDuringGame { get; set; } = true;
        /// <summary>Minimum runtime scan interval, accumulated from host updates.</summary>
        public TimeSpan RuntimeScanInterval { get; set; } = TimeSpan.FromSeconds(2);

        internal void Validate()
        {
            SecurityCompat.NotNull(ProcessDetection, nameof(ProcessDetection));
            SecurityCompat.NotNull(DebuggerDetection, nameof(DebuggerDetection));
            SecurityCompat.NotNull(TimeManipulation, nameof(TimeManipulation));
            SecurityCompat.NotNull(FileIntegrity, nameof(FileIntegrity));
            SecurityCompat.NotNull(AssemblyIntegrity, nameof(AssemblyIntegrity));
            ProcessDetection.Validate();
            TimeManipulation.Validate();
            AssemblyIntegrity.Validate();
            if (RuntimeScanInterval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(RuntimeScanInterval));
        }

        internal AntiCheatSettings Copy()
        {
            Validate();
            var copy = (AntiCheatSettings)MemberwiseClone();
            copy.ProcessDetection = ProcessDetection.Copy();
            copy.DebuggerDetection = DebuggerDetection.Copy();
            copy.TimeManipulation = TimeManipulation.Copy();
            copy.FileIntegrity = FileIntegrity.Copy();
            copy.AssemblyIntegrity = AssemblyIntegrity.Copy();
            return copy;
        }
    }

    /// <summary>Running-process names and optional accessible executable metadata.</summary>
    public sealed class ProcessDetectionSettings
    {
        /// <summary>Enables process checks.</summary>
        public bool Enabled { get; set; } = true;
        /// <summary>Enables built-in Cheat Engine name/metadata matching.</summary>
        public bool DetectCheatEngine { get; set; } = true;
        /// <summary>Reads accessible executable paths and file version metadata. Potentially expensive.</summary>
        public bool InspectExecutableMetadata { get; set; } = true;
        /// <summary>Additional case-insensitive names; an optional .exe suffix is ignored.</summary>
        public IReadOnlyList<string> BlockedProcessNames { get; set; } = Array.Empty<string>();
        /// <summary>Case-insensitive path fragments; requires metadata inspection for real process scans.</summary>
        public IReadOnlyList<string> BlockedExecutablePathFragments { get; set; } = Array.Empty<string>();

        internal void Validate()
        {
            ValidateEntries(BlockedProcessNames, nameof(BlockedProcessNames));
            ValidateEntries(BlockedExecutablePathFragments, nameof(BlockedExecutablePathFragments));
        }
        private static void ValidateEntries(IReadOnlyList<string> entries, string name)
        {
            SecurityCompat.NotNull(entries, name);
            foreach (string entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry) || entry.IndexOf('\0') >= 0)
                    throw new ArgumentException("Matching entries cannot be empty or contain null characters.", name);
            }
        }
        internal ProcessDetectionSettings Copy()
        {
            var copy = (ProcessDetectionSettings)MemberwiseClone();
            copy.BlockedProcessNames = new List<string>(BlockedProcessNames).AsReadOnly();
            copy.BlockedExecutablePathFragments = new List<string>(BlockedExecutablePathFragments).AsReadOnly();
            return copy;
        }
    }

    /// <summary>Optional debugger signals. Development debuggers are not evidence of cheating.</summary>
    public sealed class DebuggerDetectionSettings
    {
        /// <summary>Enables debugger checks; disabled by default.</summary>
        public bool Enabled { get; set; }
        /// <summary>Reads the managed debugger signal.</summary>
        public bool DetectManagedDebugger { get; set; } = true;
        /// <summary>Uses Windows IsDebuggerPresent or Linux TracerPid where available.</summary>
        public bool DetectNativeDebugger { get; set; } = true;
        internal DebuggerDetectionSettings Copy() => (DebuggerDetectionSettings)MemberwiseClone();
    }

    /// <summary>Heuristic comparison of host game time with a local monotonic clock.</summary>
    public sealed class TimeManipulationSettings
    {
        /// <summary>Enables this check. It cannot detect manipulation of both clock sources.</summary>
        public bool Enabled { get; set; } = true;
        /// <summary>Minimum monotonic observation window.</summary>
        public TimeSpan ObservationWindow { get; set; } = TimeSpan.FromSeconds(2);
        /// <summary>Maximum permitted accumulated game-time/wall-time ratio.</summary>
        public double MaxGameTimeRatio { get; set; } = 1.75;
        /// <summary>Required number of consecutive suspicious windows.</summary>
        public int RequiredConsecutiveDetections { get; set; } = 2;
        internal void Validate()
        {
            if (ObservationWindow <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ObservationWindow));
            if (!SecurityCompat.IsFinite(MaxGameTimeRatio) || MaxGameTimeRatio <= 1)
                throw new ArgumentOutOfRangeException(nameof(MaxGameTimeRatio));
            if (RequiredConsecutiveDetections < 1) throw new ArgumentOutOfRangeException(nameof(RequiredConsecutiveDetections));
        }
        internal TimeManipulationSettings Copy() => (TimeManipulationSettings)MemberwiseClone();
    }

    /// <summary>Registered on-disk file verification settings.</summary>
    public sealed class FileIntegritySettings
    {
        /// <summary>Enables file checking.</summary>
        public bool Enabled { get; set; }
        /// <summary>Checks before host gameplay begins.</summary>
        public bool CheckOnStartup { get; set; } = true;
        /// <summary>Rehashes during runtime scans; off by default to avoid large synchronous reads.</summary>
        public bool MonitorDuringGame { get; set; }
        /// <summary>Treats disappearance of registered files as verification failure.</summary>
        public bool FailOnMissingFile { get; set; } = true;
        internal FileIntegritySettings Copy() => (FileIntegritySettings)MemberwiseClone();
    }

    /// <summary>On-disk entry-assembly checks. Not suitable for Unity IL2CPP entry-point discovery.</summary>
    public sealed class AssemblyIntegritySettings
    {
        /// <summary>Enables this check.</summary>
        public bool Enabled { get; set; }
        /// <summary>Checks at startup.</summary>
        public bool CheckOnStartup { get; set; } = true;
        /// <summary>Checks during runtime scans.</summary>
        public bool MonitorDuringGame { get; set; } = true;
        /// <summary>Trusted external build hash. Null captures a baseline and only detects later changes.</summary>
        public string? ExpectedEntryAssemblySha256 { get; set; }
        /// <summary>Fails if a usable assembly location is unavailable.</summary>
        public bool FailIfUnavailable { get; set; }
        internal void Validate()
        {
            if (ExpectedEntryAssemblySha256 != null)
                SecurityCompat.ParseSha256(ExpectedEntryAssemblySha256, nameof(ExpectedEntryAssemblySha256));
        }
        internal AssemblyIntegritySettings Copy() => (AssemblyIntegritySettings)MemberwiseClone();
    }
}
