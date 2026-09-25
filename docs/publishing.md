# Packaging and publishing

The SDK-style library project produces `LumisAPI.0.1.0.nupkg` containing the
.NET 8 and .NET 10 assemblies, plus a `.snupkg` symbol package. The library
assembly and public namespace are `Lumis`. Samples and tests are not packable.
README, MIT license, third-party notices and XML API documentation are included
in the package.

The source repository is
[hotamachisubaru-git/LumisAPI](https://github.com/hotamachisubaru-git/LumisAPI).
`RepositoryUrl`, `RepositoryType`, and `PackageProjectUrl` are already set in
`src/Lumis/Lumis.csproj`.

This uses the standard [.NET library packaging guidance](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/nuget)
and [NuGet authoring metadata](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices).

## Build and inspect

Use the .NET 10 SDK selected by `global.json`, plus the .NET 8 runtime or SDK
to run both unit test targets. The library and unit tests target .NET 8 and
.NET 10; the sample and native smoke project run on .NET 10.

```sh
dotnet restore LumisAPI.sln
dotnet build LumisAPI.sln -c Release --no-restore
dotnet test LumisAPI.sln -c Release --no-build
dotnet pack src/Lumis/Lumis.csproj -c Release --no-build -o artifacts/packages
```

Install the `.nupkg` in a separate console project, using the local package
directory plus nuget.org as restore sources. Test that native runtime assets
are found when launching the consumer, not only when using a project reference.

## Publish through GitHub Actions

The [`Publish to NuGet` workflow](../.github/workflows/publish.yml) can publish
when a `v*` tag is pushed or when a maintainer selects **Run workflow** in
GitHub Actions.

1. Verify that you own or can register the `LumisAPI` package ID on nuget.org.
   Creating a local package does not reserve the ID. Confirm the package
   author and copyright attribution before the first release.
2. Configure a nuget.org API key with permission to push the `LumisAPI` package
   as the repository's Actions secret named `NUGET_API_KEY` under
   **Settings > Secrets and variables > Actions**. Keep the key valid through
   the release; store its value only in the secret, not in workflow source.
3. Set the intended version in `Directory.Build.props`, update the changelog,
   test supported desktop platforms, and commit and push the release changes.
4. Trigger a tag release or a manual publication as described below.

For a tag release, push a tag named `v` followed by the evaluated
`PackageVersion`, such as `v0.1.0` for version `0.1.0`. For manual publication,
run the workflow against a branch to publish that branch's declared package
version, or select a release tag to publish its version.

Tag runs, including manually selected tags, must match the evaluated package
version exactly. Manual branch runs do not require a tag. The workflow checks
the release version and required secret before starting the build.

It then calls the full [CI workflow](../.github/workflows/ci.yml): Windows,
Linux and macOS builds and unit tests, plus Linux graphics and native lifecycle
smoke checks. Publishing waits for all of those jobs and downloads their exact
package artifact instead of rebuilding it. The NuGet push uses
`https://api.nuget.org/v3/index.json`; concurrent publication jobs are
serialized, and retries skip package versions already present on the feed.

NuGet package versions are immutable. Use a new version for changed package
contents; a successful retry with `--skip-duplicate` does not replace an
existing package. Check the workflow result and nuget.org package listing to
confirm that the intended version is available.

The ordinary CI workflow never publishes by itself. The publishing workflow
does not create a GitHub Release or push commits or tags to the repository.

## Publish a local package manually

After running the build and checks above, a maintainer can instead push the
local package. For example, in PowerShell with an already configured
`NUGET_API_KEY` environment variable:

```powershell
dotnet nuget push artifacts/packages/LumisAPI.0.1.0.nupkg `
  --source https://api.nuget.org/v3/index.json `
  --api-key "$env:NUGET_API_KEY"
```

After publication, consumers can run `dotnet add package LumisAPI`.
Future `LumisEngine`, `LumisEditor`, and `LumisLauncher` repositories can depend
on this package without being part of this solution. A future `LumisProject`
organization would be a separate repository move; the current source is hosted
under `hotamachisubaru-git`.
