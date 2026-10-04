namespace Lumis.Tests;

public sealed class SecureValueTests
{
    [Fact]
    public void SecureIntRoundTripsAndCanBeUpdated()
    {
        var value = new SecureInt(-12345);

        Assert.Equal(-12345, value.Value);
        value.Value = 98765;
        Assert.Equal(98765, value.Value);

        int converted = value;
        Assert.Equal(98765, converted);
    }

    [Fact]
    public void SecureLongRoundTripsExtremeValue()
    {
        var value = new SecureLong(long.MinValue);

        Assert.Equal(long.MinValue, value.Value);
        value.Value = long.MaxValue;
        Assert.Equal(long.MaxValue, value.Value);
    }

    [Fact]
    public void SecureFloatPreservesBitPattern()
    {
        float original = BitConverter.Int32BitsToSingle(unchecked((int)0x7FC01234));
        var value = new SecureFloat(original);

        Assert.Equal(
            BitConverter.SingleToInt32Bits(original),
            BitConverter.SingleToInt32Bits(value.Value));
    }

    [Fact]
    public void SecureDoublePreservesBitPattern()
    {
        double original = BitConverter.Int64BitsToDouble(unchecked((long)0x7FF8000012345678UL));
        var value = new SecureDouble(original);

        Assert.Equal(
            BitConverter.DoubleToInt64Bits(original),
            BitConverter.DoubleToInt64Bits(value.Value));
    }

    [Theory]
    [MemberData(nameof(CorruptedValues))]
    public void CorruptedSecureValuesRaiseMemoryTampering(Func<AntiCheatException> readCorrupted)
    {
        AntiCheatException failure = readCorrupted();

        Assert.Equal(AntiCheatViolationType.MemoryTampering, failure.Type);
        Assert.Equal(AntiCheatViolationPhase.Runtime, failure.Phase);
    }

    public static IEnumerable<object[]> CorruptedValues()
    {
        yield return new object[]
        {
            () =>
            {
                var value = new SecureInt(10);
                value.CorruptForTesting();
                return Assert.Throws<AntiCheatException>(() => _ = value.Value);
            }
        };
        yield return new object[]
        {
            () =>
            {
                var value = new SecureLong(10);
                value.CorruptForTesting();
                return Assert.Throws<AntiCheatException>(() => _ = value.Value);
            }
        };
        yield return new object[]
        {
            () =>
            {
                var value = new SecureFloat(10f);
                value.CorruptForTesting();
                return Assert.Throws<AntiCheatException>(() => _ = value.Value);
            }
        };
        yield return new object[]
        {
            () =>
            {
                var value = new SecureDouble(10d);
                value.CorruptForTesting();
                return Assert.Throws<AntiCheatException>(() => _ = value.Value);
            }
        };
    }
}
