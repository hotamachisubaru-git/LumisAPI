namespace Lumis;

/// <summary>Stores a double-precision value in an obfuscated form and verifies its integrity whenever it is read.</summary>
/// <remarks>This type preserves the original floating-point bit pattern and is not thread-safe.</remarks>
public sealed class SecureDouble
{
    private ulong encryptedValue;
    private ulong key;
    private ulong tag;

    /// <summary>Creates a secure double initialized to zero.</summary>
    public SecureDouble() : this(0d) { }

    /// <summary>Creates a secure double with the supplied initial value.</summary>
    public SecureDouble(double value)
    {
        SetValue(value);
    }

    /// <summary>Gets or sets the protected floating-point value.</summary>
    public double Value
    {
        get
        {
            ulong plain = SecureValueCodec.Decrypt(encryptedValue, key);
            if (tag != SecureValueCodec.ComputeTag(plain, key))
                throw SecureValueCodec.CreateTamperException(nameof(SecureDouble));

            return BitConverter.Int64BitsToDouble(unchecked((long)plain));
        }
        set => SetValue(value);
    }

    /// <summary>Converts a secure double to its protected value.</summary>
    public static implicit operator double(SecureDouble value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Value;
    }

    /// <summary>Creates a secure double from a normal double.</summary>
    public static implicit operator SecureDouble(double value) => new SecureDouble(value);

    private void SetValue(double value)
    {
        ulong plain = unchecked((ulong)BitConverter.DoubleToInt64Bits(value));
        key = SecureValueCodec.CreateKey();
        encryptedValue = SecureValueCodec.Encrypt(plain, key);
        tag = SecureValueCodec.ComputeTag(plain, key);
    }

    internal void CorruptForTesting()
    {
        encryptedValue ^= 1UL;
    }
}
