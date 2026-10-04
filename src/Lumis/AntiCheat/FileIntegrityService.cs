using System.Security.Cryptography;

namespace Lumis;

/// <summary>Registers files and verifies their SHA-256 integrity for <see cref="AntiCheatService"/>.</summary>
/// <remarks>This service is intended to be configured on the game thread before <see cref="LumisGame.Run"/>.</remarks>
public sealed class FileIntegrityService
{
    private readonly FileIntegritySettings settings;
    private readonly Dictionary<string, byte[]> expectedHashes;

    internal FileIntegrityService(FileIntegritySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        this.settings = settings;
        expectedHashes = new Dictionary<string, byte[]>(
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    }

    /// <summary>Gets the number of files currently registered for integrity verification.</summary>
    public int RegisteredFileCount => expectedHashes.Count;

    /// <summary>
    /// Registers a file against a trusted SHA-256 value represented by 64 hexadecimal characters.
    /// </summary>
    public void RegisterFile(string path, string expectedSha256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSha256);

        byte[] expected;
        try
        {
            expected = Convert.FromHexString(expectedSha256);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Expected SHA-256 must contain exactly 64 hexadecimal characters.", nameof(expectedSha256), ex);
        }

        if (expected.Length != 32)
            throw new ArgumentException("Expected SHA-256 must contain exactly 64 hexadecimal characters.", nameof(expectedSha256));

        expectedHashes[Path.GetFullPath(path)] = expected;
    }

    /// <summary>Registers the current contents of a file as the baseline for later integrity checks.</summary>
    /// <remarks>
    /// A snapshot baseline detects modifications after registration but cannot prove the file was trusted
    /// before the baseline was captured. Use <see cref="RegisterFile"/> with a build-time hash when possible.
    /// </remarks>
    public void RegisterCurrentFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string fullPath = Path.GetFullPath(path);
        expectedHashes[fullPath] = ComputeSha256Bytes(fullPath);
    }

    /// <summary>Registers every matching file in a directory using its current contents as the baseline.</summary>
    /// <returns>The number of files registered.</returns>
    public int RegisterDirectorySnapshot(string directoryPath, string searchPattern = "*", bool recursive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);

        string fullDirectoryPath = Path.GetFullPath(directoryPath);
        SearchOption option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        string[] files = Directory.GetFiles(fullDirectoryPath, searchPattern, option);
        Array.Sort(files, StringComparer.Ordinal);

        foreach (string file in files)
            RegisterCurrentFile(file);

        return files.Length;
    }

    /// <summary>Computes the SHA-256 hash of a file as uppercase hexadecimal text.</summary>
    public static string ComputeSha256(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Convert.ToHexString(ComputeSha256Bytes(Path.GetFullPath(path)));
    }

    internal FileIntegrityFailure? Verify()
    {
        foreach (KeyValuePair<string, byte[]> item in expectedHashes)
        {
            string path = item.Key;

            if (!File.Exists(path))
            {
                if (settings.FailOnMissingFile)
                    return new FileIntegrityFailure(path, $"Anti-cheat detected a missing protected file: '{path}'.");

                continue;
            }

            byte[] actual;
            try
            {
                actual = ComputeSha256Bytes(path);
            }
            catch (IOException ex)
            {
                return new FileIntegrityFailure(path, $"Anti-cheat could not verify protected file '{path}': {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                return new FileIntegrityFailure(path, $"Anti-cheat could not access protected file '{path}': {ex.Message}");
            }

            if (!CryptographicOperations.FixedTimeEquals(item.Value, actual))
                return new FileIntegrityFailure(path, $"Anti-cheat detected a modified protected file: '{path}'.");
        }

        return null;
    }

    private static byte[] ComputeSha256Bytes(string fullPath)
    {
        using FileStream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            1024 * 64,
            FileOptions.SequentialScan);

        return SHA256.HashData(stream);
    }
}
