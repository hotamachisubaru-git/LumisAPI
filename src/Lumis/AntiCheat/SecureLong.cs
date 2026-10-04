namespace Lumis;

/// <summary>Stores a 64-bit integer in an obfuscated form and verifies its integrity whenever it is read.</summary>
/// <remarks>This type is not thread-safe.</remarks>
public sealed class SecureLong
{
    private ulong encryptedValue;
    private ulong key;
    private ulong tag;

    /// <summary>Creates a secure long initialized to zero.</summary>
    public SecureLong() : this(0L) { }

    /// <summary>Creates a secure long with the supplied initial value.</summary>
    public SecureLong(long value)
    {
        SetValue(value);
    }

    /// <summary>Gets or sets the protected 64-bit integer value.</summary>
    public long Value
    {
        get
        {
            ulong plain = SecureValueCodec.Decrypt(encryptedValue, key);
            if (tag != SecureValueCodec.ComputeTag(plain, key))
                throw SecureValueCodec.CreateTamperException(nameof(SecureLong));

            return unchecked((long)plain);
        }
        set => SetValue(value);
    }

    /// <summary>Converts a secure long to its protected value.</summary>
    public static implicit operator long(SecureLong value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Value;
    }

    /// <summary>Creates a secure long from a normal long.</summary>
    public static implicit operator SecureLong(long value) => new SecureLong(value);

    private void SetValue(long value)
    {
        ulong plain = unchecked((ulong)value);
        key = SecureValueCodec.CreateKey();
        encryptedValue = SecureValueCodec.Encrypt(plain, key);
        tag = SecureValueCodec.ComputeTag(plain, key);
    }

    internal void CorruptForTesting()
    {
        encryptedValue ^= 1UL;
    }
}
