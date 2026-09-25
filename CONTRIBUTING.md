# Contributing

Use the .NET 10 SDK and keep changes focused. The library and sample use the
`Lumis` namespace; native backend types stay behind the library's public API.

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

Once a GitHub repository exists, protect `main`, require pull requests and
successful CI checks, and use short-lived feature branches. Require the
three `Build and test` matrix jobs and the `Linux graphics smoke` job before
merging. Branch protection must be configured in GitHub; this local scaffold
does not configure remote repository settings.

The workflow builds packages but never publishes them automatically.
