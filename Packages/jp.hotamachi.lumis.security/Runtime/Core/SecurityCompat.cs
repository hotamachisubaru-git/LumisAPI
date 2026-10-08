#nullable enable
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Lumis
{
    internal static class SecurityCompat
    {
        internal static void NotNull(object? value, string name)
        { if (value == null) throw new ArgumentNullException(name); }
        internal static void NotWhiteSpace(string? value, string name)
        {
            if (value == null) throw new ArgumentNullException(name);
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A nonempty value is required.", name);
        }
        internal static void NotDisposed(bool disposed, object owner)
        { if (disposed) throw new ObjectDisposedException(owner.GetType().Name); }
        internal static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        internal static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        internal static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        internal static byte[] RandomBytes(int count)
        {
            var bytes = new byte[count];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            return bytes;
        }
        internal static byte[] HashFile(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan))
            using (var sha = SHA256.Create()) return sha.ComputeHash(stream);
        }
        internal static string Hex(byte[] data) => BitConverter.ToString(data).Replace("-", "");
        internal static byte[] ParseSha256(string text, string name)
        {
            if (text == null) throw new ArgumentNullException(name);
            if (text.Length != 64) throw new ArgumentException("A SHA-256 hash must have 64 hexadecimal characters.", name);
            var bytes = new byte[32];
            for (int i = 0; i < bytes.Length; i++)
            {
                int high = Digit(text[i * 2]); int low = Digit(text[i * 2 + 1]);
                if (high < 0 || low < 0) throw new ArgumentException("Invalid hexadecimal hash.", name);
                bytes[i] = (byte)((high << 4) | low);
            }
            return bytes;
        }
        private static int Digit(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return -1;
        }
        internal static AntiCheatException SaveFailure() => new AntiCheatException(
            new AntiCheatViolationEventArgs(AntiCheatViolationType.SaveDataTampering,
                AntiCheatViolationPhase.Runtime, "Protected save data could not be authenticated."));
    }

    /// <summary>Conservative platform capabilities; not evidence that any detector is effective.</summary>
    public static class AntiCheatCapabilities
    {
        /// <summary>Whether desktop process APIs should be attempted. Individual processes may still deny access.</summary>
        public static bool SupportsProcessInspection
        {
            get
            {
#if UNITY_5_3_OR_NEWER && !UNITY_EDITOR && !UNITY_STANDALONE
                return false;
#else
                return SecurityCompat.IsWindows || SecurityCompat.IsLinux || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#endif
            }
        }
        /// <summary>Whether this build includes native Windows/Linux debugger signals.</summary>
        public static bool SupportsNativeDebuggerInspection => SupportsProcessInspection && (SecurityCompat.IsWindows || SecurityCompat.IsLinux);
        /// <summary>Whether the legacy AES-GCM save format is supported by this build and runtime.</summary>
        public static bool SupportsAesGcm
        {
            get
            {
#if NET8_0_OR_GREATER
                return AesGcm.IsSupported;
#else
                // Unity/NET Standard use the explicit portable authenticated format, never a silent downgrade.
                return false;
#endif
            }
        }
    }
}
