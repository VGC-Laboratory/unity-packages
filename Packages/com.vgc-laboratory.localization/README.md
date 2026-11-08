# VGC Localization

> **これは VGC のギミックが内部で使う基盤。**
> [VRC 合法チート研究会](https://goho-cheat-vrc.booth.pm/)のギミックを入れると
> 依存として一緒に入るので、自分で入れる必要はない。
>
> **外部のクリエイターが直接使うことは想定していない。**
> VRChat の SDK に依存しない作りではあるが、
> **アセットの発見をプロジェクト全体の型検索（`AssetDatabase.FindAssets("t:…")`）で行っている。**
> 持ち主の区別が無いので、独立した利用者が同じプロジェクトに同居すると混ざる。
>
> - `LocalizationTableResolver` … 全 `LocalizationTable` を `sheetName` 一つの辞書に入れる。
>   シート名が衝突すると後に読んだ方で上書きされる
> - `GimmickInstallerEditor` / `SetupWindow` … 全 `GimmickSetting` を一覧に出す。
>   他人のギミックが混ざって見える
>
> 公開 API も VGC のギミックの都合で変わりうる。
>
> ワールド制作向けのライブラリは
> [attributes](../com.vgc-laboratory.attributes/README.md) /
> [core](../com.vgc-laboratory.core/README.md) /
> [ui-extension](../com.vgc-laboratory.ui-extension) /
> [game-framework](../com.vgc-laboratory.game-framework/README.md) 側。

翻訳テーブルとエディタ言語の設定。**VRChat の SDK に依存しない**ので、
ワールド用・アバター用のどちらのプロジェクトでも使える（`vpmDependencies` は `com.vrchat.base` だけ）。

```
Runtime/   asmdef VGC.Localization.Runtime   namespace VGC.Localization
├── Language.cs              Language（5言語の列挙）と LanguageSetting（言語の継ぎ目）
├── LocalizationTable.cs     ScriptableObject。id → 言語 → 文字列
├── LocalizationTableResolver.cs  シート名からテーブルを引く
└── LocalizedBehaviour.cs    インスペクタに言語切り替えを出す基底 MonoBehaviour
Editor/    asmdef VGC.Localization.Editor
├── LocalizationTableEditor.cs
└── LocalizationTreeView.cs
```

## 言語の継ぎ目（LanguageSetting）

「今どの言語か」だけが環境によって違うので、ここを差し替え可能にしてある。

```csharp
public static Func<Language> Getter;      // 既定: EditorPrefs
public static Action<Language> Setter;    // 既定: EditorPrefs
public static event Action<Language> Changed;
public static Language Current { get; set; }
```

- **ワールド用プロジェクト**: 既定のまま。`EditorPrefs`（`VGC.Localization.Language`）に保存する。
- **アバター用プロジェクト**: [gimmicks-avatar](../com.vgc-laboratory.gimmicks-avatar/README.md) の
  `NdmfLanguageProvider` が `[InitializeOnLoad]` で差し替え、**NDMF / Modular Avatar の
  `LanguagePrefs` に相乗り**する。MA のインスペクタで言語を変えると VGC 側も切り替わる（逆も同じ）。

この形にしているので、**共通側は NDMF を一切参照しない。**
NDMF が無い環境ではアバター側のアセンブリがそもそもコンパイルされず、既定動作になる。

## 旧 `VGC.Gimmicks_MA.Lib.Localization` からの変更

| 旧 | 新 |
|---|---|
| `VGC.Gimmicks_MA.Lib.Localization` | `VGC.Localization` |
| `LocalizeConfigBehaviour` | `LocalizedBehaviour` |
| `LocalizeConfig`（NDMF 相乗り） | `VGC.Localization.Avatar.NdmfLanguageProvider` へ移動 |
| （なし） | `LanguageSetting`（継ぎ目） |

**`.cs` の `.meta` GUID は維持している**ので、利用者が持っている `LocalizationTable.asset` は
そのまま読める（アセットは型名ではなくスクリプトの GUID で参照しているため）。
一方、**ソースは名前で参照しているので、古い名前を使っているコードは直す必要がある**（破壊的変更）。
