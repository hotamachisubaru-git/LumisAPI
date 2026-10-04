using System.Text;

namespace Lumis.Tests;

public sealed class SaveDataProtectorTests
{
    [Fact]
    public void ProtectedBytesRoundTrip()
    {
        byte[] key = SaveDataProtector.GenerateKey();
        using var protector = new SaveDataProtector(key);
        byte[] input = Encoding.UTF8.GetBytes("player-level=42");

        byte[] protectedData = protector.Protect(input);
        byte[] restored = protector.Unprotect(protectedData);

        Assert.Equal(input, restored);
        Assert.NotEqual(input, protectedData);
    }

    [Fact]
    public void ProtectedStringRoundTrips()
    {
        using var protector = new SaveDataProtector(SaveDataProtector.GenerateKey());

        string token = protector.ProtectString("{\"money\":12345}");
        string restored = protector.UnprotectString(token);

        Assert.Equal("{\"money\":12345}", restored);
    }

    [Fact]
    public void ModifiedSaveDataRaisesTamperingViolation()
    {
        using var protector = new SaveDataProtector(SaveDataProtector.GenerateKey());
        byte[] protectedData = protector.Protect(Encoding.UTF8.GetBytes("trusted"));
        protectedData[^1] ^= 1;

        AntiCheatException failure =
            Assert.Throws<AntiCheatException>(() => protector.Unprotect(protectedData));

        Assert.Equal(AntiCheatViolationType.SaveDataTampering, failure.Type);
    }

    [Fact]
    public void WrongKeyRaisesTamperingViolation()
    {
        byte[] data;
        using (var writer = new SaveDataProtector(SaveDataProtector.GenerateKey()))
            data = writer.Protect(Encoding.UTF8.GetBytes("trusted"));

        using var reader = new SaveDataProtector(SaveDataProtector.GenerateKey());

        AntiCheatException failure =
            Assert.Throws<AntiCheatException>(() => reader.Unprotect(data));

        Assert.Equal(AntiCheatViolationType.SaveDataTampering, failure.Type);
    }

    [Fact]
    public void KeyMustBeExactly256Bits()
    {
        var failure = Assert.Throws<ArgumentException>(() => new SaveDataProtector(new byte[31]));

        Assert.Equal("key", failure.ParamName);
    }
}
