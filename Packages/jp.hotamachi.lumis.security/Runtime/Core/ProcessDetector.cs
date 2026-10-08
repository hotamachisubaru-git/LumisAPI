#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace Lumis
{
    internal sealed class ProcessSnapshot
    {
        internal ProcessSnapshot(int id, string name, string? executablePath = null,
            string? fileDescription = null, string? productName = null, string? originalFilename = null)
        {
            Id = id; Name = name; ExecutablePath = executablePath; FileDescription = fileDescription;
            ProductName = productName; OriginalFilename = originalFilename;
        }
        internal int Id { get; }
        internal string Name { get; }
        internal string? ExecutablePath { get; }
        internal string? FileDescription { get; }
        internal string? ProductName { get; }
        internal string? OriginalFilename { get; }
    }

    internal static class ProcessDetector
    {
        internal static IReadOnlyList<ProcessSnapshot> GetRunningProcesses(ProcessDetectionSettings settings)
        {
            if (!AntiCheatCapabilities.SupportsProcessInspection)
                throw new PlatformNotSupportedException("Process inspection is only available on supported desktop hosts.");
            var result = new List<ProcessSnapshot>();
            Process[] processes = Process.GetProcesses();
            try
            {
                int ownId;
                using (Process own = Process.GetCurrentProcess()) ownId = own.Id;
                foreach (Process process in processes)
                {
                    try
                    {
                        if (process.Id == ownId) continue;
                        string name = process.ProcessName;
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        string? path = null; string? description = null; string? product = null; string? original = null;
                        if (settings.InspectExecutableMetadata)
                        {
                            try { path = process.MainModule?.FileName; }
                            catch (Exception ex) when (IsMetadataUnavailable(ex)) { }
                            if (!string.IsNullOrEmpty(path))
                            {
                                try
                                {
                                    FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);
                                    description = info.FileDescription; product = info.ProductName; original = info.OriginalFilename;
                                }
                                catch (Exception ex) when (IsMetadataUnavailable(ex) || ex is IOException || ex is ArgumentException) { }
                            }
                        }
                        result.Add(new ProcessSnapshot(process.Id, name, path, description, product, original));
                    }
                    catch (Exception ex) when (IsMetadataUnavailable(ex)) { }
                }
            }
            finally { foreach (Process process in processes) process.Dispose(); }
            return result;
        }

        private static bool IsMetadataUnavailable(Exception ex) => ex is InvalidOperationException || ex is Win32Exception ||
            ex is NotSupportedException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException;

        internal static ProcessSnapshot? FindBlockedProcess(IEnumerable<ProcessSnapshot> processes, ProcessDetectionSettings settings)
        {
            SecurityCompat.NotNull(processes, nameof(processes)); SecurityCompat.NotNull(settings, nameof(settings));
            var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in settings.BlockedProcessNames) blocked.Add(Normalize(name));
            foreach (ProcessSnapshot process in processes)
            {
                if (string.IsNullOrWhiteSpace(process.Name)) continue;
                if (blocked.Contains(Normalize(process.Name)) || (settings.DetectCheatEngine && IsCheatEngineName(Normalize(process.Name))))
                    return process;
                if (process.ExecutablePath != null)
                    foreach (string fragment in settings.BlockedExecutablePathFragments)
                        if (process.ExecutablePath.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0) return process;
                if (!settings.DetectCheatEngine || !settings.InspectExecutableMetadata) continue;
                if (ContainsIdentifier(process.ProductName) || ContainsIdentifier(process.FileDescription) || ContainsIdentifier(process.OriginalFilename))
                    return process;
                if (process.ExecutablePath != null)
                    foreach (string part in process.ExecutablePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries))
                        if (IsCheatEngineName(Normalize(part))) return process;
            }
            return null;
        }
        private static string Normalize(string name)
        {
            name = name.Trim();
            return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - 4) : name;
        }
        private static bool IsCheatEngineName(string name)
        {
            const string compact = "cheatengine"; const string spaced = "cheat engine";
            if (name.Equals(compact, StringComparison.OrdinalIgnoreCase) || name.Equals(spaced, StringComparison.OrdinalIgnoreCase)) return true;
            if (name.StartsWith(spaced + " ", StringComparison.OrdinalIgnoreCase)) return true;
            if (!name.StartsWith(compact, StringComparison.OrdinalIgnoreCase) || name.Length <= compact.Length) return false;
            char c = name[compact.Length];
            return c == '-' || c == '_' || c == ' ' || char.IsDigit(c);
        }
        private static bool ContainsIdentifier(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            // Boundaries avoid matching innocent strings such as "CheatEngineer".
            foreach (string token in new[] { "Cheat Engine", "CheatEngine" })
            {
                int start = 0;
                while (start < text.Length)
                {
                    int index = text.IndexOf(token, start, StringComparison.OrdinalIgnoreCase);
                    if (index < 0) break;
                    int end = index + token.Length;
                    if ((index == 0 || !char.IsLetterOrDigit(text[index - 1])) &&
                        (end == text.Length || !char.IsLetter(text[end]))) return true;
                    start = end;
                }
            }
            return false;
        }
    }
}
