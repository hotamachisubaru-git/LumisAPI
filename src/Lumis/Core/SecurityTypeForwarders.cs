using System.Runtime.CompilerServices;
using Lumis;

// Preserve resolution of the public security types previously defined in Lumis.dll.
[assembly: TypeForwardedTo(typeof(AntiCheatException))]
[assembly: TypeForwardedTo(typeof(AntiCheatService))]
[assembly: TypeForwardedTo(typeof(AntiCheatSettings))]
[assembly: TypeForwardedTo(typeof(AntiCheatViolationEventArgs))]
[assembly: TypeForwardedTo(typeof(AntiCheatViolationPhase))]
[assembly: TypeForwardedTo(typeof(AntiCheatViolationType))]
[assembly: TypeForwardedTo(typeof(AssemblyIntegritySettings))]
[assembly: TypeForwardedTo(typeof(DebuggerDetectionSettings))]
[assembly: TypeForwardedTo(typeof(FileIntegrityService))]
[assembly: TypeForwardedTo(typeof(FileIntegritySettings))]
[assembly: TypeForwardedTo(typeof(ProcessDetectionSettings))]
[assembly: TypeForwardedTo(typeof(SaveDataProtector))]
[assembly: TypeForwardedTo(typeof(SecureInt))]
[assembly: TypeForwardedTo(typeof(SecureLong))]
[assembly: TypeForwardedTo(typeof(SecureFloat))]
[assembly: TypeForwardedTo(typeof(SecureDouble))]
[assembly: TypeForwardedTo(typeof(TimeManipulationSettings))]
