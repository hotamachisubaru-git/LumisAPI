#nullable enable
using System;
using UnityEngine;

namespace Lumis.Unity
{
    /// <summary>Unity lifecycle adapter. Place in a bootstrap scene and gate gameplay on IsReady.</summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Lumis/Anti Cheat")]
    public sealed class LumisAntiCheat : MonoBehaviour
    {
        [SerializeField] private bool enableInEditor = false;
        [SerializeField] private bool quitOnFailure = true;
        [SerializeField] private bool persistAcrossScenes = true;
        [SerializeField] private bool detectProcesses = true;
        [SerializeField] private bool inspectExecutableMetadata = true;
        [SerializeField] private bool detectDebugger = false;
        [SerializeField] private bool detectTimeManipulation = true;
        [SerializeField] private float scanIntervalSeconds = 2f;
        [SerializeField] private bool monitorFilesDuringGame = false;
        [SerializeField] private string[] blockedProcessNames = Array.Empty<string>();
        [SerializeField] private ProtectedUnityFile[] protectedFiles = Array.Empty<ProtectedUnityFile>();
        private AntiCheatService? service;
        private bool paused;
        private bool focused = true;
        private bool skipNextTimingSample;
        private static LumisAntiCheat? instance;

        /// <summary>True only after initialization succeeds or checks are intentionally skipped in the Editor.</summary>
        public bool IsReady { get; private set; }
        /// <summary>True when checks were intentionally skipped in the Editor, rather than actually performed.</summary>
        public bool IsEditorBypass { get; private set; }
        /// <summary>Last initialization/runtime failure, including unsupported operations or configuration errors.</summary>
        public Exception? LastFailure { get; private set; }
        /// <summary>The initialized shared service; null in Editor bypass mode or before initialization.</summary>
        public AntiCheatService? Service => service;
        /// <summary>Raised when a verification failure is handled. Subscribers must not assume this proves cheating.</summary>
        public event Action<AntiCheatViolationEventArgs>? ViolationDetected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; }

        private void Awake()
        {
            if (instance != null && instance != this) { enabled = false; return; }
            instance = this;
            if (persistAcrossScenes)
            {
                if (transform.parent == null) DontDestroyOnLoad(gameObject);
                else Debug.LogWarning("LumisAntiCheat must be on a root GameObject to persist across scenes.", this);
            }
#if UNITY_EDITOR
            if (!enableInEditor) { IsEditorBypass = true; IsReady = true; return; }
#endif
            try
            {
                if (float.IsNaN(scanIntervalSeconds) || float.IsInfinity(scanIntervalSeconds) || scanIntervalSeconds <= 0)
                    throw new ArgumentOutOfRangeException(nameof(scanIntervalSeconds));
                bool desktop = AntiCheatCapabilities.SupportsProcessInspection;
                if (detectProcesses && !desktop)
                    Debug.LogWarning("Lumis Security: process inspection is unavailable on this target and has been disabled.", this);
                var settings = new AntiCheatSettings
                {
                    Enabled = true,
                    RuntimeScanInterval = TimeSpan.FromSeconds(scanIntervalSeconds),
                    ProcessDetection = new ProcessDetectionSettings
                    {
                        Enabled = detectProcesses && desktop,
                        InspectExecutableMetadata = inspectExecutableMetadata,
                        BlockedProcessNames = blockedProcessNames ?? Array.Empty<string>()
                    },
                    DebuggerDetection = new DebuggerDetectionSettings
                    {
                        Enabled = detectDebugger,
                        DetectNativeDebugger = AntiCheatCapabilities.SupportsNativeDebuggerInspection
                    },
                    TimeManipulation = new TimeManipulationSettings { Enabled = detectTimeManipulation },
                    FileIntegrity = new FileIntegritySettings
                    {
                        Enabled = protectedFiles != null && protectedFiles.Length > 0,
                        MonitorDuringGame = monitorFilesDuringGame
                    },
                    // Unity entry assembly discovery is not a reliable Mono/IL2CPP protection target.
                    AssemblyIntegrity = new AssemblyIntegritySettings { Enabled = false }
                };
                service = new AntiCheatService(settings);
                if (protectedFiles != null)
                    foreach (ProtectedUnityFile entry in protectedFiles)
                    {
                        if (entry == null) throw new ArgumentException("Protected file entry is null.");
                        service.FileIntegrity.RegisterFile(UnityIntegrityPaths.Resolve(entry.Root, entry.RelativePath), entry.Sha256);
                    }
                service.CheckStartup();
                skipNextTimingSample = true;
                IsReady = true;
            }
            catch (Exception error) { HandleFailure(error); }
        }
        private void Update()
        {
            if (!IsReady || service == null || paused || !focused) return;
            try
            {
                if (skipNextTimingSample) { service.ResetTiming(); skipNextTimingSample = false; return; }
                // Deliberate Time.timeScale changes must not be interpreted as SpeedHack.
                service.Update(Time.unscaledDeltaTime);
            }
            catch (Exception error) { HandleFailure(error); }
        }
        private void OnApplicationPause(bool value) { paused = value; ResetHostTiming(); }
        private void OnApplicationFocus(bool value) { focused = value; ResetHostTiming(); }
        private void ResetHostTiming()
        {
            if (!IsReady || service == null) return;
            try { service.ResetTiming(); skipNextTimingSample = true; }
            catch (Exception error) { HandleFailure(error); }
        }
        /// <summary>Runs gameplay work and handles Secure/save helper violations; Unity otherwise only logs script exceptions.</summary>
        public bool ExecuteChecked(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (!IsReady) return false;
            try { action(); return true; }
            catch (AntiCheatException error) { HandleFailure(error); return false; }
        }
        /// <summary>Handles a verification exception caught in another gameplay script.</summary>
        public void ReportViolation(AntiCheatException error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));
            HandleFailure(error);
        }
        private void HandleFailure(Exception error)
        {
            IsReady = false; LastFailure = error; enabled = false;
            Debug.LogError("Lumis Security stopped gameplay checks: " + error.Message, this);
            try
            {
                if (error is AntiCheatException violation) ViolationDetected?.Invoke(violation.Violation);
            }
            catch (Exception callbackError) { Debug.LogException(callbackError, this); }
            finally
            {
                if (quitOnFailure)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    // No other process is terminated. Quit does not close WebGL browser tabs.
                    Application.Quit(1);
#endif
                }
            }
        }
        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
