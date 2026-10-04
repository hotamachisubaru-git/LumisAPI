namespace Lumis;

/// <summary>Stores a single-precision value in an obfuscated form and verifies its integrity whenever it is read.</summary>
/// <remarks>This type preserves the original floating-point bit pattern and is not thread-safe.</remarks>
public sealed class SecureFloat
{
    private ulong encryptedValue;
    private ulong key;
    private ulong tag;

    /// <summary>Creates a secure float initialized to zero.</summary>
    public SecureFloat() : this(0f) { }

    /// <summary>Creates a secure float with the supplied initial value.</summary>
    public SecureFloat(float value)
    {
        SetValue(value);
    }

    /// <summary>Gets or sets the protected floating-point value.</summary>
    public float Value
    {
        get
        {
            ulong plain = SecureValueCodec.Decrypt(encryptedValue, key);
            if (tag != SecureValueCodec.ComputeTag(plain, key))
                throw SecureValueCodec.CreateTamperException(nameof(SecureFloat));

            return BitConverter.Int32BitsToSingle(unchecked((int)(uint)plain));
        }
        set => SetValue(value);
    }

    /// <summary>Converts a secure float to its protected value.</summary>
    public static implicit operator float(SecureFloat value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Value;
    }

    /// <summary>Creates a secure float from a normal float.</summary>
    public static implicit operator SecureFloat(float value) => new SecureFloat(value);

    private void SetValue(float value)
    {
        ulong plain = unchecked((uint)BitConverter.SingleToInt32Bits(value));
        key = SecureValueCodec.CreateKey();
        encryptedValue = SecureValueCodec.Encrypt(plain, key);
        tag = SecureValueCodec.ComputeTag(plain, key);
    }

    internal void CorruptForTesting()
    {
        encryptedValue ^= 1UL;
    }
}
