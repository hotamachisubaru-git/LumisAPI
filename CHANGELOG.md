# 変更履歴

LumisAPI のすべての主要な変更はここに記録されます。
バージョン番号は [Semantic Versioning](https://semver.org/) に従います。
メジャーバージョンが 0 の間は、マイナーバージョン間で API が変更される場合があります。

## [Unreleased]

### Unity / 共通ライブラリ

- アンチチートを raylib 非依存の Lumis.Security に分離。共通 C# 8 ソースを .NET Standard 2.1 / .NET 8 / .NET 10 へビルド。
- 公開名前空間 Lumis を維持し、Lumis.dll に既存公開型の転送を追加。内部メモリレイアウトの互換性は対象外。
- Unity 向け UPM パッケージ jp.hotamachi.lumis.security と LumisAntiCheat、ブートストラップ例、EditMode テストを追加。
- Unity では unscaledDeltaTime とポーズ/フォーカス復帰時の観測リセットを使用。利用不能なプロセス検査は無効化して警告。
- IL2CPP のエントリアセンブリ探索を自動実行せず、明示的なファイルパスと信頼済み SHA-256 による検証へ対応。
- URL 形式のアセット用に、取得済みバイト列を検証する FileIntegrityService.VerifyData を追加。非同期ダウンロードはホスト側で実装。
- Unity / .NET Standard 向けに PortableSaveDataProtector を追加。AES-256-CBC + HMAC-SHA256 の認証付き LSP1 形式を明示使用。
- 旧 SaveDataProtector の GCM v1 は対応 .NET 8/10 環境で維持し、Unity / .NET Standard では未対応を明示。暗黙の形式変更は行わない。
- サービス生成時に設定とブロックリストをコピー。Secure 数値型は書き込み前にも既存値の改ざんを検証。
- .NET Standard DLL の単独実行試験、独立した暗号テストベクトル、公開型転送テスト、Unity 条件付きコンパイル検査を追加。
- Unity 条件付き検査は API スタブによるもの。Unity Editor / Mono Player / IL2CPP Player の実動作は未検証であることをドキュメントに明記。
- ローカル/CI で Lumis.Security と LumisAPI の両パッケージを作成し、公開ワークフローは依存パッケージを先に扱うよう更新。今回の変更でリリース・NuGet 公開は行わない。

### 追加機能

- C# 8 の利用側コードから現在の C# 14 まで扱えるよう、公開設定 API を通常の setter に統一し、`LangVersion=8.0` の互換性ビルドを CI に追加。
- アンチチート Level 1 として、ゲーム起動前と実行中の禁止プロセス監視を追加。
- Cheat Engine の代表的なプロセス名に加え、取得可能な実行ファイルパス、ProductName、FileDescription、OriginalFilename からの検出を追加。
- ゲーム固有の禁止プロセス名と実行ファイルパス断片を設定できるブロックルールを追加。
- オプションのデバッガ検出を追加。Windows は `IsDebuggerPresent`、Linux は `TracerPid` を利用し、その他の環境ではマネージドデバッガ検出を利用。
- 検出イベントと `AntiCheatException` に起動時/実行時フェーズ、PID、実行ファイルパス情報を追加。
- アンチチート Level 2 として、ランダムキーによる難読化と整合性タグを備えた `SecureInt` / `SecureLong` / `SecureFloat` / `SecureDouble` を追加。
- アンチチート Level 3 として、ゲームの delta time と単調増加時計を比較する SpeedHack / 時間加速検出を追加。
- アンチチート Level 4 として、信頼済み SHA-256 または現在値スナップショットによるファイル/ディレクトリ整合性検証を追加。
- アンチチート Level 5 として、AES-256-GCM の `SaveDataProtector` とエントリアセンブリ SHA-256 整合性監視を追加。

## [0.1.1] - 2026-09-28

### 修正

- ゲーム終了キー（ESC）の検出を `IsKeyPressed` から `IsKeyDown` に変更し、全シーンで確実に終了できるように修正。

## [0.1.0] - 2026-09-25

最初のリリース。

### 追加機能

- `Lumis` ネームスペースと NuGet パッケージ ID `LumisAPI` を持つ .NET 8 および .NET 10 ライブラリ。
- raylib-cs 8.1.0 に基づいた、カスタマイズ可能なネイティブウィンドウとゲームループ。
- 2D 図形、テキスト、テクスチャ、スクリーンショットキャプチャ、キーボードおよびマウス入力のサポート。
- サウンドエフェクトとストリーミング音楽の再生および音量コントロール。
- 遅延シーン遷移と明示的な enter/update/draw/exit コールバック。
- ネイティブリソースの自動クリーンアップとスレッド/ライフサイクルガード。
- バンドルされた生成済み PNG および WAV アセットを備えた HelloLumis サンプル。
- 両 .NET ターゲット向けのヘッドレスユニットテスト、デスクトップグラフィックスおよびライフサイクルのスマクテスト、
  Windows/Linux/macOS 用の CI、XML API ドキュメント。
- リリースタグの検証と必要な `NUGET_API_KEY` 設定の検証、
  全プラットフォームのビルドとテストの待機、検証済みパッケージアーティファクトの公開を行う NuGet 公開機能。
- NuGet V3 パッケージおよびシンボル公開、リリース手順、リポジトリメタデータ。
- MIT ライセンスとローカル NuGet パッケージ化手順。
