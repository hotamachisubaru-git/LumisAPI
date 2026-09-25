# 0.1.0 検証結果

実施日: 2026-09-24。Windows x64、.NET SDK 10.0.401、Release 構成で確認。

| 対象 | 結果 |
| --- | --- |
| ソリューションの復元・ビルド | 成功、警告 0、エラー 0 |
| ヘッドレス単体テスト | 62 件成功、失敗 0、スキップ 0 |
| HelloLumis、音声あり | 終了コード 0、SMOKE PASS |
| HelloLumis、音声なし | 終了コード 0、SMOKE PASS |
| PNG 保存 | 960 × 600 の描画結果を保存し、画像を目視確認 |
| ネイティブ寿命テスト、音声あり | 終了コード 0、NATIVE LIFECYCLE PASS |
| NuGet / シンボルパッケージ作成 | 成功 |
| 別プロジェクトでのパッケージ復元・ビルド | 隔離キャッシュで成功、警告・エラー 0 |
| パッケージ利用側からのウィンドウ起動 | 5 フレームで自動終了する確認用コード、終了コード 0 |

ネイティブ確認では、ウィンドウ、PNG テクスチャ、図形・文字描画、入力状態の
取得、シーン切り替え、効果音と音楽の再生・一時停止・再開・停止を実行した。
音声デバイスは WASAPI、描画は OpenGL 3.3 で初期化でき、音楽の再生位置が
進むことも確認した。

寿命テストでは、同じプロセス内で複数のゲームを順に起動し、ウィンドウ設定の
切り替えと同時起動の拒否を確認した。`OnLoad`、`Draw`、`Update`、`OnUnload`
の例外後もリソースとデバイスが解放され、次のゲームを起動できた。

NuGet については、README の C# 例をそのままコンパイルした後、終了条件だけを
追加してネイティブ起動を確認した。ライブラリ DLL、XML API ドキュメント、
README、MIT License、第三者通知、依存パッケージと native ファイルの解決も確認した。

## 未確認の範囲

- 実際のキー・マウス操作による操作感と、出力音の聴感評価。
- Linux / macOS 実機。CI ワークフローを追加したが、リモート CI は未実行。
- すべての画像・音声コーデック。サンプルでは PNG と PCM WAV を使用。
- GitHub / nuget.org への公開と、リモートの `main` 保護設定。

標準フォントは主に基本ラテン文字向け。日本語フォント読み込みは 0.1.0 の実装範囲外。

## 再実行

```sh
dotnet build LumisAPI.sln -c Release
dotnet test LumisAPI.sln -c Release --no-build
dotnet run --project samples/HelloLumis -c Release --no-build -- --smoke
dotnet run --project samples/HelloLumis -c Release --no-build -- --smoke --no-audio
dotnet run --project tests/Lumis.NativeSmoke -c Release --no-build
dotnet pack src/Lumis/Lumis.csproj -c Release --no-build -o artifacts/packages
```

音声デバイスがない環境では `Lumis.NativeSmoke` にも `--no-audio` を渡す。
グラフィックスの確認にはデスクトップセッション、または Linux の Xvfb が必要。

今回のローカル実行ログと PNG は `artifacts/` に保存した。
このディレクトリとビルド出力は Git の追跡対象外。

## 2026-09-25 GitHub ワークフローの追加検証

上記は 2026-09-24 の初期検証記録。以下は GitHub 公開後の追加確認。

- 調査開始時のローカル HEAD と GitHub `main` はともに
  `750ce0d7b8fce3d5ea7c7ff075c8f8b6a2ad1bb7`。
- [そのコミットの CI](https://github.com/hotamachisubaru-git/LumisAPI/actions/runs/36107965725)
  は Windows / Linux / macOS のビルド・テストと、Linux の描画・寿命確認がすべて成功。
- 今回の修正後、Windows で .NET 8 / .NET 10 の単体テストが各 62 件、計 124 件成功。
  Release ビルドは警告・エラー 0。
- `actionlint 1.7.12` で両ワークフローの構文を確認し、診断 0。
- 公開バージョン確認スクリプトは、ブランチ指定と正しいタグを受理し、
  不一致タグ・不正タグ・大文字違い・参照指定なしを拒否する計 6 ケースを確認。
- NuGet パッケージとシンボルパッケージを生成し、`net8.0` / `net10.0` の
  DLL と XML ドキュメント、依存関係を確認。

今回のワークフロー修正自体はローカルで検証したもの。修正後のリモート CI と
nuget.org への実際の公開は、この追加確認では実行していない。
