# VGC Localization

スプレッドシートで管理する翻訳表（`LocalizationTable`）と、エディタの表示言語。
**VRChat の SDK の API を使わない**ので、ワールド用・アバター用のどちらのプロジェクトでも使える
（`vpmDependencies` は空）。素の Unity プロジェクトでも動く。

ワールドの実行時（Udon）に翻訳を使うときは [localization-world](../com.vgc-laboratory.localization-world/README.md) を併せて入れる。

## 取得元の 2 つの方式

`SpreadsheetSource`（Create > VGC > Localization > Spreadsheet Source）で選ぶ。
翻訳表（`LocalizationTable`）でも版の一覧（`VersionSource`）でも同じものを使う。

| 方式 | 設定するもの | 向き |
|---|---|---|
| **Google Sheets CSV**（既定） | スプレッドシートの ID（URL を貼ってもよい） | ふつうはこちら。シートを公開するだけで使える |
| Apps Script | Web アプリの URL（`…/exec`） | 上級者向け。自分で GAS を書く人だけ |

### Google Sheets CSV

シートの共有設定を **「リンクを知っている全員が閲覧可」** にする。これだけで読める。

- gviz の CSV 出力を読む（`https://docs.google.com/spreadsheets/d/<ID>/gviz/tq?tqx=out:csv&headers=1&sheet=<タブ名>`）
- **タブ名で切り替えられる。** 1 つのスプレッドシートに複数の表を置ける
- 公開を忘れていると Google がログインのページ（HTML）を返す。
  その場合は「応答が HTML でした」というエラーになるので、共有設定を見直す

### Apps Script（上級者向け）

自分で `doGet` を書く場合、次の形にする。

**受け取るもの**

- `?sheet=<タブ名>` が付く（`VersionSource` や `LocalizationTable` のシート名）。
  空のときは付かないので、既定のタブにフォールバックすること

**返すもの**（どちらか。Content-Type は JSON）

翻訳の表:

```json
[
  { "id": "Hello", "values": [
      { "language": "English",  "text": "Hello" },
      { "language": "Japanese", "text": "こんにちは" } ] }
]
```

版の一覧:

```json
{ "items": [
    { "sheetName": "睡眠システム", "version": "1.2.0", "url": "https://..." }
] }
```

`url` はセルの文字列をそのまま返せばよい。カンマや改行で区切った複数の URL、空文字も受け付ける
（読み方は [gimmicks の README](../com.vgc-laboratory.gimmicks/README.md#配布先の-url)）。
改行を `\n` の 2 文字に置き換えて返していても読める。

エラー:

```json
{ "error": "シートが見つかりません: ..." }
```

- `language` は `UnityEngine.SystemLanguage` の名前（`English` / `Japanese` / `Korean` /
  `ChineseSimplified` / `ChineseTraditional` …）。読めない名前の列は読み飛ばす
- セル内の改行は `
`（2 文字）にして返しても、実際の改行のまま返してもよい。どちらも改行として扱う
- Web アプリのアクセス権を「全員」にする。そうしないとログインのページ（HTML）が返り、
  「応答が HTML でした」というエラーになる
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
（[gimmicks](../com.vgc-laboratory.gimmicks/README.md) の最新バージョンの確認用）。どちらも行の配列に直してから扱うので、
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
