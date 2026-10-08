# Unity 対応

共通アンチチートを `Lumis.Security` に分離し、Unity 用 UPM パッケージを追加しました。

- 共通実装: `Packages/jp.hotamachi.lumis.security/Runtime/Core`
- .NET プロジェクト: `src/Lumis.Security`（同じ共通ソースをリンクしてコンパイル）
- Unity ホスト: `Packages/jp.hotamachi.lumis.security/Runtime/Unity`
- 対象: .NET Standard 2.1 / .NET 8 / .NET 10、共通ソースは C# 8
- 公開名前空間: `Lumis` を維持。Unity 専用は `Lumis.Unity`

[導入・設定・制限事項・サンプルの説明](../Packages/jp.hotamachi.lumis.security/README.md)を参照してください。

## 対応方針

Unity の .NET Standard 2.1 に合わせ、ThrowIfNull、SHA256.HashData、Convert.ToHexString などへの依存を互換実装へ置き換えました。
raylib、UnityEngine に依存しない共通アセンブリを LumisAPI と Unity ホストの両方から使用します。
Lumis.dll には旧公開型の type forwarder を残し、既存の `using Lumis;` と型名を維持します。
ただし内部型、reflection による private フィールドアクセス、内部メモリレイアウトの互換性は保証しません。
設定はサービス生成時にコピーするため、生成後に元の設定を変更しても動作は変わりません。

## セーブ形式

既存 `SaveDataProtector` の GCM v1 は .NET 8/10 向けに維持しました。
Unity / .NET Standard では `PortableSaveDataProtector`（LSP1）を明示使用します。
LSP1 は別々に派生した鍵による AES-256-CBC + HMAC-SHA256、復号前の認証を使用します。
形式の自動切り替えや旧データの暗黙移行は行いません。鍵保管・再発行・セーブ巻き戻し防止は別途必要です。

## 検証範囲

- 通常の .NET 8/10 テストと Lumis ネイティブウィンドウの回帰検証
- .NET Standard 2.1 DLL を .NET 8 ホストで単独実行し、raylib / UnityEngine 非依存を検証
- 独立生成した暗号テストベクトル、破損データ、誤キー、設定コピー、ハッシュ、保護値を検証
- Unity の条件付きコードを Editor / Windows IL2CPP / Android / WebGL シンボルでコンパイル（API スタブ）
- Unity Test Runner 用 EditMode テストを同梱

**スタブのコンパイル成功は、Unity Editor 実機・Mono Player・IL2CPP 変換・モバイル/WebGL 動作の成功を意味しません。実際の Unity でのテストは未実施です。**

## リリース

今回の変更だけでは NuGet 公開もタグ作成も行いません。
次回公開時は LumisAPI と Lumis.Security の csproj を未使用の同一バージョンに更新してください。
公開ワークフローは検証済みの Lumis.Security を先に push し、次に LumisAPI を push します。
NUGET_API_KEY の対象パッケージに両 ID が含まれること、Lumis.Security の ID 利用権限があることを確認してください。
UPM の package.json バージョンは NuGet と別に管理します。配布では Git URL の参照を検証済み SHA に固定してください。

## 参照

- [Unity: .NET プロファイル](https://docs.unity3d.com/jp/current/Manual/dotnet-profile-support.html)
- [Unity: StreamingAssets のプラットフォーム差](https://docs.unity3d.com/jp/current/ScriptReference/Application-streamingAssetsPath.html)
- [Microsoft: CBC を使用する場合の復号前の認証](https://learn.microsoft.com/en-us/dotnet/standard/security/vulnerabilities-cbc-mode)
