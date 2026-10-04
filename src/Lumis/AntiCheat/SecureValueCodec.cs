using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Lumis;

internal static class SecureValueCodec
{
    private const ulong KeySalt = 0xA37F2C6D91E5B84BUL;
    private const ulong TagSalt = 0xD1B54A32D192ED03UL;

    internal static ulong CreateKey()
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        RandomNumberGenerator.Fill(bytes);
        ulong key = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
        return key == 0 ? KeySalt : key;
    }

    internal static ulong Encrypt(ulong value, ulong key)
    {
        return value ^ RotateLeft(key ^ KeySalt, 23);
    }

    internal static ulong Decrypt(ulong encryptedValue, ulong key)
    {
        return encryptedValue ^ RotateLeft(key ^ KeySalt, 23);
    }

    internal static ulong ComputeTag(ulong value, ulong key)
    {
        ulong mixed = value ^ RotateLeft(key, 17) ^ TagSalt;
        mixed ^= mixed >> 30;
        mixed *= 0xBF58476D1CE4E5B9UL;
        mixed ^= mixed >> 27;
        mixed *= 0x94D049BB133111EBUL;
        mixed ^= mixed >> 31;
        return mixed;
    }

    internal static AntiCheatException CreateTamperException(string valueType)
    {
        return new AntiCheatException(new AntiCheatViolationEventArgs(
            AntiCheatViolationType.MemoryTampering,
            AntiCheatViolationPhase.Runtime,
            $"Anti-cheat detected tampering with {valueType}."));
    }

    private static ulong RotateLeft(ulong value, int offset)
    {
        return (value << offset) | (value >> (64 - offset));
    }
}
