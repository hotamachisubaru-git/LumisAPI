using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using Lumis;

internal static class Program
{
    private static int checks;
    private static void Main()
    {
        Assembly core = typeof(SecureInt).Assembly;
        Check(core.GetName().Name == "Lumis.Security", "standalone assembly");
        Check(core.GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName == ".NETStandard,Version=v2.1", "netstandard asset selected");
        Check(!core.GetReferencedAssemblies().Any(a => a.Name.Contains("Raylib") || a.Name.Contains("Unity")), "no engine dependency");
        var money = new SecureInt(100); money.Value += 50;
        Check(money.Value == 150, "integer arithmetic");
        money.CorruptForTesting();
        ExpectTamper(() => { int ignored = money.Value; }, AntiCheatViolationType.MemoryTampering);
        ExpectTamper(() => money.Value = 0, AntiCheatViolationType.MemoryTampering);
        Check(new SecureLong(long.MinValue).Value == long.MinValue, "long bit pattern");
        float nan = BitConverter.Int32BitsToSingle(unchecked((int)0x7FC01234));
        Check(BitConverter.SingleToInt32Bits(new SecureFloat(nan).Value) == BitConverter.SingleToInt32Bits(nan), "float NaN payload");
        Check(BitConverter.DoubleToInt64Bits(new SecureDouble(-0d).Value) == BitConverter.DoubleToInt64Bits(-0d), "double negative zero");

        byte[] key = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        using (var saves = new PortableSaveDataProtector(key))
        {
            // Independently generated AES-CBC/HMAC vector (fixed key and IV), not produced by this implementation.
            const string vector = "TFNQMSAhIiMkJSYnKCkqKywtLi8VMaO4qd8KN0d5Bo4ReFcOo2piXPY5JVeS9H5xCvvdpw2HHixYLccXrSnSchxR/bsqX8o21v/+sxE0q8fnWVfY";
            Check(saves.UnprotectString(vector) == "Lumis Unity セーブ: HP=100", "independent save format vector");
            foreach (string text in new[] { "", "x", "HP=100 所持金=150" })
                Check(saves.UnprotectString(saves.ProtectString(text)) == text, "portable text round trip");
            byte[] first = saves.Protect(new byte[0]);
            Check(!first.SequenceEqual(saves.Protect(new byte[0])), "random IV");
            foreach (int position in new[] { 0, 4, 20, first.Length - 1 })
            {
                byte[] corrupt = (byte[])first.Clone(); corrupt[position] ^= 1;
                ExpectTamper(() => saves.Unprotect(corrupt), AntiCheatViolationType.SaveDataTampering);
            }
            ExpectTamper(() => saves.Unprotect(new byte[3]), AntiCheatViolationType.SaveDataTampering);
            ExpectTamper(() => saves.UnprotectString("not base64!"), AntiCheatViolationType.SaveDataTampering);
            using (var wrong = new PortableSaveDataProtector(PortableSaveDataProtector.GenerateKey()))
                ExpectTamper(() => wrong.Unprotect(first), AntiCheatViolationType.SaveDataTampering);
        }
        var disposed = new PortableSaveDataProtector(key); disposed.Dispose();
        Expect<ObjectDisposedException>(() => disposed.Protect(new byte[0]));
        Check(!SaveDataProtector.IsSupported, "portable build explicitly reports no legacy GCM");
        Expect<PlatformNotSupportedException>(() => new SaveDataProtector(key));
        Check(FileIntegrityService.VerifyData(Encoding.UTF8.GetBytes("trusted"), "A9A089195C68D2ADEEE23BEAA2C3A93B1D4CDF09046E7A9E520B3B166DFF3E6A"), "known SHA-256");

        var names = new[] { "trainer" };
        var settings = new AntiCheatSettings { Enabled = true, ProcessDetection = new ProcessDetectionSettings { DetectCheatEngine = false, BlockedProcessNames = names } };
        var monitor = new AntiCheatService(settings, () => new[] { new ProcessSnapshot(7, "trainer") });
        settings.Enabled = false; names[0] = "allowed";
        ExpectTamper(monitor.CheckStartup, AntiCheatViolationType.BlockedProcess);
        monitor.ViolationDetected += (sender, e) => { throw new InvalidOperationException("observer failure"); };
        ExpectTamper(monitor.CheckStartup, AntiCheatViolationType.BlockedProcess);

        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "before");
            var files = new AntiCheatService(new AntiCheatSettings
            {
                Enabled = true, ProcessDetection = new ProcessDetectionSettings { Enabled = false },
                FileIntegrity = new FileIntegritySettings { Enabled = true }
            });
            files.FileIntegrity.RegisterCurrentFile(path); files.CheckStartup();
            File.WriteAllText(path, "after");
            ExpectTamper(files.CheckStartup, AntiCheatViolationType.FileIntegrity);
        }
        finally { File.Delete(path); }
        double now = 0;
        var time = new TimeManipulationDetector(new TimeManipulationSettings { ObservationWindow = TimeSpan.FromSeconds(1), RequiredConsecutiveDetections = 2 }, () => now);
        now = 1; Check(!time.Observe(2), "first timing window");
        time.Reset(); now = 2; Check(!time.Observe(2), "resume resets strikes");
        now = 3; Check(time.Observe(2), "consecutive acceleration");
        Console.WriteLine("Portable security smoke: " + checks + " checks passed against netstandard2.1 (not a Unity runtime test).");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAILED: " + message);
        checks++;
    }
    private static void Expect<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { checks++; return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private static void ExpectTamper(Action action, AntiCheatViolationType expected)
    {
        try { action(); }
        catch (AntiCheatException failure) { Check(failure.Type == expected, "violation kind"); return; }
        throw new Exception("Expected AntiCheatException");
    }
}
