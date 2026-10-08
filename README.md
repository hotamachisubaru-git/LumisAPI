# LumisAPI

LumisAPI は、C# 向けの軽量なゲーム開発 API です。
[raylib-cs](https://github.com/raylib-cs/raylib-cs) と [raylib](https://www.raylib.com/) を基盤に、デスクトップ向け 2D ゲームを開発できます。

アンチチート部分は、エンジンに依存しない **Lumis.Security** として分離しました。LumisAPI と Unity が同じ共通ソースを使用します。

**パッケージ設定バージョン:** 0.1.1 · **ライセンス:** MIT

| コンポーネント | 対象 |
|---|---|
| LumisAPI 本体 | .NET 8 / .NET 10、raylib ベースのデスクトップゲーム |
| Lumis.Security 共通ライブラリ | .NET Standard 2.1 / .NET 8 / .NET 10、共通ソースは C# 8 |
| Lumis Security Unity パッケージ | Unity 2022.3 / Unity 6 の .NET Standard 2.1 を対象としたソース実装 |

この README は開発中の `main` を説明します。**Unity 対応を含む変更は、既存の NuGet 0.1.1 に自動反映されません。** 新規リリース時は LumisAPI と Lumis.Security を未使用の同一バージョンへ更新する必要があります。

リポジトリのビルドには `global.json` で指定された .NET 10 SDK、両対象のユニットテストには .NET 8 ランタイムも必要です。公開 API は C# 8 利用側プロジェクトでコンパイル検証しています。本体の最小ランタイムは引き続き .NET 8 です。

## Unity で使う

Unity の Package Manager で「Add package from git URL」を選び、次を指定します。

```text
https://github.com/hotamachisubaru-git/LumisAPI.git?path=Packages/jp.hotamachi.lumis.security#main
```

配布時は `#main` を検証済みのコミット SHA に固定してください。API Compatibility Level は .NET Standard 2.1 を使用します。**Unity には LumisAPI 本体や raylib を追加する必要はありません。**

ブートストラップシーンに `Lumis.Unity.LumisAntiCheat` を配置し、`IsReady` を確認してからゲームシーンを読み込みます。Samples の Bootstrap example に最小例があります。

Unity ではエンジンのウィンドウは先に作られます。LumisAPI 本体と異なり、Unity のウィンドウ生成前に検査する機能ではなく、**ゲームプレイの開始を許可・拒否する方式**です。別スクリプトの Awake を自動的にすべて停止するわけではないため、ゲームシーンを分離してください。

**Unity Editor / Mono Player / IL2CPP Player での実動作は未検証です。** CI の Unity 条件付きソース検査は API スタブによるコンパイル検証であり、実際の Unity ビルドや IL2CPP 変換の成功を保証しません。

[Unity 導入・設定・制限事項](Packages/jp.hotamachi.lumis.security/README.md) / [共通化の構成と検証範囲](docs/unity.md)

## 主な機能

- ウィンドウ / ゲームループ — リサイズ、フレーム制御、delta time、終了処理
- 2D 描画 — 図形、テキスト、テクスチャ、色調変更、拡大縮小、回転、スクリーンショット
- 入力 / オーディオ / シーン管理 — キーとマウスの状態、効果音、ストリーミング音楽、遅延シーン遷移
- アンチチート Level 1〜5 — プロセス検査、保護数値型、時間比較、ファイル検証、認証付きセーブ保護
- テクスチャ / オーディオの自動解放、ゲームスレッド検証、NuGet への XML API ドキュメント同梱

## インストールとクイックスタート

公開済みの NuGet パッケージを利用する場合:

```sh
dotnet add package LumisAPI --version 0.1.1
```

開発中のコードを実行する場合は [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) をインストールし、リポジトリのルートで実行します。

```sh
dotnet restore LumisAPI.sln
dotnet build LumisAPI.sln -c Release --no-restore
dotnet run --project samples/HelloLumis -c Release --no-build
```

サンプルには PNG / WAV アセットを同梱しています。WASD / 矢印キーで移動、左クリックでポインタ位置へ移動、Space で効果音、M で音楽切り替え、Tab でシーン切り替え、Escape で終了します。
オーディオデバイスがない環境では `dotnet run` の引数に `-- --no-audio` を追加してください。

## 基本例（LumisAPI 本体）

コンソールプロジェクトの `Program.cs` を次に置き換えます。C# 8 でも使用できる、明示的な Main メソッドの例です。

```csharp
using System.Numerics;
using Lumis;

internal static class Program
{
    private static void Main()
    {
        using var game = new Game();
        game.Run();
    }
}

public class Game : LumisGame
{
    public Game() : base(new GameSettings
    {
        Title = "Hello, Lumis!",
        Width = 960,
        Height = 540,
        EnableAudio = false
    }) { }

    protected override void Update(float deltaTime)
    {
        if (Input.IsKeyPressed(Key.Escape)) Exit();
    }

    protected override void Draw()
    {
        Graphics.DrawText("Hello, Lumis!", new Vector2(40, 40), 32, Color.White);
        Graphics.DrawCircle(new Vector2(160, 160), 40, new Color(96, 210, 255));
    }
}
```

## アンチチート Level 1〜5

ここでの Level は本プロジェクトの機能分類であり、第三者による安全性認証ではありません。共通サービスの検査はデフォルトでは無効です。利用する機能だけを明示的に設定してください。

### Level 1 — プロセス / Cheat Engine / デバッガ検出

プロセス名、取得可能な実行ファイルパス、ProductName / FileDescription / OriginalFilename、独自ブロックリストを使用します。メタデータを OS に拒否されたプロセスの情報は取得できません。

```csharp
var settings = new GameSettings
{
    AntiCheat = new AntiCheatSettings
    {
        Enabled = true,
        MonitorDuringGame = true,
        RuntimeScanInterval = System.TimeSpan.FromSeconds(2),
        ProcessDetection = new ProcessDetectionSettings
        {
            DetectCheatEngine = true,
            InspectExecutableMetadata = true,
            BlockedProcessNames = new[] { "MyGameTrainer" },
            BlockedExecutablePathFragments = new[] { "tools/trainer" }
        },
        DebuggerDetection = new DebuggerDetectionSettings { Enabled = false }
    }
};
```

LumisGame では起動前とゲーム中に検査します。共通サービスを直接使う場合は `new AntiCheatService(settings.AntiCheat)` の後、ホスト側から `CheckStartup()` とフレームごとの `Update(unscaledDeltaTime)` を呼び出します。設定は生成時にコピーされ、その後の元設定の変更ではサービスの動作は変わりません。

デバッガ検査は開発を妨げないよう明示有効化方式です。Windows は IsDebuggerPresent、Linux は TracerPid、その他はマネージドデバッガ状態を使用します。Unity モバイル / WebGL のプロセス検査は無効化し、警告します。

### Level 2 — 保護された数値型

```csharp
var money = new SecureInt(1000);
var experience = new SecureLong(50000);
var speed = new SecureFloat(4.5f);
var multiplier = new SecureDouble(1.25);
money.Value += 500;
```

書き込みごとのランダムキーによる難読化と整合性タグを使用し、不一致時は MemoryTampering の AntiCheatException が発生します。新しい値を書き込む際にも既存値を検証します。
秘密情報を保管する暗号化ストレージではなく、完全なメモリ改造や検査コード自体の無効化を防ぐものではありません。Unity の Inspector / JsonUtility のシリアライズ型でもありません。

### Level 3 — SpeedHack / 時間比較

TimeManipulationSettings は、積算したゲーム時間と Stopwatch の単調増加時間を比較します。既定値は観測期間 2 秒、比率 1.75、連続異常 2 回です。通常の処理停止による誤検出を抑えるための閾値がありますが、チート有無を確実に判定するものではありません。

Unity ホストは Time.unscaledDeltaTime を使用し、timeScale の演出変更を検出対象にしません。ポーズ・フォーカス復帰時には ResetTiming() で観測をリセットします。両方の時計が同時に操作されるケースは検出できません。

### Level 4 — ファイル整合性

FileIntegritySettings.Enabled を有効化し、Run() / CheckStartup() の前に対象を登録します。

```csharp
// service は生成済みの AntiCheatService。
service.FileIntegrity.RegisterFile(path, trustedSha256);
service.FileIntegrity.RegisterCurrentFile(otherPath);
service.FileIntegrity.RegisterDirectorySnapshot(assetDirectory, "*.json");
```

信頼済みのビルド時 SHA-256 を指定する RegisterFile が基本です。現在のファイルを登録するスナップショット方式では、登録前の改ざんを検出できません。登録後に追加されたファイルも自動では対象になりません。

起動時と実行中のチェックを設定できます。大きいファイルの頻繁な再ハッシュはメインスレッドを停止させるため、実行中監視は必要な小規模ファイルに限定してください。

Unity の Android / WebGL など URL 形式の StreamingAssets は同期 File API で扱いません。アプリ側で UnityWebRequest により取得したバイト列を `FileIntegrityService.VerifyData(bytes, trustedSha256)` で検証できます。

### Level 5 — セーブデータ / アセンブリ整合性

.NET 8 / .NET 10 の対応環境では、既存の SaveDataProtector の AES-256-GCM 形式を維持します。
Unity / .NET Standard では **PortableSaveDataProtector** を明示的に使用します。

```csharp
// 新規セーブ用に一度だけ生成し、セーブとは別に安全に永続化します。
byte[] key = PortableSaveDataProtector.GenerateKey();
using var saves = new PortableSaveDataProtector(key);
string protectedSave = saves.ProtectString(json);
string restoredJson = saves.UnprotectString(protectedSave);
```

ポータブル形式 LSP1 は AES-256-CBC と HMAC-SHA256 を組み合わせ、異なる派生鍵を使用してヘッダー / IV / 暗号文を復号前に認証します。旧 GCM とは別形式であり、暗黙の形式変更や平文へのフォールバックはありません。
Unity / .NET Standard ビルドの SaveDataProtector.IsSupported は false です。旧 GCM データを移行する場合は、対応 .NET 環境で復号した後、明示的に LSP1 として再保存します。

鍵を毎回生成すると過去のセーブを復号できません。鍵の保管・アカウントへの紐付けはアプリ側の責任です。クライアントに埋め込んだ鍵の抽出や、過去の正しいセーブへの巻き戻しは別途対策が必要です。

LumisAPI 本体では AssemblyIntegritySettings に信頼済みの ExpectedEntryAssemblySha256 を指定して、エントリアセンブリのディスク上の内容を検証できます。未指定時は最初の状態を基準とし、以後の変更だけを検出します。single-file 配布などの取得不能時は FailIfUnavailable で扱いを選択します。

Unity ホストではエントリアセンブリの自動探索を無効化しています。Windows IL2CPP では、たとえば最終ビルド後の GameAssembly.dll のハッシュを Protected Files に登録します。ロード済みコードやインジェクションの検出ではありません。

### 違反時の動作と制限

サービス検査はイベント AntiCheat.ViolationDetected を発生させ、AntiCheatException を投げます。種類、フェーズ、プロセス名、PID、実行パス、保護ファイルパスを取得できます。LumisGame 内の例外は通常のリソース解放を通ります。

Secure 型とセーブヘルパーは例外を直接投げます。**Unity では他のスクリプトの例外が記録されるだけでゲームが継続する場合があるため**、`LumisAntiCheat.ExecuteChecked`、または catch 後の `ReportViolation` に接続してください。Quit On Failure が無効の場合や WebGL では、アプリ側でも IsReady を確認して進行を止める必要があります。

全機能はユーザーモードの補助防御です。誤検出や回避があり、カーネルレベルの監視ではありません。自動 BAN の根拠にはせず、オンラインゲームの重要状態は信頼できるサーバーで検証してください。

## ローカルパッケージからインストール

共通依存パッケージを先に作成します。

```sh
dotnet pack src/Lumis.Security/Lumis.Security.csproj -c Release -o artifacts/packages
dotnet pack src/Lumis/Lumis.csproj -c Release -o artifacts/packages
dotnet new console -n MyGame -f net10.0
cd MyGame
dotnet add package LumisAPI --version 0.1.1 --source ../artifacts/packages --no-restore
dotnet restore --source ../artifacts/packages --source https://api.nuget.org/v3/index.json
```

依存する Lumis.Security は同じローカルフォルダ、Raylib-cs は nuget.org から復元します。公開済みの同一バージョンと混同しないよう、配布時は両プロジェクトのバージョンを更新してください。リポジトリ外からは artifacts/packages の絶対パスを指定します。

## リポジトリ構成

```text
LumisAPI/
├─ src/
│  ├─ Lumis/                 # raylib 本体、Security の公開型転送
│  │  ├─ Core/
│  │  ├─ Graphics/
│  │  ├─ Input/
│  │  ├─ Audio/
│  │  └─ Scene/
│  └─ Lumis.Security/        # 下記 Core ソースをリンクして複数 TFM へビルド
├─ Packages/jp.hotamachi.lumis.security/
│  ├─ Runtime/Core/         # エンジン非依存の共通ソース（C# 8）
│  ├─ Runtime/Unity/        # Unity ホスト
│  ├─ Tests/Editor/
│  └─ Samples~/Bootstrap/
├─ samples/HelloLumis/
├─ tests/
│  ├─ Lumis.Tests/
│  ├─ Lumis.CSharp8Consumer/
│  ├─ Lumis.Security.PortableSmoke/
│  ├─ Lumis.UnityCompileCheck/  # Unity API スタブ、製品には同梱しない
│  └─ Lumis.NativeSmoke/
├─ docs/
└─ LumisAPI.sln
```

## 開発と検証

```sh
dotnet build LumisAPI.sln -c Release
dotnet test LumisAPI.sln -c Release --no-build
dotnet build tests/Lumis.CSharp8Consumer/Lumis.CSharp8Consumer.csproj -c Release
dotnet run --project tests/Lumis.Security.PortableSmoke -c Release
dotnet run --project samples/HelloLumis -c Release -- --smoke --no-audio
dotnet run --project tests/Lumis.NativeSmoke -c Release -- --no-audio
```

PortableSmoke は先にソリューションをビルドして作成した .NET Standard 2.1 DLL を参照し、.NET 8 ホスト上で実行します。Unity ランタイムの実行試験ではありません。

CI は Windows / Linux / macOS のビルドとユニットテスト、C# 8 利用側、ポータブル単独試験、両 NuGet パッケージ作成を検証します。Linux では Xvfb による実ウィンドウ描画とリソース寿命の smoke test も実行します。

Unity 条件付きコードは Editor / Windows IL2CPP / Android / WebGL シンボルを API スタブでコンパイルします。**Unity Editor の実行、実際の IL2CPP 変換、各プラットフォームの実機動作は未検証です。** 同梱の EditMode テストは Unity Test Runner で別途実行してください。

オーディオも検証する場合は --no-audio を外します。サンプルの --capture screenshot.png でフレームを保存できます。実際の配布対象マシン上でのネイティブ動作確認は引き続き必要です。

raylib 本体の同梱デスクトップアセットは win-x64 / win-x86 / linux-x64 / osx-x64 / osx-arm64 向けです。本体のブラウザ、モバイル、Windows ARM64、Linux ARM64 配布は未対応です。これはエンジン非依存の Security / Unity ホストとは別の制約です。

## ドキュメント

- [Unity 導入ガイド](Packages/jp.hotamachi.lumis.security/README.md) / [共通化・互換性](docs/unity.md)
- [API / ライフサイクル](docs/api.md)
- [コントリビューション](CONTRIBUTING.md)
- [パッケージ作成 / 公開](docs/publishing.md)
- [ローカル検証結果](docs/verification.md)
- [変更履歴](CHANGELOG.md)
- [サードパーティ通知](THIRD-PARTY-NOTICES.md)

## ライセンス

LumisAPI、Lumis.Security と同梱サンプルは [MIT License](LICENSE) のもとで提供されます。raylib-cs と raylib にはそれぞれの zlib ライセンスと著作権表示が適用されます。
