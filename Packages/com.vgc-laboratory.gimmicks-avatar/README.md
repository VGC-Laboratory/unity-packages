# VGC Gimmicks (Avatar)

VGC のアバターギミックが共通で使う基盤。**アバター専用**（`com.vrchat.avatars` と Modular Avatar に依存）。

配布済み 1.0 系（各ギミックの unitypackage に同梱されていた `Assets/VGC/Gimmicks_MA/Lib`）の後継。
unitypackage 同梱から VPM パッケージへ配布形態が変わる区切りとして **2.0.0** から始める。
**公開 API（型・メンバー・シリアライズされるフィールド名）は 1.0 系から消さない。**
利用者のプロジェクトに旧版のギミックが残っていても動くようにするため。

```
Runtime/                         asmdef: VGC.Gimmicks.Avatar.Runtime
├── Attributes/ReadOnlyAttribute.cs
├── Installer/                   AvatarGimmickSetting / AvatarGimmickMarker /
│                                AvatarGimmickInstaller / AvatarGimmickUtility
└── Localization/                LocalizationTable / LocalizationTableResolver /
                                 LocalizationExMenu / LocalizeConfig / LocalizedMenuLabels
Editor/                          asmdef: VGC.Gimmicks.Avatar.Editor
├── Installer/                   AvatarGimmickInstallerEditor / SetupWindow
├── Localization/                LocalizationExMenuPlugin（NDMF プラグイン）/
│                                LocalizationTableEditor / LocalizationTreeView /
│                                LocalizationTableNdmfExtensions
└── VersionManager/              AvatarGimmickVersionManager
Config/LocalizationTable.asset   実行時に使う翻訳テーブル
VGCResources/EmptyAnimation.anim
```

- 名前空間は `VGC.Gimmicks_MA.Lib` と `VGC.Gimmicks_MA.Lib.Localization`。
- asmdef の名前と GUID は変えない。**配布済みの各ギミックの asmdef が GUID で参照している。**
  - `VGC.Gimmicks.Avatar.Runtime` … `433d64cc240a8214ea3fa88408f14787`
  - `VGC.Gimmicks.Avatar.Editor` … `43840042a51294744a849c40be6f5eee`
- `versionDefines`: `nadena.dev.modular-avatar` → `MODULAR_AVATAR`。

## 依存関係

| 参照先 | 来る場所 |
|---|---|
| `nadena.dev.modular-avatar.core` | Modular Avatar |
| `nadena.dev.ndmf` | NDMF（MA 経由で入る） |
| `UniTask` | `com.vrchat.base`（avatars SDK 経由で入る） |
| `VRC.SDK3A` / `VRC.SDKBase` | avatars SDK（`autoReferenced` なので asmdef に書かれていない） |

使っている MA / NDMF の API は `ObjectRegistry.TryRegisterReplacedObject` / `GetReference`、
`Localizer`、`LanguagePrefs`、`Plugin<T>` / `ExportsPlugin` / `BuildPhase` / `ErrorReport`、
`ModularAvatarMenuItem` / `ModularAvatarMenuInstaller`。
いずれも MA 1.18 系にあるものなので、`vpmDependencies` は `>=1.18.0 <2.0.0-a` としている。

## 旧フォルダの削除（legacyFolders）

`package.json` の `legacyFolders` に旧フォルダとその GUID を書いてある。
VCC・vrc-get（各ギミックに同梱している自動導入ツールも中身は vrc-get）がこのパッケージを入れるとき、
旧フォルダを消してくれる。GUID でも照合するので、利用者がフォルダを移動していても消える。

| 旧フォルダ | GUID |
|---|---|
| `Assets\VGC\Gimmicks_MA\Lib` | `c6dcaad7d767b274c85fc33f805b383f` |
| `Assets\VGC\VGCResources` | `f6d57f4e3e4bc244cb83ffa7f9103f0b` |

旧 1.0 の unitypackage を後から取り込み直すと旧フォルダが戻ってきて GUID が重複する。
この場合の扱いは未確認（設計書 5.2 の課題）。

## 開発専用で入れていないもの

`Lib/Config/LocalizeDownloader.asset`（スプレッドシートの取り込み設定）はパッケージに入れない。
移動元の VGCAvatarGimmick_MA に残してある。
