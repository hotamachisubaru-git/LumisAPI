using System.Text;

namespace Lumis.Tests;

public sealed class PortableSaveDataTests
{
    [Fact]
    public void PortableFormatMatchesIndependentVector()
    {
        byte[] key = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        using var protector = new PortableSaveDataProtector(key);
        Assert.Equal("Lumis Unity セーブ: HP=100", protector.UnprotectString(
            "TFNQMSAhIiMkJSYnKCkqKywtLi8VMaO4qd8KN0d5Bo4ReFcOo2piXPY5JVeS9H5xCvvdpw2HHixYLccXrSnSchxR/bsqX8o21v/+sxE0q8fnWVfY"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(20)]
    [InlineData(67)]
    public void EnvelopeCorruptionFailsAuthentication(int offset)
    {
        using var protector = new PortableSaveDataProtector(PortableSaveDataProtector.GenerateKey());
        byte[] bytes = protector.Protect(Encoding.UTF8.GetBytes("small save"));
        bytes[offset] ^= 1;
        Assert.Equal(AntiCheatViolationType.SaveDataTampering,
            Assert.Throws<AntiCheatException>(() => protector.Unprotect(bytes)).Type);
    }

    [Fact]
    public void MutatingCallerSettingsCannotDisableCopiedConfiguration()
    {
        var names = new[] { "trainer" };
        var settings = new AntiCheatSettings
        {
            Enabled = true,
            ProcessDetection = new ProcessDetectionSettings { DetectCheatEngine = false, BlockedProcessNames = names }
        };
        var service = new AntiCheatService(settings, () => new[] { new ProcessSnapshot(1, "trainer") });
        settings.Enabled = false;
        names[0] = "allowed";
        Assert.Throws<AntiCheatException>(service.CheckStartup);
    }

    [Fact]
    public void WritesDoNotEraseExistingValueCorruption()
    {
        var money = new SecureInt(10);
        money.CorruptForTesting();
        Assert.Throws<AntiCheatException>(() => money.Value = 11);
    }

    [Fact]
    public void ObserverFailureDoesNotHideAntiCheatFailure()
    {
        var service = new AntiCheatService(new AntiCheatSettings { Enabled = true },
            () => new[] { new ProcessSnapshot(2, "cheatengine") });
        service.ViolationDetected += (_, _) => throw new InvalidOperationException("observer");
        var error = Assert.Throws<AntiCheatException>(service.CheckStartup);
        Assert.IsType<InvalidOperationException>(error.InnerException);
    }
}
