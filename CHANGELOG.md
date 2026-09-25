# Changelog

All notable changes to LumisAPI are documented here.
Versions follow [Semantic Versioning](https://semver.org/).
The API may change between minor versions while the major version is zero.

## [Unreleased]

## [0.1.0] - 2026-09-25

Initial release.

### Added

- A .NET 8 and .NET 10 library with the `Lumis` namespace and NuGet package ID `LumisAPI`.
- A configurable native window and game loop powered by raylib-cs 8.1.0.
- 2D shapes, text, textures, screenshot capture, keyboard and mouse input.
- Sound effects and streamed music with playback and volume controls.
- Deferred scene transitions and explicit enter/update/draw/exit callbacks.
- Automatic native resource cleanup and thread/lifecycle guards.
- The HelloLumis sample with bundled, generated PNG and WAV assets.
- Headless unit tests for both .NET targets, desktop graphics and lifecycle smoke tests,
  Windows/Linux/macOS CI, and XML API documentation.
- NuGet publishing that validates release tags and required `NUGET_API_KEY` configuration,
  waits for all platform builds and tests, and publishes their verified package artifacts.
- NuGet V3 package and symbol publishing, release instructions, and repository metadata.
- MIT license and local NuGet packaging instructions.
