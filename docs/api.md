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
before raylib creates the game window. Runtime checks execute before user update
callbacks when their configured scan or observation interval is due.

### Level 1: process and debugger detection

`ProcessDetectionSettings` supports built-in Cheat Engine matching, custom
process names and custom executable-path fragments. When
`InspectExecutableMetadata` is enabled, LumisAPI also attempts to inspect the
process executable path plus ProductName, FileDescription and OriginalFilename.
Metadata access can be denied by the operating system; inaccessible processes
are skipped rather than crashing the game.

`DebuggerDetectionSettings.Enabled` is false by default. Managed debugger
detection is portable. Native checks additionally use `IsDebuggerPresent` on
Windows and `/proc/self/status` `TracerPid` on Linux.

### Level 2: secure numeric values

`SecureInt`, `SecureLong`, `SecureFloat`, and `SecureDouble` store an
obfuscated representation instead of the plain numeric bits and verify a keyed
integrity tag on every read. Assigning `Value` rotates the random key and tag.
Integrity failure throws `AntiCheatException` with
`AntiCheatViolationType.MemoryTampering`.

These wrappers make simple memory-search/write attacks harder and detect direct
corruption of their protected representation. They do not hide secrets from an
attacker capable of fully reverse engineering and rewriting the client.

### Level 3: time manipulation

`TimeManipulationSettings` compares accumulated frame delta time with a
monotonic `Stopwatch` clock. The default configuration observes two-second
windows, allows a 1.75x ratio, and requires two consecutive suspicious windows.
Only suspicious acceleration is treated as a violation; normal stalls that make
game time advance more slowly do not trigger this detector.

### Level 4: registered-file integrity

`AntiCheat.FileIntegrity` manages SHA-256 baselines.

```csharp
AntiCheat.FileIntegrity.RegisterFile(path, trustedSha256);
AntiCheat.FileIntegrity.RegisterCurrentFile(otherPath);
AntiCheat.FileIntegrity.RegisterDirectorySnapshot(assetDirectory, "*.json");
```

`RegisterFile` is the preferred option when a trusted hash is available from
the build/release pipeline. `RegisterCurrentFile` and
`RegisterDirectorySnapshot` are runtime snapshots and therefore only prove
that a file has not changed since registration.

`FileIntegritySettings.CheckOnStartup` controls the startup check and
`MonitorDuringGame` controls repeated runtime hashing. Runtime monitoring is
disabled by default because hashing large assets repeatedly can be expensive.

### Level 5: save-data and entry-assembly integrity

`SaveDataProtector` uses AES-256-GCM authenticated encryption. `Protect` /
`Unprotect` work with bytes, while `ProtectString` / `UnprotectString`
produce Base64 text envelopes. A malformed payload, wrong key or changed
ciphertext raises `AntiCheatException` with `SaveDataTampering`.

The constructor requires a 32-byte key; `GenerateKey()` creates one. Key
management remains the application's responsibility. Client-embedded keys can
be extracted, so server-provided or platform-protected keys are preferable
where practical.

`AssemblyIntegritySettings` verifies the entry assembly with SHA-256. Supplying
`ExpectedEntryAssemblySha256` allows the first check to compare with an
external trusted build hash. Without it, the first check becomes the baseline
and only later on-disk changes can be detected. `FailIfUnavailable` can make a
missing assembly path fail closed, but should remain off for deployment modes
such as single-file publishing where `Assembly.Location` may be empty.

### Violation behavior

Service-detected violations raise `AntiCheat.ViolationDetected` immediately
before `AntiCheatException` is thrown. The event and exception include the
startup/runtime phase and process name, PID, executable path or protected file
path when applicable. Startup violations prevent window/audio initialization.
Runtime violations travel through the normal `LumisGame` cleanup path.

Secure numeric and save-data helpers throw `AntiCheatException` directly when
their local integrity/authentication check fails.

All five levels are user-mode defenses. They are designed to layer multiple
independent checks against casual and moderately capable client manipulation,
not to prove a client trustworthy. Server-authoritative validation remains the
strongest protection for networked game state.

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
