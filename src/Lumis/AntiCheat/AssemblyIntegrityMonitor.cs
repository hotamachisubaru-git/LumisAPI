using System.Reflection;
using System.Security.Cryptography;

namespace Lumis;

internal sealed class AssemblyIntegrityMonitor
{
    private readonly AssemblyIntegritySettings settings;
    private readonly Func<string?> entryAssemblyPathProvider;
    private byte[]? expectedHash;
    private string? baselinePath;

    internal AssemblyIntegrityMonitor(
        AssemblyIntegritySettings settings,
        Func<string?>? entryAssemblyPathProvider = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        this.settings = settings;
        this.entryAssemblyPathProvider = entryAssemblyPathProvider ?? GetEntryAssemblyPath;

        if (!string.IsNullOrWhiteSpace(settings.ExpectedEntryAssemblySha256))
            expectedHash = Convert.FromHexString(settings.ExpectedEntryAssemblySha256);
    }

    internal AssemblyIntegrityFailure? Verify()
    {
        string? path = entryAssemblyPathProvider();
        if (string.IsNullOrWhiteSpace(path))
        {
            return settings.FailIfUnavailable
                ? new AssemblyIntegrityFailure(null, "Anti-cheat could not resolve the entry assembly file for integrity verification.")
                : null;
        }

        path = Path.GetFullPath(path);
        if (!File.Exists(path))
        {
            return settings.FailIfUnavailable
                ? new AssemblyIntegrityFailure(path, $"Anti-cheat could not find the entry assembly file: '{path}'.")
                : null;
        }

        byte[] actual;
        try
        {
            using FileStream stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                1024 * 64,
                FileOptions.SequentialScan);
            actual = SHA256.HashData(stream);
        }
        catch (IOException ex)
        {
            return new AssemblyIntegrityFailure(path, $"Anti-cheat could not verify the entry assembly '{path}': {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return new AssemblyIntegrityFailure(path, $"Anti-cheat could not access the entry assembly '{path}': {ex.Message}");
        }

        if (expectedHash is null)
        {
            expectedHash = actual;
            baselinePath = path;
            return null;
        }

        if (baselinePath is not null &&
            !string.Equals(
                baselinePath,
                path,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            return new AssemblyIntegrityFailure(
                path,
                $"Anti-cheat detected that the entry assembly path changed from '{baselinePath}' to '{path}'.");
        }

        baselinePath ??= path;

        if (!CryptographicOperations.FixedTimeEquals(expectedHash, actual))
            return new AssemblyIntegrityFailure(path, $"Anti-cheat detected a modified entry assembly: '{path}'.");

        return null;
    }

    private static string? GetEntryAssemblyPath()
    {
        string? location = Assembly.GetEntryAssembly()?.Location;
        return string.IsNullOrWhiteSpace(location) ? null : location;
    }
}
