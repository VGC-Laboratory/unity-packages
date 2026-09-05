---
name: vgc-attributes
description: >-
  Field auto-wiring attributes from the VGC Attributes package
  (com.vgc-laboratory.attributes, namespace VGC.Attributes.Runtime). Use when writing or
  reviewing MonoBehaviour / UdonSharpBehaviour code in a Unity project that has this package
  installed, and a field needs a reference to another component, a sibling / child / parent
  object, an interface implementation, a transform value, a sequential index, or a Project
  asset referenced by GUID (Texture2D, Material, AudioClip, prefab, ScriptableObject). Prefer these
  attributes over GetComponent / GetComponentInParent / GetComponentInChildren in Start(),
  over FindObjectOfType, over Resources.Load, and over bare [SerializeField] that the user must
  wire by hand in the Inspector. Also covers AutoPopulateField, SelfField, FindFirstField, AssetGuidField, AutoAssignIndexField,
  SelfPositionField, SelfRotationField, SelfLocalScaleField, AddButtonSendCustomEventField,
  AddEventTriggerSendCustomEventField, ExecutorScope, ExecutorOrder, and the Attribute Executor.
license: MIT
---

# VGC.Attributes — フィールド自動配線

`com.vgc-laboratory.attributes`。エディタ／ビルド時にフィールドへ参照や値を焼き込む属性群。
**実行時の検索コストはゼロ**で、Inspector での手作業も不要になる。

このスキルは「どれを選ぶか」だけを扱う。実装詳細・実行タイミング・内部構造は
パッケージの `README.md`（`Packages/com.vgc-laboratory.attributes/README.md`）を参照。

## 使えるかの確認

`Packages/com.vgc-laboratory.attributes/` があるか、`vpm-manifest.json` に
`com.vgc-laboratory.attributes` があること。無ければ通常の Unity の書き方をする。

```csharp
using VGC.Attributes.Runtime;
```

## 選択

| やりたいこと | 書き方 |
|---|---|
| 同じ GameObject のコンポーネント | `[SerializeField, SelfField] private Rigidbody _rb;` |
| 子階層から1つ（自身含む） | `[SerializeField, AutoPopulateField(scope: ExecutorScope.Children)] private Canvas _canvas;` |
| 子階層から全部（配列） | `[SerializeField, AutoPopulateField(typeof(EntryButton), ExecutorScope.Children, ExecutorOrder.Hierarchy)] private EntryButton[] _buttons;` |
| **interface を実装した全コンポーネント** | `[SerializeField, AutoPopulateField(typeof(IGameHostChanged), ExecutorScope.Children)] private UdonSharpBehaviour[] _callbacks;` |
| 親をたどって1つ | `[SerializeField, AutoPopulateField(typeof(GameSystemMain), ExecutorScope.Parents)] private GameSystemMain _system;` |
| 特定の型を持つ最も近い親 | `[SerializeField, AutoPopulateField(typeof(EntryPanelBase), ExecutorScope.NearestParent, ExecutorOrder.Hierarchy, required: true)] private EntryPanelBase _panel;` |
| シーン全体から最初の1つ | `[SerializeField, FindFirstField] private GameManager _manager;` |
| 同型コンポーネントに連番 | `[SerializeField, AutoAssignIndexField(typeof(EntryPanelBase))] private int _index;` |
| Transform 値の焼き込み | `[SerializeField, SelfPositionField] private Vector3 _initialPos;` |
| **Project のアセットを GUID で** | `[SerializeField, AssetGuidField("2a3f…c1")] private Texture2D _icon;` |
| フォルダ配下のアセット全部 | `[SerializeField, AssetGuidField("フォルダのGUID")] private AudioClip[] _clips;` |
| アトラス内 Sprite など | `[SerializeField, AssetGuidField("…", subAssetName: "icon_0")] private Sprite _sprite;` |

**interface を `targetType` にできる**のが効く場面が多い。`UdonSharpBehaviour[]` で受けて
`SendCustomEvent(nameof(IFoo._OnBar))` で呼ぶ、というコールバック配線が手作業ゼロになる。

## ExecutorScope

| 値 | 範囲 |
|---|---|
| `Self` | 自身の GameObject のみ |
| `Children` | 自身を含む子階層全体 |
| `ChildrenExcludeSelf` | 子階層全体（自身を除く） |
| `Parent` | 直接の親のみ |
| `Parents` | 自身を含む親階層全体 |
| `ParentHierarchy` | 直接の親を基準に子階層全体 |
| `NearestParent` | `anchorType` を持つ最も近い親、そのオブジェクトのみ |
| `NearestParentHierarchy` | 同上を基準に子階層全体 |
| `Root` / `RootHierarchy` | ルートのみ / ルート基準の子階層全体 |
| `Scene`（既定） | シーン全体 |

`ExecutorOrder.Hierarchy` を付けると Hierarchy の並び順にソートされる。
**配列で順序が意味を持つ場合は必ず付ける**（既定は `None` で順不同）。

## 落とし穴

- **値は Executor が走ったときに書き込まれる。** Play モード移行時とビルド時は無条件に走るので
  動作に問題は出ないが、エディタで見ているだけの状態では空のことがある
- **`required: true`** を付けると未発見時に警告が出る。必須の参照には付ける
- **`AutoAssignIndexField` は `anchorType` でグループを決める。** 省略すると
  シーン内の同型全部が1グループになり、パネルごとの連番にならない
- Inspector には読み取り専用で表示される。手で編集しても Executor に上書きされる
- 配列は `T[]` と `List<T>` の両方に対応
- **`AssetGuidField` の GUID は `.meta` ファイルの `guid`。** アセットを移動しても追従するが、
  削除・再インポートで GUID が変わると解決できなくなる（未発見時は `null` が入り警告が出る）。
  フォルダ指定は `FindAssets` なのでサブフォルダも辿り、path 順（序数比較）に並ぶ

## Worlds SDK が要る属性

次の2つは別アセンブリ（`VGC.Attributes.Udon.Runtime`）にあり、
`com.vrchat.worlds` がある環境でのみコンパイルされる。

```csharp
using VGC.Attributes.Udon.Runtime;

// uGUI Button の onClick に SendCustomEvent を配線
[SerializeField, AddButtonSendCustomEventField(nameof(_OnClick))] private Button _button;

// EventTrigger に SendCustomEvent を配線（クラス属性）
[RequireComponent(typeof(EventTrigger)),
 AddEventTriggerSendCustomEventField(EventTriggerType.PointerEnter, nameof(_OnHighlight))]
public class MyButton : UdonSharpBehaviour { }
```

配列に対して `addIndex: true` を指定すると `{eventName}_{i}` が配線される。

## 使わないほうがいい場面

- **実行時に動的生成されるオブジェクト** — 焼き込みはエディタ時なので追従しない
- **プレハブのインスタンスごとに違う参照** — シーン上の位置で決まるものにしか使えない
- パッケージが入っていないプロジェクト
