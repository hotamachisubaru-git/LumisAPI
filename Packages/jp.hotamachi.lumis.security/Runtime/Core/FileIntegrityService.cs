#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Lumis
{
    internal sealed class FileIntegrityFailure
    {
        internal FileIntegrityFailure(string path, string message) { FilePath = path; Message = message; }
        internal string FilePath { get; }
        internal string Message { get; }
    }
    /// <summary>SHA-256 verification of explicitly registered files. Configure and use on the host thread.</summary>
    public sealed class FileIntegrityService
    {
        private readonly FileIntegritySettings settings;
        private readonly Dictionary<string, byte[]> hashes;
        internal FileIntegrityService(FileIntegritySettings settings)
        {
            SecurityCompat.NotNull(settings, nameof(settings));
            this.settings = settings.Copy();
            hashes = new Dictionary<string, byte[]>(SecurityCompat.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        }
        /// <summary>Number of registered files.</summary>
        public int RegisteredFileCount => hashes.Count;
        /// <summary>Registers a trusted external build hash; does not read the file yet.</summary>
        public void RegisterFile(string path, string expectedSha256)
        {
            SecurityCompat.NotWhiteSpace(path, nameof(path));
            byte[] expected = SecurityCompat.ParseSha256(expectedSha256, nameof(expectedSha256));
            hashes[Path.GetFullPath(path)] = expected;
        }
        /// <summary>Captures current contents. Only later modifications can be detected by this baseline.</summary>
        public void RegisterCurrentFile(string path)
        {
            SecurityCompat.NotWhiteSpace(path, nameof(path));
            string full = Path.GetFullPath(path);
            hashes[full] = SecurityCompat.HashFile(full);
        }
        /// <summary>Snapshots matching files; files added later are not included. Returns the number registered.</summary>
        public int RegisterDirectorySnapshot(string directoryPath, string searchPattern = "*", bool recursive = true)
        {
            SecurityCompat.NotWhiteSpace(directoryPath, nameof(directoryPath));
            SecurityCompat.NotWhiteSpace(searchPattern, nameof(searchPattern));
            string[] files = Directory.GetFiles(Path.GetFullPath(directoryPath), searchPattern,
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.Ordinal);
            foreach (string path in files) RegisterCurrentFile(path);
            return files.Length;
        }
        /// <summary>Hashes a file, returning uppercase hexadecimal. This does not establish trust.</summary>
        public static string ComputeSha256(string path)
        {
            SecurityCompat.NotWhiteSpace(path, nameof(path));
            return SecurityCompat.Hex(SecurityCompat.HashFile(Path.GetFullPath(path)));
        }
        /// <summary>Checks already-loaded bytes, including assets obtained by UnityWebRequest, against a trusted hash.</summary>
        public static bool VerifyData(byte[] data, string expectedSha256)
        {
            SecurityCompat.NotNull(data, nameof(data));
            byte[] expected = SecurityCompat.ParseSha256(expectedSha256, nameof(expectedSha256));
            using (var sha = SHA256.Create()) return CryptographicOperations.FixedTimeEquals(expected, sha.ComputeHash(data));
        }
        internal FileIntegrityFailure? Verify()
        {
            foreach (KeyValuePair<string, byte[]> item in hashes)
            {
                try
                {
                    byte[] actual = SecurityCompat.HashFile(item.Key);
                    if (!CryptographicOperations.FixedTimeEquals(item.Value, actual))
                        return new FileIntegrityFailure(item.Key, "Protected file hash mismatch: " + item.Key);
                }
                catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
                {
                    if (settings.FailOnMissingFile) return new FileIntegrityFailure(item.Key, "Protected file is missing: " + item.Key);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    return new FileIntegrityFailure(item.Key, "Protected file could not be read: " + item.Key);
                }
            }
            return null;
        }
    }
}
