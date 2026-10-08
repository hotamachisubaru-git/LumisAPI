namespace Lumis.Tests;

public sealed class SecurityCompatibilityTests
{
    [Fact]
    public void OriginalAssemblyQualifiedNamesResolveThroughTypeForwarders()
    {
        Assert.Same(typeof(SecureInt), Type.GetType("Lumis.SecureInt, Lumis", throwOnError: true));
        Assert.Same(typeof(AntiCheatSettings), Type.GetType("Lumis.AntiCheatSettings, Lumis", throwOnError: true));
        Assert.Same(typeof(SaveDataProtector), Type.GetType("Lumis.SaveDataProtector, Lumis", throwOnError: true));
        Assert.Contains(typeof(AntiCheatService), typeof(LumisGame).Assembly.GetForwardedTypes());
        Assert.Equal("Lumis.Security", typeof(AntiCheatService).Assembly.GetName().Name);
    }
}
