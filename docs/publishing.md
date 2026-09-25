# Packaging and publishing

The SDK-style library project produces `LumisAPI.0.1.0.nupkg` and a symbol
package. The library assembly and public namespace are `Lumis`. Samples and
tests are not packable. README, MIT license, third-party notices and XML API
documentation are included in the package.

This uses the standard [.NET library packaging guidance](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/nuget)
and [NuGet authoring metadata](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices).

## Build and inspect

```sh
dotnet restore LumisAPI.sln
dotnet build LumisAPI.sln -c Release --no-restore
dotnet test LumisAPI.sln -c Release --no-build
dotnet pack src/Lumis/Lumis.csproj -c Release --no-build -o artifacts/packages
```

Install the `.nupkg` in a separate console project, using the local package
directory plus nuget.org as restore sources. Test that native runtime assets
are found when launching the consumer, not only when using a project reference.

## Before the first public release

1. Create the intended GitHub repository and select its real owner. The proposed
   `LumisProject` organization is a future option, not a configured remote.
2. Set `RepositoryUrl`, `RepositoryType` (`git`), and `PackageProjectUrl` in
   `src/Lumis/Lumis.csproj` to the actual repository URLs. Confirm the author and
   copyright attribution before publishing.
3. Verify that you own or can register the `LumisAPI` package ID on nuget.org.
   Creating a local package does not reserve the ID.
4. Check `Directory.Build.props` for the intended version, update the changelog,
   run CI and test supported desktop platforms. Tag the release commit.
5. Publish using your own NuGet credentials. Store credentials in a secret
   store or CI secrets, never in source control.

For example, in PowerShell with an already configured `NUGET_API_KEY`:

```powershell
dotnet nuget push artifacts/packages/LumisAPI.0.1.0.nupkg `
  --source https://api.nuget.org/v3/index.json `
  --api-key $env:NUGET_API_KEY
```

Publication is a separate maintainer action. Neither the local setup nor the
CI workflow pushes to GitHub or nuget.org.

After publication, consumers can run `dotnet add package LumisAPI`.
Future `LumisEngine`, `LumisEditor`, and `LumisLauncher` repositories can depend
on this package without being part of this solution.
