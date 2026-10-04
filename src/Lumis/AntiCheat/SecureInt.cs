namespace Lumis;

/// <summary>Stores a 32-bit integer in an obfuscated form and verifies its integrity whenever it is read.</summary>
/// <remarks>
/// This is a user-mode anti-tamper primitive, not cryptographic secret storage.
/// It is not thread-safe.
/// </remarks>
public sealed class SecureInt
{
    private ulong encryptedValue;
    private ulong key;
    private ulong tag;

    /// <summary>Creates a secure integer initialized to zero.</summary>
    public SecureInt() : this(0) { }

    /// <summary>Creates a secure integer with the supplied initial value.</summary>
    public SecureInt(int value)
    {
        SetValue(value);
    }

    /// <summary>Gets or sets the protected integer value.</summary>
    public int Value
    {
        get
        {
            ulong plain = SecureValueCodec.Decrypt(encryptedValue, key);
            if (tag != SecureValueCodec.ComputeTag(plain, key))
                throw SecureValueCodec.CreateTamperException(nameof(SecureInt));

            return unchecked((int)(uint)plain);
        }
        set => SetValue(value);
    }

    /// <summary>Converts a secure integer to its protected value.</summary>
    public static implicit operator int(SecureInt value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Value;
    }

    /// <summary>Creates a secure integer from a normal integer.</summary>
    public static implicit operator SecureInt(int value) => new SecureInt(value);

    private void SetValue(int value)
    {
        ulong plain = unchecked((uint)value);
        key = SecureValueCodec.CreateKey();
        encryptedValue = SecureValueCodec.Encrypt(plain, key);
        tag = SecureValueCodec.ComputeTag(plain, key);
    }

    internal void CorruptForTesting()
    {
        encryptedValue ^= 1UL;
    }
}
