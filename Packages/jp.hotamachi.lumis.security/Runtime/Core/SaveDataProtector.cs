#nullable enable
using System;
using System.Security.Cryptography;
using System.Text;

namespace Lumis
{
    /// <summary>Legacy AES-256-GCM save format. Available on supported .NET 8+ runtimes, not the Unity source build.</summary>
    /// <remarks>Use PortableSaveDataProtector explicitly in Unity. The two envelope formats are not interchangeable.</remarks>
    public sealed class SaveDataProtector : IDisposable
    {
        private readonly byte[] key;
        private bool disposed;
        /// <summary>Whether the legacy GCM format is supported by this build/runtime.</summary>
        public static bool IsSupported => AntiCheatCapabilities.SupportsAesGcm;
        /// <summary>Copies a 32-byte key. Throws PlatformNotSupportedException when GCM is unavailable.</summary>
        public SaveDataProtector(byte[] key)
        {
            SecurityCompat.NotNull(key, nameof(key));
            if (key.Length != 32) throw new ArgumentException("A 32-byte key is required.", nameof(key));
            if (!IsSupported) throw new PlatformNotSupportedException("Use PortableSaveDataProtector on Unity/NET Standard. No silent format downgrade is performed.");
            this.key = (byte[])key.Clone();
        }
        /// <summary>Generates a random key. Persist it securely; do not generate a replacement on every load.</summary>
        public static byte[] GenerateKey() => SecurityCompat.RandomBytes(32);
        /// <summary>Creates a version-1 nonce/tag/ciphertext envelope, preserving the existing Lumis format.</summary>
        public byte[] Protect(byte[] data)
        {
            SecurityCompat.NotDisposed(disposed, this); SecurityCompat.NotNull(data, nameof(data));
#if NET8_0_OR_GREATER
            byte[] envelope = new byte[checked(29 + data.Length)];
            envelope[0] = 1;
            byte[] nonce = SecurityCompat.RandomBytes(12);
            Buffer.BlockCopy(nonce, 0, envelope, 1, 12);
            using (var aes = new AesGcm(key, 16))
                aes.Encrypt(nonce, data, envelope.AsSpan(29), envelope.AsSpan(13, 16));
            return envelope;
#else
            throw new PlatformNotSupportedException("AES-GCM is unavailable in this build.");
#endif
        }
        /// <summary>Authenticates and decrypts the legacy envelope.</summary>
        public byte[] Unprotect(byte[] protectedData)
        {
            SecurityCompat.NotDisposed(disposed, this); SecurityCompat.NotNull(protectedData, nameof(protectedData));
#if NET8_0_OR_GREATER
            if (protectedData.Length < 29 || protectedData[0] != 1) throw SecurityCompat.SaveFailure();
            byte[] plain = new byte[protectedData.Length - 29];
            try
            {
                using (var aes = new AesGcm(key, 16))
                    aes.Decrypt(protectedData.AsSpan(1, 12), protectedData.AsSpan(29), protectedData.AsSpan(13, 16), plain);
                return plain;
            }
            catch (CryptographicException) { CryptographicOperations.ZeroMemory(plain); throw SecurityCompat.SaveFailure(); }
#else
            throw new PlatformNotSupportedException("AES-GCM is unavailable in this build.");
#endif
        }
        /// <summary>Protects UTF-8 text as Base64.</summary>
        public string ProtectString(string text)
        {
            SecurityCompat.NotNull(text, nameof(text));
            byte[] plain = Encoding.UTF8.GetBytes(text);
            try { return Convert.ToBase64String(Protect(plain)); }
            finally { CryptographicOperations.ZeroMemory(plain); }
        }
        /// <summary>Authenticates a Base64 envelope and returns UTF-8 text.</summary>
        public string UnprotectString(string protectedText)
        {
            SecurityCompat.NotDisposed(disposed, this); SecurityCompat.NotWhiteSpace(protectedText, nameof(protectedText));
            byte[] envelope;
            try { envelope = Convert.FromBase64String(protectedText); }
            catch (FormatException) { throw SecurityCompat.SaveFailure(); }
            byte[] plain = Unprotect(envelope);
            try { return Encoding.UTF8.GetString(plain); }
            finally { CryptographicOperations.ZeroMemory(plain); }
        }
        /// <summary>Clears the owned key copy. Callers remain responsible for their original key buffer.</summary>
        public void Dispose()
        {
            if (disposed) return;
            CryptographicOperations.ZeroMemory(key); disposed = true;
        }
    }
}
