# VGC Gimmicks

ギミックやツールの**版の確認**と、VGC のギミックの**定義・導入**の共通基盤。
**VRChat の SDK が無いプロジェクトでも使える**（[後述](#vrchat-の-sdk-が無いプロジェクト)）。
ワールド用・アバター用のどちらのプロジェクトでも使える
（`vpmDependencies` は [localization](../com.vgc-laboratory.localization/README.md) だけ）。

## 版の確認（誰でも使える）

自分のツールの最新版をスプレッドシートで管理し、インスペクタに更新のお知らせを出せる。

1. スプレッドシートに版の一覧を作る。1 行目は見出し、A 列にキー（ツールの識別名）、B 列に最新版（`1.2.3`）、C 列に配布先の URL（[書き方](#配布先の-url)）。
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
- 配布先が複数あるときは `VersionChecker.GetUrls` で全部取れる。`GetUrl` は最初の 1 つを返す。
- **`VersionSource` が null のときは版の確認をしない。** 他人のシートを黙って見に行くことはない。

**書き込み（スプレッドシートの更新）はこのパッケージに入っていない。**

### 配布先の URL

C 列には **配布先をいくつでも書ける。** Booth と GitHub、日本語版と英語版のページ、のように並べてよい。

| 書き方 | 例 |
|---|---|
| カンマで区切る（半角 `,`・全角 `，`・読点 `、` のどれでも） | `https://xxx.booth.pm/items/1, https://github.com/xxx/yyy/releases` |
| セル内で改行する（Alt + Enter） | 1 行に 1 つ |
| 空白で区切る | `https://a.example https://b.example` |

- **URL が 1 つ**なら、更新ボタンを押すとそのページが開く。
- **2 つ以上**なら、更新ボタンを押すと行き先を選ぶメニューが出る（ホスト名で並ぶ。書いた順）。
- **空欄**でもよい。新しい版のお知らせだけ出て、ボタンは押せない。
- `-` や `なし` のように **URL として読めないものは無視する**（http / https 以外も無視）。同じ URL を 2 回書いても 1 つにまとまる。
- URL そのものにカンマが入るときは `%2C` と書く。

### 一覧を分ける

`VersionSource` はいくつでも作れる。取得元やタブを変えた `VersionSource` を用意し、
`GimmickSetting` の Version Source にそれぞれ指定すれば、**一覧を分けられる。**

| 分け方 | Google Sheets CSV | Apps Script |
|---|---|---|
| 別のスプレッドシート | ○ | ○ |
| 同じスプレッドシートの別タブ | ○ | GAS が `?sheet=` を見ている必要がある |

- キャッシュは「取得元 ＋ タブ名」ごとに分かれる。
- 導入 UI は使われている取得元をすべて取りに行くので、**1 つの画面に複数の取得元が混在してよい。**
- **Version Source が空のギミックは版の確認をしない。** 確認したいなら必ず設定する。

取得元の作り方と、自分で GAS を書くときの仕様は
[localization の README](../com.vgc-laboratory.localization/README.md) にある。

### VGC の取得元

VGC 自身の版の一覧は、1 つのスプレッドシートをアバター用とワールド用のタブに分けてある。
このパッケージの `Config/` に、そのまま使えるアセットが入っている。

| アセット | タブ |
|---|---|
| `Config/VGCAvatarGimmicks.asset` | アバターギミック |
| `Config/VGCWorldGimmicks.asset` | ワールドギミック |

どちらも `Config/VGCSpreadsheet.asset`（VGC の Apps Script、読み取り専用デプロイ）を見ている。
VGC のギミックを作るときは、`GimmickSetting` の Version Source にこのどちらかを入れる。

## ギミックの定義と導入（誰でも使える）

```
Runtime/   asmdef VGC.Gimmicks.Runtime   namespace VGC.Gimmicks
├── GimmickSetting.cs     ScriptableObject。配布元・版・取得元・プレハブ・依存・クロスプラットフォーム対応
├── VersionSource.cs      ScriptableObject。版の一覧を読むシート
├── GimmickInstaller.cs   導入用のマーカー MonoBehaviour（VRChat の SDK があれば IEditorOnly）
├── GimmickMarker.cs      導入済みのマーカー MonoBehaviour（VRChat の SDK があれば IEditorOnly）
└── IGimmickSettings.cs   設定用のコンポーネントの目印（設定ウィンドウが表示する）
Editor/    asmdef VGC.Gimmicks.Editor
├── VersionChecker.cs          取得元ごとに版の一覧を読み、手元の版と比べる
├── UpdateNoticeGUI.cs         更新のお知らせ / 更新を確認ボタン / このパッケージの翻訳表（GimmickTexts）
├── GimmickInstallerEditor.cs  導入 UI（版チェックの表示を含む）
├── GimmickSettingsWindow.cs   設定ウィンドウ（IGimmickSettings を付けたコンポーネントだけを表示する）
└── Animation/AnimatorFactory.cs  アニメーターをメモリ上で組み立てる（namespace VGC.Gimmicks.Animation）
Config/
├── GimmickInstallerTable.asset  導入 UI と更新のお知らせの文言（シート「メインウィンドウ」）
├── VGCSpreadsheet.asset         VGC の Apps Script（読み取り専用デプロイ）
├── VGCAvatarGimmicks.asset      ↑ のタブ「アバターギミック」
└── VGCWorldGimmicks.asset       ↑ のタブ「ワールドギミック」
```

- `GimmickSetting` と同じフォルダに `LocalizationTable` を置く。表の `GimmickName` の訳が表示名になる。
- 版チェックのキーは `GimmickSetting` の Version Key（空なら同じフォルダの表のシート名）。取得元は Version Source（空なら確認しない）。
- 導入 UI は、プロジェクトにある `GimmickSetting` をすべて一覧に出す（型で探す）。
  ギミックのアセットを置くだけで一覧に並ぶので、登録作業は要らない。

### 配布元ごとにまとまる

`GimmickSetting` の **Publisher**（配布元）に名前を入れると、導入 UI がその名前ごとにまとめて並べる。

- 別の配布元のギミックが同じプロジェクトに入っていても取り違えない。
- **空でも動く。** 空のものは見出し無しで先頭にまとまる。
- 配布元が 1 つしか無いときは見出しを出さない（今までの見た目のまま）。

自分のツールを配る場合は、自分の名前やサークル名を入れておくとよい。
利用者のプロジェクトに他の作者のギミックが入ったときに、どれが自分のものか分かる。

### 自分のウィンドウに導入 UI を出す

`GimmickInstallerEditor` の関数で、導入 UI を自分のウィンドウに描ける。

```csharp
// まとめて描く（GimmickInstaller のインスペクタと同じ）
GimmickInstallerEditor.DrawSettings(avatarRoot, settings);

// 「更新確認」ボタンを上に固定し、一覧だけをスクロールさせる
GimmickInstallerEditor.DrawVersionCheckButton(settings);
_scroll = EditorGUILayout.BeginScrollView(_scroll);
GimmickInstallerEditor.DrawGimmickList(avatarRoot, settings);
EditorGUILayout.EndScrollView();
```

- 最後の引数に `LocalizationTable` を渡すと文言を差し替えられる（省くとこのパッケージの表）。
- 「設定」ボタンは[設定ウィンドウ](#設定ウィンドウ050-から)を開く。
- 各ギミックのボタン（導入・削除・設定／Prefab 選択・更新）は、**言語によらず同じ幅で横に並ぶ。**
  文字が入りきらないときは文字を小さくし（11 → 8）、それでも入らなければカーソルを乗せると全文が出る。
- 位置と見た目を自分で決めたい更新ボタンは `UpdateNoticeGUI.DrawOpenButton(Rect, GUIContent, urls, GUIStyle)` で描ける。

### 設定ウィンドウ（0.5.0 から）

導入 UI の「設定」ボタンは、ギミックの設定用コンポーネントだけを出すウィンドウを開く。
ギミックの GameObject を選ぶと MA のコンポーネントや `GimmickMarker` も並ぶので、設定項目だけを見せるため。

設定用のコンポーネントに、目印の `IGimmickSettings` を付ける（メンバーは無い）。

```csharp
using VGC.Gimmicks;

public class MyGimmickInstaller : MonoBehaviour, IGimmickSettings
{
    [SerializeField] private float _speed = 1f;
}
```

- ウィンドウは、インスタンスとその子から目印の付いたコンポーネントを探し、そのインスペクタだけを並べる。
  表示にはコンポーネントのエディタをそのまま使うので、`[CustomEditor]` があればそれが出る。
- 同じギミックを複数入れているときは、上部で切り替えられる。「ヒエラルキーで選択」で GameObject を選べる（今までの動き）。
- **目印の付いたコンポーネントが無いギミックは、今までどおり GameObject を選ぶ。** 既存の配布物の動きは変わらない。
- 「Prefab選択」ボタン（`GimmickSetting` の Use Setting がオフ）の動きは変わらない。
- 自分のコードから開くときは `GimmickSettingsWindow.Open(instances)`。あるかどうかは `GimmickSettingsWindow.HasSettings(instance)`。
- 文言は `Config/GimmickInstallerTable.asset` の `SettingsWindow.Title` / `.SelectInHierarchy` / `.NotFound`。
  表にキーが無いときは英語で出す。

## アニメーターを組み立てる（AnimatorFactory、0.5.0 から）

`VGC.Gimmicks.Animation.AnimatorFactory` は、メモリ上で `AnimatorController` を組み立てる。
使うのは `UnityEditor` と `UnityEngine` だけなので、アバターに限らず使える。

`AnimatorController.AddLayer` や `AnimatorStateMachine.AddState` は Undo を記録するので、
開いているシーンに未保存の印（*）が付く。NDMF のビルドやプレビューのたびに印が付くと利用者が混乱する。
`AnimatorFactory` は `new` したオブジェクトを配列に入れて代入する形で作るので、印が付かない。
できたコントローラーはアセットとして保存しない前提（MA Merge Animator などに渡す）。

```csharp
using VGC.Gimmicks.Animation;

var factory = new AnimatorFactory();
factory.AddParameter("Enabled", AnimatorControllerParameterType.Bool);   // 同じ名前は 2 回目以降を無視する

var layer = factory.AddLayer("MyGimmick");
var off = layer.AddState("Off", offClip);                                // 最初に足したステートが既定
var on = layer.AddState("On", AnimatorFactory.ConstantClip("On", "Body", typeof(GameObject), "m_IsActive", 1f));
AnimatorFactory.AddTransition(off, on, AnimatorFactory.If("Enabled"));   // 0 秒・exitTime なし
AnimatorFactory.AddTransition(on, off, AnimatorFactory.IfNot("Enabled"));

AnimatorController controller = factory.Build("MyGimmick");
```

| 種類 | 関数 |
|---|---|
| 組み立て | `AddParameter` / `AddLayer` / `Build` |
| レイヤー（`LayerBuilder`） | `AddState`（Write Defaults は既定でオフ） / `AddAnyStateTransition` / `DefaultState` |
| 条件 | `If` / `IfNot` / `IntEquals` / `IntNotEqual` / `Greater` / `Less` |
| 遷移 | `AddTransition` / `AddExitTimeTransition` |
| そのほか | `AddBehaviour` / `ConstantClip`（1 つのプロパティを一定の値にする、キー 1 個のクリップ） |

使う側の Editor の asmdef に `VGC.Gimmicks.Editor` の参照を足す。依存は `>=0.5.0 <1.0.0` と書く。

## VRChat の SDK が無いプロジェクト

**0.3.1 から、VRChat の SDK（`com.vrchat.base`）が入っていないプロジェクトでもコンパイルできる。**
VRChat に依存しないツールも、版の確認（`VersionChecker` / `UpdateNoticeGUI`）にこのパッケージを使える。
そういうツールの依存は `>=0.3.1 <1.0.0` と書く（0.3.0 までは SDK が無いとコンパイルが通らない）。

- SDK があるときだけ、`GimmickInstaller` と `GimmickMarker` を `VRC.SDKBase.IEditorOnly`（エディタ専用のコンポーネントの印）にする。
  記号 `VGC_VRCSDK_BASE` は、`VGC.Gimmicks.Runtime` の asmdef の `versionDefines` が `com.vrchat.base` を見て定義する。
  記号はこの asmdef の中でだけ効く。ほかのアセンブリで使うときは、その asmdef にも同じ `versionDefines` を書く。
- インターフェースの有無でシリアライズは変わらないので、このコンポーネントを使うプレハブの参照は切れない。

## プラットフォーム固有の部分

| パッケージ | アセンブリ |
|---|---|
| [com.vgc-laboratory.gimmicks-avatar](../com.vgc-laboratory.gimmicks-avatar/README.md) | `VGC.Gimmicks.Avatar.Runtime` / `.Editor` |
| `com.vgc-laboratory.gimmicks-world`（将来） | `VGC.Gimmicks.World.Runtime` / `.Editor` |

## 0.1.0 からの変更（破壊的）

| 0.1.0 | 0.2.0 |
|---|---|
| `GimmickVersionManager`（取得元は固定の `URL`） | `VersionChecker`（取得元 `VersionSource` を引数で渡す。null なら確認しない） |
| `GimmickVersionManager.URL` | `VersionChecker.VGCAppsScriptUrl` |
| `HasUpdate(id, local, out remote)` | `HasUpdate(source, key, local, out remote)` |
| 起動時（`[InitializeOnLoad]`）に取得 | 最初に問い合わせたときに取得 |
| （なし） | `VersionSource` / `UpdateNoticeGUI` / `GimmickSetting` の Version Source・Version Key |
| UniTask に依存 | 依存しない（`SpreadsheetReader` で取得） |
| 導入 UI の文言をシート名「メインウィンドウ」で全体から検索 | このパッケージの `Config/GimmickInstallerTable.asset` を GUID で読む（gimmicks-avatar から移した） |
