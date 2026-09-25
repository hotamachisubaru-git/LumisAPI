# LumisAPI

LumisAPI is a lightweight game development API for C#.

Build desktop 2D games with a small, documented API backed by
[raylib-cs](https://github.com/raylib-cs/raylib-cs) and
[raylib](https://www.raylib.com/).

**Version:** 0.1.0 · **License:** MIT · **Library targets:** .NET 8 and .NET 10

The library targets `net8.0` and `net10.0`. Building this repository requires
the .NET 10 SDK selected by `global.json`. The sample and native smoke checks
run on .NET 10; the unit tests run on .NET 8 and .NET 10.
The repository includes local packaging and a
[NuGet release workflow](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/docs/publishing.md).

## Features

- Window / Game Loop — resizable window, frame pacing, delta time, orderly shutdown
- 2D Rendering — shapes, text, textures, tinting, scaling, rotation, screenshots
- Input — keyboard and mouse held / pressed / released states
- Audio — sound effects, streamed music, playback controls and volume
- Scene Management — scene lifecycle callbacks and deferred transitions
- Automatic cleanup of textures and audio resources, with game-thread checks
- XML API documentation included in the NuGet package

## Installation

Add LumisAPI to a .NET 8 or .NET 10 project:

```sh
dotnet add package LumisAPI --version 0.1.0
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

## Install from a local package

Create the package first:

```sh
dotnet pack src/Lumis/Lumis.csproj -c Release -o artifacts/packages
dotnet new console -n MyGame -f net10.0
cd MyGame
dotnet add package LumisAPI --version 0.1.0 --source ../artifacts/packages --no-restore
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
│  └─ Scene/
├─ samples/HelloLumis/
├─ tests/
│  ├─ Lumis.Tests/
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
dotnet run --project samples/HelloLumis -c Release -- --smoke --no-audio
dotnet pack src/Lumis/Lumis.csproj -c Release -o artifacts/packages
```

Unit tests cover scene transitions, lifetime guards, and settings without opening
a window. The sample's `--smoke` mode opens a real window, exercises graphics,
input polling and scene transitions, and closes automatically. Omit
`--no-audio` to exercise native audio as well. Add `--capture screenshot.png`
to save a frame during the smoke run.

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
