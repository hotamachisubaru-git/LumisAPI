#nullable enable
using System;
using System.Security.Cryptography;
using System.Text;

namespace Lumis
{
    /// <summary>AES-256-CBC plus HMAC-SHA256 encrypt-then-MAC saves for .NET Standard/Unity.</summary>
    /// <remarks>
    /// Uses separate derived encryption and authentication keys. Authenticates header, IV and ciphertext
    /// before decryption. Format LSP1 is not the existing GCM format. Does not prevent replay/rollback.
    /// Requires working platform RNG/AES/HMAC implementations; no plaintext fallback. Not thread-safe.
    /// </remarks>
    public sealed class PortableSaveDataProtector : IDisposable
    {
        private const int HeaderLength = 20; // LSP1 + 16-byte IV
        private const int TagLength = 32;
        private const int MaxDataLength = 64 * 1024 * 1024;
        private readonly byte[] encryptionKey;
        private readonly byte[] authenticationKey;
        private bool disposed;
        /// <summary>Derives distinct keys from a random 32-byte master key, not from a password.</summary>
        public PortableSaveDataProtector(byte[] key)
        {
            SecurityCompat.NotNull(key, nameof(key));
            if (key.Length != 32) throw new ArgumentException("A random 32-byte master key is required.", nameof(key));
            using (var hmac = new HMACSHA256(key))
            {
                encryptionKey = hmac.ComputeHash(Encoding.ASCII.GetBytes("Lumis.Security/LSP1/encryption"));
                authenticationKey = hmac.ComputeHash(Encoding.ASCII.GetBytes("Lumis.Security/LSP1/authentication"));
            }
        }
        /// <summary>Generates a random master key. Key persistence and user/device binding belong to the host.</summary>
        public static byte[] GenerateKey() => SecurityCompat.RandomBytes(32);
        /// <summary>Encrypts and authenticates at most 64 MiB of data in an LSP1 envelope.</summary>
        public byte[] Protect(byte[] data)
        {
            SecurityCompat.NotDisposed(disposed, this); SecurityCompat.NotNull(data, nameof(data));
            if (data.Length > MaxDataLength) throw new ArgumentOutOfRangeException(nameof(data));
            byte[] iv = SecurityCompat.RandomBytes(16);
            byte[] cipher;
            using (var aes = CreateAes(iv))
            using (var encryptor = aes.CreateEncryptor()) cipher = encryptor.TransformFinalBlock(data, 0, data.Length);
            byte[] result = new byte[checked(HeaderLength + cipher.Length + TagLength)];
            result[0] = (byte)'L'; result[1] = (byte)'S'; result[2] = (byte)'P'; result[3] = (byte)'1';
            Buffer.BlockCopy(iv, 0, result, 4, 16);
            Buffer.BlockCopy(cipher, 0, result, HeaderLength, cipher.Length);
            using (var hmac = new HMACSHA256(authenticationKey))
            {
                byte[] tag = hmac.ComputeHash(result, 0, result.Length - TagLength);
                Buffer.BlockCopy(tag, 0, result, result.Length - TagLength, TagLength);
            }
            return result;
        }
        /// <summary>Authenticates the entire envelope before decrypting; rejects wrong keys and altered data.</summary>
        public byte[] Unprotect(byte[] protectedData)
        {
            SecurityCompat.NotDisposed(disposed, this); SecurityCompat.NotNull(protectedData, nameof(protectedData));
            int length = protectedData.Length;
            if (length < HeaderLength + 16 + TagLength || length > MaxDataLength + HeaderLength + 16 + TagLength ||
                (length - HeaderLength - TagLength) % 16 != 0) throw SecurityCompat.SaveFailure();
            // Snapshot caller-owned data to avoid authenticate/decrypt races on the input buffer.
            byte[] envelope = (byte[])protectedData.Clone();
            if (envelope[0] != 'L' || envelope[1] != 'S' || envelope[2] != 'P' || envelope[3] != '1')
                throw SecurityCompat.SaveFailure();
            using (var hmac = new HMACSHA256(authenticationKey))
            {
                byte[] expected = hmac.ComputeHash(envelope, 0, length - TagLength);
                if (!CryptographicOperations.FixedTimeEquals(expected, envelope.AsSpan(length - TagLength, TagLength)))
                    throw SecurityCompat.SaveFailure();
            }
            byte[] iv = new byte[16]; Buffer.BlockCopy(envelope, 4, iv, 0, 16);
            try
            {
                using (var aes = CreateAes(iv))
                using (var decryptor = aes.CreateDecryptor())
                    return decryptor.TransformFinalBlock(envelope, HeaderLength, length - HeaderLength - TagLength);
            }
            catch (CryptographicException) { throw SecurityCompat.SaveFailure(); }
        }
        /// <summary>Protects UTF-8 text as a Base64 LSP1 envelope.</summary>
        public string ProtectString(string text)
        {
            SecurityCompat.NotNull(text, nameof(text));
            byte[] plain = Encoding.UTF8.GetBytes(text);
            try { return Convert.ToBase64String(Protect(plain)); }
            finally { CryptographicOperations.ZeroMemory(plain); }
        }
        /// <summary>Authenticates Base64 LSP1 data and restores UTF-8 text.</summary>
        public string UnprotectString(string protectedText)
        {
            SecurityCompat.NotDisposed(disposed, this); SecurityCompat.NotWhiteSpace(protectedText, nameof(protectedText));
            if (protectedText.Length > ((MaxDataLength + HeaderLength + 16 + TagLength + 2) / 3) * 4)
                throw SecurityCompat.SaveFailure();
            byte[] envelope;
            try { envelope = Convert.FromBase64String(protectedText); }
            catch (FormatException) { throw SecurityCompat.SaveFailure(); }
            byte[] plain = Unprotect(envelope);
            try { return Encoding.UTF8.GetString(plain); }
            finally { CryptographicOperations.ZeroMemory(plain); }
        }
        private Aes CreateAes(byte[] iv)
        {
            Aes aes = Aes.Create();
            aes.KeySize = 256; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
            aes.Key = encryptionKey; aes.IV = iv;
            return aes;
        }
        /// <summary>Clears derived keys. Does not clear the caller's master-key buffer.</summary>
        public void Dispose()
        {
            if (disposed) return;
            CryptographicOperations.ZeroMemory(encryptionKey);
            CryptographicOperations.ZeroMemory(authenticationKey);
            disposed = true;
        }
    }
}
