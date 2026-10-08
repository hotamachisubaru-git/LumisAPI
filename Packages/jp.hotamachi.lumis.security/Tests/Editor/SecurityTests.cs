using System;
using Lumis;
using Lumis.Unity;
using NUnit.Framework;

public sealed class SecurityTests
{
    [Test]
    public void SecureValueReadsAndDetectsCorruption()
    {
        var money = new SecureInt(100); money.Value += 50;
        Assert.AreEqual(150, money.Value);
        money.CorruptForTesting();
        Assert.Throws<AntiCheatException>(() => { int ignored = money.Value; });
    }
    [Test]
    public void PortableSavesAuthenticate()
    {
        using (var saves = new PortableSaveDataProtector(PortableSaveDataProtector.GenerateKey()))
        {
            Assert.AreEqual("セーブ", saves.UnprotectString(saves.ProtectString("セーブ")));
            byte[] data = saves.Protect(new byte[] { 1, 2, 3 });
            data[data.Length - 1] ^= 1;
            Assert.Throws<AntiCheatException>(() => saves.Unprotect(data));
        }
    }
    [Test]
    public void ParentTraversalIsRejected()
    {
        Assert.Throws<ArgumentException>(() => UnityIntegrityPaths.Resolve(UnityIntegrityRoot.Data, "../other"));
    }
}
