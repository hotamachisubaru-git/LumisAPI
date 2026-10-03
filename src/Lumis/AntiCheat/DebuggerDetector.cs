using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Lumis;

internal static class DebuggerDetector
{
    internal static bool IsDebuggerAttached(DebuggerDetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.DetectManagedDebugger && Debugger.IsAttached)
            return true;

        if (!settings.DetectNativeDebugger)
            return false;

        if (OperatingSystem.IsWindows())
            return IsWindowsDebuggerAttached();

        if (OperatingSystem.IsLinux())
            return IsLinuxDebuggerAttached();

        return false;
    }

    private static bool IsWindowsDebuggerAttached()
    {
        try
        {
            return IsDebuggerPresent();
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    private static bool IsLinuxDebuggerAttached()
    {
        const string statusPath = "/proc/self/status";

        try
        {
            foreach (string line in File.ReadLines(statusPath))
            {
                if (!line.StartsWith("TracerPid:", StringComparison.Ordinal))
                    continue;

                string value = line["TracerPid:".Length..].Trim();
                return int.TryParse(value, out int tracerPid) && tracerPid != 0;
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return false;
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsDebuggerPresent();
}
