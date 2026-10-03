using System.ComponentModel;
using System.Diagnostics;

namespace Lumis;

internal static class ProcessDetector
{
    internal static IReadOnlyList<ProcessSnapshot> GetRunningProcesses(ProcessDetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Process[] processes = Process.GetProcesses();
        var snapshots = new List<ProcessSnapshot>(processes.Length);
        bool inspectMetadata =
            settings.InspectExecutableMetadata &&
            (settings.DetectCheatEngine || settings.BlockedExecutablePathFragments.Count > 0);

        try
        {
            foreach (Process process in processes)
            {
                try
                {
                    if (process.Id == Environment.ProcessId)
                        continue;

                    string processName = process.ProcessName;
                    if (string.IsNullOrWhiteSpace(processName))
                        continue;

                    string? executablePath = null;
                    string? fileDescription = null;
                    string? productName = null;
                    string? originalFilename = null;

                    if (inspectMetadata)
                    {
                        executablePath = TryGetExecutablePath(process);
                        if (!string.IsNullOrWhiteSpace(executablePath))
                        {
                            TryGetVersionMetadata(
                                executablePath,
                                out fileDescription,
                                out productName,
                                out originalFilename);
                        }
                    }

                    snapshots.Add(new ProcessSnapshot(
                        process.Id,
                        processName,
                        executablePath,
                        fileDescription,
                        productName,
                        originalFilename));
                }
                catch (InvalidOperationException)
                {
                    // A process may exit between enumeration and reading its metadata.
                }
                catch (Win32Exception)
                {
                    // Protected processes can deny metadata access.
                }
                catch (NotSupportedException)
                {
                    // Some process metadata is unavailable on some platforms.
                }
            }
        }
        finally
        {
            foreach (Process process in processes)
                process.Dispose();
        }

        return snapshots;
    }

    internal static ProcessSnapshot? FindBlockedProcess(
        IEnumerable<ProcessSnapshot> runningProcesses,
        ProcessDetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(runningProcesses);
        ArgumentNullException.ThrowIfNull(settings);

        var customBlockedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string blockedProcessName in settings.BlockedProcessNames)
            customBlockedNames.Add(NormalizeProcessName(blockedProcessName));

        foreach (ProcessSnapshot process in runningProcesses)
        {
            if (string.IsNullOrWhiteSpace(process.Name))
                continue;

            string normalized = NormalizeProcessName(process.Name);

            if (settings.DetectCheatEngine && IsCheatEngineProcessName(normalized))
                return process;

            if (customBlockedNames.Contains(normalized))
                return process;

            if (MatchesBlockedPath(process.ExecutablePath, settings.BlockedExecutablePathFragments))
                return process;

            if (settings.DetectCheatEngine &&
                settings.InspectExecutableMetadata &&
                HasCheatEngineMetadata(process))
            {
                return process;
            }
        }

        return null;
    }

    private static string? TryGetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (Win32Exception)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static void TryGetVersionMetadata(
        string executablePath,
        out string? fileDescription,
        out string? productName,
        out string? originalFilename)
    {
        fileDescription = null;
        productName = null;
        originalFilename = null;

        try
        {
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
            fileDescription = versionInfo.FileDescription;
            productName = versionInfo.ProductName;
            originalFilename = versionInfo.OriginalFilename;
        }
        catch (ArgumentException)
        {
        }
        catch (FileNotFoundException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
        catch (NotSupportedException)
        {
        }
    }

    private static bool MatchesBlockedPath(string? executablePath, IEnumerable<string> blockedFragments)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
            return false;

        foreach (string fragment in blockedFragments)
        {
            if (executablePath.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static bool HasCheatEngineMetadata(ProcessSnapshot process)
    {
        if (ContainsCheatEngineIdentifier(process.FileDescription) ||
            ContainsCheatEngineIdentifier(process.ProductName) ||
            ContainsCheatEngineIdentifier(process.OriginalFilename))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(process.ExecutablePath))
            return false;

        string[] pathParts = process.ExecutablePath.Split(
            new[] { '/', '\\' },
            StringSplitOptions.RemoveEmptyEntries);

        foreach (string pathPart in pathParts)
        {
            if (IsCheatEngineProcessName(NormalizeProcessName(pathPart)))
                return true;
        }

        return false;
    }

    private static bool ContainsCheatEngineIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string normalized = NormalizeProcessName(value);
        if (IsCheatEngineProcessName(normalized))
            return true;

        return value.IndexOf("Cheat Engine", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.IndexOf("CheatEngine", StringComparison.OrdinalIgnoreCase) >= 0;
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
