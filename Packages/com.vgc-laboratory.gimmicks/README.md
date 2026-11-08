# VGC Gimmicks

VGC のギミックの共通基盤。**VRChat の SDK に依存しない**ので、
ワールド用・アバター用のどちらのプロジェクトでも使える（`vpmDependencies` は `com.vrchat.base` と localization）。

「どのギミックがあって、何版で、プロジェクトへ入れる」までを扱う。

```
Runtime/   asmdef VGC.Gimmicks.Runtime   namespace VGC.Gimmicks
├── GimmickSetting.cs     ScriptableObject。版・プレハブ・依存・クロスプラットフォーム対応・名前（翻訳から引く）
├── GimmickInstaller.cs   導入用のマーカー MonoBehaviour（IEditorOnly）
└── GimmickMarker.cs      導入済みのマーカー MonoBehaviour（IEditorOnly）
Editor/    asmdef VGC.Gimmicks.Editor
├── GimmickVersionManager.cs   版の一覧を取得して手元の版と比べる
└── GimmickInstallerEditor.cs  導入 UI（版チェックの表示を含む）
```

- `VGC.Gimmicks` は `VGC.Localization` に依存する（`GimmickSetting.Name` が翻訳テーブルを読む）。逆は無い。
- `com.vrchat.base` に依存するのは、`IEditorOnly` と UniTask を使うため。

## プラットフォーム固有の部分

| パッケージ | アセンブリ |
|---|---|
| [com.vgc-laboratory.gimmicks-avatar](../com.vgc-laboratory.gimmicks-avatar/README.md) | `VGC.Gimmicks.Avatar.Runtime` / `.Editor` |
| `com.vgc-laboratory.gimmicks-world`（将来） | `VGC.Gimmicks.World.Runtime` / `.Editor` |

ワールド側のコードができるまで、ワールドのプロジェクトはこのパッケージを直接入れて使える。

## 版の取得について

`GimmickVersionManager` は **読み取りしかしない。**
Apps Script の**読み取り専用デプロイ**へ `UnityWebRequest.Get` で問い合わせ、
`{sheetName, version, url}` の一覧を取って手元の版と比べるだけ。
この URL に POST しても拒否される。

**書き込み（スプレッドシートの更新）はこのパッケージに入っていない。**
書き込み用の URL とトークンは開発用リポジトリ側にしかない。

### 取得は間引いている

`[InitializeOnLoad]` なので、そのまま毎回取得すると Apps Script の実行回数の上限に近づく。
取得結果と取得時刻を `EditorPrefs` に残し、`CacheLifetime`（既定 6 時間）を過ぎるまでは取り直さない。

- 起動時にキャッシュから復元する。これをしないと `HasUpdate` が空のまま false を返し、
  更新があっても「更新なし」と誤表示してしまう。
- 利用者が「バージョン確認」を押したときは `Fetch(force: true)` でキャッシュを無視する。
- `EditorPrefs` は Unity のバージョンごと・利用者ごとなので、プロジェクトをまたいで共有される。
  版の一覧はプロジェクトに依らない情報なので問題ない。

## 旧 `VGC.Gimmicks_MA.Lib` からの変更

| 旧 | 新 |
|---|---|
| `VGC.Gimmicks_MA.Lib` | `VGC.Gimmicks` |
| `AvatarGimmickVersionManager` | `GimmickVersionManager` |
| `AvatarGimmickSetting` | `GimmickSetting` |
| `AvatarGimmickInstaller` / `AvatarGimmickMarker` | `GimmickInstaller` / `GimmickMarker` |
| `AvatarGimmickInstallerEditor` | `GimmickInstallerEditor` |
| `AvatarGimmickUtility` | `VGC.Gimmicks.Avatar.AvatarUtility` へ移動 |
| `AvatarGimmickSetupWindow` | `VGC.Gimmicks.Avatar.SetupWindow` へ移動（メニューは `VGC/Gimmick Setup`） |
| `ReadOnlyAttribute` | 削除（使われていなかった） |

**シリアライズされるフィールド名は変えていない**（`GimmickSetting._maPrefab` など）。
名前の不一致はコンパイルエラーで気づけるが、フィールド名の変更は値が静かに失われるため。
