#nullable enable
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace Lumis
{
    internal sealed class AssemblyIntegrityFailure
    {
        internal AssemblyIntegrityFailure(string? path, string message) { FilePath = path; Message = message; }
        internal string? FilePath { get; }
        internal string Message { get; }
    }
    internal sealed class AssemblyIntegrityMonitor
    {
        private readonly AssemblyIntegritySettings settings;
        private readonly Func<string?> pathProvider;
        private byte[]? expected;
        private string? baselinePath;
        internal AssemblyIntegrityMonitor(AssemblyIntegritySettings settings, Func<string?>? entryAssemblyPathProvider = null)
        {
            SecurityCompat.NotNull(settings, nameof(settings)); settings.Validate();
            this.settings = settings.Copy();
            pathProvider = entryAssemblyPathProvider ?? (() => Assembly.GetEntryAssembly()?.Location);
            if (settings.ExpectedEntryAssemblySha256 != null)
                expected = SecurityCompat.ParseSha256(settings.ExpectedEntryAssemblySha256, nameof(settings.ExpectedEntryAssemblySha256));
        }
        internal AssemblyIntegrityFailure? Verify()
        {
            string? path = pathProvider();
            if (string.IsNullOrWhiteSpace(path))
                return settings.FailIfUnavailable ? new AssemblyIntegrityFailure(null, "Entry assembly location is unavailable.") : null;
            path = Path.GetFullPath(path);
            if (baselinePath != null && !string.Equals(baselinePath, path,
                SecurityCompat.IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                return new AssemblyIntegrityFailure(path, "Entry assembly path changed.");
            byte[] actual;
            try { actual = SecurityCompat.HashFile(path); }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                // Once a baseline exists, disappearing is a failure, not an unsupported deployment.
                return settings.FailIfUnavailable || baselinePath != null
                    ? new AssemblyIntegrityFailure(path, "Entry assembly file is missing.") : null;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            { return new AssemblyIntegrityFailure(path, "Entry assembly file could not be read."); }
            baselinePath = path;
            if (expected == null) { expected = actual; return null; }
            return CryptographicOperations.FixedTimeEquals(expected, actual) ? null
                : new AssemblyIntegrityFailure(path, "Entry assembly hash mismatch.");
        }
    }
}
