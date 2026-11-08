# VGC Versioning

ギミックの版の取得・比較と、ギミックの設定・導入。**VRChat の SDK に依存しない**ので、
ワールド用・アバター用のどちらのプロジェクトでも使える。

```
Runtime/   asmdef VGC.Versioning.Runtime   namespace VGC.Versioning
├── GimmickSetting.cs     ScriptableObject。版・プレハブ・依存・名前（翻訳から引く）
├── GimmickInstaller.cs   導入用のマーカー MonoBehaviour（IEditorOnly）
└── GimmickMarker.cs      導入済みのマーカー MonoBehaviour（IEditorOnly）
Editor/    asmdef VGC.Versioning.Editor
├── GimmickVersionManager.cs   版の一覧を取得して比較する
└── GimmickInstallerEditor.cs  導入 UI
```

- `VGC.Versioning` は `VGC.Localization` に依存する（`GimmickSetting.Name` が翻訳テーブルを読む）。逆は無い。
- `com.vrchat.base` に依存するのは、`IEditorOnly` と UniTask を使うため。

## 版の取得について

`GimmickVersionManager` は **読み取りしかしない**。
Apps Script のエンドポイントへ `UnityWebRequest.Get` で問い合わせ、
`{sheetName, version, url}` の一覧を取って手元の版と比べるだけ。

**書き込み（スプレッドシートの更新）はこのパッケージに入っていない。**
書き込みは開発用リポジトリ側のツールが行い、トークンを必要とする。
トークンは開発者のマシンの `EditorPrefs` にあり、このパッケージにもリポジトリにも入っていない。

## 旧 `VGC.Gimmicks_MA.Lib` からの変更

| 旧 | 新 |
|---|---|
| `VGC.Gimmicks_MA.Lib` | `VGC.Versioning` |
| `AvatarGimmickVersionManager` | `GimmickVersionManager` |
| `AvatarGimmickSetting` | `GimmickSetting` |
| `AvatarGimmickInstaller` / `AvatarGimmickMarker` | `GimmickInstaller` / `GimmickMarker` |
| `AvatarGimmickInstallerEditor` | `GimmickInstallerEditor` |
| `AvatarGimmickUtility` | `VGC.Versioning.Avatar.AvatarUtility` へ移動 |
| `ReadOnlyAttribute` | 削除（使われていなかった。必要なら `com.vgc-laboratory.attributes`） |

**シリアライズされるフィールド名は変えていない**（`GimmickSetting._maPrefab` など）。
名前の不一致はコンパイルエラーで気づけるが、フィールド名の変更は値が静かに失われるため。
