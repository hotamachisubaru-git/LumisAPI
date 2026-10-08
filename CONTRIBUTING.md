# Contributing

Use the .NET 10 SDK selected by `global.json`, and install the .NET 8 runtime
or SDK to run both unit test targets. Lumis and unit tests target .NET 8 and
.NET 10; Lumis.Security also targets .NET Standard 2.1. The sample and native
smoke project target .NET 10. Public core types use `Lumis`; Unity adapter types
use `Lumis.Unity`. Native backend types stay behind the game API.

The canonical security sources are in
`Packages/jp.hotamachi.lumis.security/Runtime/Core`. The .NET security project
links these files; do not maintain separate Unity and desktop implementations.
Keep the shared sources C# 8 compatible and free of engine dependencies.

Before proposing a change, run:

```sh
dotnet restore LumisAPI.sln
dotnet restore tests/Lumis.CSharp8Consumer/Lumis.CSharp8Consumer.csproj
dotnet build LumisAPI.sln -c Release --no-restore
dotnet build tests/Lumis.CSharp8Consumer/Lumis.CSharp8Consumer.csproj -c Release --no-restore
dotnet test LumisAPI.sln -c Release --no-build
dotnet run --project tests/Lumis.Security.PortableSmoke -c Release
dotnet build tests/Lumis.UnityCompileCheck/Lumis.UnityCompileCheck.csproj -c Release -p:UnityTestProfile=Editor
dotnet run --project samples/HelloLumis -c Release --no-build -- --smoke --no-audio
dotnet run --project tests/Lumis.NativeSmoke -c Release --no-build -- --no-audio
dotnet pack src/Lumis.Security/Lumis.Security.csproj -c Release --no-build -o artifacts/packages
dotnet pack src/Lumis/Lumis.csproj -c Release --no-build -o artifacts/packages
```

Also compile UnityTestProfile=WindowsIL2CPP, Android and WebGL when changing
conditional source. These checks use API stubs, NOT Unity. Run the package's
EditMode tests and build real Mono/IL2CPP players before claiming runtime
support. Preserve `.meta` GUIDs in the UPM package.

For audio changes, also run the sample and native lifecycle checks with audio
enabled on a desktop with an audio output device. Unit tests do not prove
native graphics or sound behavior.

Document public members with XML comments. Keep public configuration APIs
consumable from C# 8 unless a deliberate compatibility break is documented.
Add tests for behavioral changes, describe compatibility changes, and update
`CHANGELOG.md`. Do not commit build artifacts or API keys.

## Keeping main usable

Configure branch protection in repository settings to require pull requests
and successful CI checks. Require the three Build and test jobs, Linux graphics
smoke, and the four Unity conditional compilation jobs before merging.
Short-lived `validation/**` branches also trigger CI so direct main updates can
be checked first. Do not bypass configured branch protection.

The CI workflow builds packages without publishing them. The separate
Publish to NuGet workflow uses verified artifacts and publishes the security
dependency before LumisAPI using NUGET_API_KEY. Tag pushes and manual runs can
publish; follow the [publishing guide](docs/publishing.md).
