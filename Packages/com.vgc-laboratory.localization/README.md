# VGC Localization

スプレッドシートで管理する翻訳表（`LocalizationTable`）と、エディタの表示言語。
**VRChat の SDK に依存しない**ので、ワールド用・アバター用のどちらのプロジェクトでも使える
（`vpmDependencies` は `com.vrchat.base` だけ）。

ワールドの実行時（Udon）に翻訳を使うときは [localization-world](../com.vgc-laboratory.localization-world/README.md) を併せて入れる。

## 使い方

1. **スプレッドシートを用意する**。1 行目は見出しで、A 列に `key`、B 列以降に言語名（`SystemLanguage` の名前: `Japanese`, `English`, `Korean`, `ChineseSimplified` …）。2 行目以降が 1 キー 1 行。言語として読めない見出しの列（メモ欄など）は読み飛ばす。
2. 共有設定を「リンクを知っている全員が閲覧可」にする。
3. Create > VGC > Localization > **Spreadsheet Source** を作り、シート ID（URL をそのまま貼ってもよい）を入れる。
4. Create > VGC > Localization > **Localization Table** を作り、Source と Sheet Name を入れて **Import**。
5. コードから引く。

```csharp
[SerializeField] LocalizationTable _texts;

EditorGUILayout.LabelField(_texts.Get("Title"));           // エディタの表示言語
EditorGUILayout.LabelField(_texts.Get("Count", 3));        // string.Format
var ja = _texts.Get("Title", SystemLanguage.Japanese);      // 言語を指定
```

訳が無いときは、表の Default Language → 最初に見つかった訳 → キー の順に返す。

### 取得方法

| `SpreadsheetSource.Kind` | 仕組み | 向いているとき |
|---|---|---|
| `GoogleSheetsCsv`（既定） | 公開シートを gviz の CSV 出力で直接読む | シート ID だけで使いたい |
| `AppsScript` | Apps Script の Web アプリ（`?sheet=シート名` で GET） | 書き込みや整形を Apps Script でしたい |

Apps Script の応答は、翻訳の配列 `[{id, values:[{language, text}]}]` か、版の一覧 `{items:[{sheetName, version, url}]}`
（[gimmicks](../com.vgc-laboratory.gimmicks/README.md) の版チェック用）。どちらも行の配列に直してから扱うので、
取得方法が違っても同じシートの形で使える。

## 言語

言語は `UnityEngine.SystemLanguage`。**使う言語の一覧と既定言語は Project Settings > VGC > Localization** で決める
（`ProjectSettings/VGCLocalizationSettings.asset`）。一覧の並び順が言語メニューの並びで、
ワールド側（localization-world）の言語の番号にもなる。

| API | 内容 |
|---|---|
| `LanguageSetting.Current` | エディタの表示言語。既定は `EditorPrefs` に保存（初回は OS の言語が一覧にあればそれ） |
| `LanguageSetting.Available` / `Default` | Project Settings の一覧と既定言語 |
| `LanguageSetting.Changed` | 言語（または一覧）が変わったとき |
| `LanguageSetting.Getter` / `Setter` | 言語の保存先の差し替え口。アバター用プロジェクトでは [gimmicks-avatar](../com.vgc-laboratory.gimmicks-avatar/README.md) の `NdmfLanguageProvider` が NDMF / Modular Avatar の `LanguagePrefs` に相乗りさせる |
| `ILocalizable` | 開いている EditorWindow / Editor が実装すると、言語が変わったときに `OnLanguageChanged` が呼ばれる。そのあと全ビューを描き直す |
| `LocalizeUIHelper.InspectorGUI()` | 言語を選ぶポップアップ（一覧の言語を自称で出す） |
| `LanguageNames.NativeName` / `TryParse` | 言語の自称（日本語、English …）、見出し文字列からの変換 |

## 翻訳表を探す（LocalizationTableResolver）

**プロジェクト全体をシート名で探すことはしない。** 別の作者の表と名前が衝突するため（「メインウィンドウ」など）。
次のどれかで場所を絞る。基本は、使う側が `LocalizationTable` をフィールドで直接持つこと。

| API | 用途 |
|---|---|
| `LoadByGuid(guid)` | 自分のパッケージに入れた決まった表（移動・改名しても変わらない） |
| `LoadAtPath(path)` | パスで読む |
| `FindInFolder(folder, sheetName)` | フォルダの中から探す |
| `FindNextTo(asset, sheetName)` | アセットと同じフォルダから探す（`GimmickSetting` がこれを使う） |

## 構成

```
Runtime/   asmdef VGC.Localization.Runtime   namespace VGC.Localization
├── LanguageSetting.cs            LanguageSetting / LocalizationProjectSettings / LanguageNames / ILocalizable
├── LocalizationTable.cs          ScriptableObject。キー → 言語 → 文字列
├── SpreadsheetSource.cs          ScriptableObject。取り込み元のスプレッドシート
├── LocalizationTableResolver.cs  GUID / パス / フォルダで表を探す（エディタのみ）
└── LocalizedBehaviour.cs         インスペクタに言語の選択を出す基底 MonoBehaviour
Editor/    asmdef VGC.Localization.Editor
├── Import/                       CsvParser / SpreadsheetReader / LocalizationTableImporter（namespace VGC.Localization.Import）
├── LocalizationTableEditor.cs    表のインスペクタ（Import ボタン、表の閲覧）
├── LocalizationSettingsProvider.cs  Project Settings > VGC > Localization
└── LanguageChangeNotifier.cs     ILocalizable への通知
```

## 0.1.0 からの変更（破壊的）

| 0.1.0 | 0.2.0 |
|---|---|
| `Language`（5 言語の列挙） | `UnityEngine.SystemLanguage`。保存してある言語名は同じなので、既存の `LocalizationTable.asset` はそのまま読める |
| `LocalizationTableResolver.Get(sheetName)`（全体から検索） | `LoadByGuid` / `LoadAtPath` / `FindInFolder` / `FindNextTo` |
| 取り込みは無し（開発用リポジトリの別ツール） | `SpreadsheetSource` と表のインスペクタの Import |
| `Get(id, Language fallback)` | `Get(id)` は表の `Default Language` に戻る |
