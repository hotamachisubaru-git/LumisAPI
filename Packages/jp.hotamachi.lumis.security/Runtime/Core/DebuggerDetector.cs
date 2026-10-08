#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Lumis
{
    internal static class DebuggerDetector
    {
        internal static bool IsDebuggerAttached(DebuggerDetectionSettings settings)
        {
            if (settings.DetectManagedDebugger && Debugger.IsAttached) return true;
            if (!settings.DetectNativeDebugger || !AntiCheatCapabilities.SupportsNativeDebuggerInspection) return false;
            if (SecurityCompat.IsWindows)
            {
                try { return IsDebuggerPresent(); }
                catch (DllNotFoundException) { return false; }
                catch (EntryPointNotFoundException) { return false; }
            }
            if (SecurityCompat.IsLinux)
            {
                try
                {
                    foreach (string line in File.ReadLines("/proc/self/status"))
                        if (line.StartsWith("TracerPid:", StringComparison.Ordinal))
                            return int.TryParse(line.Substring(10).Trim(), out int pid) && pid != 0;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return false;
        }
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsDebuggerPresent();
    }
}
