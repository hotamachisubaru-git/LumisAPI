# API and lifecycle

All public types live in `Lumis`. The library targets .NET 8 and .NET 10 and
uses `System.Numerics.Vector2` for positions. Unit tests cover both targets;
the sample and native smoke project target .NET 10. Public configuration types
use ordinary setters so a C# 8 consumer can configure the API. CI builds a
dedicated `LangVersion=8.0` consumer project in addition to the normal .NET 10
build. Window and resource
operations must stay on the main thread. Construct, run and dispose the game
on that thread.
`Exit()` is the only operation intended for other threads.

## Game

Derive from `LumisGame`, configure a `GameSettings` instance before passing
it to the constructor, and call `Run()`. Each game instance runs once, and only one may run in the
process at a time. Escape does not close the window automatically; handle it
through `Input` when desired. The close button always ends the loop.

The lifecycle is:

1. Run enabled anti-cheat startup checks before native window creation.
2. Open the window and optional audio device.
3. Call `OnLoad()` to load textures, sounds and music and request an initial scene.
4. Each frame: run any due runtime anti-cheat scan, apply a pending scene, call
   game `Update(deltaTime)`, scene `Update(deltaTime)`, update music streams,
   clear the background, then call game `Draw()` and scene `Draw(Graphics2D)`.
5. On shutdown: exit the active scene, call `OnUnload()`, release remaining
   tracked resources, close audio, then close the window.

`deltaTime` is elapsed time in seconds, capped at 0.25 after stalls. This is a
variable-step game loop. Multiply velocity by delta time for movement; it is
not a fixed-step physics engine. A scene switch requested during a callback
becomes active on the following frame.

Cleanup still runs if a callback throws. The original exception is rethrown;
if cleanup also fails, all exceptions are reported in an `AggregateException`.
`OnUnload` must handle partially initialized fields because it also runs when
`OnLoad` fails. Call `Exit` to stop the loop or `Dispose` on the game thread to
request a stop; cleanup completes before `Run` returns.

## Anti-cheat

Anti-cheat is opt-in through `GameSettings.AntiCheat`. Startup checks execute
before raylib creates the game window. Runtime monitoring is enabled by default
after anti-cheat itself is enabled and runs at the configured
`RuntimeScanInterval` (two seconds by default).

```csharp
var settings = new GameSettings
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
            BlockedProcessNames = new[] { "MyGameTrainer.exe" },
            BlockedExecutablePathFragments = new[] { "tools/trainer" }
        },
        DebuggerDetection = new DebuggerDetectionSettings
        {
            Enabled = false
        }
    }
};
```

### Process detection

`ProcessDetectionSettings.Enabled` controls process checks.
`DetectCheatEngine` enables the built-in matcher for common Cheat Engine
process names. When `InspectExecutableMetadata` is enabled, Lumis also tries to
read each accessible executable path and version metadata. This can identify
some renamed executables when fields such as ProductName, FileDescription or
OriginalFilename still contain Cheat Engine identifiers.

`BlockedProcessNames` is matched case-insensitively and ignores an optional
`.exe` suffix. `BlockedExecutablePathFragments` adds case-insensitive path
fragment matching for game-specific trainers or tools.

Process enumeration works on supported desktop runtimes. Reading another
process's executable path or version metadata can be denied by the operating
system; inaccessible metadata is skipped instead of failing the game.

### Debugger detection

`DebuggerDetectionSettings.Enabled` is false by default so development builds
can be debugged normally. When enabled, the managed debugger state can be
checked on every platform. Native debugger detection is additionally supported
on Windows through `IsDebuggerPresent` and on Linux through
`/proc/self/status` `TracerPid`. macOS currently relies on the managed
debugger signal.

### Violation behavior

A detected violation raises `AntiCheat.ViolationDetected` immediately before
throwing `AntiCheatException`. The event and exception report whether the
violation occurred during startup or runtime and include process name, PID and
executable path when available.

A startup violation prevents the native window, audio device and `OnLoad()`
from starting. A runtime violation exits through the normal `LumisGame`
exception cleanup path, so scenes, resources, audio and the window are still
cleaned up before the exception is re-thrown.

