# パッケージ作成と公開

開発中の構成では、次の 2 つの NuGet パッケージを作成します。

| パッケージ | 内容 |
|---|---|
| LumisAPI | .NET 8 / .NET 10 のゲーム API。Raylib-cs と Lumis.Security に依存 |
| Lumis.Security | .NET Standard 2.1 / .NET 8 / .NET 10 のエンジン非依存アンチチート補助 API |

両パッケージに README、MIT ライセンス、XML API ドキュメント、シンボルパッケージを付けます。LumisAPI には raylib 関連のサードパーティ通知も同梱します。テストとサンプルは NuGet に含めません。

Unity 用のソースは別の [UPM パッケージ](../Packages/jp.hotamachi.lumis.security/README.md) として Git URL から導入できます。UPM と NuGet のバージョンは別管理です。

## ビルドと検証

global.json が指定する .NET 10 SDK と、テスト用の .NET 8 ランタイムまたは SDK が必要です。

```sh
dotnet restore LumisAPI.sln
dotnet build LumisAPI.sln -c Release --no-restore
dotnet test LumisAPI.sln -c Release --no-build
dotnet run --project tests/Lumis.Security.PortableSmoke -c Release
dotnet pack src/Lumis.Security/Lumis.Security.csproj -c Release --no-build -o artifacts/packages
dotnet pack src/Lumis/Lumis.csproj -c Release --no-build -o artifacts/packages
```

別のコンソールプロジェクトへ .nupkg をインストールし、artifacts/packages と nuget.org の両方を復元元に指定してください。プロジェクト参照だけでなく、パッケージ経由でネイティブアセットと Lumis.Security 依存関係を解決できることを確認します。

## 公開前の準備

1. nuget.org で LumisAPI と Lumis.Security の ID を使用できる所有権・公開権限があることを確認します。ローカルでパッケージを作成しても ID は予約されません。
2. 両 ID を push できる API キーを、リポジトリの Actions secret `NUGET_API_KEY` に設定します。キーをソースやログへ書かないでください。
3. `src/Lumis/Lumis.csproj` と `src/Lumis.Security/Lumis.Security.csproj` の Version を、**未公開の同一バージョン**へ変更します。各 csproj が値を指定しているため、Directory.Build.props だけの変更では更新されません。
4. CHANGELOG と検証結果を更新し、実際の配布対象で動作確認した変更をコミットします。

NuGet の既存バージョンは上書きできません。現在の開発設定が 0.1.1 でも、この変更によって既存の公開済み 0.1.1 が置き換わることはありません。

## GitHub Actions による公開

[Publish to NuGet](../.github/workflows/publish.yml) は、`v*` タグを push したとき、または GitHub Actions の Run workflow で手動実行したときに公開します。通常の CI や main への push だけでは公開しません。

タグは評価された PackageVersion と一致する `v<version>` である必要があります。手動のブランチ実行では、そのブランチに宣言されたバージョンを使用します。[検証スクリプト](../.github/scripts/Get-PublishVersion.ps1) は両パッケージのバージョン一致も検査します。

公開ワークフローは全 CI の成功を待って、CI が作成した正確なパッケージアーティファクトをダウンロードします。再ビルドせず、**Lumis.Security、LumisAPI の順**で nuget.org に push します。ID や API キーの権限が不足していると、この公開処理は失敗します。

--skip-duplicate は既存バージョンを置き換えるものではありません。正しい版が公開されたことをワークフロー結果と NuGet 一覧で確認してください。
ワークフローは GitHub Release、コミット、タグを自動作成しません。

## ローカルパッケージを手動公開

検証後、PowerShell で両パッケージを順に公開する例です。PACKAGE_VERSION と NUGET_API_KEY は事前に設定してください。

```powershell
foreach ($id in @('Lumis.Security', 'LumisAPI')) {
    dotnet nuget push "artifacts/packages/$id.$env:PACKAGE_VERSION.nupkg" `
        --source https://api.nuget.org/v3/index.json `
        --api-key "$env:NUGET_API_KEY"
    if ($LASTEXITCODE -ne 0) { throw "公開失敗: $id" }
}
```

## Unity 検証について

UPM ソースの .NET Standard / C# 8 ビルドや、Unity API スタブのコンパイル成功だけを根拠に、Unity Editor / IL2CPP 対応の実動作保証をしないでください。実機検証の有無とプラットフォーム別制限を [Unity ガイド](unity.md) に明記します。

## 参考資料

- [Microsoft: .NET ライブラリの NuGet パッケージ化](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/nuget)
- [Microsoft: NuGet パッケージ作成のベストプラクティス](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices)
