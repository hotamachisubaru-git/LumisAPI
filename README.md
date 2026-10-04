# LumisAPI

LumisAPI は、C# 向けの軽量なゲーム開発 API です。

[raylib-cs](https://github.com/raylib-cs/raylib-cs) と
[raylib](https://www.raylib.com/) を基盤に、シンプルでドキュメント化された API から
デスクトップ向け 2D ゲームを開発できます。

**バージョン:** 0.1.1 · **ライセンス:** MIT · **ライブラリ対象:** .NET 8 / .NET 10

ライブラリは `net8.0` と `net10.0` を対象としています。
このリポジトリのビルドには、`global.json` で指定された .NET 10 SDK が必要です。
サンプルとネイティブ smoke test は .NET 10、ユニットテストは .NET 8 と .NET 10 の両方で実行します。

公開設定 API は専用の C# 8 consumer build でも検証し、
.NET 10 ビルドでは現在の C# ツールチェーンも検証しています。
そのため、互換ランタイムを対象にしていれば、C# 8 から C# 14 までのプロジェクトから
LumisAPI を利用できます。

最小ランタイムは .NET 8 のままです。
これは Raylib-cs 8.1.0 が .NET 8 と .NET 10 を対象としているためです。

このリポジトリにはローカルパッケージ作成機能と
[NuGet 公開ワークフロー](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/docs/publishing.md)
も含まれています。

## 主な機能

- ウィンドウ / ゲームループ — リサイズ可能なウィンドウ、フレーム制御、delta time、安全な終了処理
- 2D 描画 — 図形、テキスト、テクスチャ、色調変更、拡大縮小、回転、スクリーンショット
- 入力 — キーボード / マウスの押下中・押した瞬間・離した瞬間の取得
- オーディオ — 効果音、ストリーミング音楽、再生制御、音量調整
- シーン管理 — シーンのライフサイクルコールバックと遅延シーン遷移
- アンチチート Level 1 — 起動時 / 実行中のプロセス、Cheat Engine、デバッガ検出
- アンチチート Level 2 — 改ざん検出付き `SecureInt` / `SecureLong` / `SecureFloat` / `SecureDouble`
- アンチチート Level 3 — SpeedHack / ゲーム時間加速の検出
- アンチチート Level 4 — ゲームデータ / アセットの SHA-256 ファイル整合性検証
- アンチチート Level 5 — 認証付きセーブデータ保護とエントリアセンブリ整合性監視
- テクスチャ / オーディオリソースの自動解放とゲームスレッド検証
- NuGet パッケージへの XML API ドキュメント同梱

## インストール

.NET 8 または .NET 10 のプロジェクトに LumisAPI を追加します。

```sh
dotnet add package LumisAPI --version 0.1.1
```

## クイックスタート

[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) をインストールし、
リポジトリのルートで以下を実行します。

```sh
dotnet restore LumisAPI.sln
dotnet build LumisAPI.sln -c Release --no-restore
dotnet run --project samples/HelloLumis -c Release --no-build
```

サンプルには PNG / WAV アセットが含まれています。

- **WASD / 矢印キー**: 移動
- **左クリック**: マウスポインタ位置へ移動
- **Space**: 効果音再生
- **M**: 音楽の再生 / 停止
- **Tab**: シーン切り替え
- **Escape**: 終了

オーディオデバイスがない環境では、`dotnet run` に
`-- --no-audio` を追加してください。

## 基本例

コンソールプロジェクトの `Program.cs` を次の内容に置き換えます。

```csharp
using System.Numerics;
using Lumis;

using var game = new Game();
game.Run();

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
        if (Input.IsKeyPressed(Key.Escape))
            Exit();
    }

    protected override void Draw()
    {
        Graphics.DrawText("Hello, Lumis!", new Vector2(40, 40), 32, Color.White);
        Graphics.DrawCircle(new Vector2(160, 160), 40, new Color(96, 210, 255));
    }
}
```

## アンチチート Level 1〜5

アンチチートはデフォルトでは無効です。
ゲームで必要なレベルだけ有効化してください。

LumisAPI のアンチチートはユーザーモードで動作し、
実行中に違反を検出してゲームを停止する場合でも、通常のクリーンアップ処理を維持します。

### Level 1 — プロセス / Cheat Engine / デバッガ検出

Level 1 はネイティブウィンドウを開く前に検査を行い、
設定に応じてゲーム実行中も定期的に再スキャンします。

Cheat Engine の検出では、OS から取得できる場合に以下を利用します。

- プロセス名
- 実行ファイルパス
- ProductName
- FileDescription
- OriginalFilename
- 任意の禁止プロセス名
- 任意の禁止実行パス断片

```csharp
AntiCheat = new AntiCheatSettings
{
    Enabled = true,
    MonitorDuringGame = true,
    RuntimeScanInterval = TimeSpan.FromSeconds(2),
    ProcessDetection = new ProcessDetectionSettings
    {
        DetectCheatEngine = true,
        InspectExecutableMetadata = true,
        BlockedProcessNames = new[] { "MyGameTrainer" },
        BlockedExecutablePathFragments = new[] { "tools/trainer" }
    },
    DebuggerDetection = new DebuggerDetectionSettings
    {
        Enabled = false
    }
}
```

Windows では追加で `IsDebuggerPresent`、
Linux では `TracerPid` を利用します。
macOS では現在、マネージドデバッガの状態を利用します。

通常の開発やデバッグを妨げないよう、デバッガ検出は明示的に有効化する方式です。

### Level 2 — 保護された数値型

所持金、HP、スコア、経験値など、
メモリエディタの対象になりやすい値には Secure 系の数値型を利用できます。

```csharp
var money = new SecureInt(1000);
var experience = new SecureLong(50000);
var speed = new SecureFloat(4.5f);
var multiplier = new SecureDouble(1.25);

money.Value += 500;
```

保存される値は、書き込みごとに生成されるランダムキーで難読化され、
さらに整合性タグを保持します。

整合性検証に失敗した場合は、
`MemoryTampering` を持つ `AntiCheatException` が発生します。

これらはメモリ改ざん対策用の仕組みであり、
秘密情報そのものを安全に保存するための暗号化ストレージではありません。

### Level 3 — SpeedHack / 時間改変検出

Level 3 では、ゲーム側で積算した delta time と
独立した単調増加時計を比較します。

通常のフレーム落ちや一時的な停止による誤検知を抑えるため、
デフォルトでは複数の観測期間で連続して異常が確認された場合に検出します。

```csharp
TimeManipulation = new TimeManipulationSettings
{
    Enabled = true,
    ObservationWindow = TimeSpan.FromSeconds(2),
    MaxGameTimeRatio = 1.75,
    RequiredConsecutiveDetections = 2
}
```

単調増加時計そのものを正しく改変・隠蔽できる高度なツールまでは防げないため、
他のアンチチートレベルと組み合わせて使用してください。

### Level 4 — ファイル整合性

ファイル整合性を有効にし、`Run()` を呼び出す前に保護対象を登録します。

ローカルの現在状態を基準にするより、
ビルド時に生成した信頼済み SHA-256 を指定する方が強い検証になります。

```csharp
AntiCheat = new AntiCheatSettings
{
    Enabled = true,
    FileIntegrity = new FileIntegritySettings
    {
        Enabled = true,
        CheckOnStartup = true,
        MonitorDuringGame = true
    }
};

// 派生ゲームのコンストラクタで、base コンストラクタ呼び出し後に登録:
AntiCheat.FileIntegrity.RegisterFile(
    "data/items.json",
    "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF");

// 現在のファイルを基準値として使用する場合:
AntiCheat.FileIntegrity.RegisterCurrentFile("assets/player.png");
```

`RegisterDirectorySnapshot` を使用すると、
ディレクトリ内のファイルをまとめて基準値として登録できます。

スナップショット方式は「登録後に変更されたか」は検出できますが、
登録前から改ざんされていたファイルが正しいものかどうかまでは証明できません。

### Level 5 — セーブデータ / 実行ファイル整合性

`SaveDataProtector` は AES-256-GCM を使用します。
そのため、保護済みセーブデータを書き換えると認証検証に失敗します。

```csharp
byte[] key = SaveDataProtector.GenerateKey();

using var saves = new SaveDataProtector(key);
string protectedSave = saves.ProtectString(json);
string restoredJson = saves.UnprotectString(protectedSave);
```

キーはユーザーが編集できるセーブデータとは別に管理してください。

クライアント内に完全に埋め込まれたキーは、
十分な解析能力を持つ攻撃者には最終的に抽出される可能性があります。
可能であれば、ランチャー、OS / プラットフォーム側の保護領域、
またはサーバーから提供されるキーの方が強い方式です。

エントリアセンブリ自体の整合性も監視できます。

```csharp
AssemblyIntegrity = new AssemblyIntegritySettings
{
    Enabled = true,
    CheckOnStartup = true,
    MonitorDuringGame = true,
    ExpectedEntryAssemblySha256 = trustedExeOrDllHash
}
```

信頼済みハッシュを指定しない場合、
LumisAPI は最初の検査時点のエントリアセンブリを基準値として使用します。
この場合、検出できるのは基準値取得後のディスク上の変更だけです。

single-file publish などではアセンブリパスを取得できない場合があります。
その際にエラーとするかどうかは `FailIfUnavailable` で設定できます。

### 違反検出時の動作と制限

サービス型のアンチチート違反を検出すると、
`AntiCheatException` を投げる直前に
`AntiCheat.ViolationDetected` が発生します。

イベントと例外から、以下の情報を取得できます。

- 違反の種類
- 起動時 / 実行中のどちらで検出されたか
- プロセス名
- PID
- 実行ファイルパス
- 保護対象ファイルパス

実行中に `AntiCheatException` が発生した場合も、
LumisAPI の通常のクリーンアップ処理を通ってから例外が再送出されます。

この 5 段階のアンチチートは、
一般的なクライアント改ざんやカジュアルなチートの難易度を大きく上げることを目的としています。

ただし、カーネルレベルのアンチチートではありません。
プロセス隠蔽、コードインジェクション、カーネルレベルの操作、
完全に改造されたクライアントなどを完全に防ぐことはできません。

オンラインゲームでは、重要なゲーム状態を信頼できるサーバー側でも検証してください。

## ローカルパッケージからインストール

まずローカル NuGet パッケージを作成します。

```sh
dotnet pack src/Lumis/Lumis.csproj -c Release -o artifacts/packages
dotnet new console -n MyGame -f net10.0
cd MyGame
dotnet add package LumisAPI --version 0.1.1 --source ../artifacts/packages --no-restore
dotnet restore --source ../artifacts/packages --source https://api.nuget.org/v3/index.json
```

Raylib-cs の依存関係は nuget.org から復元されます。
このリポジトリ外の consumer から利用する場合は、
`artifacts/packages` への絶対パスを指定してください。

## リポジトリ構成

```text
LumisAPI/
├─ src/Lumis/
│  ├─ Core/
│  ├─ Graphics/
│  ├─ Input/
│  ├─ Audio/
│  ├─ Scene/
│  └─ AntiCheat/
├─ samples/HelloLumis/
├─ tests/
│  ├─ Lumis.Tests/
│  ├─ Lumis.CSharp8Consumer/
│  └─ Lumis.NativeSmoke/
├─ docs/
├─ README.md
├─ LICENSE
├─ CHANGELOG.md
└─ LumisAPI.sln
```

## 開発

両方のユニットテスト対象を実行するには、
.NET 10 SDK に加えて .NET 8 ランタイム、または .NET 8 SDK もインストールしてください。

```sh
dotnet test LumisAPI.sln -c Release
dotnet build tests/Lumis.CSharp8Consumer/Lumis.CSharp8Consumer.csproj -c Release
dotnet run --project samples/HelloLumis -c Release -- --smoke --no-audio
dotnet pack src/Lumis/Lumis.csproj -c Release -o artifacts/packages
```

ユニットテストでは、ウィンドウを開かずに以下を検証します。

- シーン遷移
- ライフサイクルガード
- 設定値検証
- アンチチート Level 1〜5

また、`LangVersion=8.0` を指定した専用 consumer project により、
公開 API が誤って C# 9 以降の構文を必須にしていないことを継続的に検証します。

サンプルの `--smoke` モードでは実際のウィンドウを開き、
描画、入力ポーリング、シーン遷移を実行したあと自動終了します。

オーディオも検証する場合は `--no-audio` を外してください。
フレームを保存する場合は `--capture screenshot.png` を追加できます。

連続したウィンドウ作成、コールバック失敗時の復旧、
ネイティブリソースのクリーンアップを検証する場合:

```sh
dotnet run --project tests/Lumis.NativeSmoke -c Release -- --no-audio
```

オーディオリソースのクリーンアップも検証する場合は
`--no-audio` を外してください。

CI では Windows / Linux / macOS 上で両ライブラリ対象をビルド・テストし、
Linux では Xvfb を利用した実グラフィックス / ライフサイクル smoke test も実行します。

ただし、実際の配布対象マシン上でのネイティブ動作確認は引き続き必要です。

Raylib-cs 8.1.0 のデスクトップ用ランタイムアセットは以下に対応しています。

- `win-x64`
- `win-x86`
- `linux-x64`
- `osx-x64`
- `osx-arm64`

現在、以下の配布先は未対応です。

- ブラウザ
- モバイル
- Windows ARM64
- Linux ARM64

## ドキュメント

- [API / ライフサイクルガイド](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/docs/api.md)
- [コントリビューションガイド / main を正常に保つ方法](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/CONTRIBUTING.md)
- [パッケージ作成 / 公開手順](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/docs/publishing.md)
- [ローカル検証結果](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/docs/verification.md)
- [変更履歴](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/CHANGELOG.md)
- [サードパーティ通知](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/THIRD-PARTY-NOTICES.md)

## ライセンス

LumisAPI と同梱サンプルアセットは
[MIT License](https://github.com/hotamachisubaru-git/LumisAPI/blob/main/LICENSE)
のもとで提供されます。

raylib-cs と raylib には、それぞれの zlib ライセンスと著作権表示が適用されます。