This remains a user-mode defense layer. Renaming plus stripping metadata,
process hiding, injection techniques and kernel-level attacks can bypass
client-side checks. Treat it as layered friction rather than proof that a client
is trustworthy, and add value integrity, file integrity and server-authoritative
validation in later levels.

## Graphics

`Graphics` exposes `DrawText`, `DrawRectangle`, `DrawRectangleLines`,
`DrawCircle`, `DrawLine`, and `DrawTexture`. Drawing must happen in a draw
callback. Use `BackgroundColor` for automatic frame clearing, or `Clear` to
replace it during drawing. Pixel coordinates start at the top left.

```csharp
// OnLoad:
sprite = Graphics.LoadTexture(Path.Combine(AppContext.BaseDirectory, "Assets", "sprite.png"));

// Draw:
Graphics.DrawTexture(sprite, new Vector2(100, 100));
Graphics.DrawTexture(sprite, new Rect(300, 100, 128, 128), Color.White);
```

The region overload also takes a source rectangle, destination rectangle,
origin and clockwise rotation in degrees. `Color` has byte RGBA components;
`WithAlpha` changes transparency. `Rect` has finite, nonnegative dimensions,
with `Contains` and `Intersects` helpers. Negative-size sprite flipping is not
part of this initial API.

The built-in text font primarily supports basic Latin; custom fonts and
Japanese glyph loading are not implemented in 0.1.0. `MeasureText` matches
the font used for drawing. Call `CaptureScreenshot("frame.png")` at the end
of `Draw` to save everything drawn so far in the current frame. Its output
directory must already exist; an existing file at that path is overwritten.

## Input

`Input.IsKeyDown(Key.W)` checks a held key. `IsKeyPressed` and `IsKeyReleased`
check frame transitions; repeated queries do not consume those transitions.
Equivalent methods exist for `MouseButton`. `MousePosition`, `MouseDelta`,
`MouseWheelDelta` and `IsCursorOnScreen` expose pointer information.

## Audio

Audio is enabled by default. Set `GameSettings.EnableAudio = false` when no
audio device is needed or available. A requested audio device that cannot
initialize causes `Run` to fail clearly rather than silently mute the game.

```csharp
// OnLoad:
sound = Audio.LoadSound("Assets/hit.wav");
music = Audio.LoadMusic("Assets/theme.ogg");
music.IsLooping = true;
music.Volume = 0.4f;
music.Play();

// Update:
if (Input.IsKeyPressed(Key.Space)) sound.Play();
```

Sounds are loaded in memory. Each `SoundClip` has one voice; `Play` restarts
it. Music is streamed and automatically updated by the loop; keep its source
file available until disposal. Both types expose `Play`, `Pause`, `Resume`,
`Stop`, `IsPlaying`, and `Volume`. Volumes must be finite and in `[0, 1]`.
`MusicTrack` also exposes read-only `Duration` and `Position`.
Supported codecs depend on the bundled raylib build; the sample uses PCM WAV.

## Scenes and ownership

Derive from `LumisScene` and override `OnEnter`, `Update`, `Draw`, and `OnExit`
as needed. Pass game services to your scene's constructor. Request transitions
with `Scenes.Switch(scene)`; the last request in a frame wins. Switching to
the active instance cancels an outstanding transition without re-entering it.

Scenes are reusable and are not automatically disposed. Use `OnExit` for
scene-specific cleanup when appropriate. If a scene's `OnEnter` or `OnExit`
throws, the manager leaves no scene current. An unsuccessful entry does not
trigger `OnExit`; game-owned native resources still get final cleanup.

Textures, clips and tracks implement `IDisposable`. Release them early when
no longer needed; otherwise the game releases them before their native device
closes. Resources cannot be transferred between games. Native operations
reject disposed resources; texture dimensions and `IsDisposed` remain readable. File paths
are relative to the process working directory; prefer paths based on
`AppContext.BaseDirectory` for copied sample or deployed assets.
