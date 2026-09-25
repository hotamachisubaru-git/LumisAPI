# Contributing

Use the .NET 10 SDK selected by `global.json`, and install the .NET 8 runtime
or SDK to run both unit test targets. The library and unit tests target .NET 8
and .NET 10; the sample and native smoke project target .NET 10. Public API
types use the `Lumis` namespace; native backend types stay behind the library's
public API. Keep changes focused.

Before proposing a change, run:

```sh
dotnet restore LumisAPI.sln
dotnet build LumisAPI.sln -c Release --no-restore
dotnet test LumisAPI.sln -c Release --no-build
dotnet run --project samples/HelloLumis -c Release --no-build -- --smoke --no-audio
dotnet run --project tests/Lumis.NativeSmoke -c Release --no-build -- --no-audio
dotnet pack src/Lumis/Lumis.csproj -c Release --no-build -o artifacts/packages
```

For audio changes, also run the sample and native lifecycle checks with audio
enabled on a desktop with an audio output device. Unit tests do not prove
native graphics or sound behavior.

Document public members with XML comments. Add tests for behavioral changes,
describe compatibility changes, and update `CHANGELOG.md`. Do not commit
build artifacts or API keys.

## Keeping main usable

Configure branch protection for `main` in the
[GitHub repository](https://github.com/hotamachisubaru-git/LumisAPI) to require
pull requests and successful CI checks, and use short-lived feature branches.
Require the three `Build and test` matrix jobs and the `Linux graphics smoke`
job before merging. Branch protection is managed in GitHub repository settings.

The CI workflow builds packages without publishing them. The separate
`Publish to NuGet` workflow runs the full CI workflow and publishes its tested
package artifact using the `NUGET_API_KEY` repository secret. Tag pushes and
manual runs can publish; follow the [publishing guide](docs/publishing.md).
