#nullable enable
using System;

namespace Lumis
{
    // Lightweight obfuscation/integrity checking, not cryptographic authentication or secret storage.
    internal sealed class ProtectedBits
    {
        private ulong encryptedValue;
        private ulong key;
        private ulong tag;
        private bool initialized;
        private readonly string typeName;
        internal ProtectedBits(ulong value, string typeName) { this.typeName = typeName; Write(value); }
        internal ulong Read()
        {
            ulong value = encryptedValue ^ Rotate(key ^ 0xA37F2C6D91E5B84BUL, 23);
            if (tag != Tag(value, key))
                throw new AntiCheatException(new AntiCheatViolationEventArgs(AntiCheatViolationType.MemoryTampering,
                    AntiCheatViolationPhase.Runtime, "Protected value verification failed: " + typeName));
            return value;
        }
        internal void Write(ulong value)
        {
            if (initialized) Read(); // Do not erase evidence of corruption by writing a new value.
            key = BitConverter.ToUInt64(SecurityCompat.RandomBytes(8), 0);
            encryptedValue = value ^ Rotate(key ^ 0xA37F2C6D91E5B84BUL, 23);
            tag = Tag(value, key);
            initialized = true;
        }
        internal void Corrupt() { encryptedValue ^= 1UL; }
        private static ulong Rotate(ulong value, int count) => (value << count) | (value >> (64 - count));
        private static ulong Tag(ulong value, ulong key)
        {
            unchecked
            {
                ulong mixed = value ^ Rotate(key, 17) ^ 0xD1B54A32D192ED03UL;
                mixed ^= mixed >> 30; mixed *= 0xBF58476D1CE4E5B9UL;
                mixed ^= mixed >> 27; mixed *= 0x94D049BB133111EBUL;
                return mixed ^ (mixed >> 31);
            }
        }
    }

    /// <summary>Obfuscated 32-bit value with integrity checks. Not thread-safe or Unity-serializable.</summary>
    public sealed class SecureInt
    {
        private readonly ProtectedBits bits;
        /// <summary>Initializes zero.</summary>
        public SecureInt() : this(0) { }
        /// <summary>Initializes a protected value.</summary>
        public SecureInt(int value) { bits = new ProtectedBits(unchecked((uint)value), nameof(SecureInt)); }
        /// <summary>Reads or writes a value, checking the previous representation for corruption.</summary>
        public int Value { get => unchecked((int)(uint)bits.Read()); set => bits.Write(unchecked((uint)value)); }
        /// <summary>Reads the protected value.</summary>
        public static implicit operator int(SecureInt value) { SecurityCompat.NotNull(value, nameof(value)); return value.Value; }
        /// <summary>Creates a protected value.</summary>
        public static implicit operator SecureInt(int value) => new SecureInt(value);
        internal void CorruptForTesting() => bits.Corrupt();
    }
    /// <summary>Obfuscated 64-bit value with integrity checks. Not thread-safe or Unity-serializable.</summary>
    public sealed class SecureLong
    {
        private readonly ProtectedBits bits;
        /// <summary>Initializes zero.</summary>
        public SecureLong() : this(0L) { }
        /// <summary>Initializes a protected value.</summary>
        public SecureLong(long value) { bits = new ProtectedBits(unchecked((ulong)value), nameof(SecureLong)); }
        /// <summary>Reads or writes a value, checking the previous representation for corruption.</summary>
        public long Value { get => unchecked((long)bits.Read()); set => bits.Write(unchecked((ulong)value)); }
        /// <summary>Reads the protected value.</summary>
        public static implicit operator long(SecureLong value) { SecurityCompat.NotNull(value, nameof(value)); return value.Value; }
        /// <summary>Creates a protected value.</summary>
        public static implicit operator SecureLong(long value) => new SecureLong(value);
        internal void CorruptForTesting() => bits.Corrupt();
    }
    /// <summary>Obfuscated single-precision value preserving all bits, including NaNs. Not thread-safe.</summary>
    public sealed class SecureFloat
    {
        private readonly ProtectedBits bits;
        /// <summary>Initializes zero.</summary>
        public SecureFloat() : this(0f) { }
        /// <summary>Initializes a protected value.</summary>
        public SecureFloat(float value) { bits = new ProtectedBits(unchecked((uint)BitConverter.SingleToInt32Bits(value)), nameof(SecureFloat)); }
        /// <summary>Reads or writes the protected bit pattern.</summary>
        public float Value
        {
            get => BitConverter.Int32BitsToSingle(unchecked((int)(uint)bits.Read()));
            set => bits.Write(unchecked((uint)BitConverter.SingleToInt32Bits(value)));
        }
        /// <summary>Reads the protected value.</summary>
        public static implicit operator float(SecureFloat value) { SecurityCompat.NotNull(value, nameof(value)); return value.Value; }
        /// <summary>Creates a protected value.</summary>
        public static implicit operator SecureFloat(float value) => new SecureFloat(value);
        internal void CorruptForTesting() => bits.Corrupt();
    }
    /// <summary>Obfuscated double-precision value preserving all bits, including NaNs. Not thread-safe.</summary>
    public sealed class SecureDouble
    {
        private readonly ProtectedBits bits;
        /// <summary>Initializes zero.</summary>
        public SecureDouble() : this(0d) { }
        /// <summary>Initializes a protected value.</summary>
        public SecureDouble(double value) { bits = new ProtectedBits(unchecked((ulong)BitConverter.DoubleToInt64Bits(value)), nameof(SecureDouble)); }
        /// <summary>Reads or writes the protected bit pattern.</summary>
        public double Value
        {
            get => BitConverter.Int64BitsToDouble(unchecked((long)bits.Read()));
            set => bits.Write(unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));
        }
        /// <summary>Reads the protected value.</summary>
        public static implicit operator double(SecureDouble value) { SecurityCompat.NotNull(value, nameof(value)); return value.Value; }
        /// <summary>Creates a protected value.</summary>
        public static implicit operator SecureDouble(double value) => new SecureDouble(value);
        internal void CorruptForTesting() => bits.Corrupt();
    }
}
