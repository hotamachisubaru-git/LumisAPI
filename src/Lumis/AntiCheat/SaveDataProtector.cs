using System.Security.Cryptography;
using System.Text;

namespace Lumis;

/// <summary>
/// Protects save-data payloads with AES-256-GCM authenticated encryption so modification is detected.
/// </summary>
/// <remarks>
/// Keep the key outside user-editable save data. A key embedded in a client can eventually be
/// extracted by a determined attacker, so server-provided or platform-protected keys are preferred.
/// </remarks>
public sealed class SaveDataProtector : IDisposable
{
    private const byte FormatVersion = 1;
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] key;
    private bool disposed;

    /// <summary>Creates a save-data protector from a 32-byte AES-256 key.</summary>
    public SaveDataProtector(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != KeySize)
            throw new ArgumentException("Save-data protection requires a 32-byte AES-256 key.", nameof(key));

        this.key = (byte[])key.Clone();
    }

    /// <summary>Generates a new cryptographically random 32-byte key.</summary>
    public static byte[] GenerateKey()
    {
        return RandomNumberGenerator.GetBytes(KeySize);
    }

    /// <summary>Encrypts and authenticates arbitrary save-data bytes.</summary>
    public byte[] Protect(byte[] data)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(data);

        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] ciphertext = new byte[data.Length];
        byte[] tag = new byte[TagSize];

        using (var aes = new AesGcm(key, TagSize))
            aes.Encrypt(nonce, data, ciphertext, tag);

        byte[] envelope = new byte[1 + NonceSize + TagSize + ciphertext.Length];
        envelope[0] = FormatVersion;
        Buffer.BlockCopy(nonce, 0, envelope, 1, NonceSize);
        Buffer.BlockCopy(tag, 0, envelope, 1 + NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, envelope, 1 + NonceSize + TagSize, ciphertext.Length);
        return envelope;
    }

    /// <summary>Verifies and decrypts protected save-data bytes.</summary>
    /// <exception cref="AntiCheatException">Thrown when the payload is malformed or authentication fails.</exception>
    public byte[] Unprotect(byte[] protectedData)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(protectedData);

        if (protectedData.Length < 1 + NonceSize + TagSize || protectedData[0] != FormatVersion)
            throw CreateSaveTamperException("Anti-cheat rejected malformed or unsupported protected save data.");

        ReadOnlySpan<byte> nonce = protectedData.AsSpan(1, NonceSize);
        ReadOnlySpan<byte> tag = protectedData.AsSpan(1 + NonceSize, TagSize);
        ReadOnlySpan<byte> ciphertext = protectedData.AsSpan(1 + NonceSize + TagSize);
        byte[] plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return plaintext;
        }
        catch (AuthenticationTagMismatchException)
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw CreateSaveTamperException("Anti-cheat detected modified or incorrectly keyed protected save data.");
        }
        catch (CryptographicException)
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw CreateSaveTamperException("Anti-cheat could not authenticate protected save data.");
        }
    }

    /// <summary>Protects UTF-8 text and returns a Base64 envelope suitable for a text save file.</summary>
    public string ProtectString(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Convert.ToBase64String(Protect(Encoding.UTF8.GetBytes(text)));
    }

    /// <summary>Verifies and decrypts a Base64 UTF-8 save-data envelope.</summary>
    public string UnprotectString(string protectedText)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedText);

        byte[] envelope;
        try
        {
            envelope = Convert.FromBase64String(protectedText);
        }
        catch (FormatException)
        {
            throw CreateSaveTamperException("Anti-cheat rejected malformed Base64 protected save data.");
        }

        return Encoding.UTF8.GetString(Unprotect(envelope));
    }

    /// <summary>Clears the in-memory key copy held by this protector.</summary>
    public void Dispose()
    {
        if (disposed)
            return;

        CryptographicOperations.ZeroMemory(key);
        disposed = true;
        GC.SuppressFinalize(this);
    }

    private static AntiCheatException CreateSaveTamperException(string message)
    {
        return new AntiCheatException(new AntiCheatViolationEventArgs(
            AntiCheatViolationType.SaveDataTampering,
            AntiCheatViolationPhase.Runtime,
            message));
    }
}
