# Changelog

All notable changes to LumisAPI are documented here.
Versions follow [Semantic Versioning](https://semver.org/).
The API may change between minor versions while the major version is zero.

## [Unreleased]

## [0.1.0] - 2026-09-24

Initial local version; not yet published to nuget.org.

### Added

- A .NET 10 library with the `Lumis` namespace and NuGet package ID `LumisAPI`.
- A configurable native window and game loop powered by raylib-cs 8.1.0.
- 2D shapes, text, textures, screenshot capture, keyboard and mouse input.
- Sound effects and streamed music with playback and volume controls.
- Deferred scene transitions and explicit enter/update/draw/exit callbacks.
- Automatic native resource cleanup and thread/lifecycle guards.
- The HelloLumis sample with bundled, generated PNG and WAV assets.
- Headless unit tests, desktop smoke mode, CI, and XML API documentation.
- MIT license and local NuGet packaging instructions.
