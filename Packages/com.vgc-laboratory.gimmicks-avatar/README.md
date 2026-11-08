# VGC Gimmicks (Avatar)

VGC のギミックの**アバター固有の部分**。共通部分は
[gimmicks](../com.vgc-laboratory.gimmicks/README.md) と
[localization](../com.vgc-laboratory.localization/README.md) にある。

```
Runtime/
├── Gimmicks/        asmdef VGC.Gimmicks.Avatar.Runtime      namespace VGC.Gimmicks.Avatar
│   └── AvatarUtility.cs          アバターのルート探索、階層パスの取得
└── Localization/    asmdef VGC.Localization.Avatar.Runtime  namespace VGC.Localization.Avatar
    ├── NdmfLanguageProvider.cs   エディタ言語を NDMF の LanguagePrefs に相乗りさせる
    ├── LocalizationExMenu.cs     エクスプレッションメニューの翻訳
    └── LocalizedMenuLabels.cs    子孫の MA Menu Item のラベルをビルド時に差し替える
Editor/
├── Gimmicks/        asmdef VGC.Gimmicks.Avatar.Editor
│   └── SetupWindow.cs            導入ウィンドウ（メニュー: VGC/Gimmick Setup）
└── Localization/    asmdef VGC.Localization.Avatar.Editor
    ├── LocalizationExMenuPlugin.cs      NDMF プラグイン
    └── LocalizationTableNdmfExtensions.cs  NDMF の Localizer との橋渡し
```

`VGC.Localization.Avatar.*` の 2 つは `versionDefines`（`nadena.dev.modular-avatar` → `MODULAR_AVATAR`）を持つ。

導入 UI の文言は [gimmicks](../com.vgc-laboratory.gimmicks/README.md) の
`Config/GimmickInstallerTable.asset` にある（このパッケージは翻訳表を持たない）。

## エディタ言語を NDMF に相乗りさせる

`NdmfLanguageProvider` が `[InitializeOnLoad]` で
[localization](../com.vgc-laboratory.localization/README.md) の `LanguageSetting` に差し込み、
**NDMF / Modular Avatar と共通の言語設定（`LanguagePrefs`）**につなぐ。
MA のインスペクタで言語を変えると VGC 側も切り替わる（逆も同じ）。

この形にしているので、**共通側は NDMF を一切参照しない。**
NDMF が無いワールド用プロジェクトではこのアセンブリ自体がコンパイルされず、
`LanguageSetting` は既定の `EditorPrefs` 動作になる。

## 導入ウィンドウ

メニュー **VGC/Gimmick Setup**。アバターを選ぶと `GimmickInstaller` を付け、
プロジェクト内の `GimmickSetting` を一覧にして導入・更新できる。

一覧は **配布元（`GimmickSetting.Publisher`）ごとにまとめて並ぶ。**
別々の配布元のギミックが同じプロジェクトに入っていても取り違えない。
詳しくは [gimmicks の README](../com.vgc-laboratory.gimmicks/README.md) を参照。

## 旧フォルダの削除（legacyFolders）

`package.json` の `legacyFolders` に、unitypackage 同梱時代の旧フォルダとその GUID を書いてある。
VCC・vrc-get がこのパッケージを入れるとき、旧フォルダを消してくれる。GUID でも照合するので、
利用者がフォルダを移動していても消える。

| 旧フォルダ | GUID |
|---|---|
| `Assets\VGC\Gimmicks_MA\Lib` | `c6dcaad7d767b274c85fc33f805b383f` |

## 経緯

配布済み 1.0 系（各ギミックの unitypackage に同梱されていた `Assets/VGC/Gimmicks_MA/Lib`）の後継。
unitypackage 同梱から VPM パッケージへ配布形態が変わる区切りとして 2.0.0 から始めた。

当初は名前空間 `VGC.Gimmicks_MA.Lib` のまま 1 パッケージだったが、
中身の大半がアバター固有でないと分かったため、機能ごとに
`VGC.Localization` / `VGC.Gimmicks` へ切り出し、このパッケージにはアバター固有の部分だけを残した。
**旧 1.0 系の公開 API との互換は維持していない**（破壊的変更）。
