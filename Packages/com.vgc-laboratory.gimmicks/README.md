# VGC Gimmicks

ギミックやツールの**版の確認**と、VGC のギミックの**定義・導入**の共通基盤。
**VRChat の SDK に依存しない**ので、ワールド用・アバター用のどちらのプロジェクトでも使える
（`vpmDependencies` は `com.vrchat.base` と [localization](../com.vgc-laboratory.localization/README.md)）。

## 版の確認（誰でも使える）

自分のツールの最新版をスプレッドシートで管理し、インスペクタに更新のお知らせを出せる。

1. スプレッドシートに版の一覧を作る。1 行目は見出し、A 列にキー（ツールの識別名）、B 列に最新版（`1.2.3`）、C 列にダウンロード URL。
2. 共有設定を「リンクを知っている全員が閲覧可」にする。
3. Create > VGC > Localization > **Spreadsheet Source** でシート ID を登録する。
4. Create > VGC > Gimmicks > **Version Source** を作り、Source とシート名を入れる。
5. インスペクタやウィンドウで呼ぶ。

```csharp
[SerializeField] VersionSource _versionSource;

UpdateNoticeGUI.Draw(_versionSource, "MyTool", "1.0.0");   // 新しい版があるときだけ表示する
UpdateNoticeGUI.DrawCheckButton(_versionSource);           // 「更新を確認」ボタン（キャッシュを無視して取り直す）

// 自分で表示したいとき
if (VersionChecker.HasUpdate(_versionSource, "MyTool", "1.0.0", out var latest))
    Application.OpenURL(VersionChecker.GetUrl(_versionSource, "MyTool"));
```

- 取得は最初に問い合わせたときに始まり、結果を取得元ごとに `EditorPrefs` へ残す。`CacheLifetime`（既定 6 時間）を過ぎるまで取り直さない。
- 版の比較は数値で行う（`1.10.0 > 1.9.0`）。先頭の `v`、`-beta` や `+build` 以降は比べない。数値でない部分は 0 として扱い、例外にしない。
- `VersionSource` を指定しない（null）ときは、VGC のギミックの版の一覧（`VersionChecker.VGCAppsScriptUrl`、Apps Script の読み取り専用デプロイ）を見る。

**書き込み（スプレッドシートの更新）はこのパッケージに入っていない。**

### 一覧を分ける

`VersionSource` はいくつでも作れる。取得元やタブを変えた `VersionSource` を用意し、
`GimmickSetting` の Version Source にそれぞれ指定すれば、**一覧を分けられる。**

| 分け方 | Google Sheets CSV | Apps Script |
|---|---|---|
| 別のスプレッドシート | ○ | ○ |
| 同じスプレッドシートの別タブ | ○ | GAS が `?sheet=` を見ている必要がある |

- キャッシュは「取得元 ＋ タブ名」ごとに分かれる。
- 導入 UI は使われている取得元をすべて取りに行くので、**1 つの画面に複数の取得元が混在してよい。**
- **Version Source が空のギミックは、VGC の既定の一覧を見る。**
  分けるときは各 `GimmickSetting` に設定すること。

取得元の作り方と、自分で GAS を書くときの仕様は
[localization の README](../com.vgc-laboratory.localization/README.md) にある。
## ギミックの定義と導入（誰でも使える）

```
Runtime/   asmdef VGC.Gimmicks.Runtime   namespace VGC.Gimmicks
├── GimmickSetting.cs     ScriptableObject。配布元・版・取得元・プレハブ・依存・クロスプラットフォーム対応
├── VersionSource.cs      ScriptableObject。版の一覧を読むシート
├── GimmickInstaller.cs   導入用のマーカー MonoBehaviour（IEditorOnly）
└── GimmickMarker.cs      導入済みのマーカー MonoBehaviour（IEditorOnly）
Editor/    asmdef VGC.Gimmicks.Editor
├── VersionChecker.cs          取得元ごとに版の一覧を読み、手元の版と比べる
├── UpdateNoticeGUI.cs         更新のお知らせ / 更新を確認ボタン / このパッケージの翻訳表（GimmickTexts）
└── GimmickInstallerEditor.cs  導入 UI（版チェックの表示を含む）
Config/
└── GimmickInstallerTable.asset  導入 UI と更新のお知らせの文言（シート「メインウィンドウ」）
```

- `GimmickSetting` と同じフォルダに `LocalizationTable` を置く。表の `GimmickName` の訳が表示名になる。
- 版チェックのキーは `GimmickSetting` の Version Key（空なら同じフォルダの表のシート名）。取得元は Version Source（空なら VGC の版の一覧）。
- 導入 UI は、プロジェクトにある `GimmickSetting` をすべて一覧に出す（型で探す）。
  ギミックのアセットを置くだけで一覧に並ぶので、登録作業は要らない。

### 配布元ごとにまとまる

`GimmickSetting` の **Publisher**（配布元）に名前を入れると、導入 UI がその名前ごとにまとめて並べる。

- 別の配布元のギミックが同じプロジェクトに入っていても取り違えない。
- **空でも動く。** 空のものは見出し無しで先頭にまとまる。
- 配布元が 1 つしか無いときは見出しを出さない（今までの見た目のまま）。

自分のツールを配る場合は、自分の名前やサークル名を入れておくとよい。
利用者のプロジェクトに他の作者のギミックが入ったときに、どれが自分のものか分かる。

## プラットフォーム固有の部分

| パッケージ | アセンブリ |
|---|---|
| [com.vgc-laboratory.gimmicks-avatar](../com.vgc-laboratory.gimmicks-avatar/README.md) | `VGC.Gimmicks.Avatar.Runtime` / `.Editor` |
| `com.vgc-laboratory.gimmicks-world`（将来） | `VGC.Gimmicks.World.Runtime` / `.Editor` |

## 0.1.0 からの変更（破壊的）

| 0.1.0 | 0.2.0 |
|---|---|
| `GimmickVersionManager`（取得元は固定の `URL`） | `VersionChecker`（取得元 `VersionSource` を引数で渡す。省略時は VGC の版の一覧） |
| `GimmickVersionManager.URL` | `VersionChecker.VGCAppsScriptUrl` |
| `HasUpdate(id, local, out remote)` | `HasUpdate(source, key, local, out remote)` |
| 起動時（`[InitializeOnLoad]`）に取得 | 最初に問い合わせたときに取得 |
| （なし） | `VersionSource` / `UpdateNoticeGUI` / `GimmickSetting` の Version Source・Version Key |
| UniTask に依存 | 依存しない（`SpreadsheetReader` で取得） |
| 導入 UI の文言をシート名「メインウィンドウ」で全体から検索 | このパッケージの `Config/GimmickInstallerTable.asset` を GUID で読む（gimmicks-avatar から移した） |
