#nullable enable
using System;
using System.IO;
using UnityEngine;

namespace Lumis.Unity
{
    /// <summary>Explicit file-system roots. Android/WebGL StreamingAssets URLs are not synchronous file roots.</summary>
    public enum UnityIntegrityRoot
    {
        /// <summary>Application.streamingAssetsPath on file-system platforms.</summary>
        StreamingAssets,
        /// <summary>Application.dataPath, for example Managed/Assembly-CSharp.dll in a compatible desktop Mono build.</summary>
        Data,
        /// <summary>Application.persistentDataPath. Do not register a save that the game legitimately rewrites.</summary>
        PersistentData,
        /// <summary>Parent of Application.dataPath on Windows/Linux players, for example GameAssembly.dll on Windows.</summary>
        DesktopPlayerDirectory
    }
    /// <summary>Inspector-configurable build hash. Empty hashes are rejected, not captured automatically.</summary>
    [Serializable]
    public sealed class ProtectedUnityFile
    {
        /// <summary>File-system root.</summary>
        public UnityIntegrityRoot Root = UnityIntegrityRoot.StreamingAssets;
        /// <summary>Relative file path without traversal.</summary>
        public string RelativePath = "";
        /// <summary>64-character trusted SHA-256 from the build pipeline.</summary>
        public string Sha256 = "";
    }
    /// <summary>Conservative file path resolution for Unity's platform-dependent asset locations.</summary>
    public static class UnityIntegrityPaths
    {
        /// <summary>Resolves a local file only. Throws for unsupported roots or traversal; does not verify symlink targets.</summary>
        public static string Resolve(UnityIntegrityRoot root, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || relativePath.IndexOf('\0') >= 0 ||
                relativePath.IndexOf(':') >= 0 || Path.IsPathRooted(relativePath))
                throw new ArgumentException("Use a nonempty relative file path.", nameof(relativePath));
            foreach (string part in relativePath.Split(new[] { '/', '\\' }))
                if (part == "..") throw new ArgumentException("Parent traversal is not allowed.", nameof(relativePath));
            string basePath;
            switch (root)
            {
                case UnityIntegrityRoot.StreamingAssets: basePath = Application.streamingAssetsPath; break;
                case UnityIntegrityRoot.Data: basePath = Application.dataPath; break;
                case UnityIntegrityRoot.PersistentData: basePath = Application.persistentDataPath; break;
                case UnityIntegrityRoot.DesktopPlayerDirectory:
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
                    basePath = Path.GetDirectoryName(Application.dataPath) ?? ""; break;
#else
                    throw new PlatformNotSupportedException("DesktopPlayerDirectory is only defined for Windows/Linux players.");
#endif
                default: throw new ArgumentOutOfRangeException(nameof(root));
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new PlatformNotSupportedException("Synchronous protected-file paths are not supported by this WebGL adapter. Verify loaded bytes instead.");
#else
            if (string.IsNullOrWhiteSpace(basePath) || basePath.IndexOf("://", StringComparison.Ordinal) >= 0 ||
                basePath.StartsWith("jar:", StringComparison.OrdinalIgnoreCase))
                throw new PlatformNotSupportedException("This asset location is not a local filesystem path. Use UnityWebRequest and FileIntegrityService.VerifyData.");
            string normalized = relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(basePath, normalized));
#endif
        }
    }
}
