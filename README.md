# LumisAPI

LumisAPI is a lightweight game development API for C#.

Build desktop 2D games with a small, documented API backed by
[raylib-cs](https://github.com/raylib-cs/raylib-cs) and
[raylib](https://www.raylib.com/).

**Version:** 0.1.1 · **License:** MIT · **Library targets:** .NET 8 and .NET 10

The library targets `net8.0` and `net10.0`. Building this repository requires
the .NET 10 SDK selected by `global.json`. The sample and native smoke checks
run on .NET 10; the unit tests run on .NET 8 and .NET 10.

The public configuration API is verified with a dedicated C# 8 consumer build,
while the .NET 10 build covers the current C# toolchain. This means projects
using C# 8 through C# 14 can consume LumisAPI as long as they target a compatible
runtime. The minimum runtime remains .NET 8 because Raylib-cs 8.1.0 targets
.NET 8 and .NET 10.

The repository includes local packaging and a
[NuGet release workflow](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/docs/publishing.md).

## Features

- Window / Game Loop — resizable window, frame pacing, delta time, orderly shutdown
- 2D Rendering — shapes, text, textures, tinting, scaling, rotation, screenshots
- Input — keyboard and mouse held / pressed / released states
- Audio — sound effects, streamed music, playback controls and volume
- Scene Management — scene lifecycle callbacks and deferred transitions
- Anti-Cheat Level 1 — process / Cheat Engine / debugger detection at startup and runtime
- Anti-Cheat Level 2 — tamper-detecting `SecureInt`, `SecureLong`, `SecureFloat`, and `SecureDouble`
- Anti-Cheat Level 3 — SpeedHack / game-time acceleration detection
- Anti-Cheat Level 4 — SHA-256 file integrity verification for game data and assets
- Anti-Cheat Level 5 — authenticated save-data protection and entry-assembly integrity monitoring
- Automatic cleanup of textures and audio resources, with game-thread checks
- XML API documentation included in the NuGet package

## Installation

Add LumisAPI to a .NET 8 or .NET 10 project:

```sh
dotnet add package LumisAPI --version 0.1.1
```

## Quick start

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0),
then run from the repository root:

```sh
dotnet restore LumisAPI.sln
dotnet build LumisAPI.sln -c Release --no-restore
dotnet run --project samples/HelloLumis -c Release --no-build
```

The sample includes its own PNG and WAV assets. Use **WASD / arrow keys** to
move, **left click** to move to the pointer, **Space** to play a sound,
**M** to toggle music, **Tab** to change scenes, and **Escape** to exit.
If no audio device is available, pass `-- --no-audio` to `dotnet run`.

## Example

Replace the contents of a console project's `Program.cs` with:

```csharp
using System.Numerics;
using Lumis;

using var game = new Game();
game.Run();

public class Game : LumisGame
{
    public Game() : base(new GameSettings
    {
        Title = "Hello, Lumis!",
        Width = 960,
        Height = 540,
        EnableAudio = false
    }) { }

    protected override void Update(float deltaTime)
    {
        if (Input.IsKeyPressed(Key.Escape))
            Exit();
    }

    protected override void Draw()
    {
        Graphics.DrawText("Hello, Lumis!", new Vector2(40, 40), 32, Color.White);
        Graphics.DrawCircle(new Vector2(160, 160), 40, new Color(96, 210, 255));
    }
}
```

## Anti-cheat Levels 1–5

Anti-cheat is disabled by default. Enable only the layers your game needs.
LumisAPI keeps the checks in user mode and preserves normal cleanup when a
runtime violation stops the game.

### Level 1 — process, Cheat Engine and debugger detection

Level 1 checks before the native window opens and can continue scanning while
the game is running. The Cheat Engine matcher uses process names plus executable
path and version metadata when the operating system allows access.

```csharp
AntiCheat = new AntiCheatSettings
{
    Enabled = true,
    MonitorDuringGame = true,
    RuntimeScanInterval = TimeSpan.FromSeconds(2),
    ProcessDetection = new ProcessDetectionSettings
    {
        DetectCheatEngine = true,
        InspectExecutableMetadata = true,
        BlockedProcessNames = new[] { "MyGameTrainer" },
        BlockedExecutablePathFragments = new[] { "tools/trainer" }
    },
    DebuggerDetection = new DebuggerDetectionSettings
    {
        Enabled = false
    }
}
```

Windows additionally uses `IsDebuggerPresent`; Linux checks `TracerPid`.
macOS currently uses the managed debugger signal. Debugger detection is opt-in
so normal development is not blocked.

### Level 2 — protected numeric values

Use the secure numeric wrappers for values such as money, health, score or
experience that are common memory-edit targets.

```csharp
var money = new SecureInt(1000);
var experience = new SecureLong(50000);
var speed = new SecureFloat(4.5f);
var multiplier = new SecureDouble(1.25);

money.Value += 500;
```

The stored representation is obfuscated with a per-write random key and carries
an integrity tag. A failed integrity check throws `AntiCheatException` with
`MemoryTampering`. These values are anti-tamper primitives, not secret storage.

### Level 3 — SpeedHack / time manipulation

Level 3 compares accumulated game delta time with an independent monotonic
clock. It requires multiple suspicious observation windows by default to reduce
false positives from normal frame stalls.

```csharp
TimeManipulation = new TimeManipulationSettings
{
    Enabled = true,
    ObservationWindow = TimeSpan.FromSeconds(2),
    MaxGameTimeRatio = 1.75,
    RequiredConsecutiveDetections = 2
}
```

A tool that also successfully manipulates or hides the underlying monotonic
clock can bypass this layer, so it should be combined with the other levels.

### Level 4 — file integrity

Enable file integrity, then register trusted files before calling `Run()`.
A build-time SHA-256 value is stronger than taking a snapshot from the local
machine at startup.

```csharp
AntiCheat = new AntiCheatSettings
{
    Enabled = true,
    FileIntegrity = new FileIntegritySettings
    {
        Enabled = true,
        CheckOnStartup = true,
        MonitorDuringGame = true
    }
};

// In the derived game constructor, after the base constructor:
AntiCheat.FileIntegrity.RegisterFile(
    "data/items.json",
    "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF");

// Or use the current file as the baseline:
AntiCheat.FileIntegrity.RegisterCurrentFile("assets/player.png");
```

`RegisterDirectorySnapshot` can baseline a directory. Snapshot mode detects
later changes but cannot prove the files were trustworthy before registration.

### Level 5 — save-data and executable integrity

`SaveDataProtector` uses AES-256-GCM, so edited save data fails authentication.

```csharp
byte[] key = SaveDataProtector.GenerateKey();

using var saves = new SaveDataProtector(key);
string protectedSave = saves.ProtectString(json);
string restoredJson = saves.UnprotectString(protectedSave);
```

Keep the key outside user-editable save data. A key embedded entirely in a
client can eventually be extracted by a determined attacker; a launcher,
platform-protected secret or server-provided key is stronger.

Entry-assembly integrity can also be monitored:

```csharp
AssemblyIntegrity = new AssemblyIntegritySettings
{
    Enabled = true,
    CheckOnStartup = true,
    MonitorDuringGame = true,
    ExpectedEntryAssemblySha256 = trustedExeOrDllHash
}
```

If no trusted hash is supplied, LumisAPI snapshots the entry assembly on the
first check and can detect later on-disk changes only. Single-file deployments
may not expose an assembly path; `FailIfUnavailable` controls that behavior.

### Violation behavior and limits

A service-detected violation raises `AntiCheat.ViolationDetected` immediately
before throwing `AntiCheatException`. The event and exception identify the
violation type, startup/runtime phase, and process or file details when known.
Runtime exceptions pass through the normal Lumis cleanup path.

These five levels substantially raise the cost of casual client-side cheating,
but they are not kernel anti-cheat and cannot make an untrusted client
authoritative. Process hiding, code injection, kernel-level manipulation, or a
fully modified client can still bypass local checks. Networked games should
validate important state on a trusted server.

## Install from a local package

Create the package first:

```sh
dotnet pack src/Lumis/Lumis.csproj -c Release -o artifacts/packages
dotnet new console -n MyGame -f net10.0
cd MyGame
dotnet add package LumisAPI --version 0.1.1 --source ../artifacts/packages --no-restore
dotnet restore --source ../artifacts/packages --source https://api.nuget.org/v3/index.json
```

The raylib-cs dependency is restored from nuget.org. For a consumer outside
this checkout, use an absolute path to `artifacts/packages`.

## Repository layout

```text
LumisAPI/
├─ src/Lumis/
│  ├─ Core/
│  ├─ Graphics/
│  ├─ Input/
│  ├─ Audio/
│  ├─ Scene/
│  └─ AntiCheat/
├─ samples/HelloLumis/
├─ tests/
│  ├─ Lumis.Tests/
│  ├─ Lumis.CSharp8Consumer/
│  └─ Lumis.NativeSmoke/
├─ docs/
├─ README.md
├─ LICENSE
├─ CHANGELOG.md
└─ LumisAPI.sln
```

## Development

Install the .NET 8 runtime as well as the .NET 10 SDK to run both unit test
targets. Installing the .NET 8 SDK also supplies that runtime.

```sh
dotnet test LumisAPI.sln -c Release
dotnet build tests/Lumis.CSharp8Consumer/Lumis.CSharp8Consumer.csproj -c Release
dotnet run --project samples/HelloLumis -c Release -- --smoke --no-audio
dotnet pack src/Lumis/Lumis.csproj -c Release -o artifacts/packages
```

Unit tests cover scene transitions, lifetime guards, settings, and anti-cheat
Levels 1–5 without opening a window. A separate `LangVersion=8.0` consumer project
guards the public API against accidentally requiring C# 9-or-newer syntax. The sample's `--smoke` mode opens a real
window, exercises graphics, input polling and scene transitions, and closes
automatically. Omit `--no-audio` to exercise native audio as well. Add
`--capture screenshot.png` to save a frame during the smoke run.

To verify sequential windows, callback failure recovery, and native cleanup:

```sh
dotnet run --project tests/Lumis.NativeSmoke -c Release -- --no-audio
```

Omit `--no-audio` to verify audio resource cleanup as well.

The CI workflow builds and tests both library targets on Windows, Linux and
macOS, plus Linux graphics and lifecycle smoke checks under
Xvfb. Native runtime testing on your target machine is still
necessary. Desktop runtime assets come from raylib-cs 8.1.0 for `win-x64`,
`win-x86`, `linux-x64`, `osx-x64`, and `osx-arm64`. This API does not currently
support browser, mobile, Windows ARM64, or Linux ARM64 deployment.

- [API and lifecycle guide](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/docs/api.md)
- [Contributing and keeping main healthy](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/CONTRIBUTING.md)
- [Packaging and publishing](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/docs/publishing.md)
- [Local verification results (Japanese)](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/docs/verification.md)
- [Changelog](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/CHANGELOG.md)
- [Third-party notices](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/THIRD-PARTY-NOTICES.md)

## License

LumisAPI and the included sample assets are licensed under the [MIT License](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/LICENSE).
raylib-cs and raylib retain their own zlib licenses and copyright notices.
