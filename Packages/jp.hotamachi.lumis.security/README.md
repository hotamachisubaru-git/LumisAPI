# Lumis Security / Unity

LumisAPI と Unity が共有する、raylib 非依存のアンチチート補助ライブラリです。
公開名前空間は既存 API との互換性のため `Lumis`、Unity コンポーネントは `Lumis.Unity` です。
共通ソースは C# 8 でコンパイルし、.NET Standard 2.1 / .NET 8 / .NET 10 を対象とします。

**Unity 2022.3 / Unity 6 の .NET Standard 2.1 を対象にした実装です。Unity Editor、Mono Player、IL2CPP Player での実動作確認は別途必要です。通常の .NET CI やスタブによるコンパイル検査は Unity 実機試験ではありません。**

## 導入

Package Manager の「Add package from git URL」に次を指定します。

```text
https://github.com/hotamachisubaru-git/LumisAPI.git?path=Packages/jp.hotamachi.lumis.security#main
```

配布時は `#main` の代わりに検証済みのコミット SHA を固定してください。
Git で導入できない場合は、このフォルダを Unity プロジェクトの `Packages` にコピーする方法もあります。
API Compatibility Level は .NET Standard 2.1 に設定します。**Lumis.dll、Raylib-cs、.NET 8 用 DLL を Unity に追加する必要はありません。**
この UPM パッケージと Lumis.Security.dll を同時に導入すると型が重複するため、どちらか一方だけ使用してください。

## 最小構成

1. 空のブートストラップシーンにルート GameObject を作成し、`LumisAntiCheat` を追加します。
2. Samples の Bootstrap example をインポートし、`LumisBootstrap` の参照とゲームシーン名を設定します。
3. ゲームシーンを Build Settings / Build Profiles のシーン一覧へ追加し、ブートストラップを先頭にします。
4. `IsReady` が true になってからゲーム処理を開始します。

Unity のウィンドウ生成そのものを阻止する機能ではありません。起動済み Unity エンジン内でゲームシーン開始を許可・拒否します。
早い実行順だけで他の全スクリプトの Awake を止めることはできないため、ブートストラップを分けてください。
Editor 内では既定で検査を省略し、`IsEditorBypass` が true になります。実際に検査する場合は Enable In Editor を有効にします。

## 各機能

| 機能 | Unity 用の実装・制限 |
|---|---|
| プロセス / Cheat Engine 検出 | デスクトップ向け。Android / iOS / WebGL では自動的に無効化して警告します。OS に拒否された個別メタデータは取得できません。 |
| SecureInt / Long / Float / Double | 共通実装。メモリ表現の難読化と整合性検査。Unity の Inspector / JsonUtility の保存形式ではありません。 |
| 時間の比較 | `Time.unscaledDeltaTime` と Stopwatch を比較。timeScale による演出を検出対象にせず、ポーズ・フォーカス復帰で観測をリセットします。両時計が同時に改変されると検出できません。 |
| ファイル SHA-256 | Inspector の Protected Files に信頼済みハッシュを設定。欠損 / 不一致 / 読み取り失敗時は開始を拒否します。大量ファイルの実行中再ハッシュはメインスレッド負荷に注意。 |
| セーブ | `PortableSaveDataProtector` の明示的な認証付き形式 LSP1 を使用。AES-CBC と HMAC-SHA256 の Encrypt-then-MAC、別々の派生鍵、復号前の定数時間 MAC 比較。 |
| エントリアセンブリ | Unity アダプターでは自動チェックを無効化。IL2CPP のネイティブファイルを通常のファイル検証に明示登録してください。 |

Windows IL2CPP の例: Protected Files の Root を `DesktopPlayerDirectory`、Relative Path を `GameAssembly.dll` にし、最終ビルド後の実ファイルの SHA-256 を設定します。
Windows/Linux 以外のアプリ配置を推測しません。署名・パッケージングで変更されるファイルのハッシュは、最終成果物から作成してください。
この検査はディスク上のファイルを検証するだけで、ロード済みコードやインジェクションを検証しません。

Android / WebGL の StreamingAssets は URL であるため、このコンポーネントは同期 File API で読もうとせず明示的にエラーにします。
アプリ側で UnityWebRequest により取得したバイト列は `FileIntegrityService.VerifyData(bytes, trustedSha256)` で検証できます。ダウンロードや非同期ブートストラップは本パッケージで自動実装していません。
WebGL の暗号実装やファイルシステムについて実動作は未検証です。`Application.Quit` はブラウザタブを閉じないため、アプリ側でも入力・ゲーム進行を停止してください。

## 保護値の例

```csharp
using Lumis;
using Lumis.Unity;

private SecureInt money = new SecureInt(1000);

// Unity は他の MonoBehaviour の例外をログに記録して継続する場合があります。
// 自動監視とは別に、保護値の処理を明示的にホストへ接続してください。
antiCheat.ExecuteChecked(() => money.Value += 500);
```

`Secure*` やセーブヘルパーの例外は、自動的に別コンポーネントのイベントへ転送されません。
`ExecuteChecked`、または catch した `AntiCheatException` を `ReportViolation` に渡してください。
Quit On Failure が false の場合、`IsReady` は false になりますが、他の GameObject は勝手に停止しません。ホスト側で進行を止めてください。

## セーブ保護と鍵管理

```csharp
using Lumis;

// 新規ユーザー/新規セーブ用に一度だけ生成し、保存とは別の保護領域で永続化します。
byte[] key = PortableSaveDataProtector.GenerateKey();
using (var saves = new PortableSaveDataProtector(key))
{
    string encrypted = saves.ProtectString("{\"money\":1000}");
    string json = saves.UnprotectString(encrypted);
}
```

読み込むたびに鍵を作り直すと復号できません。鍵保管、アカウントへの紐付け、OS の保護領域との連携はアプリ側の責任です。
LSP1 は旧 `SaveDataProtector` の AES-GCM v1 と別形式です。暗黙の変換 / 無認証保存へのフォールバックはありません。
Unity / .NET Standard ビルドの `SaveDataProtector.IsSupported` は false で、旧 GCM の使用は PlatformNotSupportedException になります。
旧データは対応 .NET ランタイムで復号し、LSP1 で明示的に再保存してください。
どちらの形式も過去の正しいセーブへの巻き戻しや、鍵を持つ改造クライアントによる再署名を防ぐものではありません。

## 検証

リポジトリ CI は共通コードの .NET Standard 2.1 / .NET 8 / .NET 10 ビルド、既存ユニットテスト、C# 8 利用側ビルド、.NET Standard DLL の単独実行試験を行います。
Unity 条件付きコンパイルは Editor / Windows IL2CPP / Android / WebGL 用シンボルをスタブで確認します。**実際の Unity コンパイル・IL2CPP 変換・実機テストとは異なります。**
Unity Test Runner 用の小規模な EditMode テストも `Tests/Editor` に同梱します。必要に応じてプロジェクトの manifest.json の testables にパッケージ名を追加して実行してください。

このパッケージはユーザーモードの補助防御です。検出不能、誤検出、クライアント改造による回避があり得ます。自動 BAN の根拠にはせず、オンラインゲームの重要状態はサーバー側で検証してください。
