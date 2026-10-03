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
- Anti-Cheat Level 1 — startup and runtime process monitoring, Cheat Engine name/metadata/path detection, custom block rules, optional debugger detection
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

## Anti-cheat Level 1

Anti-cheat is disabled by default. When enabled, LumisAPI checks before the
native window opens and can continue checking while the game is running.

The built-in Cheat Engine detector uses several signals when available:

- common process-name variants such as `cheatengine-x86_64`
- executable version metadata such as ProductName, FileDescription and OriginalFilename
- executable path segments such as a default `Cheat Engine 7.5` installation directory
- custom blocked process names and executable-path fragments
- optional debugger detection

```csharp
public Game() : base(new GameSettings
{
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
})
{
    AntiCheat.ViolationDetected += (_, violation) =>
        Console.Error.WriteLine(violation.Message);
}
```

If a violation is found during startup, `Run()` raises
`AntiCheatService.ViolationDetected` and throws `AntiCheatException` before
the game window opens. If a runtime scan detects a violation, the same exception
stops the loop and normal Lumis cleanup still runs before the exception is
re-thrown.

Debugger detection is opt-in so normal development is not blocked. The managed
debugger signal is portable; native detection is additionally strengthened on
Windows with `IsDebuggerPresent` and on Linux with `TracerPid`.

This is still a user-mode anti-cheat layer, not a guarantee that the client is
trusted. A sufficiently modified tool can strip metadata, move paths, hide
processes, or otherwise bypass client-side checks. Later levels should add value
integrity, file integrity and server-side validation where applicable.

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
matching without opening a window. A separate `LangVersion=8.0` consumer project
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
