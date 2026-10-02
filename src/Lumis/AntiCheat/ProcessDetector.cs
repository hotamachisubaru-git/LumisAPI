using System.ComponentModel;
using System.Diagnostics;

namespace Lumis;

internal static class ProcessDetector
{
    internal static IReadOnlyList<string> GetRunningProcessNames()
    {
        Process[] processes = Process.GetProcesses();
        var names = new List<string>(processes.Length);

        try
        {
            foreach (Process process in processes)
            {
                try
                {
                    string processName = process.ProcessName;
                    if (!string.IsNullOrWhiteSpace(processName))
                        names.Add(processName);
                }
                catch (InvalidOperationException)
                {
                    // A process may exit between enumeration and reading its name.
                }
                catch (Win32Exception)
                {
                    // Some platforms or protected processes can deny metadata access.
                }
                catch (NotSupportedException)
                {
                    // Skip process metadata that is unavailable on the current platform.
                }
            }
        }
        finally
        {
            foreach (Process process in processes)
                process.Dispose();
        }

        return names;
    }

    internal static string? FindBlockedProcess(
        IEnumerable<string> runningProcessNames,
        ProcessDetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(runningProcessNames);
        ArgumentNullException.ThrowIfNull(settings);

        var customBlockedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string blockedProcessName in settings.BlockedProcessNames)
            customBlockedNames.Add(NormalizeProcessName(blockedProcessName));

        foreach (string runningProcessName in runningProcessNames)
        {
            if (string.IsNullOrWhiteSpace(runningProcessName))
                continue;

            string normalized = NormalizeProcessName(runningProcessName);

            if (settings.DetectCheatEngine && IsCheatEngineProcessName(normalized))
                return runningProcessName;

            if (customBlockedNames.Contains(normalized))
                return runningProcessName;
        }

        return null;
    }

    private static string NormalizeProcessName(string processName)
    {
        string normalized = processName.Trim();
        if (normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[..^4];

        return normalized;
    }

    private static bool IsCheatEngineProcessName(string processName)
    {
        const string compactName = "cheatengine";
        const string spacedName = "cheat engine";

        if (processName.Equals(compactName, StringComparison.OrdinalIgnoreCase) ||
            processName.Equals(spacedName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (processName.StartsWith(spacedName, StringComparison.OrdinalIgnoreCase))
        {
            return processName.Length > spacedName.Length &&
                   processName[spacedName.Length] == ' ';
        }

        if (!processName.StartsWith(compactName, StringComparison.OrdinalIgnoreCase) ||
            processName.Length <= compactName.Length)
        {
            return false;
        }

        char suffix = processName[compactName.Length];
        return suffix is '-' or '_' or ' ' || char.IsDigit(suffix);
    }
}
